
if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.Case = (function () {
    "use strict";
    const Constants = Object.freeze({
        CaseEntityName: "incident",
        SubCatEntityName: "cm_casesubcategory",
        CaseCatEntityName: "cm_casecategory",
        BpfStages: {
            Identify: "Identify",
            Research: "Research",
            Resolve: "Resolve"
        }
    });

    const Helpers = {    
        onLoad: async (executionContext) => {
            const formContext = executionContext.getFormContext();
            formContext.data.process.removeOnStageChange(Helpers.isResponseCatalogAvailable);
            formContext.data.process.addOnStageChange(Helpers.isResponseCatalogAvailable);

            //Execution sequesce
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
                    throw ("No checklist available for this category");
                }
            } catch (err) {
                Helpers.openStringifiedErrorDialog("An error occurred ", err);
                console.error({ "Error": `An error occurred: ${err}` });
                throw err;

            } finally {
                Xrm.Utility.closeProgressIndicator();
            }
        },
        showProgressPromise: (text) => {
            "use strict";
            return new Promise(function (resolve, reject) {
                Xrm.Utility.showProgressIndicator(text);
                setTimeout(resolve, 100);
            });
        },
        areAnyRespCatAvailable: async (subCatId, caseCatId) => {
            // const subCatRecord = await Xrm.WebApi.retrieveMultipleRecords
            //     (Constants.SubCatEntityName, `?$select=${Constants.SubCatEntityName}id&$filter=${Constants.SubCatEntityName}id eq ${subCatId}`);

            // const caseCatRecord = await Xrm.WebApi.retrieveMultipleRecords
            //     (Constants.CaseCatEntityName, `?$select=${Constants.CaseCatEntityName}id&$filter=${Constants.CaseCatEntityName}id eq ${caseCatId}`);

            const isSubCatRecordValid = await Xrm.WebApi.retrieveMultipleRecords("cm_casechecklistcatalog", `?$select=cm_casechecklistcatalogid&$filter=_cm_casesubcategory_value eq  ${subCatId}`) ? true : false;

            const isCaseCatRecordd = await Xrm.WebApi.retrieveMultipleRecords("cm_casechecklistcatalog", `?$select=cm_casechecklistcatalogid&$filter=_cm_casecategory_value eq ${caseCatId}`) ? true : false;

            return (isSubCatRecordValid !== false && isCaseCatRecordd !== false);
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
