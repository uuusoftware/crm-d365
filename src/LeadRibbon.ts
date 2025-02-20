/* eslint-disable @typescript-eslint/no-explicit-any */

if (!(window.hasOwnProperty("CC"))) {
    (window as any).CC = {};
}

namespace CC {
    export class LeadRibbon {
        private static Constants = Object.freeze({
            OptionSets: Object.freeze({
                QUALIFIED: 1,
            }),
            PopUpConfig: Object.freeze({
                DURATION_IN_SECONDS: 15000,
            }),
        });

        public static async isQualified(primaryControl: Xrm.FormContext): Promise<boolean> {
            const leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
            const lead = await Xrm.WebApi.retrieveMultipleRecords("lead", `?$select=statuscode&$filter=leadid eq ${leadId}`);
            return lead?.entities[0]?.statuscode === LeadRibbon.Constants.OptionSets.QUALIFIED;
        }

        /**
         * Initiate Lead Qualification
         * @param primaryControl - The form context
         */
        public static async qualify(primaryControl: Xrm.FormContext): Promise<void> {
            if (!primaryControl.data.isValid) {
                return;
            }
            let isUpdated: Xrm.Async.PromiseLike<{id: string}> | null = null;
            const formContext = primaryControl;
            
            try {
                await LeadRibbon.showProgressPromise("Qualifying Lead...");
                const leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
                await formContext.data.save();
                const leadRecord = { statecode: LeadRibbon.Constants.OptionSets.QUALIFIED };
                isUpdated = await Xrm.WebApi.updateRecord("lead", leadId, leadRecord);
            } catch (err: any) {
                LeadRibbon.openStringifiedErrorDialog("An error occurred", err);
                console.error({ "Error": `An error occurred: ${err}` });
                throw err;
            } finally {
                Xrm.Utility.closeProgressIndicator();
                LeadRibbon.notifyUser(formContext, isUpdated !== null ? "Lead qualified. Processing Opportunities" : "Lead not qualified");
            }
        }

        private static openStringifiedErrorDialog(errorHeader = "Please contact your administrator.", error: any = "Unexpected Error"): void {
            Xrm.Navigation.openErrorDialog({
                message: `${errorHeader} \nError: ${JSON.stringify(error?.error?.message || error?.message || error)}`,
                details: JSON.stringify(error, Object.getOwnPropertyNames(error)),
            });
        }

        private static notifyUser(formContext: Xrm.FormContext, message: string): void {
            const formNotificationKey = `${formContext.data.entity.getEntityName()}${Math.random().toString()}`;
            formContext.ui.setFormNotification(message, "INFO", formNotificationKey);
            setTimeout(() => formContext.ui.clearFormNotification(formNotificationKey), LeadRibbon.Constants.PopUpConfig.DURATION_IN_SECONDS);
        }

        private static async showProgressPromise(message: string): Promise<void> {
            Xrm.Utility.showProgressIndicator(message);
            return new Promise(resolve => setTimeout(resolve, 1000)); // Simulating async operation
        }
    }
}
