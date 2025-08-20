using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;

namespace Plugins {
    public class ShareRecordsByProgram : PluginBase {
        public ShareRecordsByProgram(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ShareRecordsByProgram)) { }
        /// <summary>
        ///     Update every record that contains cm_program
        /// 
        ///     Steps:
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_checklistmaster cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_leadclosurechecklistmaster cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_leadclosurechecklistresponse cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_programassociation cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_questioncatalog cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of cm_questionresponse cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of connection cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Create of incident cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_checklistmaster cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_leadclosurechecklistmaster cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_leadclosurechecklistresponse cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_programassociation cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_questioncatalog cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of cm_questionresponse cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of connection cm_program
        ///         Sync Plugins.ShareRecordsByProgram: Update of incident cm_program
        ///     
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
            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService)
               ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Error creating CommonBusinessLogic");

            if (context.MessageName != "Create" && context.MessageName != "Update") {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Account and message must be Create or Update or Associate");
            }

            if (context.PrimaryEntityName != cm_ChecklistMaster.EntityLogicalName
                && context.PrimaryEntityName != cm_LeadClosureChecklistMaster.EntityLogicalName
                && context.PrimaryEntityName != cm_LeadClosureChecklistResponse.EntityLogicalName
                && context.PrimaryEntityName != cm_ProgramAssociation.EntityLogicalName
                && context.PrimaryEntityName != cm_QuestionCatalog.EntityLogicalName
                && context.PrimaryEntityName != cm_QuestionResponse.EntityLogicalName
                && context.PrimaryEntityName != Connection.EntityLogicalName
                && context.PrimaryEntityName != Incident.EntityLogicalName) {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity not included in the available list");
            }

            try {
                bool result = commonBusinessLogic.ShareRecordWithOwnProgram(context);
                if (result) {
                    tracingService.Trace("Record successfully shared");
                }
            } catch (Exception ex) {
                context.InputParameters.TryGetValue("Target", out var target);
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName ?? target?.ToString() ?? "Unknown"} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"ShareRecordsByProgram Process End");
            }
        }
    }
}
