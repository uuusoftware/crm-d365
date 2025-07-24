if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.Lead = (function () {
    "use strict";

    // This global variable must be set in any entry point to this file. E.g. OnSave, OnLoad, OnChange
    let formContext = null; // Global within CM.Lead scope

    const Constants = Object.freeze({
        entityName: "lead",
        required: "required",
        notRequired: "none",        
    });

    const Helpers = {
        onLoad: async (executionContext) => {
            formContext = executionContext.getFormContext(); // initialize once

            Helpers.setDefaultCountryOnLoad();
            Helpers.toggleServiceTypeFieldOnLoad();
            Helpers.handleContactAndCompanyFieldsOnload();

            formContext.getAttribute("cm_existingcontact")?.addOnChange((ctx) =>
                Helpers.onExistingContactChange(ctx)
            );
            formContext.getAttribute("cm_existingcustomer")?.addOnChange((ctx) =>
                Helpers.onExistingCompanyChange(ctx)
            );
        },

        handleContactAndCompanyFieldsOnload: () => {
            const contactFormAttribute = ["fullname_compositionLinkControl_firstname", "fullname_compositionLinkControl_lastname", "jobtitle", "telephone1", "mobilephone", "emailaddress1"];
            const companyFormAttribute = ["companyname", "websiteurl", "address1_line1", "address1_line2", 
                "address1_line3", "address1_city", "cm_country", "cm_stateprovince", "address1_postalcode"];

            Helpers.toggleFieldsVisibility(false, ...contactFormAttribute, ...companyFormAttribute);
            Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...contactFormAttribute, ...companyFormAttribute);

            Helpers.toggleFieldsRequirementLevel(Constants.required, "parentcontactid");
            Helpers.toggleFieldsVisibility(true, "parentcontactid");

            Helpers.toggleFieldsRequirementLevel(Constants.required, "parentaccountid");
            Helpers.toggleFieldsVisibility(true, "parentaccountid");

        },

        onExistingContactChange: (executionContext) => {
            formContext = executionContext.getFormContext();

            const attribute = executionContext.getEventSource();
            const isExistingContact = attribute.getValue();

            if (isExistingContact === true) {
                Helpers.toggleFieldsVisibility(true, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "parentcontactid");

                Helpers.toggleFieldsVisibility(false, "fullname_compositionLinkControl_firstname", "fullname_compositionLinkControl_lastname", "jobtitle", "telephone1", "mobilephone", "emailaddress1");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "fullname_compositionLinkControl_firstname", "fullname_compositionLinkControl_lastname",);

            } else if (isExistingContact === false) {
                Helpers.toggleFieldsVisibility(true, "fullname_compositionLinkControl_firstname", "fullname_compositionLinkControl_lastname", "jobtitle", "telephone1", "mobilephone", "emailaddress1");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "fullname_compositionLinkControl_firstname", "fullname_compositionLinkControl_lastname",);

                Helpers.toggleFieldsVisibility(false, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "parentcontactid");
            } else {
                throw new Error("Invalid value for isExistingContact");
            }
        },

        onExistingCompanyChange: (executionContext) => {
            formContext = executionContext.getFormContext();

            const attribute = executionContext.getEventSource();
            const isExistingCompany = attribute.getValue();

            if (isExistingCompany === true) {
                Helpers.toggleFieldsVisibility(true, "parentaccountid");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "parentaccountid");

                Helpers.toggleFieldsVisibility(false, "companyname", "websiteurl", "address1_line1", "address1_line2", 
                    "address1_line3", "address1_city", "cm_country", "cm_stateprovince", "address1_postalcode");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "companyname");

            } else if (isExistingCompany === false) {
                Helpers.toggleFieldsVisibility(true, "companyname", "websiteurl", "address1_line1", "address1_line2", 
                    "address1_line3", "address1_city", "cm_country", "cm_stateprovince", "address1_postalcode");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "companyname");

                Helpers.toggleFieldsVisibility(false, "parentaccountid");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "parentaccountid");
            } else {
                throw new Error("Invalid value for isExistingCompany");
            }
        },

        toggleFieldsVisibility: (setVisibleTo, ...attributes) => {
            attributes.forEach(attr => {
                const control = formContext.getControl(attr);
                if (control) {
                    control.setVisible(setVisibleTo);
                }
            });
        },

        toggleFieldsRequirementLevel: (requiredLevel, ...attributes) => {
            attributes.forEach(attr => {
                const attribute = formContext.getAttribute(attr);
                if (attribute) {
                    attribute.setRequiredLevel(requiredLevel); // "none", "required", or "recommended"
                }
            });
        },

        toggleServiceTypeFieldOnLoad: () => {
            const leadType = formContext.getAttribute("cm_leadtype").getValue();

            if (leadType === 121540001) {
                formContext.getControl("cm_servicetype").setVisible(true);
                formContext.getAttribute("cm_servicetype").setRequiredLevel("required");

                formContext.getControl("cm_othertype").setVisible(false);
                formContext.getAttribute("cm_othertype").setRequiredLevel("none");
                formContext.getAttribute("cm_othertype").setValue(null);
            }
            else if (leadType === 121540003) {
                formContext.getControl("cm_othertype").setVisible(true);
                formContext.getAttribute("cm_othertype").setRequiredLevel("required");

                formContext.getControl("cm_servicetype").setVisible(false);
                formContext.getAttribute("cm_servicetype").setRequiredLevel("none");
                formContext.getAttribute("cm_servicetype").setValue(null);
            }
            else {
                formContext.getControl("cm_servicetype").setVisible(false);
                formContext.getAttribute("cm_servicetype").setRequiredLevel("none");
                formContext.getAttribute("cm_servicetype").setValue(null);

                formContext.getControl("cm_othertype").setVisible(false);
                formContext.getAttribute("cm_othertype").setRequiredLevel("none");
                formContext.getAttribute("cm_othertype").setValue(null);
            }
        },

        setDefaultCountryOnLoad: () => {
            const countryField = formContext.getAttribute("cm_country");

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