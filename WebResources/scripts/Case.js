
if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.Case = (function () {
    "use strict";
    const Constants = Object.freeze({
        CaseEntityName: "incident",
        SubCatEntityName: "cm_casesubcategory",
        CaseCatEntityName: "cm_casecategory",
        CheckListCatalogEntity: "cm_casechecklistcatalog",
        SurveyEntityName: "msfp_surveyinvite",
        ContactEntityName: "contact",
        BpfStages: {
            Identify: "Identify",
            Research: "Research",
            Resolve: "Resolve"
        },
        Tabs: {
            Checklist: "tab_8"
        },
        SetTimeoutInterval: {
            POPUPDELAY: 5000
        }
    });

    const Helpers = {    
        onLoad: async (executionContext) => {
            const formContext = executionContext.getFormContext();
            formContext.data.process.removeOnStageChange(Helpers.isResponseCatalogAvailable);
            formContext.data.process.addOnStageChange(Helpers.isResponseCatalogAvailable);
            // "This message can not be used to set the state of incident to Resolved. In order to set state of incident to Resolved, use the CloseIncidentRequest message instead."
            // https://github.com/bcgov/EMCR-DFA/blob/c7581931bbddb637c3ffd678432fa4ffd8e708aa/Dynamics/OldGitRepo/DFA.CRM.Web/JS/incident/dfa_incident.js#L1149
            // https://xrm-oss.github.io/Xrm-WebApi-Client/module-Requests.html

            // UNCOMMENT AFTER CHANGING THE BPF STATUS BACK TO ACTIVE WHEN THERE'S AN ERROR
            // formContext.data.process.addOnProcessStatusChange(Helpers.onBpfStatusChange)

            //Execution sequence
            //1# formContext.data.process.addOnStageSelected => triggers when the BPF is clicked
            //2# formContext.data.process.addOnPreStageChange => triggers when next stage or back  is clicked (best for validation)
            //3# formContext.data.process.addOnStageChange => triggers when next stage changes (best for handling next stage)
        },
        isResponseCatalogAvailable: async (executionContext) => {
            const formContext = executionContext.getFormContext();            
            const currentStageName = formContext.data.process.getActiveStage().getName();

            if(currentStageName != Constants.BpfStages.Research){
                return;
            }

            const identifyId = formContext.data.process.getActiveProcess().getStages().getAll().at(0).getId();
            try {
                await Helpers.showProgressPromise("Loading Responses...");
                const caseId = formContext.data.entity.getId().replace(/[{}]/g, "").toLowerCase();

                const caseRecord = await Helpers.retrieveRecords(
                    Constants.CaseEntityName, 
                    "incidentid", 
                    caseId, 
                    ["_cm_causecategory_value","_cm_incidentcategory_value", "_primarycontactid_value"]);
                
                const subCatId = caseRecord.at(0)._cm_causecategory_value;
                const caseCatId = caseRecord.at(0)._cm_incidentcategory_value;
                const contactId = caseRecord.at(0)._primarycontactid_value;
                    
                const surveyRecord = await Helpers.retrieveRecords(
                    Constants.SurveyEntityName, 
                    "_regardingobjectid_value", 
                    caseId);
                        
                const contactRecord = await Helpers.retrieveRecords(
                    Constants.ContactEntityName, 
                    "contactid", 
                    contactId,
                    ["emailaddress1"]);

                if (surveyRecord.length > 0 && !contactRecord.at(0).emailaddress1){
                    formContext.data.process.setActiveStage(identifyId);
                    Helpers.openStringifiedErrorDialog("An error occurred ", 
                        "Missing email ID. Please add an email ID to the contact before proceeding");
                    return;
                }
                if (!subCatId || !caseCatId){
                    formContext.data.process.setActiveStage(identifyId);
                    Helpers.openStringifiedErrorDialog("An error occurred ", 
                        "Please add Case Category and Sub Category");
                    return;
                }
            /*
                  
                // Check if there are cm_casechecklistcatalog records available for case or sub category
                const isRespAvailable = await Helpers.areAnyRespCatAvailable(subCatId,caseCatId);
                // If none are available it returns the bpf stage, and set cm_generatechecklist to false so it doesn't try again
                if (!isRespAvailable){
                    formContext.data.process.setActiveStage(identifyId);
                    
                    const caseRecord = { cm_generatechecklist: false };
                    _ = await Xrm.WebApi.updateRecord("incident", caseId, caseRecord);
                    
                    Helpers.openStringifiedErrorDialog("An error occurred ", 
                    "No checklist available for this category. Please choose another Incident Category or Cause Category");
                    return;
                } else {
                    Helpers.checkAndMoveToChecklistTab(formContext, caseId);
                }
            */

            } catch (err) {
                Helpers.openStringifiedErrorDialog("An error occurred ", err);
                console.error({ "Error": `An error occurred: ${err}` });
            } finally {
                Xrm.Utility.closeProgressIndicator();
            }
        },
        checkAndMoveToChecklistTab: async (formContext, caseId) => {
            Helpers.notifyUser(formContext, "Checklist is being generated");
        
            let attempts = 0;
            let maxAttempts = 5;
            const interval = 3000; // 3 seconds
            const checklistTab = formContext.ui.tabs.get(Constants.Tabs.Checklist);
        
            let checkInterval = setInterval(async function () {
                try {
                    const responses = await Xrm.WebApi.retrieveMultipleRecords(
                        "cm_casechecklistresponse",
                        `?$select=cm_casechecklistresponseid&$filter=_cm_case_value eq ${caseId}`
                    );
        
                    if (responses && responses.entities.length > 0) {
                        clearInterval(checkInterval); // Stop checking
        
                        if (checklistTab) {
                            checklistTab.setVisible(true);
                            checklistTab.setFocus();
                        }
                    } else {
                        attempts++;
                        console.log(`Attempt ${attempts}: No responses found.`);
        
                        if (attempts >= maxAttempts) {
                            clearInterval(checkInterval);
                            console.warn("Checklist responses not found after 5 attempts.");
                        }
                    }
                } catch (error) {
                    clearInterval(checkInterval);
                }
            }, interval);
        },
        onBpfStatusChange: async (executionContext)=> {
            const formContext = executionContext.getFormContext();
            const caseId = formContext.data.entity.getId().replace(/[{}]/g, "").toLowerCase();

            const bpfStatus = formContext.data.process.getStatus();

            if (bpfStatus === "finished") {
                // const caseRecord = { statecode: 1 };    
                // _ = await Xrm.WebApi.updateRecord("incident", caseId, caseRecord);
                //https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.closeincidentrequest?view=dataverse-sdk-latest
                
                Helpers.resolveCase(formContext, caseId);
                console.log("Incident successfully resolved.");
            }
        },
        showProgressPromise: (text) => {
            "use strict";
            return new Promise(function (resolve, reject) {
                Xrm.Utility.showProgressIndicator(text);
                setTimeout(resolve, 100);
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
        areAnyRespCatAvailable: async (subCatId, caseCatId) => {

            const isSubCatRecordValid = await Xrm.WebApi.retrieveMultipleRecords(
                Constants.CheckListCatalogEntity, `?$select=cm_casechecklistcatalogid&$filter=_cm_casesubcategory_value eq  ${subCatId}`);

            const isCaseCatRecord = await Xrm.WebApi.retrieveMultipleRecords(
                Constants.CheckListCatalogEntity, `?$select=cm_casechecklistcatalogid&$filter=_cm_casecategory_value eq ${caseCatId}`);

            return (isSubCatRecordValid.entities.lenght > 0 || isCaseCatRecord.entities.length > 0);
        },
        openStringifiedErrorDialog: (errorHeader = "Please contact your administrator.", error = "Unexpected Error") => {
            Xrm.Navigation.openErrorDialog({
                message: `${errorHeader} \nError: ${JSON.stringify((error?.error?.message || error?.message || error))}`,
                details: JSON.stringify(error, Object.getOwnPropertyNames(error))
            });
        },
        resolveCase: function (formContext, caseId) {
    
            var incidentresolution = {
                "subject": "Case Closed - ",
                "incidentid@odata.bind": "/incidents(" + caseId + ")",   //Id of incident
                "@odata.type": "Microsoft.Dynamics.CRM.incidentresolution",
                "timespent": 0,
                "description": ""
            };
    
            var newEntityId = "";
            Xrm.WebApi.online.createRecord("incidentresolution", incidentresolution).then(
                function success(result) {
                    newEntityId = result.id;
                    var parameters = {};
    
                    incidentresolution.activityid = newEntityId;
                    parameters.IncidentResolution = incidentresolution;
                    parameters.Status = 5;//Closed
    
                    var closeIncidentRequest = {
                        IncidentResolution: parameters.IncidentResolution,
                        Status: parameters.Status,
    
                        getMetadata: function () {
                            return {
                                boundParameter: null,
                                parameterTypes: {
                                    "IncidentResolution": {
                                        "typeName": "mscrm.incidentresolution",
                                        "structuralProperty": 5
                                    },
                                    "Status": {
                                        "typeName": "Edm.Int32",
                                        "structuralProperty": 1
                                    }
                                },
                                operationType: 0,
                                operationName: "CloseIncident"
                            };
                        }
                    };
    
                    Xrm.WebApi.online.execute(closeIncidentRequest).then(
                        function success(result) {
                            if (result.ok) {
                                formContext.data.refresh(false);
                            }
                        },
                        function (error) {
                            Xrm.Utility.alertDialog(error.message);
                        }
                    );
    
                },
                function (error) {
                    Xrm.Utility.alertDialog(error.message);
                }
            );
        },
        retrieveRecords: async (entityName, lookupField, lookupValue, retrievedFields = []) => {
            try {
                let selectClause = retrievedFields.length > 0 ? `$select=${retrievedFields.join(",")}` : "";

                let filterClause = `$filter=${lookupField} eq ${lookupValue}`;

                let query = [selectClause, filterClause].filter(Boolean).join("&");

                const response = await Xrm.WebApi.retrieveMultipleRecords(entityName, `?${query}`);
                return response.entities; // returns array of records
            } catch (error) {
                console.error(`Error retrieving ${entityName}:`, error);
                throw error;
            }
        }
    };    
    return {
        onLoad: Helpers.onLoad
    };
}());
