using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models;
using Opportunity = Plugins.Models.Opportunity;
using cm_ProgramAssociation = Plugins.Models.cm_ProgramAssociation;
using Team = Plugins.Models.Team;
using cm_leadopptype = Plugins.Models.cm_leadopptype;

namespace Plugins
{
    public class SyncPluginOppoCloseCreateValidation : PluginBase
    {
        public SyncPluginOppoCloseCreateValidation(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SyncPluginOppoCloseCreateValidation))
        {
        }
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            var tracingService = localPluginContext.TracingService;
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.PrimaryEntityName != OpportunityClose.EntityLogicalName || context.MessageName != "Create")
            {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Opportunity and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            OpportunityClose opportunityCloseRecord = commonBusinessLogic
                .GetRecordById<OpportunityClose>(context.PrimaryEntityId) ??
                throw new InvalidPluginExecutionException("Invalid plugin execution: OpportunityClose not found");

            if (opportunityCloseRecord.OpportunityStateCode != OpportunityClose_opportunity_statecode.Won)
            {
                context.SharedVariables["mustSkip"] = true;
                return;
            }
            try
            {
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

                // Validation to avoid Opp - producer closure if Qualification Status is In Progress or Not Qualified
                if (opportunityRecord.cm_OpportunityType == cm_leadopptype.Producer &&
     ((int)opportunityRecord.cm_QualificationStatus == 121540000 ||  // In Progress
      (int)opportunityRecord.cm_QualificationStatus == 121540003))   // Not Qualified
                {
                    throw new InvalidPluginExecutionException("Opportunity cannot be closed when the Qualification Status is In Progress or Not Qualified.");
                }





                List<cm_LeadClosureChecklistResponse> leadClosureChecklistResponseRecords = commonBusinessLogic
                    .GetLeadClosureChecklistResponseByMaster(leadClosureChecklistMasterRecord.Id, opportunityRecord.Id);

                if (opportunityRecord.cm_OpportunityType == cm_leadopptype.Producer)

                {
                    if (leadClosureChecklistResponseRecords.Any())
                    {
                        leadClosureChecklistResponseRecords.ForEach(response =>
                        {
                            // Proceed only if the response is marked as "Required to Close"
                            if (response.cm_RequiredToClose == true)
                            {
                                // Check for Yes/No type and compare expected vs actual answer

                                if (response.cm_AnswerType == cm_answertype.YesNo && (int)response.cm_ValidateClosureOnlyifOppQualificationStatus == 121540000)
                                {
                                    if (
        (int?)response.cm_ExpectedAnswerToClose != (int?)response.cm_AnswerYesNo)
                                    {
                                        throw new InvalidPluginExecutionException(
                                            "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                                        );
                                    }


                                }
                                else if (response.cm_AnswerType == cm_answertype.YesNo && (int)response.cm_ValidateClosureOnlyifOppQualificationStatus != 121540000)
                                {
                                    if ((int?)response.cm_ValidateClosureOnlyifOppQualificationStatus == (int?)opportunityRecord.cm_QualificationStatus &&
        (int?)response.cm_ExpectedAnswerToClose != (int?)response.cm_AnswerYesNo)
                                    {
                                        throw new InvalidPluginExecutionException(
                                            "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                                        );
                                    }


                                }

                                // Check for TextBox type and validate non-empty response
                                if (response.cm_AnswerType == cm_answertype.TextBox &&
                                    (string.IsNullOrEmpty(response.cm_AnswerText)))
                                {
                                    throw new InvalidPluginExecutionException(
                                        "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                                    );
                                }
                            }
                        });
                    }

                }
                // For Opp Types other than producer
                else
                {
                    if (leadClosureChecklistResponseRecords.Any())
                    {
                        leadClosureChecklistResponseRecords.ForEach(response =>
                        {
                            // Proceed only if the response is marked as "Required to Close"
                            if (response.cm_RequiredToClose == true)
                            {
                                // Check for Yes/No type and compare expected vs actual answer
                                if (response.cm_AnswerType == cm_answertype.YesNo)
                                {
                                    if (
        (int?)response.cm_ExpectedAnswerToClose != (int?)response.cm_AnswerYesNo)
                                    {
                                        throw new InvalidPluginExecutionException(
                                            "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                                        );
                                    }


                                }

                                // Check for TextBox type and validate non-empty response
                                if (response.cm_AnswerType == cm_answertype.TextBox &&
                                    (string.IsNullOrEmpty(response.cm_AnswerText)))
                                {
                                    throw new InvalidPluginExecutionException(
                                        "Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’."
                                    );
                                }
                            }
                        });
                    }
                }
            }


            //    if (leadClosureChecklistResponseRecords.Any())
            //    {
            //        leadClosureChecklistResponseRecords.ForEach(response =>
            //        {
            //            if ((response.cm_AnswerType == cm_answertype.YesNo &&
            //                    (response.cm_AnswerYesNo == null || response.cm_AnswerYesNo == cm_leadclosurechecklistresponse_cm_answeryesno.No)) ||
            //                (response.cm_AnswerType == cm_answertype.TextBox &&
            //                    (response.cm_AnswerText == null || response.cm_AnswerText == string.Empty)))
            //            {
            //                throw new InvalidPluginExecutionException("Failed to meet the closure criteria. You may resolve the closure questions before converting the Opportunity as ‘Won’.");
            //            }
            //        });
            //    }
            //}


            catch (Exception ex)
            {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
            finally
            {
                tracingService.Trace($"SyncPluginOppoCloseCreateValidation Process End");
            }
        }
    }
}
