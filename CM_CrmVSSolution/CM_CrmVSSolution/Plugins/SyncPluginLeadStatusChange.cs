using CM_CrmVSSolution.Common.Models;
using Common;
using Microsoft.Xrm.Sdk;
using Plugins;
using System;

namespace CM_CrmVSSolution.Plugins {
    public class SyncPluginLeadStatusChange : PluginBase {
        public SyncPluginLeadStatusChange(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SyncPluginLeadStatusChange)) {
        }
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext) {
            if (localPluginContext == null) {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            var tracingService = localPluginContext.TracingService;

            if (context.PrimaryEntityName != Lead.EntityLogicalName || context.MessageName != "Update") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Lead and message must be Update");
            }

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            Lead leadRecord = commonBusinessLogic
                .GetRecordById(context.PrimaryEntityId, Lead.EntityLogicalName) as Lead;

            if (leadRecord.StatusCode.Value != lead_statuscode.Qualified) {
                return;
            }

            try {
                

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
                tracingService.Trace($"AsyncPluginContactUpdate Process End");
            }
        }
    }
}
