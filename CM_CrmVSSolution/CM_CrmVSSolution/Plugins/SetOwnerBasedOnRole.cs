using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {
    public class SetOwnerBasedOnRole : PluginBase {
        public SetOwnerBasedOnRole(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SetOwnerBasedOnRole)) {
        }
        /// <summary>
        ///     Steps:
        ///     Sync Plugins.SetOwnerBasedOnRole: Create of account cm_role
        ///     Sync Plugins.SetOwnerBasedOnRole: Update of account cm_role
        ///     
        ///     Plugin updates the account owner by assigning it to the Team that maps to the Role (cm_Role)
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

            if (context.PrimaryEntityName != Account.EntityLogicalName || (context.MessageName != "Create" && context.MessageName != "Update")) {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Account and message must be Create or Update");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            try {
                #region Load Records
                var accountRecord = commonBusinessLogic.GetRecordById<Account>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("accountRecord can not be null");
                #endregion

                if (accountRecord.cm_Role == null)
                    return;

                Team teamRecord = commonBusinessLogic.GetTeamByAccountRole(accountRecord);

                commonBusinessLogic.ChangeRecordOwner(Account.EntityLogicalName, accountRecord.Id, teamRecord.Id);
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"SetOwnerBasedOnRole Process End");
            }
        }
    }
}
