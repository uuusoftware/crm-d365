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
                    .GetRecordById<Opportunity>(context.PrimaryEntityId);
                tracingService.Trace($"opportunityRecord {opportunityRecord.Id}");

                cm_ProgramAssociation programAssociationRecord = commonBusinessLogic
                    .GetRecordById<cm_ProgramAssociation>(opportunityRecord.cm_AssociatedProgram.Id);
                tracingService.Trace($"programAssociationRecord {programAssociationRecord.Id}");

                Team teamRecord = commonBusinessLogic
                    .GetRecordById<Team>(programAssociationRecord.cm_Program.Id);
                tracingService.Trace($"teamRecord {teamRecord.Id}");

                cm_LeadClosureChecklistMaster leadClosureChecklistMasterRecord = commonBusinessLogic
                    .GetLeadClosureCheckLMasterByTeam(teamRecord.Id, opportunityRecord.cm_OpportunityType) ??
                        throw new InvalidPluginExecutionException("No Lead Closure Checklist Master found for this Opportunity type and Team");

                List<cm_LeadClosureChecklistCatalog> leadClosureChecklistCatalogList = commonBusinessLogic
                    .GetLeadClosureChecklistCatalogCat(leadClosureChecklistMasterRecord.Id);
                #endregion

                if (leadClosureChecklistCatalogList.Any()) {
                    commonBusinessLogic.CreateLeadClosureResponses(leadClosureChecklistCatalogList, opportunityRecord, teamRecord.Id);
                } else {
                    throw new InvalidPluginExecutionException("No question were found in the catalog matching the criteria.");
                }
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
