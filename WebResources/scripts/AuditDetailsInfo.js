/* eslint no-restricted-globals: 0 */
function openAuditRecordDetails(selectedEntities) {
  if (selectedEntities.length > 0) {
    Xrm.WebApi.retrieveRecord(selectedEntities[0]?.TypeName, selectedEntities[0]?.Id, "?$select=createdon,_userid_value,objecttypecode")
    .then( function (response) {
      var dialogParams = {
        createdon: response['createdon@OData.Community.Display.V1.FormattedValue'],
        userid: response['_userid_value@OData.Community.Display.V1.FormattedValue'],
        objecttypecode: response.objecttypecode, // entity field for UI
        entitytype_name: selectedEntities[0]?.TypeName,
        auditrecord_id: selectedEntities[0]?.Id.toString(),
        objecttype_code: response.objecttypecode, // parameter to pass to custom control
        objectid_value: response._objectid_value,
        created_on: response['createdon@OData.Community.Display.V1.FormattedValue'], // parameter to pass to custom control
        FormHeader: response['operation@OData.Community.Display.V1.FormattedValue'],
      };

      var dialogOptions = {
        width: 800,
        height: 480,
        position: 1,
      };

      Xrm.Navigation.openDialog('AuditDetailsInfo', dialogOptions, dialogParams)
      .then( function (response) {
          console.log("Dialog displayed successfully");
        },
        function (error) {
          Xrm.Navigation.openAlertDialog({ text: 'Error displaying dialog:' + ' ' + error?.message });
        }
      )
    },
    function (error) {
      Xrm.Navigation.openAlertDialog({ text: 'Error retrieving record:' + ' ' + error?.message });
    });
  }
}

function OnDialogClose(context) {
  context.getFormContext().ui.close();
}

function deleteAuditHistory(entityName, id, gridControl) {
  var dialogOptions = {
    height: 220,
    width: 600
  };
  var confirmDialog = {
    text: 'Are you sure you want to delete this audit history?',
    title: 'Confirm Delete',
    confirmButtonLabel: 'Delete',
    cancelButtonLabel: 'Cancel'
  };
  Xrm.Navigation.openConfirmDialog(confirmDialog, dialogOptions).then(function (response) {
    if (response.confirmed)
      {
        // Get the logical name of the primary key field
        getPrimaryKeyLogicalName(entityName)
       .then(primaryKeyLogicalName => {
          const requestData = {
            Target: {
              '@odata.type': `Microsoft.Dynamics.CRM.${entityName}`,
              [primaryKeyLogicalName]: id.slice(1, -1)
            }
          };

          const requestOptions = {
            method: 'POST',
            headers: {
              'Accept': 'application/json',
              'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
          };

          fetch(`${window.location.origin}/api/data/v9.1/DeleteRecordChangeHistory`, requestOptions)
          .then(response => {
            if (response.ok) {
              return response.json();
              } else {
                console.log('Error deleting audit history:', response.statusText);
                throw new Error(response.statusText);
              }
            })
            .then(data => {
              // Perform grid control refresh after a short delay
              setTimeout(function () {
                gridControl.refresh();
              }, 500);
              console.log('Audit history deleted successfully');
            })
          .catch(error => {
            Xrm.Navigation.openAlertDialog({ text: 'Error deleting audit history:' + ' ' + error?.message })
          });
        })
        .catch(error => {
          console.log('Error getting primary key logical name:', error);
        });
      }
      else {
        // for Cancel button
        console.log('Deletion cancelled');
      }
  })
  .catch(error => {
    Xrm.Navigation.openAlertDialog({ text: 'Error opening confirmation dialog:' + ' ' + error?.message })
  });
}

// Function to get primary key logical name
function getPrimaryKeyLogicalName(entityName) {
  return fetch(`${window.location.origin}/api/data/v9.1/EntityDefinitions(LogicalName='${entityName}')?$select=PrimaryIdAttribute`, {
    method: 'GET',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    }
  })
  .then(response => {
    if (response.ok) {
      return response.json();
    } else {
      throw new Error('Failed to fetch entity metadata');
    }
  })
  .then(data => {
    return data.PrimaryIdAttribute;
  });
}
