using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class QueueItemPureChatCaseCreate : PluginBase {
        private readonly string _queueNameEnvVarSchema;

        public QueueItemPureChatCaseCreate(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(QueueItemPureChatCaseCreate)) {
            _queueNameEnvVarSchema = (unsecureConfiguration ?? string.Empty).Trim();
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

            var context = localPluginContext.PluginExecutionContext;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            var tracingService = localPluginContext.TracingService;
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.PrimaryEntityName != QueueItem.EntityLogicalName || context.MessageName != "Create") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Queue Item and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                tracingService.Trace("QueueItemPureChatCaseCreate STEP 1 start. PrimaryId: {0}", context.PrimaryEntityId);

                #region Load Records
                QueueItem queueItemRecord = commonBusinessLogic.GetRecordById<QueueItem>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("queueItemRecord can not be null");
                #endregion

                if (queueItemRecord.QueueId == null || queueItemRecord.ObjectId == null) {
                    tracingService.Trace("QueueItem missing QueueId or ObjectId. Exiting.");
                    return;
                }

                // Validate queue item against environment variable
                if (!string.IsNullOrWhiteSpace(_queueNameEnvVarSchema)) {
                    string expectedQueueName = commonBusinessLogic.GetEnvironmentVariableValue(service, _queueNameEnvVarSchema, tracingService);

                    if (!string.IsNullOrWhiteSpace(expectedQueueName)) {
                        Queue queue = commonBusinessLogic.GetRecordById<Queue>(queueItemRecord.QueueId.Id);
                        string actualQueueName = queue?.Name;

                        tracingService.Trace("Expected Queue (env var '{0}'): '{1}', Actual Queue: '{2}'",
                                      _queueNameEnvVarSchema, expectedQueueName, actualQueueName);

                        if (!string.Equals(expectedQueueName, actualQueueName, StringComparison.OrdinalIgnoreCase)) {
                            tracingService.Trace("Queue name mismatch. Exiting.");

                            return;
                        }
                    } else {
                        tracingService.Trace("Environment variable '{0}' not found or empty; skipping queue name validation.", _queueNameEnvVarSchema);
                    }
                }

                // Ensure the queue item points to an Email
                if (!string.Equals(queueItemRecord.ObjectId.LogicalName, Email.EntityLogicalName, StringComparison.OrdinalIgnoreCase)) {
                    tracingService.Trace("QueueItem.ObjectId is not Email. LogicalName: {0}. Exiting.", queueItemRecord.ObjectId.LogicalName);
                    return;
                }

                // Load Email (subject/description/meta)
                ColumnSet emailCols = new ColumnSet(
                    Email.Fields.Subject,
                    Email.Fields.Description,
                    Email.Fields.From,
                    Email.Fields.To,
                    Email.Fields.RegardingObjectId
                );

                // Accepting entity meta may not be on early-bound; pull via generic attributes
                emailCols.AddColumns("acceptingentityidname", "acceptingentityidtype", "emailsender", "emailsendertype", "sender", "torecipients");

                Email email = commonBusinessLogic.GetRecordById<Email>(queueItemRecord.ObjectId.Id)
                            ?? throw new InvalidPluginExecutionException("Email could not be loaded.");

                string subject = email.Subject ?? "(No Subject)";
                string desc = email.Description ?? string.Empty;

                // Pull meta attributes safely
                string acceptingName = email.GetAttributeValue<string>("acceptingentityidname");
                string acceptingType = email.GetAttributeValue<string>("acceptingentityidtype");
                EntityReference emailSenderRef = email.GetAttributeValue<EntityReference>("emailsender");
                var emailSenderType = email.GetAttributeValue<string>("emailsendertype");
                IEnumerable<string> fromAddresses = commonBusinessLogic.EmailPartyAddresses(email.From);
                IEnumerable<string> toPartyIds = commonBusinessLogic.EmailPartyIds(email.To);
                string senderAddress = email.GetAttributeValue<string>("sender");       // SMTP of From
                string toRecipients = email.GetAttributeValue<string>("torecipients");  // SMTP list

                // Trace a small slice so you can confirm end-to-end
                tracingService.Trace("Email.Subject: {0}", subject);
                tracingService.Trace("Email.Description (first 200): {0}", (desc.Length > 200 ? desc.Substring(0, 200) + "..." : desc));
                tracingService.Trace("Accepting: name='{0}', type='{1}'", acceptingName, acceptingType);
                tracingService.Trace("EmailSender: {0} ({1})", emailSenderRef?.Id, emailSenderType);
                tracingService.Trace("From parties: {0}", string.Join("; ", fromAddresses));
                tracingService.Trace("To parties: {0}", string.Join("; ", toPartyIds));
                tracingService.Trace("Sender SMTP: {0}", senderAddress);
                tracingService.Trace("ToRecipients SMTP: {0}", toRecipients);
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"QueueItemPureChatCaseCreate Process End");
            }
        }
    }

    internal class CaseDto {
        public cm_incident_cm_casepriority cm_CasePriority { get; set; }
        public List<cm_caseprogram> cm_CaseProgram { get; set; }
        public EntityReference cm_CauseCategory { get; set; }
        public EntityReference cm_Contract { get; set; }
        public EntityReference cm_IncidentCategory { get; set; }
        public EntityReference cm_Program { get; set; }
        public EntityReference CustomerId { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public EntityReference PrimaryContactId { get; set; }
        public EntityReference ParentCaseId { get; set; }
        public bool? cm_ComplianceFlagSetonAccount { get; set; }
        public EntityReference OwnerId { get; set; }
        public string cm_ReportedBy { get; set; }
        public DateTime? cm_ReportedOn { get; set; }
        public string cm_Channel { get; set; }
        public string cm_OtherChannel { get; set; }
        public EntityReference cm_ReportedById { get; set; }
#if MODELBUILDER_EMIT_ENUMS
        public Incident_CaseTypeCode? CaseTypeCode { get; set; }
#else
        public OptionSetValue CaseTypeCode { get; set; }
#endif
    }
}
