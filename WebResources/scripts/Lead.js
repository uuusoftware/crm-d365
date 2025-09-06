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

            // On Form Save Event
            _formContext.data.entity.addOnSave((cxt => Helpers.onSave(cxt)))

            // On Form Load Functions
            Helpers.setDefaultCountryOnLoad();
            Helpers.toggleServiceTypeFieldOnLoad(executionContext);
            Helpers.handleContactAndCompanyFieldsOnload();

            // On Tab Change EvenT
            const programsTab = _formContext.ui.tabs.get("Programs");
            if (programsTab) {
                programsTab.removeTabStateChange(Helpers.onTabStateChange);
                programsTab.addTabStateChange(Helpers.onTabStateChange);
            }

            // On Change Field Events
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
                Helpers.parentAccountOnChange(ctx)
            );

            _formContext.getAttribute("parentcontactid")?.addOnChange((ctx) =>
                Helpers.parentContactOnChange(ctx)
            );
        },
        /**
         * @description
         * @param executionContext 
         */
        onSave: async (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();
            const leadId = _formContext.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
            console.log(`Lead with Id: ${leadId} saved`);
        },

        onTabStateChange: (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();

            // "colapsed" = moving from this tab to another, "expanded" = moving from another to this
            if(_formContext.ui.tabs.get("Programs").getDisplayState().toLowerCase() !== "expanded"){
                return;
            }

            if (_formContext.data.getIsDirty()) {
                const confirmStrings = {
                    text: "You have unsaved changes. Please save before proceeding to Programs",
                    title: "Save Reminder",
                    confirmButtonLabel: "Save",
                    cancelButtonLabel: "Continue Without Saving"
                };
                const confirmOptions = { height: 200, width: 450 };

                Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions)
                    .then(function (result) {
                        if (result.confirmed) {
                            console.log("User chose to save the form.");
                            _formContext.data.save();
                        } else {
                            console.log("User chose to continue without saving.");
                        }
                    })
                    .catch(function (err) {
                        console.error("Error showing confirm dialog:", err);
                    });
            }
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
                "jobtitle", "telephone1", "mobilephone", "emailaddress1"
            ];
            const newContactRequiredFields = [
                "fullname_compositionLinkControl_firstname", "firstname",
                "fullname_compositionLinkControl_lastname", "lastname",
            ];

            // on existing account change, also change existing contact to no
            // if Existing Customer? == no Existing Contact? should be set to 'no' and 'read only'
            // if Existing Customer? == yes, Lead Type should be set to the lead type from that account and read only 

            if (isExistingContact === true) {
                Helpers.toggleFieldsVisibility(true, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.required, "parentcontactid");

                Helpers.toggleFieldsVisibility(false, ...newContactFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newContactFields);

                Helpers.setFieldReadOnly("fullname_compositionLinkControl_firstname", true);
                Helpers.setFieldReadOnly("fullname_compositionLinkControl_lastname", true);

                Helpers.clearFieldsValues(...newContactFields);
            } else if (isExistingContact === false) {
                Helpers.toggleFieldsVisibility(true, ...newContactFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, ...newContactRequiredFields);

                Helpers.toggleFieldsVisibility(false, "parentcontactid");
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, "parentcontactid");

                Helpers.setFieldReadOnly("fullname_compositionLinkControl_firstname", false);
                Helpers.setFieldReadOnly("fullname_compositionLinkControl_lastname", false);

                Helpers.clearFieldsValues("parentcontactid");
            } else {
                throw new Error("Invalid value for isExistingContact");
            }
        },

        handleIsExistingCustomer: (isExistingCustomer) => {
            const newCompanyFields = [
                "websiteurl", "address1_line1", "address1_line2",
                "address1_line3", "address1_city", "cm_stateprovince", "address1_postalcode"
            ];
            const newCompanyRequiredFields = ["parentaccountid"]; //Customer

            if (isExistingCustomer === true) {

                Helpers.toggleFieldsVisibility(true, ...newCompanyRequiredFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, ...newCompanyRequiredFields);

                Helpers.toggleFieldsVisibility(false, ...newCompanyFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newCompanyFields);

                Helpers.setFieldReadOnly("companyname", true);

                Helpers.clearFieldsValues(...newCompanyFields);
            } else if (isExistingCustomer === false) {

                Helpers.toggleFieldsVisibility(true, ...newCompanyFields);
                Helpers.toggleFieldsRequirementLevel(Constants.required, "companyname");

                Helpers.toggleFieldsVisibility(false, ...newCompanyRequiredFields);
                Helpers.toggleFieldsRequirementLevel(Constants.notRequired, ...newCompanyRequiredFields);

                Helpers.setFieldReadOnly("companyname", false);

                Helpers.clearFieldsValues(...newCompanyRequiredFields);
            } else {
                throw new Error("Invalid value for isExistingCustomer");
            }
        },
        parentAccountOnChange: (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();

            // Remove the values from contact lookup when the account/customer changes
            Helpers.clearFieldsValues("parentcontactid");
            Helpers.setCompanyName();
        },
        /**
         * @description Set first name and last name according to the chosen parent contact
         * Both first and last names are hidden when parentcontactid is shown, but it's necessary for other logic to work.
         * @param {*} executionContext 
         * @returns 
         */
        parentContactOnChange: async (executionContext) => {
            _formContext = _formContext || executionContext.getFormContext();

            const firstNameAttr = _formContext.getAttribute("firstname");
            const lastNameAttr = _formContext.getAttribute("lastname");

            const contactValue = _formContext.getAttribute("parentcontactid").getValue()?.at(0).id;
            if (!contactValue) {
                firstNameAttr.setValue(null);
                lastNameAttr.setValue(null);
                return;
            };

            const contactId = contactValue.replace(/[{}]/g, "").toLowerCase();
            const contactRecord = await Xrm.WebApi.retrieveRecord("contact", contactId, "?$select=firstname,lastname");
            debugger;

            firstNameAttr.setValue(contactRecord.firstname || "");
            lastNameAttr.setValue(contactRecord.lastname || "");
        },

        /**
         * @description Set field companyname to the name of the company (parentaccountid).
         * The Field companyname is hidden when parentaccountid is shown, but it's necessary for other logic to work.
         * E.g. Program Association uses it to se the lead and account name  
         */
        setCompanyName: () => {
            const accountValue = _formContext.getAttribute("parentaccountid").getValue()?.at(0).name;
            const companyAttr = _formContext.getAttribute("companyname");
            _formContext.getAttribute("firstname").setValue(null);
            _formContext.getAttribute("lastname").setValue(null);

            if (!accountValue) {
                companyAttr.setValue(null);
                return;
            }

            companyAttr.setValue(accountValue);
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
        setFieldReadOnly: (fieldName, isReadOnly) => {
            if (!_formContext || !fieldName) return;

            const control = _formContext.getControl(fieldName);
            if (control) {
                control.setDisabled(isReadOnly);
            } else {
                console.warn(`Control for field '${fieldName}' not found.`);
            }
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