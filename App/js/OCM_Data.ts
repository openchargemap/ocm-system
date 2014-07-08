/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />

module OCM {
    export class POI_SearchParams {
        constructor() { }
        public countryCode: string = null;
        public latitude: number = null;
        public longitude: number = null;
        public distance: number = null;
        public distanceUnit: string = null;
        public connectionTypeID: number = null;
        public operatorID: number = null;
        public levelID: number = null;
        public countryID: number = null;
        public usageTypeID: number = null;
        public statusTypeID: number = null;
        public submissionStatusTypeID: number = null;

        public maxResults: number = 100;
        public additionalParams: string = null;
        public includeComments: boolean = false;
        public enableCaching: boolean = true; //FIXME: need way for user to override cached data
    }

    export interface ConnectionInfo {
        ID: number;
        Reference: string;
        ConnectionType: any;
        StatusType: any;
        Level: any;
        CurrentType: any;
        Amps: number;
        Voltage: number;
        PowerKW: number;
        Quantity: number;
        Comments?: string;
    };

    export class API {

        public serviceBaseURL: string = "http://api.openchargemap.io/v2";
        public serviceBaseURL_Standard: string = "http://api.openchargemap.io/v2";
        public serviceBaseURL_Sandbox: string = "http://sandbox.api.openchargemap.io/v2";

        public hasAuthorizationError: boolean = false;

        public ATTRIBUTION_METADATAFIELDID = 4;
        public referenceData: any;
        public clientName: string = "ocm.api.default";

        public authorizationErrorCallback: any;
        public generalErrorCallback: any;
        public allowMirror: boolean = false;        

        fetchLocationDataList(countrycode, lat, lon, distance, distanceunit, maxresults, includecomments, callbackname, additionalparams, errorcallback) {

            if (countrycode === null) countrycode = "";
            if (additionalparams === null) additionalparams = "";

            if (!errorcallback) errorcallback = this.handleGeneralAjaxError;

            $.ajax({
                type: "GET",
                url: this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json&countrycode=" + countrycode + "&longitude=" + lon + "&latitude=" + lat + "&distance=" + distance + "&distanceunit=" + distanceunit + "&includecomments=" + includecomments + "&maxresults=" + maxresults + "&" + additionalparams + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            });
        }

        fetchLocationDataListByParam(params: OCM.POI_SearchParams, callbackname, errorcallback) {

            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + (this.allowMirror ? " &allowmirror=true" : "") + "&verbose=false&output=json";
            var serviceParams = "";
            if (params.countryCode != null) serviceParams += "&countrycode=" + params.countryCode;
            if (params.latitude != null) serviceParams += "&latitude=" + params.latitude;
            if (params.longitude != null) serviceParams += "&longitude=" + params.longitude;
            if (params.distance != null) serviceParams += "&distance=" + params.distance;
            if (params.distanceUnit != null) serviceParams += "&distanceunit=" + params.distanceUnit;
            if (params.includeComments != null) serviceParams += "&includecomments=" + params.includeComments;
            if (params.maxResults != null) serviceParams += "&maxresults=" + params.maxResults;
            if (params.countryID != null) serviceParams += "&countryid=" + params.countryID;
            if (params.levelID != null) serviceParams += "&levelid=" + params.levelID;
            if (params.connectionTypeID != null) serviceParams += "&connectiontypeid=" + params.connectionTypeID;
            if (params.operatorID != null) serviceParams += "&operatorid=" + params.operatorID;
            if (params.usageTypeID != null) serviceParams += "&usagetypeid=" + params.usageTypeID;
            if (params.statusTypeID != null) serviceParams += "&statustypeid=" + params.statusTypeID;
            if (params.submissionStatusTypeID != null) serviceParams += "&submissionstatustypeid=" + params.submissionStatusTypeID;

            if (params.enableCaching == false) serviceParams += "&enablecaching=false";
            if (params.additionalParams != null) serviceParams += "&" + params.additionalParams;

            if (!errorcallback) errorcallback = this.handleGeneralAjaxError;

            var ajaxSettings: JQueryAjaxSettings = {
                type: "GET",
                url: serviceURL + serviceParams + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            };

            $.ajax(ajaxSettings);
        }

        fetchLocationById(id, callbackname, errorcallback) {
            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&output=json&includecomments=true&chargepointid=" + id;
            if (!errorcallback) errorcallback = this.handleGeneralAjaxError;

            var ajaxSettings: JQueryAjaxSettings = {
                type: "GET",
                url: serviceURL + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            };

            $.ajax(ajaxSettings);
        }

        handleGeneralAjaxError(result, ajaxOptions, thrownError) {
            this.hasAuthorizationError = false;

            if (result.status == 200) {
                //all ok
            } else if (result.status == 401) {
                //unauthorised, user session has probably expired
                this.hasAuthorizationError = true;
                if (this.authorizationErrorCallback) {
                    this.authorizationErrorCallback();
                } else {
                    if (console) console.log("Your session has expired. Please sign in again.");
                }
            }
            else {
                if (this.generalErrorCallback) {
                    this.generalErrorCallback();
                } else {
                    if (console) console.log("There was a problem transferring data. Please check your internet connection.");
                }
            }
        }

        fetchCoreReferenceData(callbackname, authSessionInfo) {

            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var ajaxSettings: JQueryAjaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/referencedata/?client=" + this.clientName + "&output=json" + (this.allowMirror ? "&allowmirror=true" : "") + "&verbose=false&callback=" + callbackname + "&" + authInfoParams,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: this.handleGeneralAjaxError
            };

            $.ajax(ajaxSettings);
        }

        fetchGeocodeResult(address, successCallback, authSessionInfo, errorCallback) {

            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var ajaxSettings: JQueryAjaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/geocode/?client=" + this.clientName + "&address=" + address + "&output=json&verbose=false&camelcase=true&" + authInfoParams,
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                success: successCallback,
                error: (errorCallback!=null? errorCallback:this.handleGeneralAjaxError)
            };

            $.ajax(ajaxSettings);
        }

        getAuthParamsFromSessionInfo(authSessionInfo) {
            var authInfoParams = "";

            if (authSessionInfo != null) {
                if (authSessionInfo.Identifier != null) authInfoParams += "&Identifier=" + authSessionInfo.Identifier;
                if (authSessionInfo.SessionToken != null) authInfoParams += "&SessionToken=" + authSessionInfo.SessionToken;

                return authInfoParams;
            }
            return "";
        }

        submitLocation(data, authSessionInfo, completedCallback, failureCallback) {

            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var jsonString = JSON.stringify(data);

            var ajaxSettings: JQueryAjaxSettings = {
                type: "POST",
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=cp_submission&format=json" + authInfoParams,
                data: jsonString,
                complete: function (jqXHR, textStatus) { completedCallback(jqXHR, textStatus); },
                crossDomain: true,
                error: this.handleGeneralAjaxError
            };

            $.ajax(ajaxSettings);
        }

        submitUserComment(data, authSessionInfo, completedCallback, failureCallback) {

            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            var jsonString = JSON.stringify(data);

            $.ajax({
                type: "POST",
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=comment_submission&format=json" + authInfoParams,
                data: jsonString,
                success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
                crossDomain: true,
                error: failureCallback
            });
        }

        submitMediaItem(data, authSessionInfo, completedCallback, failureCallback, progressCallback) {

            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

            $.ajax({
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=mediaitem_submission" + authInfoParams,
                type: 'POST',
                xhr: function () {  // custom xhr
                    var myXhr = $.ajaxSettings.xhr();
                    if (myXhr.upload) { // check if upload property exists
                        myXhr.upload.addEventListener('progress', progressCallback, false); // for handling the progress of the upload
                    }
                    return myXhr;
                },
                success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
                error: (failureCallback==null?this.handleGeneralAjaxError: failureCallback),
                data: data,
                cache: false,
                contentType: false,
                processData: false,
                crossDomain: true
            });
        }

        getRefDataByID(refDataList, id) {
            if (id != "") id = parseInt(id);

            if (refDataList != null) {
                for (var i = 0; i < refDataList.length; i++) {
                    if (refDataList[i].ID == id) {
                        return refDataList[i];
                    }
                }
            }
            return null;
        }

        sortCoreReferenceData() {
            //sort reference data lists
            this.sortReferenceData(this.referenceData.ConnectionTypes);
            this.sortReferenceData(this.referenceData.Countries);
            this.sortReferenceData(this.referenceData.Operators);
            this.sortReferenceData(this.referenceData.DataProviders);
            this.sortReferenceData(this.referenceData.UsageTypes);
            this.sortReferenceData(this.referenceData.StatusTypes);
            this.sortReferenceData(this.referenceData.CheckinStatusTypes);
        }

        sortReferenceData(sourceList) {
            sourceList.sort(this.sortListByTitle);
        }


        getMetadataValueByMetadataFieldID(metadataValues, id) {
            if (id != "") id = parseInt(id);

            if (metadataValues != null) {
                for (var i = 0; i < metadataValues.length; i++) {
                    if (metadataValues[i].ID == id) {
                        return metadataValues[i];
                    }
                }
            }
            return null;
        }

        sortListByTitle(a, b) {
            if (a.Title < b.Title) return -1;
            if (a.Title > b.Title) return 1;
            if (a.Title == b.Title) return 0;

            return 0;
        }

        isLocalStorageAvailable() {
            return typeof window.localStorage != 'undefined';
        }

        setCachedDataObject(itemName, itemValue) {
            if (this.isLocalStorageAvailable()) {
                if (typeof itemValue === 'undefined') itemValue = null;
                if (itemValue === null) {
                    localStorage.removeItem(itemName);
                }
                else {
                    localStorage.setItem(itemName, JSON.stringify(itemValue));
                }

            }
        }

        getCachedDataObject(itemName) {
            if (this.isLocalStorageAvailable()) {
                var val = localStorage.getItem(itemName);
                if (val != null && val.length > 0) {
                    return JSON.parse(val);
                }
            }
            return null;
        }
    }
}