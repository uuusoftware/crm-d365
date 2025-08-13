using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Common.Models;
using Microsoft.Xrm.Sdk.Query;

namespace Plugins {
    /// <summary>
    ///     Synchronous Plugins.ConnectionUpdateAccountManager: Create of connection
    ///     Synchronous Plugins.ConnectionUpdateAccountManager: Update of connection
    ///     
    ///     Update account (record1) account manager (cm_accountmanager) with systemuser (record2)  
    /// </summary>
    public class ConnectionUpdateAccountManager : PluginBase {
        public ConnectionUpdateAccountManager(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ConnectionUpdateAccountManager)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext) {

            if (localPluginContext == null)
                throw new ArgumentNullException(nameof(localPluginContext));

            var context = localPluginContext.PluginExecutionContext;
            var tracing = localPluginContext.TracingService;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            try {
                Entity connection = (Entity)context.InputParameters["Target"];

                var record1 = connection.GetAttributeValue<EntityReference>("record1id");
                var record2 = connection.GetAttributeValue<EntityReference>("record2id");
                var roleTo = connection.GetAttributeValue<EntityReference>("record2roleid");

                if (record1 == null || record2 == null || roleTo == null)
                    return;

                tracing.Trace($"record1: {record1.Id}, record2: {record2.Id}, roleTo: {roleTo.Id}");


                if (record1.LogicalName != "account")
                    return;

                Entity roleToEntity = service.Retrieve("connectionrole", roleTo.Id, new ColumnSet("name"));
                string roleToName = roleToEntity.GetAttributeValue<string>("name");

                if (!roleToName.Equals("Account Manager", StringComparison.OrdinalIgnoreCase))
                    return;

                var query = new QueryExpression("connection") {
                    ColumnSet = new ColumnSet("connectionid"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("record1id", ConditionOperator.Equal, record1.Id),
                            new ConditionExpression("record2roleid", ConditionOperator.Equal, roleTo.Id)
                        }
                    }
                };
                // Exclude current connection if it already has an ID (on update scenario or edge case on create)
                if (connection.Id != Guid.Empty) {
                    query.Criteria.AddCondition("connectionid", ConditionOperator.NotEqual, connection.Id);
                }
                var existingConnections = service.RetrieveMultiple(query);

                if (existingConnections.Entities.Any())
                    throw new InvalidPluginExecutionException("Only one Account Manager connection is allowed per account.");
                else {
                    //Update Account Manager column in Account table
                    if (roleToName.Equals("Account Manager", StringComparison.OrdinalIgnoreCase)) {

                        Entity accountToUpdate = new Entity("account", record1.Id);
                        accountToUpdate["cm_accountmanager"] = new EntityReference("systemuser", record2.Id);
                        service.Update(accountToUpdate);
                    }
                }

                tracing.Trace("ConnectionUpdateAccountManager Process End");
            } catch (Exception ex) {
                tracing.Trace("Plugin exception: " + ex.ToString());
                throw new InvalidPluginExecutionException(ex.Message);
            }

        }
    }
}
