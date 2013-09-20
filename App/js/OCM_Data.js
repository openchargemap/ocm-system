//"use strict";

function OCM_LocationSearchParams() {
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

    this.clientName = "ocm.webapp";
}

function OCM_Data() {
    this.serviceBaseURL = "http://api.openchargemap.io/v2";
    this.hasAuthorizationError = false;

    this.ATTRIBUTION_METADATAFIELDID = 4;
}

OCM_Data.prototype.fetchLocationDataList = function (countrycode, lat, lon, distance, distanceunit, maxresults, includecomments, callbackname, additionalparams, errorcallback) {

    if (countrycode === null) countrycode = "";
    if (additionalparams === null) additionalparams = "";

    if (!errorcallback) errorcallback = this.handleGeneralAjaxError;

    $.ajax({
        type: "GET",
        url: this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json&countrycode=" + countrycode + "&longitude=" + lon + "&latitude=" + lat + "&distance=" + distance + "&distanceunit=" + distanceunit + "&includecomments=" + includecomments + "&maxresults=" + maxresults + "&" + additionalparams + "&callback=" + callbackname,
        jsonp: false,
        contentType: "application/json;",
        dataType: "jsonp",
        crossDomain: true,
        error: errorcallback
    });
};

OCM_Data.prototype.fetchLocationDataListByParam = function (params, callbackname, errorcallback) {

    var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json";
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

    $.ajax({
        type: "GET",
        url: serviceURL + serviceParams + "&callback=" + callbackname,
        jsonp: false,
        contentType: "application/json;",
        dataType: "jsonp",
        crossDomain: true,
        error: errorcallback
    });
};

OCM_Data.prototype.fetchLocationById = function (id, callbackname, errorcallback) {
    var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&output=json&includecomments=true&chargepointid=" + id;
    if (!errorcallback) errorcallback = this.handleGeneralAjaxError;

    $.ajax({
        type: "GET",
        url: serviceURL + "&callback=" + callbackname,
        jsonp: false,
        contentType: "application/json;",
        dataType: "jsonp",
        crossDomain: true,
        error: errorcallback
    });
};

OCM_Data.prototype.handleGeneralAjaxError = function (result, ajaxOptions, thrownError) {
    this.hasAuthorizationError = false;

    if (result.status == 200) {
        //all ok
    } else if (result.status == 401) {
        //unauthorised, user session has probably expired
        this.hasAuthorizationError = true;
        if (this.authorizationErrorCallback) {
            this.authorizationErrorCallback();
        } else {
            alert("Your session has expired. Please sign in again.");
        }
    }
    else {
        if (this.generalErrorCallback) {
            this.generalErrorCallback();
        } else {
            alert("There was a problem transferring data. Please check your internet connection.");
        }
    }
};

OCM_Data.prototype.fetchCoreReferenceData = function (callbackname, authSessionInfo) {

    var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

    $.ajax({
        type: "GET",
        url: this.serviceBaseURL + "/referencedata/?client=" + this.clientName + "&output=json&verbose=false&callback=" + callbackname+"&"+authInfoParams,
        jsonp: false,
        contentType: "application/json;",
        dataType: "jsonp",
        crossDomain: true,
        error: this.handleGeneralAjaxError
    });
};

OCM_Data.prototype.fetchGeocodeResult = function (address, successCallback, authSessionInfo) {

    var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

    $.ajax({
        type: "GET",
        url: this.serviceBaseURL + "/geocode/?client=" + this.clientName + "&address="+address+"&output=json&verbose=false&camelcase=true&" + authInfoParams,
        contentType: "application/json;",
        dataType: "jsonp",
        crossDomain: true,
        success: successCallback,
        error: this.handleGeneralAjaxError
    });
};

OCM_Data.prototype.getAuthParamsFromSessionInfo = function (authSessionInfo) {
    var authInfoParams = "";

    if (authSessionInfo != null) {
        if (authSessionInfo.Identifier != null) authInfoParams += "&Identifier=" + authSessionInfo.Identifier;
        if (authSessionInfo.SessionToken != null) authInfoParams += "&SessionToken=" + authSessionInfo.SessionToken;

        return authInfoParams;
    }
    return "";
};

OCM_Data.prototype.submitLocation = function (data, authSessionInfo, completedCallback, failureCallback) {

    var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

    var jsonString = JSON.stringify(data);

    $.ajax({
        type: "POST",
        url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=cp_submission&format=json" + authInfoParams,
        data: jsonString,
        complete: function (jqXHR, textStatus) { completedCallback(jqXHR, textStatus); },
        crossDomain: true,
        error: this.handleGeneralAjaxError
    });
};

OCM_Data.prototype.submitUserComment = function (data, authSessionInfo, completedCallback, failureCallback) {

    var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

    var jsonString = JSON.stringify(data);

    $.ajax({
        type: "POST",
        url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=comment_submission&format=json" + authInfoParams,
        data: jsonString,
        success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
        crossDomain: true,
        error: this.handleGeneralAjaxError
    });
};

OCM_Data.prototype.submitMediaItem = function (data, authSessionInfo, completedCallback, failureCallback) {

    var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);

    $.ajax({
        url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=mediaitem_submission" + authInfoParams,
        type: 'POST',
        xhr: function () {  // custom xhr
            var myXhr = $.ajaxSettings.xhr();
            if (myXhr.upload) { // check if upload property exists
                //myXhr.upload.addEventListener('progress', progressHandlingFunction, false); // for handling the progress of the upload
            }
            return myXhr;
        },
        success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
        error: this.handleGeneralAjaxError,
        data: data,
        cache: false,
        contentType: false,
        processData: false,
        crossDomain: true
    });

};

OCM_Data.prototype.getRefDataByID = function (refDataList, id) {
    if (id != "") id = parseInt(id);

    if (refDataList != null) {
        for (var i = 0; i < refDataList.length; i++) {
            if (refDataList[i].ID == id) {
                return refDataList[i];
            }
        }
    }
    return null;
};

OCM_Data.prototype.sortCoreReferenceData = function () {
    //sort reference data lists
    this.sortReferenceData(this.referenceData.ConnectionTypes);
    this.sortReferenceData(this.referenceData.Countries);
    this.sortReferenceData(this.referenceData.Operators);
    this.sortReferenceData(this.referenceData.DataProviders);
    this.sortReferenceData(this.referenceData.UsageTypes);
    this.sortReferenceData(this.referenceData.StatusTypes);
    this.sortReferenceData(this.referenceData.CheckinStatusTypes);
};

OCM_Data.prototype.sortReferenceData = function (sourceList) {
    sourceList.sort(this.sortListByTitle);
};


OCM_Data.prototype.getMetadataValueByMetadataFieldID = function (metadataValues, id) {
    if (id != "") id = parseInt(id);

    if (metadataValues != null) {
        for (var i = 0; i < metadataValues.length; i++) {
            if (metadataValues[i].ID == id) {
                return metadataValues[i];
            }
        }
    }
    return null;
};

OCM_Data.prototype.sortListByTitle = function (a, b) {
    if (a.Title < b.Title) return -1;
    if (a.Title > b.Title) return 1;
    if (a.Title == b.Title) return 0;

    return 0;
};

OCM_Data.prototype.isLocalStorageAvailable = function () {
    return typeof window.localStorage != 'undefined';
};

OCM_Data.prototype.setCachedDataObject = function (itemName, itemValue) {
    if (this.isLocalStorageAvailable()) {
        if (typeof itemValue === 'undefined') itemValue = null;
        if (itemValue === null) {
            localStorage.removeItem(itemName);
        }
        else {
            localStorage.setItem(itemName, JSON.stringify(itemValue));
        }

    }
};

OCM_Data.prototype.getCachedDataObject = function (itemName) {
    if (this.isLocalStorageAvailable()) {
        var val = localStorage.getItem(itemName);
        if (val != null && val.length > 0) {
            return JSON.parse(val);
        }
    }
    return null;
};