/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
/// <reference path="OCM_CommonUI.ts" />

var OCM;
(function (OCM) {
    (function (AppMode) {
        AppMode[AppMode["STANDARD"] = 0] = "STANDARD";
        AppMode[AppMode["LOCALDEV"] = 1] = "LOCALDEV";
        AppMode[AppMode["SANDBOXED"] = 2] = "SANDBOXED";
    })(OCM.AppMode || (OCM.AppMode = {}));
    var AppMode = OCM.AppMode;

    /** View Model for core functionality */
    var AppViewModel = (function () {
        function AppViewModel() {
            this.selectedPOI = null;
            this.poiList = null;
            this.favouritesList = null;
            this.searchPosition = null;

            this.resultsBatchID = -1;
        }
        return AppViewModel;
    })();
    OCM.AppViewModel = AppViewModel;

    /** App configuration settings */
    var AppConfig = (function () {
        function AppConfig() {
            this.autoRefreshMapResults = false;
            this.launchMapOnStartup = false;
            this.maxResults = 100;
            this.searchTimeoutMS = 20000;
            this.searchFrequencyMinMS = 500;
        }
        return AppConfig;
    })();
    OCM.AppConfig = AppConfig;

    /** App state settings*/
    var AppState = (function () {
        function AppState() {
            this.appMode = 0 /* STANDARD */;
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
    })();
    OCM.AppState = AppState;

    /** Base for App Components */
    var AppBase = (function (_super) {
        __extends(AppBase, _super);
        function AppBase() {
            _super.call(this);

            this.appState = new AppState();
            this.appConfig = new AppConfig();

            this.ocm_data = new OCM.API();
            this.ocm_geo = new OCM.Geolocation(this.ocm_data);
            this.mappingManager = new OCM.Mapping();

            this.viewModel = new AppViewModel();
        }
        AppBase.prototype.getLoggedInUserInfo = function () {
            var userInfo = {
                "Identifier": this.getCookie("Identifier"),
                "Username": this.getCookie("Username"),
                "SessionToken": this.getCookie("OCMSessionToken"),
                "Permissions": this.getCookie("AccessPermissions")
            };
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
                return this.ocm_data.getCachedDataObject("_pref_" + c_name);
            } else {
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
            if (typeof exdays === "undefined") { exdays = 1; }
            if (this.appState.isRunningUnderCordova) {
                this.ocm_data.setCachedDataObject("_pref_" + c_name, value);
            } else {
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
                this.ocm_data.setCachedDataObject("_pref_" + c_name, null);
            } else {
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
            if (typeof defaultToUnspecified === "undefined") { defaultToUnspecified = null; }
            if (typeof useTitleAsValue === "undefined") { useTitleAsValue = null; }
            if (typeof unspecifiedText === "undefined") { unspecifiedText = null; }
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
                } else {
                    $dropdown.append($('<option > </option>').val(refDataList[i].ID).html(refDataList[i].Title));
                }
            }

            if (selectedValue != null)
                $dropdown.val(selectedValue);
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
            if (this.ocm_data.hasAuthorizationError == true) {
                return false;
            }

            var userInfo = this.getLoggedInUserInfo();

            if (userInfo.Username !== null && userInfo.Username !== "" && userInfo.SessionToken !== null && userInfo.SessionToken !== "") {
                return true;
            } else {
                return false;
            }
        };

        AppBase.prototype.getParameterFromURL = function (name, url) {
            var sval = "";

            if (url.indexOf("?") >= 0) {
                var params = url.substr(url.indexOf("?") + 1);

                params = params.split("&");
                var temp = "";

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
                navigator.notification.alert(msg, function () {
                    ;
                    ;
                }, 'Open Charge Map', 'OK');
            } else {
                bootbox.alert(msg, function () {
                });
            }
        };
        return AppBase;
    })(OCM.Base);
    OCM.AppBase = AppBase;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_AppBase.js.map
