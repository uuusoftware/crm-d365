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

        /// <summary>
        ///     This plugin fires on updating an Open Lead, fetches its program associations (or aborts if none), 
        ///     then creates a Contact, Account, and Opportunities per association, linking them together.
        ///     
        ///     It mimics the OOB lead to opportunity behavior but instead of one, it creates multiple opportunities
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

                // Creates an opportunity for each lead using the same account and contact
                programAssociation.ForEach(association => {
                    programAssociationGuids.Add(commonBusinessLogic.CreateOpportunityForLead(leadRecord, association, contactId, accountId));
                    commonBusinessLogic.UpdateProgramAsscAccount(association, accountId);
                });

                commonBusinessLogic.SetParentCustomer(contactId, accountId);
                tracingService.Trace($"Records created:\n Account: {accountId}\n Contact: {contactId} ");
                tracingService.Trace($"Program Associations: {string.Join(", ", programAssociationGuids)}");

            } catch (Exception ex) {
                if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0) {
                    ex = aggregateException.InnerExceptions[0];
                }
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"SyncPluginLeadStatusChange Process End");
            }
        }
    }
}
