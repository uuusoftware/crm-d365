using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;

namespace Plugins {

    public class CommonBusinessLogic {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracingService;

        public CommonBusinessLogic(IOrganizationService service, ITracingService tracingService) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        }
        public T GetRecordById<T>(Guid id) where T : Entity {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    switch (typeof(T).Name) // Using Name since switch does not support Type directly
                    {
                        case "Account":
                        return svcContext.AccountSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Contact":
                        return svcContext.ContactSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_Province":
                        return svcContext.cm_ProvinceSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_ProgramAssociation":
                        return svcContext.cm_ProgramAssociationSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_QuestionCatalog":
                        return svcContext.cm_QuestionCatalogSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_QuestionResponse":
                        return svcContext.cm_QuestionResponseSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "SalesOrder":
                        return svcContext.SalesOrderSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Connection":
                        return svcContext.ConnectionSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Lead":
                        return svcContext.LeadSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Opportunity":
                        return svcContext.OpportunitySet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Team":
                        return svcContext.TeamSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "Incident":
                        return svcContext.IncidentSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_CaseCategory":
                        return svcContext.cm_CaseCategorySet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_CaseChecklistResponse":
                        return svcContext.cm_CaseChecklistResponseSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_CaseChecklistCatalog":
                        return svcContext.cm_CaseChecklistCatalogSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_CaseSubCategory":
                        return svcContext.cm_CaseSubCategorySet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "OpportunityClose":
                        return svcContext.OpportunityCloseSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_LeadClosureChecklistCatalog":
                        return svcContext.cm_LeadClosureChecklistCatalogSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_LeadClosureChecklistResponse":
                        return svcContext.cm_LeadClosureChecklistResponseSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_LeadClosureChecklistMaster":
                        return svcContext.cm_LeadClosureChecklistMasterSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_incident_team":
                        return svcContext.cm_Incident_TeamSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "cm_checklistmaster":
                        return svcContext.cm_ChecklistMasterSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "msfp_survey":
                        return svcContext.msfp_surveySet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "msfp_project":
                        return svcContext.msfp_projectSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        case "msfp_customervoiceprocessor":
                        return svcContext.msfp_customervoiceprocessorSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();
                        
                        case "cm_cmcaseresolution":
                        return svcContext.cm_cmcaseresolutionSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();
                        
                        case "ProcessStage":
                        return svcContext.ProcessStageSet.FirstOrDefault(record => record.Id == id)?.ToEntity<T>();

                        default:
                        throw new ArgumentException($"GetRecordById: Unsupported entity type: {typeof(T).Name}.\nPlease add all entities from the modelbuilder.");
                    }
                }
            } catch (Exception ex) {
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        public bool? HasProgramAssociation(Guid leadId) {
            using (var svcContext = new OrgContext(_service)) {
                bool result = svcContext.cm_ProgramAssociationSet
                    .Any(programAssociation => programAssociation.cm_Lead.Id == leadId);
                if (!result) {
                    return null;
                }
                return result;
            }
        }

        public List<cm_ProgramAssociation> GetAllProgramAssociationsByLead(Guid leadId) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.cm_ProgramAssociationSet
                .Where(programAssociation => programAssociation.cm_Lead.Id == leadId)
                .ToList();
            }
        }

        public Guid CreateAccountForLead(Lead leadRecord, Guid contactId) {
            // Fields were selected according to Lead To Account relationship mapping with some exceptions
            if (leadRecord == null || contactId == null) {
                throw new InvalidPluginExecutionException("Invalid Plugin Execution: Lead and Contact are required");
            }

            try {
                Account accountRecord = new Account {
                Id = Guid.NewGuid(),
                PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId),
                OriginatingLeadId = new EntityReference(Lead.EntityLogicalName, leadRecord.Id),
                CustomerTypeCode = account_customertypecode.Prospect,
                cm_ServiceProviderType = leadRecord.cm_ServiceType,
                cm_OtherType = leadRecord.cm_OtherType,
                cm_Role = leadRecord.cm_LeadType.HasValue
                    ? new List<cm_leadopptype> { leadRecord.cm_LeadType.Value }
                    : new List<cm_leadopptype>(),
                cm_Industry = leadRecord.cm_Industry,
                cm_SubIndustry = leadRecord.cm_SubIndustry,
                Name = leadRecord.CompanyName,
                WebSiteURL = leadRecord.WebSiteUrl,
                Address1_Line1 = leadRecord.Address1_Line1,
                Address1_Line2 = leadRecord.Address1_Line2,
                Address1_Line3 = leadRecord.Address1_Line3,
                Address1_City = leadRecord.Address1_City,
                Address1_County = leadRecord.Address1_County,
                Address1_Name = leadRecord.Address1_Name,
                Address1_Fax = leadRecord.Address1_Fax,
                Address1_Telephone1 = leadRecord.Address1_Telephone1,
                Address1_Telephone2 = leadRecord.Address1_Telephone2,
                Address1_Telephone3 = leadRecord.Address1_Telephone3,
                Fax = leadRecord.Fax,
                EMailAddress1 = leadRecord.EMailAddress1,
                Address1_PostalCode = leadRecord.Address1_PostalCode,
                Description = leadRecord.Description,
                FollowEmail = leadRecord.FollowEmail,
                IndustryCode = leadRecord.IndustryCode.HasValue
                    ? (account_industrycode)leadRecord.IndustryCode
                    : (account_industrycode?)null,
                TransactionCurrencyId = leadRecord.TransactionCurrencyId,
                DoNotBulkEMail = leadRecord.DoNotBulkEMail,
                DoNotEMail = leadRecord.DoNotEMail,
                DoNotFax = leadRecord.DoNotFax,
                DoNotPhone = leadRecord.DoNotPhone,
                DoNotPostalMail = leadRecord.DoNotPostalMail,
                DoNotSendMM = leadRecord.DoNotSendMM,
                SIC = leadRecord.SIC,
                YomiName = leadRecord.YomiCompanyName
                };

                if (leadRecord.cm_Country != null) {
                    accountRecord.cm_Country = leadRecord.cm_Country;
                    accountRecord.Address1_Country = leadRecord.cm_Country.ToString();
                }

                if (leadRecord.cm_StateProvince != null) {
                    accountRecord.cm_StateProvince = leadRecord.cm_StateProvince;
                    accountRecord.Address1_StateOrProvince = leadRecord.cm_StateProvince.ToString();
                }
            
                return _service.Create(accountRecord);
            } catch {
                throw;
            }
        }

        public Guid CreateContactForLead(Lead leadRecord) {
            // Fields were selected according to Lead To Contact relationship mapping with some exceptions
            Contact contactRecord = new Contact {
                Id = Guid.NewGuid(),
                FirstName = leadRecord.FirstName,
                MiddleName = leadRecord.MiddleName,
                LastName = leadRecord.LastName,
                JobTitle = leadRecord.JobTitle,
                OriginatingLeadId = new EntityReference(Lead.EntityLogicalName, leadRecord.Id),
                LeadSourceCode = contact_leadsourcecode.DefaultValue,
                Address1_Line2 = leadRecord.Address1_Line2,
                Address1_Line3 = leadRecord.Address1_Line3,
                Address1_City = leadRecord.Address1_City,
                Address1_Country = leadRecord.Address1_Country,
                Address1_County = leadRecord.Address1_County,
                Address1_Name = leadRecord.Address1_Name,
                Address1_StateOrProvince = leadRecord.Address1_StateOrProvince,
                Address1_Fax = leadRecord.Address1_Fax,
                Address2_Country = leadRecord.Address2_Country,
                Address2_County = leadRecord.Address2_County,
                Address2_Fax = leadRecord.Address2_Fax,
                Address2_Latitude = leadRecord.Address2_Latitude,
                MobilePhone = leadRecord.MobilePhone,
                Pager = leadRecord.Pager,
                PreferredContactMethodCode = leadRecord.PreferredContactMethodCode.HasValue
                    ? (contact_preferredcontactmethodcode)leadRecord.PreferredContactMethodCode.Value
                    : default,
                Salutation = leadRecord.Salutation,
                WebSiteUrl = leadRecord.WebSiteUrl,
                Address1_Telephone1 = leadRecord.Address1_Telephone1,
                Address1_Telephone2 = leadRecord.Address1_Telephone2,
                Address1_Telephone3 = leadRecord.Address1_Telephone3,
                EMailAddress1 = leadRecord.EMailAddress1,
                Address1_PostalCode = leadRecord.Address1_PostalCode,
                Description = leadRecord.Description,
                DoNotBulkEMail = leadRecord.DoNotBulkEMail,
                DoNotEMail = leadRecord.DoNotEMail,
                DoNotFax = leadRecord.DoNotFax,
                DoNotPhone = leadRecord.DoNotPhone,
                DoNotPostalMail = leadRecord.DoNotPostalMail,
                DoNotSendMM = leadRecord.DoNotSendMM,
                Fax = leadRecord.Fax,
                FollowEmail = leadRecord.FollowEmail,
                TransactionCurrencyId = leadRecord.TransactionCurrencyId,
                YomiFirstName = leadRecord.YomiFirstName,
                YomiLastName = leadRecord.YomiLastName,
                YomiMiddleName = leadRecord.YomiMiddleName
            };

            try {
                return _service.Create(contactRecord);
            } catch {
                throw;
            }
        }

        public Guid CreateOpportunityForLead(Lead leadRecord, cm_ProgramAssociation programAssociationRecord, Guid contactId, Guid accountId) {
            Opportunity opportunityRecord = new Opportunity {
                Id = Guid.NewGuid(),
                cm_AssociatedProgram = new EntityReference(cm_ProgramAssociation.EntityLogicalName, programAssociationRecord.Id),
                QualificationComments = leadRecord.QualificationComments,
                TransactionCurrencyId = leadRecord.TransactionCurrencyId,
                InitialCommunication = leadRecord.InitialCommunication,
                PriorityCode = opportunity_prioritycode.DefaultValue,
                PurchaseTimeframe = leadRecord.PurchaseTimeFrame,
                PurchaseProcess = leadRecord.PurchaseProcess,
                cm_OpportunityType = leadRecord.cm_LeadType,
                DecisionMaker = leadRecord.DecisionMaker,
                BudgetAmount = leadRecord.BudgetAmount,
                BudgetStatus = leadRecord.BudgetStatus,
                Description = leadRecord.Description,
                CustomerId = leadRecord.CustomerId,
                CampaignId = leadRecord.CampaignId,
                Name = leadRecord.cm_LeadID + " " + programAssociationRecord.cm_Name,
                OriginatingLeadId = new EntityReference(Lead.EntityLogicalName, leadRecord.Id),
                OpportunityRatingCode = leadRecord.LeadQualityCode.HasValue
                    ? (opportunity_opportunityratingcode)leadRecord.LeadQualityCode.Value
                    : default,
                Need = leadRecord.Need,
                ParentAccountId = new EntityReference(Account.EntityLogicalName, accountId),
                ParentContactId = new EntityReference(Contact.EntityLogicalName, contactId),
            };
            try {
                _tracingService.Trace($"Opportunity for Program Association Record: {programAssociationRecord}");
                return _service.Create(opportunityRecord);
            } catch {
                throw;
            }
        }

        public void SetParentCustomerToAccount(Guid contactId, Guid accountId) {
            Contact contact = new Contact() {
                Id = contactId,
                ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountId)
            };
            _service.Update(contact);
        }

        public void UpdateProgramAsscAccount(cm_ProgramAssociation programAssc, Guid accountId) {
            cm_ProgramAssociation programAssociation = new cm_ProgramAssociation() {
                Id = programAssc.Id,
                cm_Account = new EntityReference(Account.EntityLogicalName, accountId)
            };
            _service.Update(programAssociation);
        }

        internal List<cm_QuestionCatalog> GetQuestionsListByTeam(Guid teamId, cm_leadopptype? type) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.cm_QuestionCatalogSet.Where(
                record => record.cm_Program.Id == teamId
                && record.statuscode == cm_questioncatalog_statuscode.Active
                && record.cm_QuestionFor == type).ToList();
            }
        }

        internal List<cm_CaseChecklistCatalog> GetCaseChecklistCatalogCaseCat(Guid caseCatId) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_CaseChecklistCatalogSet.Where(
                        record => record.cm_CaseCategory != null
                            && record.cm_CaseCategory.Id == caseCatId
                            && record.statuscode == cm_casechecklistcatalog_statuscode.Active).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal List<cm_CaseChecklistCatalog> GetCaseChecklistCatalogCaseSub(Guid caseSubId) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_CaseChecklistCatalogSet.Where(
                        record => record.cm_CaseSubCategory != null
                            && record.cm_CaseSubCategory.Id == caseSubId
                            && record.statuscode == cm_casechecklistcatalog_statuscode.Active).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }
        internal cm_ChecklistMaster GetChecklistmaster(Guid caseSubId, Guid caseCatId, Guid program) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_ChecklistMasterSet.Where(
                        record => record.cm_PopulateSurveyInvite == true
                            && record.cm_CaseCategory != null
                            && record.cm_CaseCategory.Id == caseCatId
                            && record.cm_CaseSubCategory != null
                            && record.cm_CaseSubCategory.Id == caseSubId
                            && record.cm_Program != null
                            && record.cm_Program.Id == program
                            && record.statuscode == cm_checklistmaster_statuscode.Active).FirstOrDefault();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal msfp_project GetDefaultMSFPProject() {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.msfp_projectSet.Where(
                        record => record.statuscode == msfp_project_statuscode.Active).FirstOrDefault();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal void CreateQuestionResponses(List<cm_QuestionCatalog> questions, Opportunity opportunity) {
            List<Guid> responseGuids = new List<Guid>();

            questions.ForEach(question => {
                cm_QuestionResponse questionResponse = new cm_QuestionResponse() {
                    Id = Guid.NewGuid(),
                    //cm_ResponseID = question.cm_QuestionID,
                    cm_QuestionText = question.cm_QuestionText,
                    cm_AnswerType = question.cm_AnswerType,
                    cm_Province = question.cm_Province,
                    cm_Program = question.cm_Program,
                    cm_Opportunity = new EntityReference(Opportunity.EntityLogicalName, opportunity.Id),
                    cm_Account = opportunity.CustomerId,
                    cm_Question = new EntityReference(cm_QuestionCatalog.EntityLogicalName, question.Id),
                    cm_AnswerYesNo = null

                };
                Guid questionId = _service.Create(questionResponse);
                responseGuids.Add(questionId);
            });
            _tracingService.Trace("Question responses created: " + string.Join(", ", responseGuids));
        }

        internal cm_LeadClosureChecklistMaster GetLeadClosureCheckLMasterByTeam(Guid teamId, cm_leadopptype? type) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.cm_LeadClosureChecklistMasterSet.Where(
                record => record.cm_Program.Id == teamId
                && record.statuscode == cm_leadclosurechecklistmaster_statuscode.Active
                && record.cm_LeadType == type).FirstOrDefault();
            }
        }

        internal List<cm_LeadClosureChecklistCatalog> GetLeadClosureChecklistCatalogCat(Guid checklistMasterId) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_LeadClosureChecklistCatalogSet.Where(
                        record => record.cm_IsConditional == false
                            && record.cm_Active == true
                            && record.cm_LeadClosureChecklistMaster.Id == checklistMasterId
                            && record.statuscode == cm_leadclosurechecklistcatalog_statuscode.Active).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal List<cm_LeadClosureChecklistResponse> GetLeadClosureChecklistResponseByMaster(Guid checklistMasterId, Guid opportunityId) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_LeadClosureChecklistResponseSet.Where(
                        record => record.cm_Opportunity.Id == opportunityId).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal void CreateLeadClosureResponses(List<cm_LeadClosureChecklistCatalog> questions, Opportunity opportunity, Guid teamId) {
            List<Guid> responseGuids = new List<Guid>();

            questions.ForEach(question => {

                Enum.TryParse(question.cm_AnswerType.ToString(), out cm_answertype answerType);

                var questionResponse = new cm_LeadClosureChecklistResponse() {
                    Id = Guid.NewGuid(),
                    cm_Question = question.cm_QuestionText,
                    cm_AnswerType = answerType,
                    cm_Program = new EntityReference(Team.EntityLogicalName, teamId),
                    cm_Opportunity = new EntityReference(Opportunity.EntityLogicalName, opportunity.Id),
                    cm_Account = opportunity.CustomerId,
                    cm_LeadClosureChecklistQuestionLink = new EntityReference(cm_LeadClosureChecklistCatalog.EntityLogicalName, question.Id),
                    cm_LeadClosureChecklistMaster = question.cm_LeadClosureChecklistMaster,
                    cm_AnswerYesNo = null,
                    cm_RequiredToClose = question.cm_RequiredToClose == cm_leadclosurechecklistcatalog_cm_requiredtoclose.Yes,
                    cm_ExpectedAnswerToClose = question.cm_ExpectedAnswerToClose,
                    cm_ValidateClosureOnlyifOppQualificationStatus =
                        (cm_leadclosurechecklistresponse_cm_validateclosureonlyifoppqualificationstatus?)
                            ((int?)question.cm_ValidateClosureOnlyifOppQualificationStatus)
                    ,


                };
                Guid questionId = _service.Create(questionResponse);
                responseGuids.Add(questionId);
            });
            _tracingService.Trace("Lead Closure Question responses created: " + string.Join(", ", responseGuids));
        }

        internal List<Guid> CreateCasechecklistResponse(List<cm_CaseChecklistCatalog> questions, Incident incident) {
            List<Guid> responseGuids = new List<Guid>();
            try {
                questions.ForEach(question => {
                    cm_CaseChecklistResponse responseRecord = new cm_CaseChecklistResponse() {
                        cm_AnswerType = question.cm_AnswerType,
                        cm_Name = question.cm_Name,
                        cm_AnswerYesNo = null,
                        cm_ItemText = question.cm_Itemtext,
                        cm_Case = new EntityReference(Incident.EntityLogicalName, incident.Id),
                        cm_ItemLink = new EntityReference(Incident.EntityLogicalName, question.Id),
                    };
                    Guid responseId = _service.Create(responseRecord);
                    responseGuids.Add(responseId);
                });
                _tracingService.Trace("Question responses created: " + string.Join(", ", responseGuids));
                return responseGuids;
            } catch (Exception ex) {
                _tracingService.Trace($"CreateCasechecklistResponse Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal List<cm_CaseChecklistResponse> GetResponsesByIncident(Incident incidentRecord) {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    return svcContext.cm_CaseChecklistResponseSet.Where(
                        record => record.cm_Case != null
                            && record.cm_Case.Id == incidentRecord.Id).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetCaseChecklistCatalogByIncident Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        internal List<Team> GetTeamsByCaseProgramLeadType(cm_caseprogram caseProgram, cm_leadopptype incidentCustomerRole) {
            _tracingService.Trace($"Searching for team with caseProgram: {caseProgram} and cm_leadopptype (Account.cm_Role): {incidentCustomerRole}");
            try {
                using (var svcContext = new OrgContext(_service)) {

                    if (caseProgram == cm_caseprogram.CircularMaterialsGeneral) {
                        return svcContext.TeamSet.Where(
                            team => team.cm_CaseProgram.Value == cm_caseprogram.CircularMaterialsGeneral).ToList();
                    }

                    return svcContext.TeamSet.Where(
                        team => team.cm_CaseProgram == caseProgram && team.cm_LeadType == incidentCustomerRole).ToList();
                }
            } catch (Exception ex) {
                _tracingService.Trace($"GetTeamsByCaseProgramLeadType Error: {ex.Message}");
                throw;
            }
        }


        internal List<Team> AssociateIncidentToTeams(Incident incidentRecord) {
            try {
                Account incidentCustomer = GetRecordById<Account>(incidentRecord.CustomerId.Id);

                List<Team> teamList = new List<Team>();

                if (!incidentCustomer.cm_Role.Any()) {
                    var error = "No roles found in customer record";
                    _tracingService.Trace($"AssociateIncidentToTeams Error: {error}");
                    throw new Exception(error);
                }

                foreach (var caseProgram in incidentRecord.cm_CaseProgram) {
                    // incidentCustomer is expected to have only one role.
                    _tracingService.Trace($"Associating Incident {incidentRecord.Id} to caseProgram: {caseProgram}");
                    teamList.AddRange(GetTeamsByCaseProgramLeadType(caseProgram, incidentCustomer.cm_Role.FirstOrDefault()));
                }

                if (!teamList.Any()) {
                    throw new InvalidPluginExecutionException("No Teams were found. Please check if the 'Customer role' and 'Case program' match to a Team");
                }

                // Disassociate the records before associating to make sure it doesn't cause a "Cannot insert duplicate key" error.
                _tracingService.Trace($"incidentRecordincidentRecord.cm_ServiceandSupportTeam: {incidentRecord.cm_ServiceandSupportTeam}");

                if (incidentRecord.cm_ServiceandSupportTeam != null) {
                    _tracingService.Trace($"Disassociate");
                    _service.Disassociate(Incident.EntityLogicalName,
                        incidentRecord.Id,
                        new Relationship(cm_Incident_Team.Fields.cm_Incident_Team_Team),
                        CreateEntityReferenceCollection(teamList));
                };

                _tracingService.Trace($"Associate");

                _service.Associate(Incident.EntityLogicalName,
                    incidentRecord.Id,
                    new Relationship(cm_Incident_Team.Fields.cm_Incident_Team_Team),
                    CreateEntityReferenceCollection(teamList));

                _tracingService.Trace($"Complete");
                
                return teamList;
            } catch (Exception ex) {
                _tracingService.Trace($"AssociateIncidentToTeams Error: {ex.Message}");
                throw;
            }

        }

        private EntityReferenceCollection CreateEntityReferenceCollection(IEnumerable<Entity> recordList) {
            var referenceCollection = new EntityReferenceCollection();

            foreach (var record in recordList) {
                referenceCollection.Add(new EntityReference(record.LogicalName, record.Id));
            }

            return referenceCollection;
        }

        /// <summary>
        ///     Method takes the parent case/incident and uses cm_CaseProgram to create child cases.
        ///     If there's one of fewer cm_CaseProgram only the parent case will be returned in the list.
        /// </summary>
        /// <param name="parentIncidentRecord"></param>
        /// <returns>List of Cases/Incidents</returns>
        internal List<Incident> CreateChildCaseOrDefault(Incident parentIncidentRecord) {
            var caseList = new List<Incident>();
            if (parentIncidentRecord.cm_CaseProgram.Count() > 1) {
                _tracingService.Trace("Creating child cases");
                try {
                    // Creates a child case for each Case Program in the parent
                    foreach (var caseProgram in parentIncidentRecord.cm_CaseProgram) {
                        Account incidentCustomer = GetRecordById<Account>(parentIncidentRecord.CustomerId.Id) ??
                            throw new InvalidPluginExecutionException("incidentCustomer cannot be null");
                        Team team = GetTeamsByCaseProgramLeadType(caseProgram, incidentCustomer.cm_Role.FirstOrDefault()).FirstOrDefault() ??
                            throw new InvalidPluginExecutionException($"Case Program \"{caseProgram}\" and Lead Type \"{incidentCustomer.cm_Role.FirstOrDefault()}\" do not return a matching Team record.");

                        _tracingService.Trace($"Processing child case from accountid: {incidentCustomer.Id} and teamid: {team.Id}");
                        var caseRecord = new Incident() {
                            Id = Guid.NewGuid(),
                            cm_CasePriority = parentIncidentRecord.cm_CasePriority,
                            cm_CaseProgram = new List<cm_caseprogram> { caseProgram },
                            cm_CauseCategory = parentIncidentRecord.cm_CauseCategory,
                            cm_Contract = parentIncidentRecord.cm_Contract,
                            cm_IncidentCategory = parentIncidentRecord.cm_IncidentCategory,
                            cm_Program = new EntityReference(Team.EntityLogicalName, team.Id),
                            CustomerId = parentIncidentRecord.CustomerId,
                            Description = parentIncidentRecord.Description,
                            Title = parentIncidentRecord.Title + " " + caseProgram,
                            PrimaryContactId = parentIncidentRecord.PrimaryContactId,
                            ParentCaseId = new EntityReference(Incident.EntityLogicalName, parentIncidentRecord.Id),
                            cm_ComplianceFlagSetonAccount = parentIncidentRecord.cm_ComplianceFlagSetonAccount,
                            OwnerId = parentIncidentRecord.OwnerId,
                            cm_ReportedBy = parentIncidentRecord.cm_ReportedBy,
                            cm_ReportedOn = parentIncidentRecord.cm_ReportedOn,
                            cm_Channel = parentIncidentRecord.cm_Channel,
                            cm_OtherChannel = parentIncidentRecord.cm_OtherChannel,
                            cm_ReportedById = parentIncidentRecord.cm_ReportedById,
                            CaseTypeCode = parentIncidentRecord.CaseTypeCode,
                        };

                        if (incidentCustomer.cm_Role.Contains(cm_leadopptype.ServiceProvider)) {
                            caseRecord.cm_EffectiveDate = parentIncidentRecord.cm_EffectiveDate;
                            caseRecord.cm_To = parentIncidentRecord.cm_To;
                            caseRecord.cm_From = parentIncidentRecord.cm_From;
                        }

                        _service.Create(caseRecord);
                        caseList.Add(caseRecord);

                        AssociateIncidentToTeams(caseRecord);
                    }
                } catch (Exception ex) {
                    _tracingService.Trace($"CreateChildCaseOrDefault Error: {ex.Message}");
                    throw;
                }
            } else {
                _tracingService.Trace("Only one case program found. No child cases were created");
            }

            // populate Incident.cm_Program even if there's only one child 

            if (caseList.Count() > 0) {
                _tracingService.Trace(string.Join(", ", caseList.Select(c => $"Cases created: {c.Id}")));
            } else {
                caseList.Add(parentIncidentRecord);
            }

            return caseList;
        }

        internal Guid CreateInvite(msfp_customervoiceprocessor invite) {
            try {
                return _service.Create(invite);
            } catch (Exception ex) {
                _tracingService.Trace($"CreateInvite Error: {ex.Message}");
                throw;
            }
        }

        internal List<Incident> GetChildrenIncidents(Incident parentIncident) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.IncidentSet
                .Where(incident => incident.ParentCaseId.Id == parentIncident.Id)
                .ToList();
            }
        }

        internal List<msfp_surveyinvite> GetSurveyInviteByIncident(Incident incident) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.msfp_surveyinviteSet
                .Where(invite => invite.RegardingObjectId.Id == incident.Id)
                .ToList();
            }
        }

        internal void ResolveChildCases(List<Incident> incidentList) {
            foreach (var incident in incidentList) {
                var incidentToUpdate = new Incident() {
                    Id = incident.Id,
                    StateCode = incident_statecode.Resolved,
                    // Description is being used as a flag to avoid recursion
                    Description = incident_statecode.Resolved.ToString(),
                };
                _service.Update(incidentToUpdate);
            }
        }

        internal cm_cmcaseresolution GetBPFRecordByIncident(Incident incident) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.cm_cmcaseresolutionSet
                .Where(bpfRecord => bpfRecord.bpf_incidentid.Id == incident.Id)
                .FirstOrDefault();
            }
        }

        /// <summary>
        /// Moves the Business Process Flow (BPF) stage of the given incident from "Identify" to "Research",
        /// if the current active stage is "Identify".
        /// </summary>
        /// <param name="incident">The incident entity for which the BPF stage transition should be evaluated.</param>
        /// <exception cref="InvalidPluginExecutionException">
        /// Thrown when no BPF record is found for the incident, indicating an invalid plugin execution context.
        /// </exception>
        internal void MoveIncidentBpfStage(Incident incident) {
            cm_cmcaseresolution bpfRecord = GetBPFRecordByIncident(incident) ??
                throw new InvalidPluginExecutionException("Invalid plugin execution: cm_cmcaseresolution record (BPF) can't be null");

            List<(string StageName, Guid ProcessStageId)> processes = GetProcessStagesById(bpfRecord.ProcessId.Id);
            _tracingService.Trace("Retrieved {0} process stages for Process ID: {1}", processes.Count, bpfRecord.ProcessId.Id);

            Guid identifyStageId = processes.FirstOrDefault(p => p.StageName == "Identify").ProcessStageId;
            Guid researchStageId = processes.FirstOrDefault(p => p.StageName == "Research").ProcessStageId;
            Guid resolveStageId = processes.FirstOrDefault(p => p.StageName == "Resolve").ProcessStageId;

            _tracingService.Trace("Stage IDs resolved: Identify={0}, Research={1}, Resolve={2}", identifyStageId, researchStageId, resolveStageId);
            _tracingService.Trace("Current ActiveStageId of BPF: {0}", bpfRecord.ActiveStageId?.Id);

            if (bpfRecord.ActiveStageId.Id == identifyStageId) {
                _tracingService.Trace("Active stage is 'Identify'. Moving to 'Research' stage.");

                cm_cmcaseresolution bpfRecordToUpdate = new cm_cmcaseresolution() {
                    Id = bpfRecord.Id,
                    ActiveStageId = new EntityReference(ProcessStage.EntityLogicalName, researchStageId),
                };
                _service.Update(bpfRecordToUpdate);
                _tracingService.Trace("Updated BPF record {0} to stage 'Research' (ID: {1})", bpfRecord.Id, researchStageId);
            }
        }
        /// <summary>
        /// Retrieves a list of process stages associated with the specified business process flow (BPF) process ID.
        /// </summary>
        /// <param name="processId">The unique identifier (GUID) of the BPF process.</param>
        /// <returns>
        /// A list of tuples, where each tuple contains:
        /// - The stage name (string)
        /// - The process stage ID (Guid; returns Guid.Empty if null)
        /// </returns>
        internal List<(string, Guid)> GetProcessStagesById(Guid processId) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.ProcessStageSet
                    .Where(stage => stage.ProcessId.Id == processId)
                    .ToList()
                    .Select(stage => (
                        stage.StageName,
                        stage.ProcessStageId ?? Guid.Empty
                    ))
                    .ToList();
            }
        }
        internal cm_Incident_Team GetAssociatedTeam(Incident incident) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.cm_Incident_TeamSet
                    .Where(incidentTeamRel => incidentTeamRel.incidentid == incident.Id)
                    .FirstOrDefault();
            }
        }

        internal (Guid, Guid) HandleLeadContactAndAccount(Lead leadRecord) {
            Guid contactId, accountId;

            // Each Lead must have an account/customer and a contact records associated to it.
            // cm_ExistingContact and cm_ExistingCustomer should tell if they should be created or used existing records

            if (leadRecord.cm_ExistingContact == true) {
                contactId = leadRecord.ParentContactId.Id;
            } else if (leadRecord.cm_ExistingContact == false) {
                contactId = CreateContactForLead(leadRecord);
            } else {
                throw new InvalidPluginExecutionException("leadRecord.cm_ExistingContact can't be null");
            }

            if (leadRecord.cm_ExistingCustomer == true) {
                accountId = leadRecord.ParentAccountId.Id;
            } else if (leadRecord.cm_ExistingCustomer == false) {
                accountId = CreateAccountForLead(leadRecord, contactId);
            } else {
                throw new InvalidPluginExecutionException("leadRecord.cm_ExistingCustomer can't be null");
            }

            SetParentCustomerToAccount(contactId, accountId);

            return (contactId, accountId);
        }

        /// <summary>
        ///     Merges a subordinate record into a target record (e.g., contact or account).
        ///     The target fields won't be replaced by any of the subordinate field values, unless specified in the updateFields.
        ///     Once the merge is complete, the subordinate field "masterid" will be updated with a lookup to the targetEntity, and status set to inactive.
        /// </summary>
        /// <param name="targetEntity">The target (master) entity into which the subordinate will be merged.</param>
        /// <param name="subordinateEntity">The duplicate entity that will be deactivated and merged into the target.</param>
        /// <param name="updateFields">
        /// Optional. A dictionary of fields to update on the target entity during the merge.
        /// Only specified fields will overwrite values on the target.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if either entity is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the entities are not of the same logical type.</exception>
        /// <example>
        /// Example: Merge two contact records and update the job title:
        /// <code>
        /// var target = new Entity("contact") { Id = targetId };
        /// var subordinate = new Entity("contact") { Id = subordinateId };
        /// var updates = new Dictionary&lt;string, object&gt; {
        ///     { "jobtitle", "Merged Contact" }
        /// };
        /// Merge(target, subordinate, updates);
        /// </code>
        /// </example>
        /// <example>
        /// Example: Merge two accounts without updating any fields:
        /// <code>
        /// var target = new Entity("account") { Id = targetAccountId };
        /// var subordinate = new Entity("account") { Id = subordinateAccountId };
        /// Merge(target, subordinate);
        /// </code>
        /// </example>
        internal void Merge(Entity targetEntity, Entity subordinateEntity, Dictionary<string, object> updateFields = null) {
            if (targetEntity == null) throw new ArgumentNullException(nameof(targetEntity));
            if (subordinateEntity == null) throw new ArgumentNullException(nameof(subordinateEntity));
            if (targetEntity.LogicalName != subordinateEntity.LogicalName)
                throw new InvalidOperationException("Entities must have the same logical name to be merged.");

            string entityLogicalName = targetEntity.LogicalName;

            Entity updateContent = null;
            if (updateFields != null && updateFields.Count > 0) {
                updateContent = new Entity(entityLogicalName);
                foreach (var field in updateFields) {
                    updateContent[field.Key] = field.Value;
                }
            }

            var mergeRequest = new MergeRequest {
                Target = new EntityReference(entityLogicalName, targetEntity.Id),
                SubordinateId = subordinateEntity.Id,
                PerformParentingChecks = false,
                UpdateContent = updateContent
            };

            _service.Execute(mergeRequest);
        }

        internal Account GetAccountByAccountNumber(string accountNumber) {
            using (var svcContext = new OrgContext(_service)) {
                return svcContext.AccountSet
                    .Where(account => account.AccountNumber == accountNumber)
                    .FirstOrDefault();
            }
        }
    }
}
