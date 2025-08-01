using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Opportunity = Plugins.Models.Opportunity;
using cm_ProgramAssociation = Plugins.Models.cm_ProgramAssociation;
using Team = Plugins.Models.Team;
using cm_leadopptype = Plugins.Models.cm_leadopptype;

namespace Plugins {
    /// <summary>
    /// This class, SyncPluginOppoCloseCreateValidation, is a Dynamics 365 plugin that executes when an Opportunity is closed as Won. 
    /// The action of closing an Opportunity creates a corresponding opportunityclose entity record. 
    /// This plugin is triggered by the creation of that opportunityclose record, allowing validation or additional logic to run at the time of closure.
    /// </summary>
    public class SyncPluginOppoCloseCreateValidation : PluginBase {
        public SyncPluginOppoCloseCreateValidation(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SyncPluginOppoCloseCreateValidation)) {
        }
        /// <summary>
        ///     This plugin fires on creating an OpportunityClose record only when the opportunity is won, otherwise it skips further processing. 
        ///     It loads the related Opportunity, its program association, team, and the appropriate lead‐closure checklist master, tracing each lookup. 
        ///     Finally, it validates required closure responses (yes/no or text) against expected criteria, 
        ///     throwing an InvalidPluginExecutionException if any closure condition isn’t met.
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

            if (context.PrimaryEntityName != OpportunityClose.EntityLogicalName || context.MessageName != "Create") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Opportunity and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            OpportunityClose opportunityCloseRecord = commonBusinessLogic
                .GetRecordById<OpportunityClose>(context.PrimaryEntityId) ??
                throw new InvalidPluginExecutionException("Invalid plugin execution: OpportunityClose not found");

            if (opportunityCloseRecord.OpportunityStateCode != OpportunityClose_opportunity_statecode.Won) {
                context.SharedVariables["mustSkip"] = true;
                return;
            }
            try {
                #region Load Entities
                Opportunity opportunityRecord = commonBusinessLogic
                    .GetRecordById<Opportunity>(opportunityCloseRecord.OpportunityId.Id) ??
                        throw new InvalidPluginExecutionException("Invalid plugin execution: Opportunity not found");
                tracingService.Trace($"opportunityRecord {opportunityRecord.Id}");

                cm_ProgramAssociation programAssociationRecord = commonBusinessLogic
                    .GetRecordById<cm_ProgramAssociation>(opportunityRecord.cm_AssociatedProgram.Id) ??
                        throw new InvalidPluginExecutionException("Invalid plugin execution: cm_ProgramAssociation not found");

                tracingService.Trace($"programAssociationRecord {programAssociationRecord.Id}");

                Team teamRecord = commonBusinessLogic
                    .GetRecordById<Team>(programAssociationRecord.cm_Program.Id) ??
                        throw new InvalidPluginExecutionException("Invalid plugin execution: Team not found");

                tracingService.Trace($"teamRecord {teamRecord.Id}");

                cm_LeadClosureChecklistMaster leadClosureChecklistMasterRecord = commonBusinessLogic
                    .GetLeadClosureCheckLMasterByTeam(teamRecord.Id, opportunityRecord.cm_OpportunityType) ??
                        throw new InvalidPluginExecutionException($"No Lead Closure Checklist Master found for Opportunity Type {opportunityRecord.cm_OpportunityType} and Team {teamRecord.Name}");

                tracingService.Trace($"leadClosureChecklistMasterRecord {leadClosureChecklistMasterRecord.Id}");

                Account accountRecord = commonBusinessLogic
                     .GetRecordById<Account>(opportunityRecord.ParentAccountId.Id) ??
                         throw new InvalidPluginExecutionException($"Invalid plugin execution: Account not found");

                tracingService.Trace($"accountRecord/ParentAccountId {accountRecord.Id}");
                #endregion

                // Validation to avoid Opp - producer closure if Qualification Status is In Progress or Not Qualified
                if (opportunityRecord.cm_OpportunityType == cm_leadopptype.Producer &&
                    (opportunityRecord.cm_QualificationStatus == cm_opportunity_cm_qualificationstatus.InProgress ||  // In Progress
                    opportunityRecord.cm_QualificationStatus == cm_opportunity_cm_qualificationstatus.NotQualified))   // Not Qualified
                {
                    throw new InvalidPluginExecutionException("Opportunity cannot be closed when the Qualification Status is In Progress or Not Qualified.");
                }

                List<cm_LeadClosureChecklistResponse> leadClosureChecklistResponseRecords = commonBusinessLogic
                    .GetLeadClosureChecklistResponseByMaster(leadClosureChecklistMasterRecord.Id, opportunityRecord.Id);

                ValidateClosureChecklist(opportunityRecord, leadClosureChecklistResponseRecords);

                // checks account number field with same SAP ID from opportunity close

                // SAPID is added to the record when the Opportunity is being "closed as won". If it matches the account number from another account 
                // it means that the account has been imported from the ERP after the Lead was Qualified. The ERP imported account is the SSOT (single source of truth)
                if (opportunityCloseRecord.cm_SAPID == null) {
                    tracingService.Trace($"Invalid plugin execution: opportunityCloseRecord.cm_SAPID can not be null");
                    return;
                }

                Account importedAccount = commonBusinessLogic.GetAccountByAccountNumber(opportunityCloseRecord.cm_SAPID);

                if(importedAccount == null) {
                    // Update account Relationship Type Code according to the Role and skip account merge
                    var typeCode = commonBusinessLogic.UpdateAccountRelationshipType(accountRecord);
                    tracingService.Trace($"No import account found. Account {accountRecord.Name} CustomerTypeCode updated to {typeCode}");
                    return;
                }

                tracingService.Trace($"Account with number \'{importedAccount.AccountNumber}\' and Id: {importedAccount.Id} found. Start merging with subordinateAccount with Id: {accountRecord.Id}");
                commonBusinessLogic.Merge(importedAccount, accountRecord);
                tracingService.Trace($"Account merge complete");

            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"SyncPluginOppoCloseCreateValidation Process End");
            }
        }

        /// <summary>
        ///     The ValidateClosureChecklist method first skips validation entirely if there are no checklist responses. 
        ///     It then marks whether the opportunity is a “Producer” type, and for each response that’s required to close it checks two cases:
        ///     
        ///     Yes/No -> it enforces the question (always for non-producer, or for producer when statuses match) and errors if the answer isn’t as expected.
        ///     Text   -> it errors if the answer is blank.
        /// </summary>
        /// <param name="opportunityRecord"></param>
        /// <param name="leadClosureChecklistResponseRecords"></param>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        private void ValidateClosureChecklist(Opportunity opportunityRecord, List<cm_LeadClosureChecklistResponse> leadClosureChecklistResponseRecords) {
            if (!leadClosureChecklistResponseRecords.Any())
                return;


            foreach (var response in leadClosureChecklistResponseRecords) {
                if (response.cm_RequiredToClose != true) continue;

                if (response.cm_AnswerType == cm_answertype.YesNo) {
                    bool isProducer = opportunityRecord.cm_OpportunityType == cm_leadopptype.Producer;
                    bool isQualifiedStatusRelevant = response.cm_ValidateClosureOnlyifOppQualificationStatus !=
                        cm_leadclosurechecklistresponse_cm_validateclosureonlyifoppqualificationstatus.NA;

                    bool shouldValidate = !isProducer || // For non-producer opps, always validate
                        !isQualifiedStatusRelevant || // For producer but not qualified
                        ((int?)response.cm_ValidateClosureOnlyifOppQualificationStatus == (int?)opportunityRecord.cm_QualificationStatus); // For producer with matching qualified status

                    if (shouldValidate && (int?)response.cm_ExpectedAnswerToClose != (int?)response.cm_AnswerYesNo) {
                        throw new InvalidPluginExecutionException(
                            "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                        );
                    }
                }

                if (response.cm_AnswerType == cm_answertype.TextBox && string.IsNullOrWhiteSpace(response.cm_AnswerText)) {
                    throw new InvalidPluginExecutionException(
                        "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                    );
                }
            }
        }
    }
}
