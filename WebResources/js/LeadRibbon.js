"use strict";
/* eslint-disable @typescript-eslint/no-explicit-any */
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
if (!(window.hasOwnProperty("CC"))) {
    window.CC = {};
}
var CC;
(function (CC) {
    var LeadRibbon = /** @class */ (function () {
        function LeadRibbon() {
        }
        LeadRibbon.isQualified = function (primaryControl) {
            var _a;
            return __awaiter(this, void 0, void 0, function () {
                var leadId, lead;
                return __generator(this, function (_b) {
                    switch (_b.label) {
                        case 0:
                            leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
                            return [4 /*yield*/, Xrm.WebApi.retrieveMultipleRecords("lead", "?$select=statuscode&$filter=leadid eq ".concat(leadId))];
                        case 1:
                            lead = _b.sent();
                            return [2 /*return*/, ((_a = lead === null || lead === void 0 ? void 0 : lead.entities[0]) === null || _a === void 0 ? void 0 : _a.statuscode) === LeadRibbon.Constants.OptionSets.QUALIFIED];
                    }
                });
            });
        };
        /**
         * Initiate Lead Qualification
         * @param primaryControl - The form context
         */
        LeadRibbon.qualify = function (primaryControl) {
            return __awaiter(this, void 0, void 0, function () {
                var isUpdated, formContext, leadId, leadRecord, err_1;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            if (!primaryControl.data.isValid) {
                                return [2 /*return*/];
                            }
                            isUpdated = null;
                            const formContext = primaryControl.ui.formContext;
                            _a.label = 1;
                        case 1:
                            _a.trys.push([1, 5, 6, 7]);
                            return [4 /*yield*/, LeadRibbon.showProgressPromise("Qualifying Lead...")];
                        case 2:
                            _a.sent();
                            leadId = primaryControl.data.entity.getId().replace(/[{}]/g, "").toLowerCase();
                            return [4 /*yield*/, formContext.data.save()];
                        case 3:
                            _a.sent();
                            leadRecord = { statecode: LeadRibbon.Constants.OptionSets.QUALIFIED };
                            return [4 /*yield*/, Xrm.WebApi.updateRecord("lead", leadId, leadRecord)];
                        case 4:
                            isUpdated = _a.sent();
                            return [3 /*break*/, 7];
                        case 5:
                            err_1 = _a.sent();
                            LeadRibbon.openStringifiedErrorDialog("An error occurred", err_1);
                            console.error({ "Error": "An error occurred: ".concat(err_1) });
                            throw err_1;
                        case 6:
                            Xrm.Utility.closeProgressIndicator();
                            LeadRibbon.notifyUser(formContext, isUpdated !== null ? "Lead qualified. Processing Opportunities" : "Lead not qualified");
                            return [7 /*endfinally*/];
                        case 7: return [2 /*return*/];
                    }
                });
            });
        };
        LeadRibbon.openStringifiedErrorDialog = function (errorHeader, error) {
            var _a;
            if (errorHeader === void 0) { errorHeader = "Please contact your administrator."; }
            if (error === void 0) { error = "Unexpected Error"; }
            Xrm.Navigation.openErrorDialog({
                message: "".concat(errorHeader, " \nError: ").concat(JSON.stringify(((_a = error === null || error === void 0 ? void 0 : error.error) === null || _a === void 0 ? void 0 : _a.message) || (error === null || error === void 0 ? void 0 : error.message) || error)),
                details: JSON.stringify(error, Object.getOwnPropertyNames(error)),
            });
        };
        LeadRibbon.notifyUser = function (formContext, message) {
            var formNotificationKey = "".concat(formContext.data.entity.getEntityName()).concat(Math.random().toString());
            formContext.ui.setFormNotification(message, "INFO", formNotificationKey);
            setTimeout(function () { return formContext.ui.clearFormNotification(formNotificationKey); }, LeadRibbon.Constants.PopUpConfig.DURATION_IN_SECONDS);
        };
        LeadRibbon.showProgressPromise = function (message) {
            return __awaiter(this, void 0, void 0, function () {
                return __generator(this, function (_a) {
                    Xrm.Utility.showProgressIndicator(message);
                    return [2 /*return*/, new Promise(function (resolve) { return setTimeout(resolve, 1000); })]; // Simulating async operation
                });
            });
        };
        LeadRibbon.Constants = Object.freeze({
            OptionSets: Object.freeze({
                QUALIFIED: 1,
            }),
            PopUpConfig: Object.freeze({
                DURATION_IN_SECONDS: 15000,
            }),
        });
        return LeadRibbon;
    }());
    CC.LeadRibbon = LeadRibbon;
})(CC || (CC = {}));
//# sourceMappingURL=LeadRibbon.js.map