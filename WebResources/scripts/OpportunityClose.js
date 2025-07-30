if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.OpportunityClose = (function () {
    "use strict";

    // This global constiable must be set in any entry point to this file. E.g. OnSave, OnLoad, OnChange
    let _formContext = null; // Global within CM.Lead scope

    const Constants = Object.freeze({
        entityName: "opportunityclose",
    });

    const Helpers = {
        /**
         * @description Use CM.OpportunityClose.onLoad to call it from the form on load event
         * @param executionContext 
         */
        onLoad: async (executionContext) => {
            _formContext = executionContext.getFormContext();
            _formContext.getAttribute("cm_sapid")?.addOnChange(async (ctx) =>
                await Helpers.checkForMergeAccountOnChange(ctx)
            );
            Helpers.setFieldsVisibility(executionContext);
        },
        /**
         * @description Use CM.OpportunityClose.onSave to call it from the form on load event
         * @param executionContext 
         */
        onSave: async (executionContext) => {
            _formContext = executionContext.getFormContext();
        },

        checkForMergeAccountOnChange: async (executionContext) => {
            _formContext = executionContext.getFormContext();

            const sapId = executionContext.getEventSource().getValue();
            if (!sapId) return;

            const records = await Xrm.WebApi.retrieveMultipleRecords("account",`?$select=accountnumber,name,accountid&$filter=accountnumber eq '${sapId}'`);

            const accountRecord = records.entities.at(0);

            accountRecord && await Helpers.showConfirmDialog(accountRecord);
        },
        showConfirmDialog: async (accountRecord) => {
            const confirmOptions = {
                title: "Merge Alert",
                subtitle: `The current Opportunity account will merge with account "${accountRecord.name}".`,
                text: "This action cannot be undone. Do you wish to continue?",
                confirmButtonLabel: "Yes",
                cancelButtonLabel: "No",
            };

            try {
                const result = await Xrm.Navigation.openConfirmDialog(confirmOptions);
                if (result.confirmed) {
                    console.log("User confirmed the action.");
                } else {
                    _formContext.getAttribute("cm_sapid").setValue(null);
                    console.log("User cancelled the action.");
                }
            } catch (error) {
                Helpers.openStringifiedErrorDialog("Error showing confirmation dialog:", error.message);
            }
        },

        openStringifiedErrorDialog: (errorHeader = "Please contact your administrator.", error = "Unexpected Error") => {
            Xrm.Navigation.openErrorDialog({
                message: `${errorHeader} \nError: ${JSON.stringify((error?.error?.message || error?.message || error))}`,
                details: JSON.stringify(error, Object.getOwnPropertyNames(error))
            });
        },

        setFieldsVisibility: async (executionContext) => {
            const formContext = executionContext.getFormContext();


            formContext.getControl("cm_npsaurl").setVisible(true);
            // formContext.getAttribute("cm_npsaurl").setRequiredLevel("required");
            formContext.getControl("cm_sapid").setVisible(true);
            formContext.getAttribute("cm_sapid").setRequiredLevel("required");
            formContext.getControl("cm_scheduleurl").setVisible(true);
            // formContext.getAttribute("cm_scheduleurl").setRequiredLevel("required");

            // Get the Opportunity ID from the Opportunity Close form
            const opportunityId = formContext.getAttribute("opportunityid").getValue();

            // if (opportunityId) {
            //     const oppId = opportunityId[0].id.replace("{", "").replace("}", ""); // Extract GUID

            //     Xrm.WebApi.retrieveRecord("opportunity", oppId, "?$select=cm_opportunitytype").then(
            //         function (result) {
            //             if (result.cm_opportunitytype !== undefined) {
            //                 const oppType = result.cm_opportunitytype;

            //                 // Check if the opportunity type is "Producer" (121540000)
            //                 if (oppType === 121540000) {
            //                     // Get the Status Reason field
            //                     const statusReason = formContext.getAttribute("opportunitystatuscode");

            //                     if (statusReason) {

            //                         const statusValue = statusReason.getValue(); // Get the numeric value of Status Reason
            //                         // Check if the Status Reason is "Won" (Make sure 1 is the correct value for "Won")
            //                         if (statusValue === 3) {

            //                             formContext.getControl("cm_npsaurl").setVisible(true);
            //                             formContext.getAttribute("cm_npsaurl").setRequiredLevel("required");
            //                             formContext.getControl("cm_sapid").setVisible(true);
            //                             formContext.getAttribute("cm_sapid").setRequiredLevel("required");
            //                             formContext.getControl("cm_scheduleurl").setVisible(true);
            //                             formContext.getAttribute("cm_scheduleurl").setRequiredLevel("required");
            //                         }
            //                     }
            //                 }
            //             }
            //         }
            //     );
            // }
        }
    };

    return {
        onLoad: Helpers.onLoad,
        onSave: Helpers.onSave
    };
}());

