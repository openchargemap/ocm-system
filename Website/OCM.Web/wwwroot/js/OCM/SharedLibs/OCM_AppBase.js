/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
/// <reference path="OCM_CommonUI.ts" />
var OCM;
(function (OCM) {
    var AppMode;
    (function (AppMode) {
        AppMode[AppMode["STANDARD"] = 0] = "STANDARD";
        AppMode[AppMode["LOCALDEV"] = 1] = "LOCALDEV";
        AppMode[AppMode["SANDBOXED"] = 2] = "SANDBOXED";
    })(AppMode = OCM.AppMode || (OCM.AppMode = {}));
    /** View Model for core functionality */
    var AppViewModel = /** @class */ (function () {
        function AppViewModel() {
            this.selectedPOI = null;
            this.poiList = null;
            this.favouritesList = null;
            this.searchPosition = null;
            this.resultsBatchID = -1;
            this.isCachedResults = false;
        }
        return AppViewModel;
    }());
    OCM.AppViewModel = AppViewModel;
    /** App configuration settings */
    var AppConfig = /** @class */ (function () {
        function AppConfig() {
            this.autoRefreshMapResults = false;
            this.launchMapOnStartup = false;
            this.maxResults = 100;
            this.searchTimeoutMS = 20000;
            this.searchFrequencyMinMS = 500;
            this.allowAnonymousSubmissions = false;
            this.enableLiveMapQuerying = false;
            this.enablePOIListView = true;
        }
        return AppConfig;
    }());
    OCM.AppConfig = AppConfig;
    /** App state settings*/
    var AppState = /** @class */ (function () {
        function AppState() {
            this.appMode = AppMode.STANDARD;
            this.isRunningUnderCordova = false;
            this.isEmbeddedAppMode = false;
            this.appInitialised = false;
            this.languageCode = "en";
            this.isLocationEditMode = false;
            this.enableCommentSubmit = true;
            this.isSearchInProgress = false;
            this._appQuitRequestCount = 0;
        }
        return AppState;
    }());
    OCM.AppState = AppState;
    /** Base for App Components */
    var AppBase = /** @class */ (function (_super) {
        __extends(AppBase, _super);
        function AppBase() {
            var _this = _super.call(this) || this;
            _this.appState = new AppState();
            _this.appConfig = new AppConfig();
            _this.apiClient = new OCM.API();
            _this.geolocationManager = new OCM.Geolocation(_this.apiClient);
            _this.mappingManager = new OCM.Mapping();
            _this.viewModel = new AppViewModel();
            return _this;
        }
        AppBase.prototype.logAnalyticsView = function (viewName) {
            if (window.analytics) {
                window.analytics.trackView(viewName);
            }
        };
        AppBase.prototype.logAnalyticsEvent = function (category, action, label, value) {
            if (label === void 0) { label = null; }
            if (value === void 0) { value = null; }
            if (window.analytics) {
                window.analytics.trackEvent(category, action, label, value);
            }
        };
        AppBase.prototype.logAnalyticsUserId = function (userId) {
            if (window.analytics) {
                window.analytics.setUserId(userId);
            }
        };
        AppBase.prototype.getLoggedInUserInfo = function () {
            var userInfo = {
                "Identifier": this.getCookie("Identifier"),
                "Username": this.getCookie("Username"),
                "SessionToken": this.getCookie("OCMSessionToken"),
                "Permissions": this.getCookie("AccessPermissions")
            };
            if (userInfo.Identifier != null) {
                this.logAnalyticsUserId(userInfo.Identifier);
            }
            return userInfo;
        };
        AppBase.prototype.setLoggedInUserInfo = function (userInfo) {
            this.setCookie("Identifier", userInfo.Identifier);
            this.setCookie("Username", userInfo.Username);
            this.setCookie("OCMSessionToken", userInfo.SessionToken);
            this.setCookie("AccessPermissions", userInfo.AccessPermissions);
        };
        AppBase.prototype.getCookie = function (c_name) {
            if (this.appState.isRunningUnderCordova) {
                return this.apiClient.getCachedDataObject("_pref_" + c_name);
            }
            else {
                //http://www.w3schools.com/js/js_cookies.asp
                var i, x, y, ARRcookies = document.cookie.split(";");
                for (i = 0; i < ARRcookies.length; i++) {
                    x = ARRcookies[i].substr(0, ARRcookies[i].indexOf("="));
                    y = ARRcookies[i].substr(ARRcookies[i].indexOf("=") + 1);
                    x = x.replace(/^\s+|\s+$/g, "");
                    if (x == c_name) {
                        var val = unescape(y);
                        if (val == "null")
                            val = null;
                        return val;
                    }
                }
                return null;
            }
        };
        AppBase.prototype.setCookie = function (c_name, value, exdays) {
            if (exdays === void 0) { exdays = 1; }
            if (this.appState.isRunningUnderCordova) {
                this.apiClient.setCachedDataObject("_pref_" + c_name, value);
            }
            else {
                if (exdays == null)
                    exdays = 1;
                //http://www.w3schools.com/js/js_cookies.asp
                var exdate = new Date();
                exdate.setDate(exdate.getDate() + exdays);
                var c_value = escape(value) + "; expires=" + exdate.toUTCString();
                document.cookie = c_name + "=" + c_value;
            }
        };
        AppBase.prototype.clearCookie = function (c_name) {
            if (this.appState.isRunningUnderCordova) {
                this.apiClient.setCachedDataObject("_pref_" + c_name, null);
            }
            else {
                var expires = new Date();
                expires.setUTCFullYear(expires.getUTCFullYear() - 1);
                document.cookie = c_name + '=; expires=' + expires.toUTCString() + '; path=/';
            }
        };
        AppBase.prototype.getParameter = function (name) {
            //Get a parameter value from the document url
            name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
            var regexS = "[\\?&]" + name + "=([^&#]*)";
            var regex = new RegExp(regexS);
            var results = regex.exec(window.location.href);
            if (results == null)
                return "";
            else
                return results[1];
        };
        AppBase.prototype.setDropdown = function (id, selectedValue) {
            if (selectedValue == null)
                selectedValue = "";
            var $dropdown = $("#" + id);
            $dropdown.val(selectedValue);
        };
        AppBase.prototype.populateDropdown = function (id, refDataList, selectedValue, defaultToUnspecified, useTitleAsValue, unspecifiedText) {
            if (defaultToUnspecified === void 0) { defaultToUnspecified = null; }
            if (useTitleAsValue === void 0) { useTitleAsValue = null; }
            if (unspecifiedText === void 0) { unspecifiedText = null; }
            var $dropdown = $("#" + id);
            $('option', $dropdown).remove();
            if (defaultToUnspecified == true) {
                if (unspecifiedText == null)
                    unspecifiedText = "Unknown";
                $dropdown.append($('<option > </option>').val("").html(unspecifiedText));
                $dropdown.val("");
            }
            for (var i = 0; i < refDataList.length; i++) {
                if (useTitleAsValue == true) {
                    $dropdown.append($('<option > </option>').val(refDataList[i].Title).html(refDataList[i].Title));
                }
                else {
                    $dropdown.append($('<option > </option>').val(refDataList[i].ID).html(refDataList[i].Title));
                }
            }
            if (selectedValue != null)
                $dropdown.val(selectedValue);
        };
        AppBase.prototype.getMultiSelectionAsArray = function ($dropdown, defaultVal) {
            //convert multi select values to string array
            if ($dropdown.val() != null) {
                var selectedVals = $dropdown.val().toString();
                return selectedVals.split(",");
            }
            else {
                return defaultVal;
            }
        };
        AppBase.prototype.showProgressIndicator = function () {
            $("#progress-indicator").fadeIn('slow');
        };
        AppBase.prototype.hideProgressIndicator = function () {
            $("#progress-indicator").fadeOut();
        };
        AppBase.prototype.setElementAction = function (elementSelector, actionHandler) {
            $(elementSelector).unbind("click");
            $(elementSelector).unbind("touchstart");
            $(elementSelector).fastClick(function (event) {
                event.preventDefault();
                actionHandler();
            });
        };
        AppBase.prototype.isUserSignedIn = function () {
            if (this.apiClient.hasAuthorizationError == true) {
                return false;
            }
            var userInfo = this.getLoggedInUserInfo();
            if (userInfo.Username !== null && userInfo.Username !== "" && userInfo.SessionToken !== null && userInfo.SessionToken !== "") {
                return true;
            }
            else {
                return false;
            }
        };
        AppBase.prototype.getParameterFromURL = function (name, url) {
            var sval = "";
            if (url.indexOf("?") >= 0) {
                var params = url.substr(url.indexOf("?") + 1);
                params = params.split("&");
                var temp = "";
                // split param and value into individual pieces
                for (var i = 0; i < params.length; i++) {
                    temp = params[i].split("=");
                    if ([temp[0]] == name) {
                        sval = temp[1];
                    }
                }
            }
            return sval;
        };
        AppBase.prototype.showMessage = function (msg) {
            if (this.appState.isRunningUnderCordova && navigator.notification) {
                navigator.notification.alert(msg, // message
                function () { ; ; }, // callback
                'Open Charge Map', // title
                'OK' // buttonName
                );
            }
            else {
                bootbox.alert(msg, function () {
                });
            }
        };
        return AppBase;
    }(OCM.Base));
    OCM.AppBase = AppBase;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_AppBase.js.map