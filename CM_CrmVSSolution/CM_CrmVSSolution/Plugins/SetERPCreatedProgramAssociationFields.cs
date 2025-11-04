using Microsoft.Xrm.Sdk;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class SetERPCreatedProgramAssociationFields : PluginBase {
        public SetERPCreatedProgramAssociationFields(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SetERPCreatedProgramAssociationFields)) {
        }
        /// <summary>
        ///     Update cm_ProgramAssociation coming from the ERP with Name and Province based on the Program and Account
        /// 
        ///     Steps:
        ///         Sync Plugins.SetERPCreatedProgramAssociationFields: Update of cm_programassociation cm_account, cm_program, cm_sourceidentifier
        ///         Sync Plugins.SetERPCreatedProgramAssociationFields: Create of cm_programassociation cm_account, cm_program, cm_sourceidentifier
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

            if (context.PrimaryEntityName != cm_ProgramAssociation.EntityLogicalName ||
                (context.MessageName != "Create" && context.MessageName != "Update")) {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be cm_ProgramAssociation and message must be Create or Update");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                var programAssocRecord = commonBusinessLogic.GetRecordById<cm_ProgramAssociation>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("cm_ProgramAssociation not found");

                if (programAssocRecord.cm_SourceIdentifier != cm_sourceidentifiertype.ERP) {
                    return;
                }

                if (programAssocRecord.cm_Program == null) {
                    throw new InvalidPluginExecutionException("Program must be valid for program associations from ERP");
                }
                if (programAssocRecord.cm_Account == null) {
                    throw new InvalidPluginExecutionException("Account must be valid for program associations from ERP");
                }

                var programRecord = commonBusinessLogic.GetRecordById<Team>(programAssocRecord.cm_Program.Id)
                    ?? throw new InvalidPluginExecutionException("Team/Program not found");

                var accountRecord = commonBusinessLogic.GetRecordById<Account>(programAssocRecord.cm_Account.Id)
                    ?? throw new InvalidPluginExecutionException("Account not found");

                var newProgramAssoc = new cm_ProgramAssociation() {
                    Id = programAssocRecord.Id,
                    cm_Name = string.Concat(accountRecord.Name.ToString(), " - ", programRecord.Name.ToString()),
                    cm_Province = programRecord.cm_Province
                };

                service.Update(newProgramAssoc);

                tracingService.Trace($"Program Association with Id: {programAssocRecord.Id} updated with name: {newProgramAssoc.cm_Name} and province {newProgramAssoc.cm_Province}");

            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"SetERPCreatedProgramAssociationFields Process End");
            }
        }
    }
}