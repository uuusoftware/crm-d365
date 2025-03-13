
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
        loadTasksFromD365: async () => {
            if (typeof Xrm !== "undefined" && Xrm.WebApi) {
                const recordId = Xrm.Utility.getPageContext().input.entityId.replace(/[{}]/g, "");
                try {
                    const record = await Xrm.WebApi.retrieveRecord(Constants.entityName, recordId, "?$select=salutation")
                    if (record.salutation) {
                        const tasks = JSON.parse(record.salutation);
                        window.dispatchEvent(new CustomEvent(Constants.customEventName, { detail: tasks }));
                    }
                } catch (err) {
                    console.error({ "Error": `An error occurred: ${err}` });
                    throw err;
                }
            } else {
                console.warn("Xrm context is not available.");
            }
        },
        saveTasksToD365: async (tasks) => {
            if (typeof Xrm !== "undefined" && Xrm.WebApi) {
                const recordId = Xrm.Utility.getPageContext().input.entityId.replace(/[{}]/g, "");
                const tasksString = JSON.stringify(tasks);

                const data = {
                    salutation: tasksString  // Replace with the correct field schema name
                };
                try {
                    await Xrm.WebApi.updateRecord(Constants.entityName, recordId, data);
                } catch (err) {
                    console.error({ "Error": `An error occurred: ${err}` });
                    throw err;
                }
            } else {
                console.warn("Xrm context is not available.");
            }
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