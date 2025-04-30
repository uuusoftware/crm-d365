using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class SyncPluginIncidentValidationBeforeResolve : PluginBase {
        public SyncPluginIncidentValidationBeforeResolve(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SyncPluginIncidentValidationBeforeResolve)) {
        }
        /// <summary>
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

            if (incidentRecord.StateCode != incident_statecode.Resolved) {
                context.SharedVariables["mustSkip"] = true;
                return;
            }
            try {
                List<cm_CaseChecklistResponse> responses = commonBusinessLogic.GetResponsesByIncident(incidentRecord);
                //tracingService.Trace($"incidentRecord: {responses.Join(',', responses.Select(res =>  res.statuscodeName ))}");

                if (responses.Any()) {
                    responses.ForEach(response => {
                        if (response.statuscode != cm_casechecklistresponse_statuscode.Complete) {
                            throw new InvalidPluginExecutionException("Please answer all questions before proceeding.");
                        }
                    });
                } else {
                    throw new InvalidPluginExecutionException("No questions found. Please move to Research Stage and complete the checklist before resolving the case.");
                }
                // do nothing
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
