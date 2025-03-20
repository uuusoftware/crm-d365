
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
            // TODO: currently throws an error on Xrm.WebApi.updateRecord: "An error has occurred. {1}{0}"
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

                const caseRecord = await Xrm.WebApi.retrieveMultipleRecords(Constants.CaseEntityName, `?$select=_cm_causecategory_value,_cm_incidentcategory_value&$filter=incidentid eq ${caseId}`);

                const subCatId = caseRecord.entities.at(0)._cm_causecategory_value;
                const caseCatId = caseRecord.entities.at(0)._cm_incidentcategory_value;

                const isRespAvailable = await Helpers.areAnyRespCatAvailable(subCatId,caseCatId);
                if (!isRespAvailable){
                    formContext.data.process.setActiveStage(identifyId);

                    const caseRecord = { cm_generatechecklist: false };
                    _ = await Xrm.WebApi.updateRecord("incident", caseId, caseRecord);

                    Helpers.openStringifiedErrorDialog("An error occurred ", "No checklist available for this category. Please choose another Incident Category or Cause Category");
                    return;
                } else {
                    Helpers.checkAndMoveToChecklistTab(formContext, caseId);
                }

            } catch (err) {
                Helpers.openStringifiedErrorDialog("An error occurred ", err);
                console.error({ "Error": `An error occurred: ${err}` });
                throw err;

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
        // onBpfStatusChange: async (executionContext)=> {
        //     const formContext = executionContext.getFormContext();
        //     const caseId = formContext.data.entity.getId().replace(/[{}]/g, "").toLowerCase();

        //     const bpfStatus = formContext.data.process.getStatus();

        //     if(bpfStatus === "finished"){
        //         const caseRecord = { statecode: 1 };    
        //         _ = await Xrm.WebApi.updateRecord("incident", caseId, caseRecord);
        //     }
        // },
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
    };    
    return {
        onLoad: Helpers.onLoad
    };
}());
