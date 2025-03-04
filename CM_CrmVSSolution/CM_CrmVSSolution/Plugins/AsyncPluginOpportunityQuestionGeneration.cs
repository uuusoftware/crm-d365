using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace Plugins {
    public class AsyncPluginOpportunityQuestionGeneration : PluginBase {
        public AsyncPluginOpportunityQuestionGeneration(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AsyncPluginOpportunityQuestionGeneration)) {
        }
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

                Opportunity opportunityRecord = commonBusinessLogic
                    .GetRecordById(context.PrimaryEntityId, Opportunity.EntityLogicalName) as Opportunity;

                cm_ProgramAssociation programAssociationRecord = commonBusinessLogic
                    .GetRecordById(opportunityRecord.cm_AssociatedProgram.Id, cm_ProgramAssociation.EntityLogicalName) as cm_ProgramAssociation;

                Team teamRecord = commonBusinessLogic
                    .GetRecordById(programAssociationRecord.cm_Program.Id, Team.EntityLogicalName) as Team;

                List<cm_QuestionCatalog> questionsList = commonBusinessLogic.GetQuestionsListByTeam(teamRecord.Id, opportunityRecord.cm_OpportunityType);

                commonBusinessLogic.CreateQuestionResponses(questionsList, opportunityRecord);

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
                tracingService.Trace($"AsyncPluginOpportunityQuestionGeneration Process End");
            }
        }
    }
}
