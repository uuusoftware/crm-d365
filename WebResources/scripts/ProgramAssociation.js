
//Set Province from Program Lookup if Province data is not entered by the user
function setProvinceFromProgram(executionContext) {
    var formContext = executionContext.getFormContext();

    var provinceAttr = formContext.getAttribute("cm_province");
    var programAttr = formContext.getAttribute("cm_program");

    if (!provinceAttr || !programAttr) {
        console.warn("Required fields not found on form.");
        return;
    }

    // If province is already filled or program is not selected, exit
    if (provinceAttr.getValue() !== null || programAttr.getValue() === null) {
        return;
    }

    var programRef = programAttr.getValue()[0];
    var programId = programRef.id.replace(/[{}]/g, "");

    // Replace 'cm_Province' with the actual navigation property name if this fails
    Xrm.WebApi.retrieveRecord("team", programId, "?$expand=cm_Province($select=cm_provinceid,cm_name)").then(
        function (result) {
            if (result.cm_Province && result.cm_Province.cm_provinceid) {
                var provinceLookup = [{
                    id: result.cm_Province.cm_provinceid,
                    name: result.cm_Province.cm_name,
                    entityType: "cm_province"
                }];
                provinceAttr.setValue(provinceLookup);
            } else {
                console.warn("No province data found on the Program.");
            }
        },
        function (error) {
            console.error("Error retrieving Program: ", error.message);
        }
    );
}
//Set Program Association Name automatically
function setProgramAssociationName(executionContext) {
    var formContext = executionContext.getFormContext();

    var leadAttr = formContext.getAttribute("cm_lead");
    var programAttr = formContext.getAttribute("cm_program");
    var nameAttr = formContext.getAttribute("cm_name");

    if (!leadAttr || !programAttr || !nameAttr) {
        console.warn("One or more required fields are missing on the form.");
        return;
    }

    var leadVal = leadAttr.getValue();
    var programVal = programAttr.getValue();

    if (leadVal && programVal) {
        var leadId = leadVal[0].id.replace(/[{}]/g, "");
        var programId = programVal[0].id.replace(/[{}]/g, "");

        // Fetch Lead and Program Name in parallel
        Promise.all([
            Xrm.WebApi.retrieveRecord("lead", leadId, "?$select=companyname"),
            Xrm.WebApi.retrieveRecord("team", programId, "?$select=name")
        ]).then(function (results) {
            var leadRecord = results[0];
            var programRecord = results[1];

            var companyName = leadRecord.companyname || "";
            var programName = programRecord.name || "";

            if (companyName && programName) {
                var fullName = companyName + " - " + programName;
                nameAttr.setValue(fullName);
            }
        }).catch(function (error) {
            console.error("Error retrieving Lead or Program: ", error.message);
        });
    }
}
