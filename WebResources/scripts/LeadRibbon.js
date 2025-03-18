/*jslint browser: true */
/*global window */
/*global Common, CC */

if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.LeadRibbon = (function () {
    "use strict";
    const Constants = Object.freeze({
        OptionSets: Object.freeze({
            QUALIFIED: 1,
        }),
        SetTimeoutInterval: Object.freeze({
            SUCCESS: 3000,
            POPUPDELAY: 15000
        })
    });

    const Helpers = {
        isQualified: async (primaryControl) => {
            const leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
            const lead = await Xrm.WebApi.retrieveMultipleRecords("lead", `?$select=statecode&$filter=leadid eq ${leadId}`);
            return lead?.entities[0]?.statecode === Constants.OptionSets.QUALIFIED;
        },
        /**
         * Initiate Lead Quilification
         * @param {ComponentFramework.Context} primaryControl 
         */
        qualify: async (primaryControl) => {
            if (!(primaryControl.data.isValid())) {
                return;
            }
            let isUpdated;
            const formContext = primaryControl.ui.formContext;
            try {
                await Helpers.showProgressPromise("Qualifying Lead...");
                const leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();

                await formContext.data.save();

                const leadRecord = { statecode: Constants.OptionSets.QUALIFIED };
                isUpdated = await Xrm.WebApi.updateRecord("lead", leadId, leadRecord) !== null;

                isUpdated && setTimeout(() => {
                    primaryControl.data.refresh(true);
                }, Constants.SetTimeoutInterval.SUCCESS);
            } catch (err) {
                Helpers.openStringifiedErrorDialog("An error occurred ", err);
                console.error({ "Error": `An error occurred: ${err}` });
                throw err;

            } finally {
                Xrm.Utility.closeProgressIndicator()
                Helpers.notifyUser(formContext, isUpdated ? "Lead qualified. Processing Opportunities" : "Lead not qualified");
            }
        },
        //Error Handler
        openStringifiedErrorDialog: (errorHeader = "Please contact your administrator.", error = "Unexpected Error") => {
            Xrm.Navigation.openErrorDialog({
                message: `${errorHeader} \nError: ${JSON.stringify((error?.error?.message || error?.message || error))}`,
                details: JSON.stringify(error, Object.getOwnPropertyNames(error))
            });
        },
        notifyUser: (formContext, message) => {
            const formNotificationKey = `${formContext.data.entity.getEntityName()}${Math.random().toString()}`

            formContext.ui.setFormNotification(
                message,
                "INFO",
                formNotificationKey
            );
            setTimeout(() => formContext.ui.clearFormNotification(formNotificationKey), Constants.SetTimeoutInterval.POPUPDELAY);
        },
        showProgressPromise: (text) => {
            "use strict";
            return new Promise(function (resolve, reject) {
                Xrm.Utility.showProgressIndicator(text);
                setTimeout(resolve, 100);
            });
        },

    };
    return {
        qualify: Helpers.qualify,
        isQualified: Helpers.isQualified
    };

}());