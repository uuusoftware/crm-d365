using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
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
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.PrimaryEntityName != Lead.EntityLogicalName || context.MessageName != "Update") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Lead and message must be Update");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            var svcContext = new OrgContext(service);
            Lead leadRecord = svcContext.LeadSet.FirstOrDefault(record => record.Id == context.PrimaryEntityId);

            if (leadRecord.StateCode.Value != lead_statecode.Open) {
                context.SharedVariables["mustSkip"] = true;
                return;
            }

            try {
                List<cm_ProgramAssociation> programAssociation = commonBusinessLogic.GetAllProgramAssociationsByLead(leadRecord.Id);

                if (!programAssociation.Any()) {
                    throw new InvalidPluginExecutionException("No Program Association found for this Lead");
                }

                Guid contactId = commonBusinessLogic.CreateContactForLead(leadRecord);
                Guid accountId = commonBusinessLogic.CreateAccountForLead(leadRecord, contactId);
                List<Guid> programAssociationGuids = new List<Guid>();

                programAssociation.ForEach(association => {
                    programAssociationGuids.Add(commonBusinessLogic.CreateOpportunityForLead(leadRecord, association, contactId, accountId));
                });

                commonBusinessLogic.SetParentCustomer(contactId, accountId);
                tracingService.Trace($"Records created:\n Account: {accountId}\n Contact: {contactId} ");
                tracingService.Trace($"Program Associations: {string.Join(", ", programAssociationGuids)}");

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
