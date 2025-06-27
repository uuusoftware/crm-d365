"use strict";
// Usage Example: Case.onLoad
if (!(window.hasOwnProperty("CM"))) {
    window.CM = {};
}
var CM;
(function (CM) {
    var Case = /** @class */ (function () {
        function Case() {
        }
        Case.prototype.onLoad = function (executionContext) {
            /* Use typings to create entity specific formContext. Like shown below is for 'Account' */
            //const formContext: Xrm.Account = executionContext.getFormContext();
            //formContext.getAttribute("accountnumber").getValue();
        };
        return Case;
    }());
    CM.Case = Case;
})(CM || (CM = {}));
//# sourceMappingURL=Case.js.map