using Microsoft.Xrm.Sdk;
using Plugins.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web.UI.WebControls;

namespace Plugins {
    public class ContactAndConnectionValidation : PluginBase {

        // Please refer to CD-498 list of ERP connection roles
        // TODO: create a field in CRM to manage this list dynamically
        private static readonly List<string> ERP_CONNECTION_ROLES = new List<string> {
            "Accounting Contact",
            "Billing Contact",
            "Emergency Contact",
            "Agreement Notices Contact",
            "Audit Data Entry",
            "Audit Manager",
            "Audit Supervisor",
            "Legal Contact",
            "Primary Contact",
            "Environmental Lead",
            "Secondary Contact"
        };

        public ContactAndConnectionValidation(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ContactAndConnectionValidation)) {
        }
        /// <summary>
        ///     Steps:
        ///         Sync Plugins.ContactAndConnectionValidation: Update of Contact
        ///         Sync Plugins.ContactAndConnectionValidation: Create of Contact
        ///         Sync Plugins.ContactAndConnectionValidation: Update of Connection
        ///         Sync Plugins.ContactAndConnectionValidation: Create of Connection
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

            if ((context.PrimaryEntityName != Contact.EntityLogicalName
                && context.PrimaryEntityName != Connection.EntityLogicalName)
                || (context.MessageName != "Update" && context.MessageName != "Create")) {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Opportunity and message must be Create");
            }

            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            var user = commonBusinessLogic.GetRecordById<SystemUser>(localPluginContext.PluginExecutionContext.InitiatingUserId)
                ?? throw new InvalidPluginExecutionException("Invalid plugin execution: System User not found");

            // True if this record was triggered by a human (interactive) user.
            // Non-interactive users (e.g., App Registrations / Service Principals) are used for API or integration calls.
            bool isUser = user.AccessMode != systemuser_accessmode.Noninteractive;

            // Skip validation for non-interactive users (e.g., data migration)
            // Must remove after the data migration
            if (!isUser) {
                tracingService.Trace($"Must remove after the data migration to avoid blocking data import");
                return;
            }

            if (context.PrimaryEntityName == Connection.EntityLogicalName) {
                ExecuteConnectionValidation(commonBusinessLogic, localPluginContext, isUser);
            } else if (context.PrimaryEntityName == Contact.EntityLogicalName) {
                ExecuteContactValidation(commonBusinessLogic, localPluginContext, isUser);
            } else {
                throw new InvalidPluginExecutionException("Invalid plugin execution: Entity must be Contact or Connection");
            }
        }

        private void ExecuteContactValidation(CommonBusinessLogic commonBusinessLogic, ILocalPluginContext localPluginContext, bool isUser) {
            var context = localPluginContext.PluginExecutionContext;
            var tracingService = localPluginContext.TracingService;

            try {
                #region Load Records
                var contactRecord = commonBusinessLogic.GetRecordById<Contact>(context.PrimaryEntityId)
                    ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Contact not found");
                tracingService.Trace($"contactRecord: {contactRecord.Id}");
                #endregion

                // Contact uniqueness validation is defined by Parent Account + Full Name + Email Address
                if (commonBusinessLogic.IsContactDuplicated(contactRecord)) {
                    throw new InvalidPluginExecutionException("Contacts must be unique for each Account");
                }

                if (contactRecord.cm_SourceIdentifier == cm_sourceidentifiertype.ERP && isUser) {
                    throw new InvalidPluginExecutionException("Users cannot modify ERP Contacts");
                }

                // Do nothing, all validation steps passed
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"ContactAndConnectionValidation Process End");
            }
        }

        private void ExecuteConnectionValidation(CommonBusinessLogic commonBusinessLogic, ILocalPluginContext localPluginContext, bool isUser) {
            var context = localPluginContext.PluginExecutionContext;
            var tracingService = localPluginContext.TracingService;

            Entity connection = (Entity)context.InputParameters["Target"];

            var connectionRecord = commonBusinessLogic.GetRecordById<Connection>(connection.Id)
                ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Connection not found");

            if (connectionRecord.Record1ObjectTypeCode != connection_record1objecttypecode.Account
                || connectionRecord.Record2ObjectTypeCode != connection_record2objecttypecode.Contact) {

                context.SharedVariables["mustSkip"] = true;
                return;
            }

            try {
                tracingService.Trace($"record1: {connectionRecord.Record1ObjectTypeCode} {connectionRecord.Record1Id.Id}, record2: {connectionRecord.Record2ObjectTypeCode} {connectionRecord.Record2Id.Id}, roleTo: connectionrole {connectionRecord.Record2RoleIdName}");

                #region Load Records              
                var accountRecord = commonBusinessLogic.GetRecordById<Account>(connectionRecord.Record1Id.Id)
                    ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Account not found");

                var contactRecord = commonBusinessLogic.GetRecordById<Contact>(connectionRecord.Record2Id.Id)
                    ?? throw new InvalidPluginExecutionException("Invalid plugin execution: Contact not found");

                var user = commonBusinessLogic.GetRecordById<SystemUser>(localPluginContext.PluginExecutionContext.InitiatingUserId)
                    ?? throw new InvalidPluginExecutionException("Invalid plugin execution: System User not found");
                #endregion

                if (isUser && contactRecord.cm_SourceIdentifier == cm_sourceidentifiertype.ERP) {
                    throw new InvalidPluginExecutionException("Users cannot add or change Connections to ERP Contacts");
                }

                if (isUser && ERP_CONNECTION_ROLES.Contains(connectionRecord.Record2RoleIdName)) {
                    throw new InvalidPluginExecutionException("Users cannot add ERP Roles to contact");
                }

                // Do nothing, all validation steps passed
            } catch (Exception ex) {
                string detailedError = $"Unexpected error while processing {context.PrimaryEntityName} record with ID " +
                    $"{context.PrimaryEntityId}: {ex.Message}\nStack Trace: {ex.StackTrace}";
                tracingService.Trace($"Error: {detailedError}");
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace($"ContactAndConnectionValidation Process End");
            }
        }
    }
}