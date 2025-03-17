using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xrm.Sdk.Messages;

namespace Plugins {
    public class AsyncPluginIncidentResponseGeneration : PluginBase {
        public AsyncPluginIncidentResponseGeneration(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AsyncPluginIncidentResponseGeneration)) {
        }
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
                if (context.PrimaryEntityName != Incident.EntityLogicalName || context.MessageName != "Update") {
                    throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Incident and message must be Update");
                }

                Incident incidentRecord = commonBusinessLogic
                    .GetRecordById<Incident>(context.PrimaryEntityId, Incident.EntityLogicalName) ??
                    throw new InvalidPluginExecutionException("Invalid plugin execution: Incident/Case not found");

                if (incidentRecord?.cm_GenerateChecklist.Value == null || incidentRecord.cm_GenerateChecklist.Value == false) {
                    return;
                }

                List<Guid> responseList = new List<Guid>();

                if (incidentRecord.cm_CauseCategory != null) {
                    List<cm_CaseChecklistCatalog> questionsList = commonBusinessLogic.GetCaseChecklistCatalogCaseSub(incidentRecord.cm_CauseCategory.Id);
                    if (questionsList.Any()) {
                        tracingService.Trace($"cm_CauseCategory Questions List {string.Join(" ,", questionsList.Select(q => q.Id))}");
                        responseList.AddRange(commonBusinessLogic.CreateCasechecklistResponse(questionsList, incidentRecord));
                    }
                    if (responseList.Count > 0) {
                        return;
                    }
                }

                if (incidentRecord.cm_IncidentCategory != null) {
                    List<cm_CaseChecklistCatalog> questionsList = commonBusinessLogic.GetCaseChecklistCatalogCaseCat(incidentRecord.cm_IncidentCategory.Id);
                    if (questionsList.Any()) {
                        tracingService.Trace($"cm_IncidentCategory Questions List {string.Join(" ,", questionsList.Select(q => q.Id))}");
                        responseList.AddRange(commonBusinessLogic.CreateCasechecklistResponse(questionsList, incidentRecord));
                    }
                    if (responseList.Count > 0) {
                        return;
                    }
                }

                throw new InvalidPluginExecutionException("Invalid plugin execution: No Case Category or Sub Category has been found");

                //if (incidentRecord.cm_IncidentCategory != null) {
                //    List<cm_CaseChecklistCatalog> questionsList = commonBusinessLogic.GetCaseChecklistCatalogCaseCat(incidentRecord.cm_IncidentCategory.Id) ??
                //        throw new InvalidPluginExecutionException("Invalid plugin execution: cm_CaseChecklistCatalog not found");

                //    if (questionsList.Any()) {
                //        tracingService.Trace($"questionsList {string.Join(" ,", questionsList.Select(q => q.Id))}");
                //        commonBusinessLogic.CreateCasechecklistResponse(questionsList, incidentRecord);
                //    } else {
                //        throw new InvalidPluginExecutionException("Invalid plugin execution: No cm_CaseChecklistCatalog has been found");
                //    }
                //} else {
                //    throw new InvalidPluginExecutionException("Invalid plugin execution: No Incident.cm_IncidentCategory has been found");
                //}
            } catch (AggregateException aggregateException) {
                var exceptions = aggregateException.InnerExceptions;
                foreach (var inner in aggregateException.InnerExceptions) {
                    tracingService.Trace($"Inner exception: {inner.Message}");
                }
                throw new InvalidPluginExecutionException("Aggregate exception occurred.", aggregateException);
            } catch (Exception ex) {
                // Check if the exception is an AggregateException and unwrap it
                if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0) {
                    ex = aggregateException.InnerExceptions[0];
                }
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(detailedError, ex);
            } finally {
                tracingService.Trace($"AsyncPluginIncidentResponseGeneration Process End");
            }
        }
    }
}
