/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

/// <reference path="OCM_CommonUI.ts" />

declare var escape: any;
declare var unescape: any; //TODO: replace with newer escaping methods
declare var bootbox: any;

module OCM {
    export enum AppMode {
        STANDARD,
        LOCALDEV,
        SANDBOXED
    }

    /** View Model for core functionality */
    export class AppViewModel {
        /** The current selected POI */
        public selectedPOI: any;

        /** The currently viewed set of POIs from latest search/favourites view */
        public poiList: Array<any>;

        /** last set of POIs from recent search */
        public searchPOIList: Array<any>;

        /** A set of POIs favourited by the user */
        public favouritesList: Array<any>;

        /** track changes in result set, avoiding duplicate processing (maps etc) */
        public resultsBatchID: number;

        public searchPosition: GeoPosition;

        public isCachedResults: boolean;

        constructor() {
            this.selectedPOI = null;
            this.poiList = null;
            this.favouritesList = null;
            this.searchPosition = null;

            this.resultsBatchID = -1;
            this.isCachedResults = false;
        }
    }

    /** App configuration settings */
    export class AppConfig {
        public launchMapOnStartup: boolean;
        public maxResults: number;
        public baseURL: string;
        public loginProviderRedirectBaseURL: string;
        public loginProviderRedirectURL: string;
        public autoRefreshMapResults: boolean;
        public newsHomeUrl: string;
        public searchTimeoutMS: number; //max time allowed before search considered timed out
        public searchFrequencyMinMS: number; //min ms between search requests

        constructor() {
            this.autoRefreshMapResults = false;
            this.launchMapOnStartup = false;
            this.maxResults = 100;
            this.searchTimeoutMS = 20000;
            this.searchFrequencyMinMS = 500;
        }
    }

    /** App state settings*/
    export class AppState {
        public appMode: AppMode;

        public isRunningUnderCordova: boolean;
        public isEmbeddedAppMode: boolean;
        public appInitialised: boolean;
        public languageCode: string;

        public isLocationEditMode: boolean;
        public menuDisplayed: boolean;
        public mapLaunched: boolean;
        public enableCommentSubmit: boolean;
        public isSearchInProgress: boolean;
        public isSearchRequested: boolean;
        public suppressSettingsSave: boolean;
        public isSubmissionInProgress: boolean;
        public _lastPageId: string;
        public _appQuitRequestCount: number;
        public _appPollForLogin: any;
        public _appSearchTimer: any;
        public _appSearchTimestamp: Date; //time of last search

        constructor() {
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
    }

    /** Base for App Components */
    export class AppBase extends Base {
        geolocationManager: OCM.Geolocation;
        apiClient: OCM.API;
        mappingManager: OCM.Mapping;

        viewModel: AppViewModel;
        appState: AppState;
        appConfig: AppConfig;

        constructor() {
            super();

            this.appState = new AppState();
            this.appConfig = new AppConfig();

            this.apiClient = new OCM.API();
            this.geolocationManager = new OCM.Geolocation(this.apiClient);
            this.mappingManager = new OCM.Mapping();

            this.viewModel = new AppViewModel();
        }

        getLoggedInUserInfo() {
            var userInfo = {
                "Identifier": this.getCookie("Identifier"),
                "Username": this.getCookie("Username"),
                "SessionToken": this.getCookie("OCMSessionToken"),
                "Permissions": this.getCookie("AccessPermissions")
            };
            return userInfo;
        }

        setLoggedInUserInfo(userInfo) {
            this.setCookie("Identifier", userInfo.Identifier);
            this.setCookie("Username", userInfo.Username);
            this.setCookie("OCMSessionToken", userInfo.SessionToken);
            this.setCookie("AccessPermissions", userInfo.AccessPermissions);
        }

        getCookie(c_name: string) {
            if (this.appState.isRunningUnderCordova) {
                return this.apiClient.getCachedDataObject("_pref_" + c_name);
            } else {
                //http://www.w3schools.com/js/js_cookies.asp
                var i, x, y, ARRcookies = document.cookie.split(";");
                for (i = 0; i < ARRcookies.length; i++) {
                    x = ARRcookies[i].substr(0, ARRcookies[i].indexOf("="));
                    y = ARRcookies[i].substr(ARRcookies[i].indexOf("=") + 1);
                    x = x.replace(/^\s+|\s+$/g, "");
                    if (x == c_name) {
                        var val = unescape(y);
                        if (val == "null") val = null;
                        return val;
                    }
                }
                return null;
            }
        }

        setCookie(c_name: string, value, exdays: number= 1) {
            if (this.appState.isRunningUnderCordova) {
                this.apiClient.setCachedDataObject("_pref_" + c_name, value);
            } else {
                if (exdays == null) exdays = 1;

                //http://www.w3schools.com/js/js_cookies.asp
                var exdate = new Date();
                exdate.setDate(exdate.getDate() + exdays);
                var c_value = escape(value) + "; expires=" + exdate.toUTCString();
                document.cookie = c_name + "=" + c_value;
            }
        }

        clearCookie(c_name: string) {
            if (this.appState.isRunningUnderCordova) {
                this.apiClient.setCachedDataObject("_pref_" + c_name, null);
            } else {
                var expires = new Date();
                expires.setUTCFullYear(expires.getUTCFullYear() - 1);
                document.cookie = c_name + '=; expires=' + expires.toUTCString() + '; path=/';
            }
        }

        getParameter(name: string) {
            //Get a parameter value from the document url
            name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
            var regexS = "[\\?&]" + name + "=([^&#]*)";
            var regex = new RegExp(regexS);
            var results = regex.exec(window.location.href);
            if (results == null)
                return "";
            else
                return results[1];
        }

        setDropdown(id, selectedValue) {
            if (selectedValue == null) selectedValue = "";
            var $dropdown = $("#" + id);
            $dropdown.val(selectedValue);
        }

        populateDropdown(id: any, refDataList: any, selectedValue: any, defaultToUnspecified: boolean= null, useTitleAsValue: boolean= null, unspecifiedText: string= null) {
            var $dropdown = $("#" + id);
            $('option', $dropdown).remove();

            if (defaultToUnspecified == true) {
                if (unspecifiedText == null) unspecifiedText = "Unknown";
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

            if (selectedValue != null) $dropdown.val(selectedValue);
        }

        getMultiSelectionAsArray($dropdown: JQuery, defaultVal: any) {
            //convert multi select values to string array
            if ($dropdown.val() != null) {
                var selectedVals = $dropdown.val().toString();
                return selectedVals.split(",");
            } else {
                return defaultVal;
            }
        }

        showProgressIndicator() {
            $("#progress-indicator").fadeIn('slow');
        }

        hideProgressIndicator() {
            $("#progress-indicator").fadeOut();
        }

        setElementAction(elementSelector, actionHandler) {
            $(elementSelector).unbind("click");
            $(elementSelector).unbind("touchstart");
            $(elementSelector).fastClick(function (event) {
                event.preventDefault();
                actionHandler();
            });
        }

        isUserSignedIn() {
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
        }

        getParameterFromURL(name, url) {
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
        }

        showMessage(msg) {
            if (this.appState.isRunningUnderCordova && navigator.notification) {
                navigator.notification.alert(
                    msg,  // message
                    function () { ;; },         // callback
                    'Open Charge Map',            // title
                    'OK'                  // buttonName
                    );
            } else {
                bootbox.alert(msg, function () {
                });
            }
        }
    }
}