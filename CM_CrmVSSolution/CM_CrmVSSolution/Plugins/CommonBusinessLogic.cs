using Plugins.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugins {

    public class CommonBusinessLogic {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracingService;

        public CommonBusinessLogic(IOrganizationService service, ITracingService tracingService) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        }
        public T GetRecordById<T>(Guid id, string entityLogicalName) where T : Entity {
            try {
                using (var svcContext = new OrgContext(_service)) {
                    switch (entityLogicalName) {
                        case Account.EntityLogicalName:
                            return svcContext.AccountSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Contact.EntityLogicalName:
                            return svcContext.ContactSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_Province.EntityLogicalName:
                            return svcContext.cm_ProvinceSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_ProgramAssociation.EntityLogicalName:
                            return svcContext.cm_ProgramAssociationSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_QuestionCatalog.EntityLogicalName:
                            return svcContext.cm_QuestionCatalogSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_QuestionResponse.EntityLogicalName:
                            return svcContext.cm_QuestionResponseSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case SalesOrder.EntityLogicalName:
                            return svcContext.SalesOrderSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Connection.EntityLogicalName:
                            return svcContext.ConnectionSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Lead.EntityLogicalName:
                            return svcContext.LeadSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Opportunity.EntityLogicalName:
                            return svcContext.OpportunitySet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Team.EntityLogicalName:
                            return svcContext.TeamSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case Incident.EntityLogicalName:
                            return svcContext.IncidentSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_CaseCategory.EntityLogicalName:
                            return svcContext.cm_CaseCategorySet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_CaseChecklistResponse.EntityLogicalName:
                            return svcContext.cm_CaseChecklistResponseSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_CaseChecklistCatalog.EntityLogicalName:
                            return svcContext.cm_CaseChecklistCatalogSet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        case cm_CaseSubCategory.EntityLogicalName:
                            return svcContext.cm_CaseSubCategorySet.FirstOrDefault(record => record.Id == id).ToEntity<T>();

                        default:
                            throw new ArgumentException($"Unsupported entity type on CommonBusinessLogic.GetRecordById: {entityLogicalName}");
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

            Account accountRecord = new Account {
                Id = Guid.NewGuid(),
                PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId),
                OriginatingLeadId = new EntityReference(Lead.EntityLogicalName, leadRecord.Id),
                CustomerTypeCode = account_customertypecode.Prospect,
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
                Address1_Country = leadRecord.Address1_Country,
                Address1_County = leadRecord.Address1_County,
                Address1_Name = leadRecord.Address1_Name,
                Address1_StateOrProvince = leadRecord.Address1_StateOrProvince,
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
            try {
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

        public void SetParentCustomer(Guid contactId, Guid accountId) {
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
    }
}
