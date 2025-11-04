using Microsoft.Xrm.Sdk;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class SyncPluginIncidentValidationBeforeResolve : PluginBase {
        public SyncPluginIncidentValidationBeforeResolve(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SyncPluginIncidentValidationBeforeResolve)) {
        }
        /// <summary>
        ///     Steps:
        ///         Sync Plugins.SyncPluginIncidentValidationBeforeResolve: Update of incident statecode
        ///         
        ///     This Dataverse plugin runs on updating an Incident to the Resolved state, otherwise it flags skipping further processing. 
        ///     It loads all checklist responses for the case and throws an error if any question is unanswered or if no questions exist. 
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

            if (context.PrimaryEntityName != Incident.EntityLogicalName || context.MessageName != "Update") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Opportunity and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            Incident incidentRecord = commonBusinessLogic.GetRecordById<Incident>(context.PrimaryEntityId) ??
                throw new InvalidPluginExecutionException("Invalid plugin execution: Incident/Case not found");

            tracingService.Trace($"incidentRecord: {incidentRecord}");
            tracingService.Trace($"incidentRecord.StateCode : {incidentRecord.StateCode}");

            // Description is being used as a flag to signify the incident has being processed and should not trigger the plugin again
            if (incidentRecord.StateCode != incident_statecode.Resolved || incidentRecord.Description != null) {
                context.SharedVariables["mustSkip"] = true;
                return;
            }
            try {
                var incidentList = new List<Incident>();

                if (incidentRecord.NumberOfChildIncidents != null && incidentRecord.NumberOfChildIncidents > 0) {
                    incidentList.AddRange(commonBusinessLogic.GetChildrenIncidents(incidentRecord));
                } else {
                    incidentList.Add(incidentRecord);
                }

                // Iterate through all incidents > get invites > filter invites > 
                foreach (var incident in incidentList) {
                    List<msfp_surveyinvite> inviteList = commonBusinessLogic.GetSurveyInviteByIncident(incident);

                    IEnumerable<string> titleListWithOpenStatus = inviteList
                        .Where(invite => invite.StateCode == msfp_surveyinvite_statecode.Open).ToList()
                        .Select(invite => invite.RegardingObjectIdName);

                    if (titleListWithOpenStatus.Any()) {
                        string incidentTitles = string.Join(", ", titleListWithOpenStatus);
                        throw new InvalidPluginExecutionException($"Please answer the Surveys in before closing the Case.\n Incidents with open surveys: {incidentTitles}");
                    }
                }

                if (incidentList.Count > 1) {
                    commonBusinessLogic.ResolveChildCases(incidentList);
                }
                // Do nothing, all validation steps passed
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"SyncPluginIncidentValidationBeforeResolve Process End");
            }
        }
    }
}