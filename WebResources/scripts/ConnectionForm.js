function toggleProgramVisibility(executionContext) {
    var formContext = executionContext.getFormContext();

    // Get the record2id (Connected To) lookup
    var record2 = formContext.getAttribute("record2id");
    if (!record2 || !record2.getValue()) {
        // No value, hide the program field
        formContext.getControl("cm_program").setVisible(false);
        return;
    }

    var record2Value = record2.getValue()[0]; // Get the first item from lookup
    var entityType = record2Value.entityType; // e.g., 'contact', 'account'

    // Show program only if record2id type is 'contact'
    if (entityType === "contact") {
        formContext.getControl("cm_program").setVisible(true);
    } else {
        formContext.getControl("cm_program").setVisible(false);
    }
}
