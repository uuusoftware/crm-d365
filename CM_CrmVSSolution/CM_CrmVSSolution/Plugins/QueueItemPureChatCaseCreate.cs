using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Plugins {
    internal class PureChatPluginConfig {
        public string QueueNameEnvVarSchema { get; set; }
        public string FallbackAccountName { get; set; }
        public string Description { get; set; }
        public string CaseTypeLabel { get; set; }
        public string ProgramLabel { get; set; }
        public string IncidentCategory { get; set; }
        public string CauseCategory { get; set; }
        public string OwnerTeam { get; set; }
        public string InquiryTypeLabel { get; set; }
    }

    public class QueueItemPureChatCaseCreate : PluginBase {
        private readonly PureChatPluginConfig _config;

        public QueueItemPureChatCaseCreate(string unsecureConfiguration, string secureConfiguration)
    : base(typeof(QueueItemPureChatCaseCreate)) {
            if (!string.IsNullOrWhiteSpace(unsecureConfiguration)) {
                try {
                    // Normalize whitespace and trim any weird characters
                    string cleanJson = unsecureConfiguration.Trim('\uFEFF', '\u200B', '\r', '\n', ' ', '\t');

                    _config = JsonConvert.DeserializeObject<PureChatPluginConfig>(cleanJson);

                    if (_config == null) {
                        throw new InvalidPluginExecutionException("Unsecure configuration JSON parsed to null. Check JSON format.");
                    }
                } catch (Exception ex) {
                    throw new InvalidPluginExecutionException($"Failed to parse unsecure configuration JSON: {ex.Message}", ex);
                }
            } else {
                _config = new PureChatPluginConfig(); // default empty
            }
        }
        /// <summary>
        ///     Steps:
        ///     Sync Plugins.QueueItemPureChatCaseCreate: Create of queueitem
        ///     
        ///     <description></description>
        /// </summary>
        /// <param name="localPluginContext"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext) {
            if (localPluginContext == null) {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;
            IOrganizationServiceFactory serviceFactory = localPluginContext.OrgSvcFactory;
            ITracingService tracingService = localPluginContext.TracingService;
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.PrimaryEntityName != QueueItem.EntityLogicalName || context.MessageName != "Create") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Queue Item and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                tracingService.Trace("QueueItemPureChatCaseCreate STEP 1 start. PrimaryId: {0}", context.PrimaryEntityId);
                tracingService.Trace($"Loaded Config: {JsonConvert.SerializeObject(_config)}");

                #region Load Records
                QueueItem queueItemRecord = commonBusinessLogic.GetRecordById<QueueItem>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("queueItemRecord can not be null");
                #endregion

                if (queueItemRecord.QueueId == null || queueItemRecord.ObjectId == null) {
                    tracingService.Trace("QueueItem missing QueueId or ObjectId. Exiting.");
                    return;
                }

                string queueNameEnvVarSchema = _config.QueueNameEnvVarSchema ?? "cm_PureChatQueueName";

                // Validate queue item against environment variable
                if (!string.IsNullOrWhiteSpace(queueNameEnvVarSchema)) {
                    string expectedQueueName = commonBusinessLogic.GetEnvironmentVariableValue(service, queueNameEnvVarSchema, tracingService) ?? "Producer Inbox";

                    if (!string.IsNullOrWhiteSpace(expectedQueueName)) {
                        Queue queue = commonBusinessLogic.GetRecordById<Queue>(queueItemRecord.QueueId.Id);
                        string actualQueueName = queue?.Name;

                        tracingService.Trace("Expected Queue (env var '{0}'): '{1}', Actual Queue: '{2}'",
                                      _config.QueueNameEnvVarSchema, expectedQueueName, actualQueueName);

                        if (!string.Equals(expectedQueueName, actualQueueName, StringComparison.OrdinalIgnoreCase)) {
                            tracingService.Trace("Queue name mismatch. Exiting.");

                            return;
                        }
                    } else {
                        tracingService.Trace("Environment variable '{0}' not found or empty; skipping queue name validation.", _config.QueueNameEnvVarSchema);
                    }
                }

                // Ensure the queue item points to an Email
                if (!string.Equals(queueItemRecord.ObjectId.LogicalName, Email.EntityLogicalName, StringComparison.OrdinalIgnoreCase)) {
                    tracingService.Trace("QueueItem.ObjectId is not Email. LogicalName: {0}. Exiting.", queueItemRecord.ObjectId.LogicalName);
                    return;
                }

                Email email = commonBusinessLogic.GetRecordById<Email>(queueItemRecord.ObjectId.Id)
                            ?? throw new InvalidPluginExecutionException("Email could not be loaded.");
                string subject = email.Subject ?? "(No Subject)";
                string desc = email.Description ?? string.Empty;

                // Parse key fields from the HTML of email body
                PureChatTranscriptParser transcriptParser = new PureChatTranscriptParser(desc);
                PureChatParseResult parsed = transcriptParser.Parse();
                tracingService.Trace($"Parsed Values - Visitor: {parsed.VisitorName}, Company: {parsed.CompanyDisplayName}, Email: {parsed.ContactEmail}, Date: {parsed.Date}, Operator: {parsed.Operator}");

                // Pull meta attributes safely
                string acceptingName = email.GetAttributeValue<string>("acceptingentityidname");
                string acceptingType = email.GetAttributeValue<string>("acceptingentityidtype");
                EntityReference emailSenderRef = email.GetAttributeValue<EntityReference>("emailsender");
                string emailSenderType = email.GetAttributeValue<string>("emailsendertype");
                IEnumerable<string> fromAddresses = commonBusinessLogic.EmailPartyAddresses(email.From);
                IEnumerable<string> toPartyIds = commonBusinessLogic.EmailPartyIds(email.To);
                string senderAddress = email.GetAttributeValue<string>("sender");
                string toRecipients = email.GetAttributeValue<string>("torecipients");

                Entity incidentCategory = commonBusinessLogic.GetRecordByColumn("cm_casecategory", "cm_name", _config.IncidentCategory ?? "Pending");
                Entity causeCategory = commonBusinessLogic.GetRecordByColumn("cm_casesubcategory", "cm_name", _config.CauseCategory ?? "Pending");
                OptionSetValue caseTypeCode = commonBusinessLogic.GetOptionSetValue("incident", "casetypecode", _config.InquiryTypeLabel ?? "General Inquiry");
                OptionSetValue caseProgram = commonBusinessLogic.GetOptionSetValue("incident", "cm_caseprogram", _config.ProgramLabel ?? "Circular Materials General");
                OptionSetValue cmCaseType = commonBusinessLogic.GetOptionSetValue("incident", "cm_cmcasetype", _config.CaseTypeLabel ?? "Producer");
                Entity owner = commonBusinessLogic.GetRecordByColumn("team", "name", "Customer Relations");

                // Find or Create Account
                Entity customer = commonBusinessLogic.GetRecordByColumn("account", "name", parsed.CompanyDisplayName);

                if (customer == null) {
                    // No matching account found - use or create fallback account
                    string fallbackAccountName = _config.FallbackAccountName ?? "Pure Chat Triage Account";
                    tracingService.Trace("No account found for '{0}'. Using fallback account '{1}'.", parsed.CompanyDisplayName, fallbackAccountName);

                    // Try to get the fallback account first
                    customer = commonBusinessLogic.GetRecordByColumn("account", "name", fallbackAccountName);

                    if (customer == null) {
                        tracingService.Trace("Fallback account not found. Creating '{0}'...", fallbackAccountName);
                        Entity fallback = new Entity("account");
                        fallback["name"] = fallbackAccountName;
                        fallback["customertypecode"] = new OptionSetValue(3); // 3 = Customer
                        fallback["cm_role"] = new OptionSetValueCollection { new OptionSetValue(121540005) }; // 121540005 = CM Customer
                        fallback["description"] = "Default account used for Pure Chat transcripts with unmatched companies.";
                        fallback["emailaddress1"] = "noreply@circularmaterials.ca"; // optional placeholder
                        fallback["ownerid"] = owner?.ToEntityReference();
                        Guid fallbackId = service.Create(fallback);

                        tracingService.Trace("Created fallback account '{0}' (Id: {1})", fallbackAccountName, fallbackId);
                        customer = service.Retrieve("account", fallbackId, new ColumnSet("accountid", "name", "primarycontactid"));
                    }
                } else {
                    tracingService.Trace("Found account '{0}' (Id: {1})", customer.GetAttributeValue<string>("name"), customer.Id);
                }

                // Handle primary contact (create if missing)
                Entity contact = null;
                EntityReference primaryContactRef = customer.GetAttributeValue<EntityReference>("primarycontactid");

                if (primaryContactRef != null) {
                    tracingService.Trace("Found primary contact on account (Id: {0})", primaryContactRef.Id);
                    contact = service.Retrieve("contact", primaryContactRef.Id, new ColumnSet("contactid", "fullname"));
                } else {
                    tracingService.Trace("Account has no primary contact. Creating one from transcript name...");

                    string fullName = parsed.VisitorName ?? "Unknown";
                    string[] parts = fullName.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    string firstName = parts.Length > 1 ? string.Join(" ", parts.Take(parts.Length - 1)) : fullName;
                    string lastName = parts.Length > 1 ? parts.Last() : "Unknown";

                    Entity newContact = new Entity("contact") {
                        ["firstname"] = firstName,
                        ["lastname"] = lastName,
                        ["customertypecode"] = new OptionSetValue(1), // 1 = Default
                        ["parentcustomerid"] = customer.ToEntityReference(), // link to account
                        ["ownerid"] = owner?.ToEntityReference()
                    };

                    Guid contactId = service.Create(newContact);
                    tracingService.Trace("Created new contact '{0} {1}' (Id: {2})", firstName, lastName, contactId);

                    // Update Account with new Primary Contact
                    Entity updateAccount = new Entity("account", customer.Id) {
                        ["primarycontactid"] = new EntityReference("contact", contactId)
                    };
                    service.Update(updateAccount);
                    tracingService.Trace("Updated account '{0}' with new primary contact.", customer.GetAttributeValue<string>("name"));

                    contact = service.Retrieve("contact", contactId, new ColumnSet("contactid", "fullname"));
                }

                // Create Case
                Entity incident = new Entity("incident") {
                    ["title"] = $"WeRecycle Chat <{subject}>",
                    ["description"] = _config.Description,
                    ["customerid"] = customer?.ToEntityReference(),
                    ["primarycontactid"] = contact?.ToEntityReference(),
                    ["cm_reportedon"] = parsed.Date,
                    ["cm_reportedbyid"] = new EntityReference("contact", contact.Id),
                    ["casetypecode"] = caseTypeCode,
                    ["cm_caseprogram"] = new OptionSetValueCollection { caseProgram },
                    ["cm_cmcasetype"] = cmCaseType,
                    ["cm_incidentcategory"] = incidentCategory?.ToEntityReference(),
                    ["cm_causecategory"] = causeCategory?.ToEntityReference(),
                    ["ownerid"] = owner?.ToEntityReference()
                };

                // Log parsed values
                tracingService.Trace($@"
Case Summary:
  Title: WeRecycle Chat <{subject}>
  Description: {_config.Description}
  Customer: {customer?.Id}
  Contact: {contact?.Id}
  Reported By: {contact?.Id}
  Reported On: {parsed.Date}
  Incident Category: {incidentCategory?.Id}
  Cause Category: {causeCategory?.Id}
  Case Program: {caseProgram?.Value}
  Case Type: {cmCaseType?.Value}
  Owner: {owner?.Id}"
                );

                // Create the Case
                Guid caseId = service.Create(incident);
                tracingService.Trace($"Created Case '{subject}' (Id: {caseId})");

                // Link the Email to the Case
                Entity updateEmail = new Entity("email", email.Id) {
                    ["regardingobjectid"] = new EntityReference("incident", caseId)
                };

                service.Update(updateEmail);
                tracingService.Trace("Linked Email '{0}' to Case '{1}'", email.Subject, subject);
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";

                tracingService.Trace($"Error: {detailedError}");

                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"QueueItemPureChatCaseCreate Process End");
            }
        }



        /// <summary>
        /// Parses Pure Chat transcript HTML (from Email.Description) to extract key fields.
        /// No external dependencies; robust to common HTML/table formatting.
        /// </summary>
        internal sealed class PureChatTranscriptParser {
            private readonly string _html;
            private readonly string _htmlCollapsed; // whitespace collapsed for regex friendliness
            private readonly string _text;          // crude text version for backup parsing

            public PureChatTranscriptParser(string html) {
                _html = html ?? string.Empty;
                _htmlCollapsed = CollapseWhitespace(_html);
                _text = HtmlToText(_html);
            }

            /// <summary>Parses and returns all core fields in one shot.</summary>
            public PureChatParseResult Parse() {
                var result = new PureChatParseResult {
                    VisitorName = GetVisitorName(),
                    ContactEmail = GetCustomerEmail(),
                    CompanyRaw = GetCompanyRaw(),
                    CompanyDisplayName = GetCompanyDisplayName(),
                    Date = GetDate(),
                    Operator = GetOperatorName(),
                    HeaderSummary = GetHeaderSummary()
                };
                return result;
            }

            /// <summary>Company raw value from table (e.g., "testprod1|Steward Testing Inc").</summary>
            public string GetCompanyRaw()
                => ExtractFieldValue("Company");

            /// <summary>Company display (e.g., take last segment after '|', else raw).</summary>
            public string GetCompanyDisplayName() {
                string raw = GetCompanyRaw();
                if (string.IsNullOrWhiteSpace(raw)) return null;

                var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .ToArray();
                return parts.Length == 0 ? null : parts.Last();
            }

            /// <summary>Gets the email address value from the "Email: " field of the email body.</summary>
            public string GetCustomerEmail() {
                // Extract from "Email:" line
                string fromRow = ExtractFieldValue("Email");
                string email = FirstEmail(fromRow);

                return string.IsNullOrWhiteSpace(email) ? null : email;
            }

            /// <summary>Visitor "Name:" value from transcript details table.</summary>
            public string GetVisitorName()
                => ExtractFieldValue("Name");

            /// <summary>Operator value from transcript ("Operator: Judith").</summary>
            public string GetOperatorName()
                => ExtractFieldValue("Operator");

            /// <summary>Date from transcript details ("Date: 4/26/2022"). Returns local DateTime? if parsed.</summary>
            public DateTime? GetDate() {
                string dateText = ExtractFieldValue("Date");
                if (string.IsNullOrWhiteSpace(dateText))
                    return null;

                // Try different formats; fall back to DateTime.TryParse.
                string[] formats = new[]
                {
                "M/d/yyyy",
                "M/d/yyyy h:mm tt",
                "M/d/yyyy hh:mm tt",
                "MMMM d, yyyy",
                "dddd, MMMM d, yyyy h:mm tt",
                "yyyy-MM-dd",
            };

                foreach (string fmt in formats) {
                    if (DateTime.TryParseExact(
                            dateText.Trim(),
                            fmt,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                            out var dt)
                        ) {

                        return dt;
                    }
                }

                if (DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var any))
                    return any;

                return null;
            }

            /// <summary>Header "Chat conversation sent by … (email)” line if present.</summary>
            public string GetHeaderSummary() {
                Match m = Regex.Match(_htmlCollapsed,
                    @"Chat\s+conversation\s+sent\s+by\s+(?<v>.+?)</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline); // E.g., Chat conversation sent by <Name> (<Email Address>)"

                if (m.Success)
                    return HtmlDecode(SimpleTrim(m.Groups["v"].Value));

                // Fallback if not in HTML
                string line = _text.Split('\n')
                                .FirstOrDefault(l => l.IndexOf("Chat conversation sent by", StringComparison.OrdinalIgnoreCase) >= 0);

                return SimpleTrim(line);
            }

            // Helpers
            private string ExtractFieldValue(string label) {
                if (string.IsNullOrWhiteSpace(label)) return null;

                // Table-based: <td>Label:</td><td>Value</td>
                string pattern = $@"<td[^>]*>\s*(?:<div[^>]*>)?\s*{Regex.Escape(label)}\s*:?\s*(?:</div>)?\s*</td>\s*<td[^>]*>(?:<div[^>]*>)?(?<val>.*?)(?:</div>)?\s*</td>";
                var match = Regex.Match(_htmlCollapsed, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                    return HtmlDecode(SimpleTrim(match.Groups["val"].Value));

                // Fallback: text-based "Label: value" until line end
                string text = _text;
                int i = text.IndexOf(label + ":", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) {
                    string after = text.Substring(i + (label + ":").Length);
                    string line = after.Split(new[] { '\r', '\n' }, StringSplitOptions.None).FirstOrDefault();
                    return SimpleTrim(line);
                }

                return null;
            }

            private static string FirstEmail(string input) {
                if (string.IsNullOrWhiteSpace(input)) return null;
                Match m = Regex.Match(input,
                    @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
                    RegexOptions.IgnoreCase);
                return m.Success ? m.Value : null;
            }

            /// <summary>Normalize spaces and remove newlines for simpler regex</summary>
            private static string CollapseWhitespace(string html) {
                if (string.IsNullOrEmpty(html)) return string.Empty;

                string s = Regex.Replace(html, @"\s+", " ");
                return s;
            }

            /// <summary>Removes HTML tags keeping only text</summary>
            private static string HtmlToText(string html) {
                if (string.IsNullOrEmpty(html)) return string.Empty;
                // Remove tags
                string noTags = Regex.Replace(html, "<[^>]+>", "\n");

                // Decode
                noTags = noTags.Replace("&nbsp;", " ").Replace("&amp;", "&");

                // Collapse whitespace
                string flat = Regex.Replace(noTags, @"[ \t\r]+", " ");

                // Normalize newlines
                string lines = Regex.Replace(flat, @"\n{2,}", "\n").Trim();
                return lines;
            }

            private static string HtmlDecode(string s) {
                if (string.IsNullOrEmpty(s)) return s;

                // Decode
                return s.Replace("&nbsp;", " ").Replace("&amp;", "&");
            }

            private static string SimpleTrim(string s) {
                if (string.IsNullOrWhiteSpace(s)) return null;

                return s.Trim(' ', '\t', '\r', '\n', '\u00A0');
            }
        }

        internal sealed class PureChatParseResult {
            public string VisitorName { get; set; }
            public string ContactEmail { get; set; }
            public string CompanyRaw { get; set; }
            public string CompanyDisplayName { get; set; }
            public DateTime? Date { get; set; }
            public string Operator { get; set; }
            public string HeaderSummary { get; set; }
        }
    }
}