/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
var OCM;
(function (OCM) {
    var POI_SearchParams = (function () {
        function POI_SearchParams() {
            this.countryCode = null;
            this.latitude = null;
            this.longitude = null;
            this.distance = null;
            this.distanceUnit = null;
            this.connectionTypeID = null;
            this.operatorID = null;
            this.levelID = null;
            this.countryID = null;
            this.usageTypeID = null;
            this.statusTypeID = null;
            this.submissionStatusTypeID = null;
            this.maxResults = 100;
            this.additionalParams = null;
            this.includeComments = false;
            this.enableCaching = true;
        }
        return POI_SearchParams;
    })();
    OCM.POI_SearchParams = POI_SearchParams;

    ;

    var API = (function () {
        function API() {
            this.serviceBaseURL = "http://api.openchargemap.io/v2";
            this.hasAuthorizationError = false;
            this.ATTRIBUTION_METADATAFIELDID = 4;
            this.clientName = "ocm.webapp";
        }
        API.prototype.fetchLocationDataList = function (countrycode, lat, lon, distance, distanceunit, maxresults, includecomments, callbackname, additionalparams, errorcallback) {
            if (countrycode === null)
                countrycode = "";
            if (additionalparams === null)
                additionalparams = "";

            if (!errorcallback)
                errorcallback = this.handleGeneralAjaxError;

            $.ajax({
                type: "GET",
                url: this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json&countrycode=" + countrycode + "&longitude=" + lon + "&latitude=" + lat + "&distance=" + distance + "&distanceunit=" + distanceunit + "&includecomments=" + includecomments + "&maxresults=" + maxresults + "&" + additionalparams + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            });
        };

        API.prototype.fetchLocationDataListByParam = function (params, callbackname, errorcallback) {
            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json";
            var serviceParams = "";
            if (params.countryCode != null)
                serviceParams += "&countrycode=" + params.countryCode;
            if (params.latitude != null)
                serviceParams += "&latitude=" + params.latitude;
            if (params.longitude != null)
                serviceParams += "&longitude=" + params.longitude;
            if (params.distance != null)
                serviceParams += "&distance=" + params.distance;
            if (params.distanceUnit != null)
                serviceParams += "&distanceunit=" + params.distanceUnit;
            if (params.includeComments != null)
                serviceParams += "&includecomments=" + params.includeComments;
            if (params.maxResults != null)
                serviceParams += "&maxresults=" + params.maxResults;
            if (params.countryID != null)
                serviceParams += "&countryid=" + params.countryID;
            if (params.levelID != null)
                serviceParams += "&levelid=" + params.levelID;
            if (params.connectionTypeID != null)
                serviceParams += "&connectiontypeid=" + params.connectionTypeID;
            if (params.operatorID != null)
                serviceParams += "&operatorid=" + params.operatorID;
            if (params.usageTypeID != null)
                serviceParams += "&usagetypeid=" + params.usageTypeID;
            if (params.statusTypeID != null)
                serviceParams += "&statustypeid=" + params.statusTypeID;
            if (params.submissionStatusTypeID != null)
                serviceParams += "&submissionstatustypeid=" + params.submissionStatusTypeID;

            if (params.enableCaching == false)
                serviceParams += "&enablecaching=false";
            if (params.additionalParams != null)
                serviceParams += "&" + params.additionalParams;

            if (!errorcallback)
                errorcallback = this.handleGeneralAjaxError;

            var ajaxSettings = {
                type: "GET",
                url: serviceURL + serviceParams + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            };

            $.ajax(ajaxSettings);
        };

        API.prototype.fetchLocationById = function (id, callbackname, errorcallback) {
            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&output=json&includecomments=true&chargepointid=" + id;
            if (!errorcallback)
                errorcallback = this.handleGeneralAjaxError;

            var ajaxSettings = {
                type: "GET",
                url: serviceURL + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            };

            $.ajax(ajaxSettings);
        };

        API.prototype.handleGeneralAjaxError = function (result, ajaxOptions, thrownError) {
            this.hasAuthorizationError = false;

            if (result.status == 200) {
                //all ok
            } else if (result.status == 401) {
                //unauthorised, user session has probably expired
                this.hasAuthorizationError = true;
                if (this.authorizationErrorCallback) {
                    this.authorizationErrorCallback();
                } else {
                    if (console)
                        console.log("Your session has expired. Please sign in again.");
                }
            } else {
                if (this.generalErrorCallback) {
                    this.generalErrorCallback();
                } else {
                    if (console)
                        console.log("There was a problem transferring data. Please check your internet connection.");
                }
            }
        };

        API.prototype.fetchCoreReferenceData = function (callbackname, authSessionInfo) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var ajaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/referencedata/?client=" + this.clientName + "&output=json&verbose=false&callback=" + callbackname + "&" + authInfoParams,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: this.handleGeneralAjaxError
            };

            $.ajax(ajaxSettings);
        };

        API.prototype.fetchGeocodeResult = function (address, successCallback, authSessionInfo) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var ajaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/geocode/?client=" + this.clientName + "&address=" + address + "&output=json&verbose=false&camelcase=true&" + authInfoParams,
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                success: successCallback,
                error: this.handleGeneralAjaxError
            };

            $.ajax(ajaxSettings);
        };

        API.prototype.getAuthParamsFromSessionInfo = function (authSessionInfo) {
            var authInfoParams = "";

            if (authSessionInfo != null) {
                if (authSessionInfo.Identifier != null)
                    authInfoParams += "&Identifier=" + authSessionInfo.Identifier;
                if (authSessionInfo.SessionToken != null)
                    authInfoParams += "&SessionToken=" + authSessionInfo.SessionToken;

                return authInfoParams;
            }
            return "";
        };

        API.prototype.submitLocation = function (data, authSessionInfo, completedCallback, failureCallback) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var jsonString = JSON.stringify(data);

            var ajaxSettings = {
                type: "POST",
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=cp_submission&format=json" + authInfoParams,
                data: jsonString,
                complete: function (jqXHR, textStatus) {
                    completedCallback(jqXHR, textStatus);
                },
                crossDomain: true,
                error: this.handleGeneralAjaxError
            };

            $.ajax(ajaxSettings);
        };

        API.prototype.submitUserComment = function (data, authSessionInfo, completedCallback, failureCallback) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var jsonString = JSON.stringify(data);

            $.ajax({
                type: "POST",
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=comment_submission&format=json" + authInfoParams,
                data: jsonString,
                success: function (result, textStatus, jqXHR) {
                    completedCallback(jqXHR, textStatus);
                },
                crossDomain: true,
                error: failureCallback
            });
        };

        API.prototype.submitMediaItem = function (data, authSessionInfo, completedCallback, failureCallback) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            $.ajax({
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=mediaitem_submission" + authInfoParams,
                type: 'POST',
                xhr: function () {
                    var myXhr = $.ajaxSettings.xhr();
                    if (myXhr.upload) {
                        //myXhr.upload.addEventListener('progress', progressHandlingFunction, false); // for handling the progress of the upload
                    }
                    return myXhr;
                },
                success: function (result, textStatus, jqXHR) {
                    completedCallback(jqXHR, textStatus);
                },
                error: this.handleGeneralAjaxError,
                data: data,
                cache: false,
                contentType: false,
                processData: false,
                crossDomain: true
            });
        };

        API.prototype.getRefDataByID = function (refDataList, id) {
            if (id != "")
                id = parseInt(id);

            if (refDataList != null) {
                for (var i = 0; i < refDataList.length; i++) {
                    if (refDataList[i].ID == id) {
                        return refDataList[i];
                    }
                }
            }
            return null;
        };

        API.prototype.sortCoreReferenceData = function () {
            //sort reference data lists
            this.sortReferenceData(this.referenceData.ConnectionTypes);
            this.sortReferenceData(this.referenceData.Countries);
            this.sortReferenceData(this.referenceData.Operators);
            this.sortReferenceData(this.referenceData.DataProviders);
            this.sortReferenceData(this.referenceData.UsageTypes);
            this.sortReferenceData(this.referenceData.StatusTypes);
            this.sortReferenceData(this.referenceData.CheckinStatusTypes);
        };

        API.prototype.sortReferenceData = function (sourceList) {
            sourceList.sort(this.sortListByTitle);
        };

        API.prototype.getMetadataValueByMetadataFieldID = function (metadataValues, id) {
            if (id != "")
                id = parseInt(id);

            if (metadataValues != null) {
                for (var i = 0; i < metadataValues.length; i++) {
                    if (metadataValues[i].ID == id) {
                        return metadataValues[i];
                    }
                }
            }
            return null;
        };

        API.prototype.sortListByTitle = function (a, b) {
            if (a.Title < b.Title)
                return -1;
            if (a.Title > b.Title)
                return 1;
            if (a.Title == b.Title)
                return 0;

            return 0;
        };

        API.prototype.isLocalStorageAvailable = function () {
            return typeof window.localStorage != 'undefined';
        };

        API.prototype.setCachedDataObject = function (itemName, itemValue) {
            if (this.isLocalStorageAvailable()) {
                if (typeof itemValue === 'undefined')
                    itemValue = null;
                if (itemValue === null) {
                    localStorage.removeItem(itemName);
                } else {
                    localStorage.setItem(itemName, JSON.stringify(itemValue));
                }
            }
        };

        API.prototype.getCachedDataObject = function (itemName) {
            if (this.isLocalStorageAvailable()) {
                var val = localStorage.getItem(itemName);
                if (val != null && val.length > 0) {
                    return JSON.parse(val);
                }
            }
            return null;
        };
        return API;
    })();
    OCM.API = API;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_Data.js.map
