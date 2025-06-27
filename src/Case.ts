// Usage Example: Case.onLoad

if (!(window.hasOwnProperty("CM"))) {
    (window as any).CM = {};
}

//! not implemented

namespace CM {
    export class Case {
        private onLoad(executionContext: Xrm.Events.EventContext) {
            /* Use typings to create entity specific formContext. Like shown below is for 'Account' */
            //const formContext: Xrm.Account = executionContext.getFormContext();
            //formContext.getAttribute("accountnumber").getValue();
        }
    }
}