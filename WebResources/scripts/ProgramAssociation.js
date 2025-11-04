if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}

CM.ProgramAssociation = (function () {
    "use strict";

    // This global constant must be set in any entry point to this file. E.g. OnSave, OnLoad, OnChange
    let _formContext = null; // Global within CM.ProgramAssociation scope
        const Constants = Object.freeze({
        entityName: "cm_ProgramAssociation",
        OptionSets: {
            QUALIFIED: 1,
        },
        SetTimeoutInterval: {
            SUCCESS: 3000,
            POPUPDELAY: 15000
        },
        LeadType: {
            Producer: "121540000"
        }
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

            const leadType = await Helpers.getLeadType();
            if (leadType) {
                _formContext.getControl("cm_program").addPreSearch(() => {
                    let filter = "<filter type='and'>";
                    filter += `<condition attribute='cm_leadtype' operator='eq' value='${leadType}' />`
                    filter += "</filter>";
                    _formContext.getControl("cm_program").addCustomFilter(filter, "team");
                });
            }

            /**
             * This filter is here for reference only.
             * It prevents the user from seeing Programs in the program association that will cause an error.
             * When qualifying the lead 
             */

            // First fetch the unique Ids asynchronously so it addPreSearch in only called when the new filter is complete.
            // const uniqueProgramIds = await Helpers.getUniqueProgramIds();
            // if (uniqueProgramIds !== null) {

            //     _formContext.getControl("cm_program").addPreSearch(() => {
            //         let filter = "<filter type='or'>";

            //         if (!uniqueProgramIds.length) {
            //             filter += "<condition attribute='teamid' operator='eq' value='00000000-0000-0000-0000-000000000000' />"
            //         } else {
            //             uniqueProgramIds.forEach(id => {
            //                 filter += `<condition attribute='teamid' operator='eq' value='${id}' />`;
            //             })
            //         }

            //         filter += "</filter>";
            //         _formContext.getControl("cm_program").addCustomFilter(filter, "team");
            //     })
            // }
        },
        /**
         * @description
         * @param executionContext
         */
        onSave: async (executionContext) => {
            _formContext = executionContext.getFormContext();
        },
        /**
         * @description Adds a custom filter to the program/team look field to includo only programs that are related to the current leadtype
         * @param {*} executionContext 
         */
        getUniqueProgramIds: async () => {
            try {
                const leadId = _formContext.getAttribute("cm_lead").getValue()?.at(0)?.id.replace(/[{}]/g, "");
                const programField = _formContext.getControl("cm_program");

                if (!programField) throw new Error("Field cm_program not found");

                const leadRecord = await Xrm.WebApi.retrieveRecord("lead", leadId, "?$select=leadid,cm_leadtype");

                if (!leadRecord || !leadRecord.cm_leadtype) throw new Error("Lead type not found for this lead record");

                if (leadRecord.cm_leadtype.toString() !== Constants.LeadType.Producer){
                    return null;
                }

                const questionRecords = await Xrm.WebApi.retrieveMultipleRecords(
                    "cm_questioncatalog", `?$select=_cm_program_value,cm_questionfor&$filter=cm_questionfor eq ${leadRecord.cm_leadtype}`);

                const programIds = questionRecords.entities.map(question => question._cm_program_value)

                if (!programIds.length) {
                    Helpers.notifyUser("No team/program found for this Lead Type", false); 
                    return [];
                }

                return [...new Set(programIds)];
            } catch (err) {
                Helpers.openStringifiedAlertDialog("", err);
                console.error({ "Error": `An error occurred while retrieving Lead or Program: ${err}` });
                if (err?.message?.raw) console.error(err.message.raw);
                throw err;
            }
        },
        getLeadType: async () => {
            try {
                const leadId = _formContext.getAttribute("cm_lead").getValue()?.at(0)?.id.replace(/[{}]/g, "");
                const programField = _formContext.getControl("cm_program");

                if (!programField) throw new Error("Field cm_program not found");

                const leadRecord = await Xrm.WebApi.retrieveRecord("lead", leadId, "?$select=leadid,cm_leadtype");

                return leadRecord.cm_leadtype;

            } catch (err) {
                Helpers.openStringifiedAlertDialog("", err);
                console.error({ "Error": `An error occurred while retrieving Lead or Program: ${err}` });
                if (err?.message?.raw) console.error(err.message.raw);
                throw err;
            }
        },
        notifyUser: (message, hasTimeout) => {
            const formNotificationKey = `${_formContext.data.entity.getEntityName()}${Math.random().toString()}`

            _formContext.ui.setFormNotification(
                message,
                "INFO",
                formNotificationKey
            );

            hasTimeout && setTimeout(() => _formContext.ui.clearFormNotification(formNotificationKey), Constants.SetTimeoutInterval.POPUPDELAY);
        },
        //Set Province from Program Lookup if Province data is not entered by the user
        setProvinceFromProgram: async (executionContext) => {
            const _formContext = executionContext.getFormContext();

            const provinceAttr = _formContext.getAttribute("cm_province");
            const programAttr = _formContext.getAttribute("cm_program");

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
            const _formContext = executionContext.getFormContext();

            const leadAttr = _formContext.getAttribute("cm_lead");
            const programAttr = _formContext.getAttribute("cm_program");
            const nameAttr = _formContext.getAttribute("cm_name");

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

