using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class AsyncPluginOpportunityLeadClosureGeneration : PluginBase {
        public AsyncPluginOpportunityLeadClosureGeneration(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AsyncPluginOpportunityLeadClosureGeneration)) {
        }
        /// <summary>
        ///     This async Dataverse plugin fires on creating an Opportunity, loads its related Program Association, Team, 
        ///     and the appropriate Lead-Closure Checklist Master, then retrieves checklist questions. 
        ///     If questions exist it generates response records, otherwise it throws an InvalidPluginExecutionException.
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

            if (context.PrimaryEntityName != Opportunity.EntityLogicalName || context.MessageName != "Create") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Opportunity and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                #region Load Records
                Opportunity opportunityRecord = commonBusinessLogic
                    .GetRecordById<Opportunity>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("Opportunity not found");

                cm_ProgramAssociation programAssociationRecord = commonBusinessLogic
                    .GetRecordById<cm_ProgramAssociation>(opportunityRecord.cm_AssociatedProgram.Id)
                    ?? throw new InvalidPluginExecutionException("cm_ProgramAssociation not found");

                Team teamRecord = commonBusinessLogic
                    .GetRecordById<Team>(programAssociationRecord.cm_Program.Id)
                    ?? throw new InvalidPluginExecutionException("Team not found");

                cm_LeadClosureChecklistMaster leadClosureChecklistMasterRecord = commonBusinessLogic
                    .GetLeadClosureCheckLMasterByTeam(teamRecord.Id, opportunityRecord.cm_OpportunityType)
                    ?? throw new InvalidPluginExecutionException("cm_LeadClosureChecklistMaster not found. Please check your Lead Type");

                List<cm_LeadClosureChecklistCatalog> leadClosureChecklistCatalogList = commonBusinessLogic
                    .GetLeadClosureChecklistCatalogCat(leadClosureChecklistMasterRecord.Id)
                    ?? throw new InvalidPluginExecutionException("cm_LeadClosureChecklistCatalog List not found");

                tracingService.Trace($"teamRecord: {teamRecord}\n" +
                                     $"programAssociationRecord: {programAssociationRecord.Id}\n" +
                                     $"leadClosureChecklistMasterRecord: {leadClosureChecklistMasterRecord.Id} \n" +
                                     $"leadClosureChecklistCatalogList count: {leadClosureChecklistCatalogList.Count} \n");
                #endregion

                if (leadClosureChecklistCatalogList.Any()) {
                    commonBusinessLogic.CreateLeadClosureResponses(leadClosureChecklistCatalogList, opportunityRecord, teamRecord.Id);
                } else {
                    throw new InvalidPluginExecutionException("No records in Lead Closure Checklist were found in the Catalog matching the current Checklist Master.");
                }

                //// All Opportunity records should be shared with the same team as the Lead is shared with
                //commonBusinessLogic.ExecuteRecordShare(opportunityRecord, teamRecord.Id);
                //commonBusinessLogic.ExecuteRecordShare(
                //    new Entity(Account.EntityLogicalCollectionName,
                //    opportunityRecord.CustomerId.Id), teamRecord.Id);
                //commonBusinessLogic.ExecuteRecordShare(
                //    new Entity(Contact.EntityLogicalCollectionName,
                //    opportunityRecord.ContactId.Id), teamRecord.Id);

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
                tracingService.Trace($"AsyncPluginOpportunityLeadClosureGeneration Process End");
            }
        }
    }
}
