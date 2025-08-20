using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Remoting.Contexts;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using System.Drawing;

namespace Plugins {
    public class ShareRecordsByProgramAssociation : PluginBase {
        public ShareRecordsByProgramAssociation(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ShareRecordsByProgramAssociation)) {
        }
        /// <summary>
        ///     Steps:
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Update of cm_programassociation cm_program
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Create of cm_programassociation cm_program
        ///     
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
            var rel = new Relationship();

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService)
               ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Error creating CommonBusinessLogic");

            // Add all possible Entities:
            if (context.MessageName != "Create" && context.MessageName != "Update" && context.MessageName != "Associate") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Message must be Create, Update or Associate");
            }

            string jsonParams = CommonBusinessLogic.SerializeParameterCollection(context.InputParameters);
            tracingService.Trace($"context.InputParameters: {jsonParams}");

            if (context.MessageName.Equals(messages.Associate.ToString(), StringComparison.OrdinalIgnoreCase)
                && !context.InputParameters.Contains("Relationship")) {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Associate Operations must contain Relationship");
            } else if (context.InputParameters.TryGetValue("Relationship", out Relationship relationship)) {
                rel = relationship;
            }

            try {
                #region Lead
                if (context.PrimaryEntityName == Lead.EntityLogicalName) {
                    var leadRecord = commonBusinessLogic.GetRecordById<Lead>(context.PrimaryEntityId);


                }

                #endregion
                #region cm_ProgramAssociation 
                // Shares Account, Lead, and Opportunity records with the cm_ProgramAssociation team
                if (context.PrimaryEntityName == cm_ProgramAssociation.EntityLogicalName) {
                    tracingService.Trace($"Primary entity: {context.PrimaryEntityName}");

                    var programAssocRecord = commonBusinessLogic.GetRecordById<cm_ProgramAssociation>(context.PrimaryEntityId)
                        ?? throw new InvalidPluginExecutionException("cm_ProgramAssociation can not be null");

                    if (programAssocRecord.cm_Program.Id == null)
                        throw new InvalidPluginExecutionException("Team (cm_program) can not be null");

                    if (programAssocRecord.cm_Account != null) {
                        var accountRecord = commonBusinessLogic.GetRecordById<Account>(programAssocRecord.cm_Account.Id);
                        commonBusinessLogic.ExecuteRecordShare(accountRecord, programAssocRecord.cm_Program.Id);
                    }

                    if (programAssocRecord.cm_Lead != null) {
                        // TODO: anything under lead that is activity must be shared too
                        // TEST: if new lead is created it should cascade to contact from account
                        var leadRecord = commonBusinessLogic.GetRecordById<Lead>(programAssocRecord.cm_Lead.Id);
                        commonBusinessLogic.ExecuteRecordShare(leadRecord, programAssocRecord.cm_Program.Id);
                    }

                    // TEST: qualification and closure questions should cascade from opps
                    List<Opportunity> opportunityList = commonBusinessLogic.GetOpportunityByProgramAssoc(programAssocRecord);
                    foreach (var opportunity in opportunityList) {
                        commonBusinessLogic.ExecuteRecordShare(opportunity, programAssocRecord.cm_Program.Id);
                    }
                }
                #endregion
                #region cm_Incident_Team
                if (rel != null && rel.SchemaName == "cm_Incident_Team") {
                    tracingService.Trace($"rel.SchemaName: {rel.SchemaName}");

                    if (!(context.InputParameters["Target"] is EntityReference primaryEntity)
                        || !(context.InputParameters["RelatedEntities"] is EntityReferenceCollection relatedEntities)
                        || !relatedEntities.Any()) {
                        throw new InvalidPluginExecutionException("Invalid input parameters: Target or RelatedEntities is missing or empty.");
                    }

                    Guid teamId = (primaryEntity.LogicalName == Team.EntityLogicalName) ? primaryEntity.Id : relatedEntities.FirstOrDefault().Id;
                    Guid incidentId = (primaryEntity.LogicalName == Incident.EntityLogicalName) ? relatedEntities.FirstOrDefault().Id : primaryEntity.Id;

                    commonBusinessLogic.ExecuteRecordShare(new Entity(Incident.EntityLogicalName, incidentId), teamId);
                }
                #endregion

            } catch (Exception ex) {
                context.InputParameters.TryGetValue("Target", out var target);
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName ?? target?.ToString() ?? "Unknown"} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"ShareRecordsByProgramAssociation Process End");
            }
        }
    }
}
