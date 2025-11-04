using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugins.Models;
using System;
using System.Linq;

namespace Plugins {
    /// <summary>
    ///     Synchronous Plugins.ConnectionUpdateAccountManager: Create of connection
    ///     Synchronous Plugins.ConnectionUpdateAccountManager: Update of connection
    ///     
    ///     For Account on Record 1: Update Account (record1) account manager (cm_accountmanager) with systemuser (record2)  
    ///     For Opportunity on Record 1: Update Opportunity cm_Program with team from opportunities cm_ProgramAssociation 
    /// </summary>
    public class ConnectionUpdateAccountManager : PluginBase {
        public ConnectionUpdateAccountManager(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ConnectionUpdateAccountManager)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext) {

            if (localPluginContext == null)
                throw new ArgumentNullException(nameof(localPluginContext));

            var context = localPluginContext.PluginExecutionContext;
            var tracingService = localPluginContext.TracingService;
            var serviceFactory = localPluginContext.OrgSvcFactory;
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            CommonBusinessLogic commonBusinessLogic = new CommonBusinessLogic(service, tracingService);

            Entity connection = (Entity)context.InputParameters["Target"];

            var record1 = connection.GetAttributeValue<EntityReference>("record1id");
            var record2 = connection.GetAttributeValue<EntityReference>("record2id");
            var roleTo = connection.GetAttributeValue<EntityReference>("record2roleid");

            if (record1 == null || record2 == null || roleTo == null) {
                context.SharedVariables["mustSkip"] = true;
                return;
            }

            if (record1.LogicalName != "account" && record1.LogicalName != "opportunity") {
                context.SharedVariables["mustSkip"] = true;
                return;
            }

            try {
                tracingService.Trace($"record1: {record1.LogicalName} {record1.Id}, record2: {record2.LogicalName} {record2.Id}, roleTo: connectionrole {roleTo.Id}");

                #region Update Connection Program
                if (record1.LogicalName == Opportunity.EntityLogicalName && record2.LogicalName == Contact.EntityLogicalName) {
                    var opportunityRecord = commonBusinessLogic.GetRecordById<Opportunity>(record1.Id)
                        ?? throw new InvalidPluginExecutionException("record1 not found");
                    if (opportunityRecord.cm_AssociatedProgram == null) return;
                    var assocProgramRecord = commonBusinessLogic.GetRecordById<cm_ProgramAssociation>(opportunityRecord.cm_AssociatedProgram.Id)
                        ?? throw new InvalidPluginExecutionException("opportunityRecord.cm_AssociatedProgram not found");
                    var teamRecord = commonBusinessLogic.GetRecordById<Team>(assocProgramRecord.cm_Program.Id)
                        ?? throw new InvalidPluginExecutionException("assocProgramRecord.cm_Program not found");

                    var connectionUpdate = new Connection() {
                        Id = context.PrimaryEntityId,
                        cm_Program = new EntityReference(Team.EntityLogicalName, teamRecord.Id)
                    };
                    service.Update(connectionUpdate);
                    tracingService.Trace($"Connection record with Id: {connectionUpdate.Id} updated with cm_program/team: {connectionUpdate.cm_Program.Id}");
                }
                #endregion

                #region Update Account cm_accountmanager
                if (record1.LogicalName == Account.EntityLogicalName) {
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

                    if (existingConnections.Entities.Any()) {
                        throw new InvalidPluginExecutionException("Only one Account Manager connection is allowed per account.");
                    } else {
                        //Update Account Manager column in Account table
                        if (roleToName.Equals("Account Manager", StringComparison.OrdinalIgnoreCase)) {

                            Entity accountToUpdate = new Entity("account", record1.Id);
                            accountToUpdate["cm_accountmanager"] = new EntityReference("systemuser", record2.Id);
                            service.Update(accountToUpdate);
                        }
                    }
                }
                #endregion
            } catch (Exception ex) {
                tracingService.Trace("Plugin exception: " + ex.ToString());
                throw new InvalidPluginExecutionException(ex.Message);
            } finally {
                tracingService.Trace("ConnectionUpdateAccountManager Process End");
            }

        }
    }
}