/**
 * Do not use
 * This file is a test to get xrm while not in the context of a triggered event from D365
 */
if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.Lead = (function () {
    "use strict";
    const Constants = Object.freeze({
        entityName: "lead",
        customEventName: "d365TasksLoaded"
    });

    const Helpers = {
        waitForXrm: (callback) => {
            if (typeof Xrm !== "undefined" && Xrm.Utility && Xrm.WebApi) {
                callback(); // Xrm is ready, run the function
            } else {
                console.warn("Waiting for Xrm to load...");
                setTimeout(() => Helpers.waitForXrm(callback), 500); // Retry every 500ms
            }
        },
    
        loadTasksFromD365: async () => {
            Helpers.waitForXrm(async () => {
                const recordId = Xrm.Utility.getPageContext().input.entityId.replace(/[{}]/g, "");
                try {
                    const record = await Xrm.WebApi.retrieveRecord(Constants.entityName, recordId, "?$select=salutation");
                    if (record.salutation) {
                        const tasks = JSON.parse(record.salutation);
                        window.dispatchEvent(new CustomEvent(Constants.customEventName, { detail: tasks }));
                    }
                } catch (err) {
                    console.error({ "Error": `An error occurred: ${err}` });
                }
            });
        },
    
        saveTasksToD365: async (tasks) => {
            Helpers.waitForXrm(async () => {
                const recordId = Xrm.Utility.getPageContext().input.entityId.replace(/[{}]/g, "");
                const tasksString = JSON.stringify(tasks);
    
                const data = {
                    salutation: tasksString
                };
                try {
                    await Xrm.WebApi.updateRecord(Constants.entityName, recordId, data);
                } catch (err) {
                    console.error({ "Error": `An error occurred: ${err}` });
                    throw err;
                }
            });
        }
    };    
    return {
        loadTasksFromD365: Helpers.loadTasksFromD365,
        saveTasksToD365: Helpers.saveTasksToD365
    };

}());

window.addEventListener("d365TasksLoaded", (event) => {
    const tasks = event.detail;
    if (window.updateTasksState) {
        window.updateTasksState(tasks);
    }
});