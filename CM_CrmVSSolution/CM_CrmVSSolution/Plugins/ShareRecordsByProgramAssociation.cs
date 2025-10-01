using Microsoft.Xrm.Sdk;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class ShareRecordsByProgramAssociation : PluginBase {
        public ShareRecordsByProgramAssociation(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ShareRecordsByProgramAssociation)) {
        }
        /// <summary>
        ///     Steps:
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Create of opportunity contactid, customerid
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Create of incident customerid, primarycontactid
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Update of cm_programassociation cm_account, cm_lead, cm_program
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Create of cm_programassociation cm_account, cm_lead, cm_program
        ///         Sync Plugins.ShareRecordsByProgramAssociation: Associate of any Entity
        ///     
        ///     Share records to teams depending on the message and entity
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

                #region Incident/Case
                if (context.PrimaryEntityName == Incident.EntityLogicalName) {
                    #region Load Records
                    var incidentRecord = commonBusinessLogic.GetRecordById<Incident>(context.PrimaryEntityId)
                        ?? throw new InvalidPluginExecutionException("Incident not found");

                    var accountRecord = commonBusinessLogic.GetRecordById<Account>(incidentRecord.CustomerId?.Id)
                        ?? throw new InvalidPluginExecutionException("Case must be created with an Account");

                    // contactRecord must be optinal so that conversion from email to case can be complete.
                    var contactRecord = commonBusinessLogic.GetRecordById<Contact>(incidentRecord.PrimaryContactId?.Id);

                    SystemUser ownerRecord = incidentRecord.OwnerId != null
                        ? commonBusinessLogic.GetRecordById<SystemUser>(incidentRecord.OwnerId?.Id)
                        : null;
                    
                    Team accountTeamRecord = commonBusinessLogic.GetTeamByAccountRole(accountRecord);
                    #endregion

                    tracingService.Trace(
                        "Loaded records: " +
                        $"IncidentId:{incidentRecord.Id}, " +
                        $"AccountId:{accountRecord.Id}, " +
                        $"ContactId:{(contactRecord != null ? contactRecord.Id.ToString() : "Not found")}, " +
                        $"OwnerId:{(ownerRecord != null ? ownerRecord.Id.ToString() : "null")}, " +
                        $"AccountTeamId:{(accountTeamRecord != null ? accountTeamRecord.Id.ToString() : "null")}"
                    );

                    commonBusinessLogic.ExecuteRecordShare(accountRecord, accountTeamRecord.Id);
                    commonBusinessLogic.ExecuteRecordShare(incidentRecord, accountTeamRecord.Id);
                    if(contactRecord != null) commonBusinessLogic.ExecuteRecordShare(contactRecord, accountTeamRecord.Id);

                    List<Team> teamList = commonBusinessLogic.GetTeamListByAccountAndIncident(accountRecord, incidentRecord);
                    foreach (var team in teamList) {
                        commonBusinessLogic.ExecuteRecordShare(accountRecord, team.Id);
                        commonBusinessLogic.ExecuteRecordShare(incidentRecord, team.Id);
                        if (contactRecord != null) commonBusinessLogic.ExecuteRecordShare(contactRecord, team.Id);
                    }

                    if (incidentRecord.cm_SourceIdentifier != cm_sourceidentifiertype.ERP) {
                        return;
                    }
                    if (ownerRecord != null && ownerRecord.AccessMode == systemuser_accessmode.Noninteractive) {
                        commonBusinessLogic.SetOwnerToAccountManager(incidentRecord);
                    }
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
                if (rel != null && rel.SchemaName == cm_Incident_Team.Fields.cm_Incident_Team_Team) {
                    // This relationship record is created when the Incident BPF moves from Identify to Research.
                    // The chosen Incident CaseProgram is the Team in this relationship
                    // This Connection record is being created by CommonBusinessLogic method AssociateIncidentToTeams
                    tracingService.Trace($"rel.SchemaName: {rel.SchemaName}");

                    if (!(context.InputParameters["Target"] is EntityReference primaryEntity)
                        || !(context.InputParameters["RelatedEntities"] is EntityReferenceCollection relatedEntities)
                        || !relatedEntities.Any()) {
                        throw new InvalidPluginExecutionException("Invalid input parameters: Target or RelatedEntities is missing or empty.");
                    }

                    Guid teamId = (primaryEntity.LogicalName == Team.EntityLogicalName) ? primaryEntity.Id : relatedEntities.FirstOrDefault().Id;
                    Guid incidentId = (primaryEntity.LogicalName == Incident.EntityLogicalName) ? primaryEntity.Id : relatedEntities.FirstOrDefault().Id;

                    commonBusinessLogic.ExecuteRecordShare(new Entity(Incident.EntityLogicalName, incidentId), teamId);
                }
                #endregion

                #region Opportunity
                if (context.PrimaryEntityName == Opportunity.EntityLogicalName) {
                    tracingService.Trace($"Primary entity: {context.PrimaryEntityName}");
                    #region Load Records
                    var opportunityRecord = commonBusinessLogic
                        .GetRecordById<Opportunity>(context.PrimaryEntityId)
                        ?? throw new InvalidPluginExecutionException("Opportunity not found");

                    var programAssociationRecord = opportunityRecord.cm_AssociatedProgram == null ? null : commonBusinessLogic
                        .GetRecordById<cm_ProgramAssociation>(opportunityRecord.cm_AssociatedProgram.Id)
                        ?? throw new InvalidPluginExecutionException("cm_ProgramAssociation not found");

                    var teamRecord = programAssociationRecord.cm_Program == null ? null : commonBusinessLogic
                        .GetRecordById<Team>(programAssociationRecord.cm_Program.Id)
                        ?? throw new InvalidPluginExecutionException("Team not found");

                    var accountRecord = opportunityRecord.ParentAccountId == null ? null : commonBusinessLogic
                        .GetRecordById<Account>(opportunityRecord.ParentAccountId.Id)
                        ?? throw new InvalidPluginExecutionException("Account not found");

                    var contactRecord = opportunityRecord.ParentContactId == null ? null : commonBusinessLogic
                        .GetRecordById<Contact>(opportunityRecord.ParentContactId.Id)
                        ?? throw new InvalidPluginExecutionException("Contact not found");

                    #endregion

                    if (opportunityRecord != null) commonBusinessLogic.ExecuteRecordShare(opportunityRecord, teamRecord.Id);
                    if (accountRecord != null) commonBusinessLogic.ExecuteRecordShare(accountRecord, teamRecord.Id);
                    if (contactRecord != null) commonBusinessLogic.ExecuteRecordShare(contactRecord, teamRecord.Id);

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
