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
            Helpers.setFieldsVisibility(executionContext);
        },
        /**
         * @description Use CM.OpportunityClose.onSave to call it from the form on load event
         * @param executionContext 
         */
        onSave: (executionContext) => {
            _formContext = executionContext.getFormContext();
            return Helpers.notifyUserOfMergeRequest(executionContext); // Return the promise!
        },

        notifyUserOfMergeRequest: (executionContext) => {
            const formContext = executionContext.getFormContext();
            const eventArgs = executionContext.getEventArgs();

            // 1) Cancel the save synchronously
            eventArgs.preventDefault();

            // 2) Do your async work
            const sapId = formContext.getAttribute("cm_sapid").getValue();
            Xrm.WebApi.retrieveMultipleRecords("account",`?$select=accountnumber,name,accountid&$filter=accountnumber eq '${sapId}'`)
                .then((records) => {
                    const accountRecord = records.entities[0];
                    // If no match, just re-save
                    if (!accountRecord) {
                        return formContext.data.save();
                    }

                    // 3) Show confirmation dialog
                    const confirmOptions = {
                        title: "Merge Alert",
                        subtitle: `This will merge with account "${accountRecord.name}".`,
                        text: "This action cannot be undone. Continue?",
                        confirmButtonLabel: "Yes",
                        cancelButtonLabel: "No",
                    };

                    return Xrm.Navigation.openConfirmDialog(confirmOptions).then((res) => {
                        if (res.confirmed) {
                            // User said “Yes” → re‑trigger the save
                            formContext.data.save();
                        }
                        // else do nothing (save stays canceled)
                    });
                })
                .catch((err) => {
                    console.error("Error during merge check:", err.message);
                    // Optionally surface error to user...
                    // Save remains canceled because we already called preventDefault()
                });

            // 4) Return nothing (or a resolved promise) so platform knows we're async,
            //    but actual save will only happen via formContext.data.save()
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

