/*jslint browser: true */
/*global window */
/*global Common, CC */

if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.CaseRibbon = (function () {
    "use strict";
    const Constants = Object.freeze({
        OptionSets: Object.freeze({
            RESOLVED: 1,
        }),
        SetTimeoutInterval: Object.freeze({
            SUCCESS: 3000,
            POPUPDELAY: 15000
        })
    });

    const Helpers = {
        isResolved: async (primaryControl) => {
            const incidentId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
            const incidentRecords = await Xrm.WebApi.retrieveMultipleRecords("incident", `?$select=statecode&$filter=incidentid eq ${incidentId}`);
            return incidentRecords?.entities[0]?.statecode === Constants.OptionSets.RESOLVED;
        },
        /**
         * This function is expected to be called from a Ribbon button.
         * @description by calling this function and providing primaryControl it's capable of retrieving the
         * case/incident GUID and execute a close incident request. 
         * For that it creates a incident resolution record and uses it to execute an OOO action.
         * After the successful request, it should notify the user and refresh the form.
         * @param {ComponentFramework.Context} primaryControl 
         */
        resolve: async (primaryControl) => {
            if (!(primaryControl.data.isValid())) {
                return;
            }
            let isSuccess = false;
            const formContext = primaryControl.ui.formContext;
            try {
                await Helpers.showProgressPromise("Resolving Case...");
                const incidentId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();

                await formContext.data.save();

                isSuccess = await Helpers.executeCloseIncidentRequest(incidentId);

                isSuccess && setTimeout(() => {
                    primaryControl.data.refresh(true);
                }, Constants.SetTimeoutInterval.SUCCESS);
            } catch (err) {
                Helpers.openStringifiedErrorDialog("An error occurred ", err);
                console.error({ "Error": `An error occurred: ${err}` });
                if(err?.message?.raw) console.error(err?.message?.raw);
                throw err;

            } finally {
                Xrm.Utility.closeProgressIndicator()
                Helpers.notifyUser(formContext, isSuccess ? "Case resolved." : "Case not resolved");
            }
        },
        executeCloseIncidentRequest: async (caseId) => {
            try {
                const incidentResolution = {
                    subject: `Case Closed - ${caseId}`,
                    "incidentid@odata.bind": `/incidents(${caseId})`,
                    "@odata.type": "Microsoft.Dynamics.CRM.incidentresolution",
                    timespent: 0,
                    description: ""
                };
        
                const { id } = await Xrm.WebApi.online.createRecord("incidentresolution", incidentResolution);
        
                // Enrich the same object with the required activityid
                incidentResolution.activityid = id;
        
                const closeIncidentRequest = {
                    IncidentResolution: incidentResolution,
                    Status: 5,
                    getMetadata() {
                        return {
                            boundParameter: null,
                            parameterTypes: {
                                IncidentResolution: {
                                    typeName: "mscrm.incidentresolution",
                                    structuralProperty: 5
                                },
                                Status: {
                                    typeName: "Edm.Int32",
                                    structuralProperty: 1
                                }
                            },
                            operationType: 0,
                            operationName: "CloseIncident"
                        };
                    }
                };        
                const response = await Xrm.WebApi.online.execute(closeIncidentRequest);
        
                if (!response.ok) {
                    throw new Error("Failed to close the incident.");
                }        
                return true;
        
            } catch (error) {
                throw {error: error}
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
        resolve: Helpers.resolve,
        isResolved: Helpers.isResolved
    };

}());