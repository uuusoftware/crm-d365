function showHideBasedOnAccount(executionContext) {
    var formContext = executionContext.getFormContext();
    var customerId = formContext.getAttribute("customerid");
    var customerIdValues = customerId.getValue();

    if (customerIdValues != null) {
        var customerGuid = customerIdValues[0].id;
        //alert(customerGuid);
        var acctfetchXml = "<fetch mapping='logical' version='1.0' output-format='xml-platform' distinct='false'>" +
            "<entity name='account'>" +
            "<attribute name='cm_role'/>" +
            "<attribute name='accountid'/>" +
            "<attribute name='statecode'/>" +
            "<filter type='and'>" +
            "<condition attribute='statecode' operator='eq' value='0'/>" +
            "<condition attribute='accountid' operator='eq' value='" + customerGuid + "'/>" +
            "</filter>" +
            "</entity>" +
            "</fetch>";

        Xrm.WebApi.retrieveMultipleRecords("account", "?fetchXml= " + encodeURIComponent(acctfetchXml)).then
            (function (result) {
                var accountRoleValue = result.entities[0].cm_role;
                var secObj = formContext.ui.tabs.get("general").sections.get("general_section_sp");

                //alert(accountRoleValue);
                if (accountRoleValue === '121540001') {
                    secObj.setVisible(true);
                }
                else {
                    secObj.setVisible(false);
                }
            },
                function (error) {
                    //console.log(error.message);
                }
            );
    }
    var parentCaseId = formContext.getAttribute("parentcaseid").getValue();

    if (parentCaseId != null) {
        formContext.getAttribute("cm_caseprogram").setRequiredLevel("none");
        formContext.getControl("cm_caseprogram").setVisible(false);
    }
}

function populateAccountNumber(executionContext) {
    var formContext = executionContext.getFormContext();
    var customerLookup = formContext.getAttribute("customerid");

    if (customerLookup && customerLookup.getValue() != null) {
        var customer = customerLookup.getValue()[0];

        if (customer.entityType === "account") {
            var accountId = customer.id.replace("{", "").replace("}", "");

            Xrm.WebApi.retrieveRecord("account", accountId, "?$select=accountnumber").then(
                function success(result) {
                    if (result.accountnumber) {
                        formContext.getAttribute("cm_accountnumbertext").setValue(result.accountnumber);
                    } else {
                        formContext.getAttribute("cm_accountnumbertext").setValue(null); // Clear if no account number
                    }
                },
                function (error) {
                    console.error("Error retrieving Account: " + error.message);
                }
            );
        }
        else {
            // If the customer is a contact, clear the field
            formContext.getAttribute("cm_accountnumbertext").setValue(null);
        }
    } else {
        // No customer selected
        formContext.getAttribute("cm_accountnumbertext").setValue(null);
    }
    var createdOn = formContext.getAttribute("createdon").getValue();
    //alert(createdOn);
    var reportedOn = formContext.getAttribute("cm_reportedon");
    var reportedOnValue = reportedOn.getValue();
    
    if (createdOn === null && reportedOnValue === null) {
        var currentDate = new Date();
        //alert(currentDate);
        reportedOn.setValue(currentDate);
    }
}

function setCaseTypeBasedOnAccountRole(executionContext) {
    var formContext = executionContext.getFormContext();
    var customerLookup = formContext.getAttribute("customerid");

    if (!customerLookup || !customerLookup.getValue()) return;

    var accountId = customerLookup.getValue()[0].id.replace("{", "").replace("}", "");

    Xrm.WebApi.retrieveRecord("account", accountId, "?$select=cm_role").then(
        function success(result) {
            var role = result["cm_role"];
            var caseTypeAttr = formContext.getAttribute("cm_cmcasetype");

            if (!caseTypeAttr) return;

            var roleValue = parseInt(role); // Ensure we are comparing numbers
            var secObj = formContext.ui.tabs.get("general").sections.get("general_section_resident");

            if (roleValue === 121540002) {

                secObj.setVisible(true);
                // Set cm_communityname as required
                var communityField = formContext.getAttribute("cm_communityname");
                if (communityField) {
                    communityField.setRequiredLevel("required");
                }

                // Set cm_cmlanguage as required
                var languageField = formContext.getAttribute("cm_cmlanguage");
                if (languageField) {
                    languageField.setRequiredLevel("required");
                }
            }

            else if (roleValue != 121540002) {

                secObj.setVisible(false);
                // Set cm_communityname as not required
                var communityField = formContext.getAttribute("cm_communityname");
                if (communityField) {
                    communityField.setRequiredLevel("none");
                }

                // Set cm_cmlanguage as required
                var languageField = formContext.getAttribute("cm_cmlanguage");
                if (languageField) {
                    languageField.setRequiredLevel("none");
                }
            }


            switch (roleValue) {
                case 121540000: // Producer
                    caseTypeAttr.setValue(121540000);
                    break;
                case 121540001: // Service Provider
                    caseTypeAttr.setValue(121540001);
                    break;
                case 121540002: // Resident
                    caseTypeAttr.setValue(121540002);
                    break;
                default:
                    caseTypeAttr.setValue(121540003); // Other
            }
        },
        function (error) {
            console.error("Error retrieving account role:", error.message);
        }
    );
}

function filterPrimaryContact(executionContext) {
  const form = executionContext.getFormContext();
  const accAttr = form.getAttribute("customerid") || form.getAttribute("accountid");
  const ctrl = form.getControl("primarycontactid");

  if (!ctrl || !accAttr) return;

  ctrl.addPreSearch(function() {
    const acc = accAttr.getValue();

    if (!acc || !acc.length) return;

    const id = acc[0].id.replace(/[{}]/g, "");
    const fetch = `
      <filter type="and">
        <condition attribute="parentcustomerid" operator="eq" value="${id}" />
      </filter>`;

    ctrl.addCustomFilter(fetch, "contact");
  });
}