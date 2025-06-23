using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class AsyncPluginIncidentResponseGeneration : PluginBase {
        public AsyncPluginIncidentResponseGeneration(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AsyncPluginIncidentResponseGeneration)) {
        }
        /// <summary>
        ///     Steps: Plugins.AsyncPluginIncidentResponseGeneration: Update of incident
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
            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                tracingService.Trace("Plugin execution started for Incident update.");

                if (context.PrimaryEntityName != Incident.EntityLogicalName || context.MessageName != "Update") {
                    tracingService.Trace($"Invalid context: Expected entity {Incident.EntityLogicalName} and message 'Update'. " +
                                         $"Got entity '{context.PrimaryEntityName}' and message '{context.MessageName}'.");
                    throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Incident and message must be Update");
                }

                tracingService.Trace($"Retrieving Incident record with ID: {context.PrimaryEntityId}");
                Incident incidentRecord = commonBusinessLogic.GetRecordById<Incident>(context.PrimaryEntityId) ??
                    throw new InvalidPluginExecutionException("Invalid plugin execution: Incident/Case not found");

                if (incidentRecord?.ParentCaseId != null) return;

                if (incidentRecord?.cm_GenerateChecklist.Value == null || incidentRecord.cm_GenerateChecklist.Value == false) return;

                tracingService.Trace("Associating Incident to teams based on caseProgram and leadType.");
                List<Team> teamsAssociated = commonBusinessLogic.AssociateIncidentToTeams(incidentRecord);

                if (incidentRecord.cm_CauseCategory?.Id == null ||
                    incidentRecord.cm_IncidentCategory?.Id == null ||
                    teamsAssociated == null) {
                    tracingService.Trace("Required fields are missing: " +
                                         $"CauseCategory: {(incidentRecord.cm_CauseCategory?.Id.ToString() ?? "null")}, " +
                                         $"IncidentCategory: {(incidentRecord.cm_IncidentCategory?.Id.ToString() ?? "null")}, " +
                                         $"Programs: {(teamsAssociated != null ? string.Join(", ", teamsAssociated.Select(a => a.Id.ToString())) : "null")}. Exiting plugin.");
                    return;
                }

                tracingService.Trace("Creating child case(s) if applicable.");
                List<Incident> incidentList = commonBusinessLogic.CreateChildCaseOrDefault(incidentRecord);
                if (incidentList == null || !incidentList.Any()) {
                    tracingService.Trace("No child incidents returned from CreateChildCaseOrDefault. Exiting plugin.");
                    return;
                }

                foreach (var incident in incidentList) {
                    tracingService.Trace($"Processing incident with ID: {incident.Id}");

                    if (incident.cm_IncidentCategory != null && incident.cm_CauseCategory != null) {
                        tracingService.Trace("Retrieving checklist master based on IncidentCategory, CauseCategory, and Program.");

                        commonBusinessLogic.MoveIncidentBpfStage(incident);

                        cm_Incident_Team associatedTeam = commonBusinessLogic.GetAssociatedTeam(incident);

                        cm_ChecklistMaster checklistMasterRecord = commonBusinessLogic.GetChecklistmaster(
                            incident.cm_CauseCategory.Id,
                            incident.cm_IncidentCategory.Id,
                            associatedTeam.teamid.Value);

                        if (checklistMasterRecord == null || checklistMasterRecord.cm_Survey == null) {
                            tracingService.Trace("ChecklistMaster or Survey is null. Skipping incident.");
                            continue;
                        }

                        tracingService.Trace($"Retrieving survey with ID: {checklistMasterRecord.cm_Survey.Id}");
                        msfp_survey surveyRecord = commonBusinessLogic.GetRecordById<msfp_survey>(checklistMasterRecord.cm_Survey.Id);

                        tracingService.Trace($"Retrieving primary contact with ID: {incident.PrimaryContactId?.Id}");
                        Contact contactRecord = commonBusinessLogic.GetRecordById<Contact>(incident.PrimaryContactId.Id);

                        tracingService.Trace("Retrieving default MSFP project.");
                        msfp_project project = commonBusinessLogic.GetDefaultMSFPProject();

                        tracingService.Trace("Preparing Customer Voice invite.");
                        msfp_customervoiceprocessor invite = new msfp_customervoiceprocessor() {
                            msfp_To = contactRecord.EMailAddress1,
                            msfp_projectid = project.Id.ToString(),
                            msfp_SurveyId = surveyRecord.msfp_sourcesurveyidentifier,
                            msfp_EmailTemplateID = "00",
                            msfp_firstname = contactRecord.FirstName,
                            msfp_lastname = contactRecord.LastName,
                            msfp_surveyvariablesjson = "{\"locale\":\"en-US\"}",
                            msfp_regarding = Incident.EntityLogicalName.ToString() + "," + incident.Id.ToString(),
                        };

                        Guid inviteId = commonBusinessLogic.CreateInvite(invite);

                        tracingService.Trace("Customer Voice invite prepared successfully.");
                        tracingService.Trace($"Invite created with Id: {inviteId}.");
                    } else {
                        tracingService.Trace("Incident missing IncidentCategory or CauseCategory. Skipping.");
                    }
                }

                tracingService.Trace("Plugin execution completed successfully.");
            } catch (AggregateException aggregateException) {
                var exceptions = aggregateException.InnerExceptions;
                foreach (var inner in aggregateException.InnerExceptions) {
                    tracingService.Trace($"Inner exception: {inner.Message}");
                }
                throw new InvalidPluginExecutionException("Aggregate exception occurred.", aggregateException);
            } catch (Exception ex) {
                if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0) {
                    ex = aggregateException.InnerExceptions[0];
                }
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"AsyncPluginIncidentResponseGeneration Process End");
            }
        }
    }
}
