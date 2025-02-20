using CM_CrmVSSolution.Common.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    public class CommonBusinessLogic {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracingService;

        public CommonBusinessLogic(IOrganizationService service, ITracingService tracingService) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        }
        public Entity GetRecordById(Guid id, string entityLogicalName) {
            var svcContext = new OrgContext(_service);

            switch (entityLogicalName) {
                case "account":
                    return svcContext.AccountSet.FirstOrDefault(record => record.Id == id);

                case "contact":
                    return svcContext.ContactSet.FirstOrDefault(record => record.Id == id);

                case "cm_province":
                    return svcContext.cm_ProvinceSet.FirstOrDefault(record => record.Id == id);

                case "cm_programassociation":
                    return svcContext.cm_ProgramAssociationSet.FirstOrDefault(record => record.Id == id);

                case "salesorder":
                    return svcContext.SalesOrderSet.FirstOrDefault(record => record.Id == id);

                case "connection":
                    return svcContext.ConnectionSet.FirstOrDefault(record => record.Id == id);

                case "lead":
                    return svcContext.LeadSet.FirstOrDefault(record => record.Id == id);

                case "opportinuty":
                    return svcContext.OpportunitySet.FirstOrDefault(record => record.Id == id);

                case "team":
                    return svcContext.TeamSet.FirstOrDefault(record => record.Id == id);

                default:
                    throw new ArgumentException($"Unsupported entity type: {entityLogicalName}");
            }
        }
    }
}
