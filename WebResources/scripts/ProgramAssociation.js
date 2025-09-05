if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.ProgramAssociation = (function () {
    "use strict";

    // This global constant must be set in any entry point to this file. E.g. OnSave, OnLoad, OnChange
    let _formContext = null; // Global within CM.Lead scope

    const Constants = Object.freeze({
        entityName: "cm_ProgramAssociation",
    });

    const Helpers = {
        /**
         * @description 
         * @param executionContext 
         */
        onLoad: async (executionContext) => {
            _formContext = executionContext.getFormContext();

            _formContext.getAttribute("cm_program")?.addOnChange(async (ctx) =>
                await Helpers.setProvinceFromProgram(ctx)
            );
            _formContext.getAttribute("cm_program")?.addOnChange(async (ctx) =>
                await Helpers.setProgramAssociationName(ctx)
            );
        },
        /**
         * @description
         * @param executionContext 
         */
        onSave: async (executionContext) => {
            _formContext = executionContext.getFormContext();
        },
        //Set Province from Program Lookup if Province data is not entered by the user
        setProvinceFromProgram: async (executionContext) => {
            const formContext = executionContext.getFormContext();

            const provinceAttr = formContext.getAttribute("cm_province");
            const programAttr = formContext.getAttribute("cm_program");

            if (!provinceAttr || !programAttr) {
                console.warn("Required fields not found on form.");
                return;
            }

            // If province is already filled or program is not selected, exit
            if (provinceAttr.getValue() !== null || programAttr.getValue() === null) {
                return;
            }

            const programRef = programAttr.getValue()[0];
            const programId = programRef.id.replace(/[{}]/g, "");
            try {
                // Replace 'cm_Province' with the actual navigation property name if this fails
                const teamRecord = await Xrm.WebApi.retrieveRecord("team", programId, "?$expand=cm_Province($select=cm_provinceid,cm_name)");

                if (teamRecord.cm_Province && teamRecord.cm_Province.cm_provinceid) {
                    const provinceLookup = [{
                        id: teamRecord.cm_Province.cm_provinceid,
                        name: teamRecord.cm_Province.cm_name,
                        entityType: "cm_province"
                    }];
                    provinceAttr.setValue(provinceLookup);
                } else {
                    console.warn("No province data found on the Program.");
                }
            } catch (err) {
                Helpers.openStringifiedAlertDialog("", err);
                console.error({ "Error": `An error occurred: ${err}` });
                if (err?.message?.raw) console.error(err?.message?.raw);
                throw err;
            }
        },

        //Set Program Association Name automatically
        setProgramAssociationName: async (executionContext) => {
            const formContext = executionContext.getFormContext();

            const leadAttr = formContext.getAttribute("cm_lead");
            const programAttr = formContext.getAttribute("cm_program");
            const nameAttr = formContext.getAttribute("cm_name");

            if (!leadAttr || !programAttr || !nameAttr) {
                console.warn("One or more required fields are missing on the form.");
                return;
            }

            const leadVal = leadAttr.getValue();
            const programVal = programAttr.getValue();

            if (leadVal && programVal) {
                const leadId = leadVal[0].id.replace(/[{}]/g, "");
                const programId = programVal[0].id.replace(/[{}]/g, "");

                try {
                    // Fetch Lead and Program Name in parallel
                    const [leadRecord, programRecord] = await Promise.all([
                        Xrm.WebApi.retrieveRecord("lead", leadId, "?$select=companyname"),
                        Xrm.WebApi.retrieveRecord("team", programId, "?$select=name")
                    ]);

                    const companyName = leadRecord.companyname || " ";
                    const programName = programRecord.name || " ";

                    if (companyName && programName) {
                        const fullName = `${companyName} - ${programName}`;
                        nameAttr.setValue(fullName);
                    }
                } catch (err) {
                    Helpers.openStringifiedAlertDialog("", err);
                    console.error({ "Error": `An error occurred while retrieving Lead or Program: ${err}` });
                    if (err?.message?.raw) console.error(err.message.raw);
                    throw err;
                }
            }
        },
        openStringifiedAlertDialog: (title = "Please contact your administrator.", text = "Unexpected Error") => {
            const alertStrings = {
                confirmButtonLabel: "Ok",
                text: `${JSON.stringify((text?.error?.message || text?.message || text))}`,
                title: title
            };
            const alertOptions = { height: 120, width: 260 };

            Xrm.Navigation.openAlertDialog(alertStrings, alertOptions);
        },
    };

    return {
        onLoad: Helpers.onLoad,
        onSave: Helpers.onSave
    };
}());

