if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.Lead = (function () {
    "use strict";

    // This global variable must be set in any entry point to this file. E.g. OnSave, OnLoad, OnChange
    let _formContext = null; // Global within CM.Lead scope

    const Constants = Object.freeze({
        entityName: "lead",
        required: "required",
        notRequired: "none",
    });

    const Helpers = {
        /**
         * @description Use CM.Lead.onLoad to call it from the form on load event
         * @param executionContext 
         */
        onLoad: async (executionContext) => {
            _formContext = executionContext.getFormContext(); // initialize once

            Helpers.setDefaultCountryOnLoad();
            Helpers.toggleServiceTypeFieldOnLoad(executionContext);
            Helpers.handleContactAndCompanyFieldsOnload();

            _formContext.getAttribute("cm_existingcontact")?.addOnChange((ctx) =>
                Helpers.onExistingContactChange(ctx)
            );
            
            _formContext.getAttribute("cm_existingcustomer")?.addOnChange((ctx) =>
                Helpers.onExistingCompanyChange(ctx)
            );

            _formContext.getAttribute("cm_leadtype")?.addOnChange((ctx) =>
                Helpers.toggleServiceTypeFieldOnLoad(ctx)
            );

            _formContext.getAttribute("parentaccountid")?.addOnChange((ctx) =>
                Helpers.removeContactWhenCustomerChanges(ctx)
            );
        },

        handleContactAndCompanyFieldsOnload: () => {
            const isExistingCustomer = _formContext.getAttribute("cm_existingcustomer").getValue();
            const isExistingContact = _formContext.getAttribute("cm_existingcontact").getValue();

            Helpers.handleIsExistingCustomer(isExistingCustomer);
            Helpers.handleIsExistingContact(isExistingContact);
        },

        onExistingContactChange: (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();
            const isExistingContact = executionContext.getEventSource().getValue();
            Helpers.handleIsExistingContact(isExistingContact);
        },

        onExistingCompanyChange: (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();
            const isExistingCompany = executionContext.getEventSource().getValue();
            Helpers.handleIsExistingCustomer(isExistingCompany);
        },

        handleIsExistingContact: (isExistingContact) => {
            const newContactFields = [
                "fullname_compositionLinkControl_firstname","firstname",
                "fullname_compositionLinkControl_lastname","lastname",
                "jobtitle", "telephone1", "mobilephone", "emailaddress1"
            ];
            const newContactRequiredFields = [
                "fullname_compositionLinkControl_firstname","firstname",
                "fullname_compositionLinkControl_lastname","lastname",
            ];

            if (isExistingContact === true) {
                Helpers.toggleFieldsVisibility(true, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "parentcontactid");

                Helpers.toggleFieldsVisibility(false, ...newContactFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newContactFields);

                Helpers.clearFieldsValues(...newContactFields);
            } else if (isExistingContact === false) {
                Helpers.toggleFieldsVisibility(true, ...newContactFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, ...newContactRequiredFields);

                Helpers.toggleFieldsVisibility(false, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "parentcontactid");

                Helpers.clearFieldsValues("parentcontactid");
            } else {
                throw new Error("Invalid value for isExistingContact");
            }
        },

        handleIsExistingCustomer: (isExistingCustomer) => {
            const newCompanyFields = [
                "companyname", "websiteurl", "address1_line1", "address1_line2",
                "address1_line3", "address1_city", "cm_country", "cm_stateprovince", "address1_postalcode"
            ];
            const newCompanyRequiredFields = ["parentaccountid"]; //Customer

            if (isExistingCustomer === true) {
                
                Helpers.toggleFieldsVisibility(true, ...newCompanyRequiredFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, ...newCompanyRequiredFields);
                
                Helpers.toggleFieldsVisibility(false, ...newCompanyFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newCompanyFields);

                Helpers.clearFieldsValues(...newCompanyFields);
            } else if (isExistingCustomer === false) {
                
                Helpers.toggleFieldsVisibility(true, ...newCompanyFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, "companyname");
                
                Helpers.toggleFieldsVisibility(false, ...newCompanyRequiredFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newCompanyRequiredFields);
                
                Helpers.clearFieldsValues(...newCompanyRequiredFields);
            } else {
                throw new Error("Invalid value for isExistingCustomer");
            }
        },
        removeContactWhenCustomerChanges: () => {
            // Remove the values from contact lookup when the account/customer changes
            Helpers.clearFieldsValues("parentcontactid");
        },

        clearFieldsValues: (...attributes) => {
            attributes.forEach(attr => {
                const attribute = _formContext.getAttribute(attr);
                if (!attribute) return;

                const type = attribute.getAttributeType();

                // Only set to false for booleans, null for all other known types
                const clearValue = type === "boolean" ? false : null;
                attribute.setValue(clearValue);
                attribute.setSubmitMode("always");
            });
        },

        toggleFieldsVisibility: (setVisibleTo, ...attributes) => {
            attributes.forEach(attr => {
                const control = _formContext.getControl(attr);
                control && control.setVisible(setVisibleTo);
            });
        },

        toggleFieldsRequirementLevel: (requiredLevel, ...attributes) => {
            attributes.forEach(attr => {
                const attribute = _formContext.getAttribute(attr);
                attribute && attribute.setRequiredLevel(requiredLevel); // "none", "required", or "recommended"
                }
            );
        },

        toggleServiceTypeFieldOnLoad: (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();

            const leadType = _formContext.getAttribute("cm_leadtype").getValue();

            // 121540001 = Service Provider
            // 121540003 = Other
            if (leadType === 121540001) {
                _formContext.getControl("cm_servicetype").setVisible(true);
                _formContext.getAttribute("cm_servicetype").setRequiredLevel("required");

                _formContext.getControl("cm_othertype").setVisible(false);
                _formContext.getAttribute("cm_othertype").setRequiredLevel("none");
                _formContext.getAttribute("cm_othertype").setValue(null);
            }
            else if (leadType === 121540003) {
                _formContext.getControl("cm_othertype").setVisible(true);
                _formContext.getAttribute("cm_othertype").setRequiredLevel("required");

                _formContext.getControl("cm_servicetype").setVisible(false);
                _formContext.getAttribute("cm_servicetype").setRequiredLevel("none");
                _formContext.getAttribute("cm_servicetype").setValue(null);
            }
            else {
                _formContext.getControl("cm_servicetype").setVisible(false);
                _formContext.getAttribute("cm_servicetype").setRequiredLevel("none");
                _formContext.getAttribute("cm_servicetype").setValue(null);

                _formContext.getControl("cm_othertype").setVisible(false);
                _formContext.getAttribute("cm_othertype").setRequiredLevel("none");
                _formContext.getAttribute("cm_othertype").setValue(null);
            }
        },

        setDefaultCountryOnLoad: () => {
            const countryField = _formContext.getAttribute("cm_country");

            if (!countryField.getValue()) {
                const fetchXml = `
                    <fetch top='1'>
                      <entity name='cm_country'>
                        <attribute name='cm_countryname'/>
                        <attribute name='cm_countrycode'/>
                        <attribute name='cm_countryid'/>
                        <filter>
                          <condition attribute='cm_countrycode' operator='eq' value='CA' />
                        </filter>
                      </entity>
                    </fetch>`;

                Xrm.WebApi.retrieveMultipleRecords("cm_country", "?fetchXml=" + encodeURIComponent(fetchXml))
                    .then((result) => {
                        if (result.entities.length > 0) {
                            const country = result.entities[0];
                            const lookupValue = [{
                                id: country["cm_countryid"],
                                name: country["cm_countryname"],
                                entityType: "cm_country"
                            }];
                            countryField.setValue(lookupValue);
                            countryField.setSubmitMode("always");
                        }
                    })
                    .catch((error) => {
                        console.error("Error retrieving country with code 'CA': ", error.message);
                    });
            }
        }
    };

    return {
        onLoad: Helpers.onLoad
    };
}());