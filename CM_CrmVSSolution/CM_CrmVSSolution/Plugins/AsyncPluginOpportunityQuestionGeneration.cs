using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class AsyncPluginOpportunityQuestionGeneration : PluginBase {
        public AsyncPluginOpportunityQuestionGeneration(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AsyncPluginOpportunityQuestionGeneration)) {
        }
        /// <summary>
        ///     This async Dataverse plugin runs when a new Opportunity is created, loads its related Program Association and Team, 
        ///     then retrieves any question catalog entries for that team and opportunity type. 
        ///     If questions exist, it generates corresponding question-response records. 
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

                List<cm_QuestionCatalog> questionsList = commonBusinessLogic.GetQuestionsListByTeam(teamRecord.Id, opportunityRecord.cm_OpportunityType);
                tracingService.Trace($"questionsList {string.Join(" ,", questionsList.Select(q => q.Id))}");
                #endregion

                if (questionsList.Any()) {
                    commonBusinessLogic.CreateQuestionResponses(questionsList, opportunityRecord);
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
                tracingService.Trace($"AsyncPluginOpportunityQuestionGeneration Process End");
            }
        }
    }
}
