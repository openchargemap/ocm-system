var OCM;
(function (OCM) {
    var POI_SearchParams = (function () {
        function POI_SearchParams() {
            this.countryCode = null;
            this.latitude = null;
            this.longitude = null;
            this.locationTitle = null;
            this.distance = null;
            this.distanceUnit = null;
            this.connectionTypeID = null;
            this.operatorID = null;
            this.levelID = null;
            this.countryID = null;
            this.usageTypeID = null;
            this.statusTypeID = null;
            this.minPowerKW = null;
            this.submissionStatusTypeID = null;
            this.maxResults = 1000;
            this.additionalParams = null;
            this.includeComments = false;
            this.compact = true;
            this.enableCaching = true;
            this.levelOfDetail = 1;
            this.polyline = null;
            this.boundingbox = null;
        }
        return POI_SearchParams;
    }());
    OCM.POI_SearchParams = POI_SearchParams;
    ;
    var API = (function () {
        function API() {
            this.serviceBaseURL = "https://api.openchargemap.io/v2";
            this.serviceBaseURL_Standard = "https://api.openchargemap.io/v2";
            this.serviceBaseURL_Sandbox = "https://sandbox.api.openchargemap.io/v2";
            this.hasAuthorizationError = false;
            this.ATTRIBUTION_METADATAFIELDID = 4;
            this.clientName = "ocm.api.default";
            this.allowMirror = false;
        }
        API.prototype.fetchLocationDataList = function (countrycode, lat, lon, distance, distanceunit, maxresults, includecomments, callbackname, additionalparams, errorcallback) {
            if (countrycode === null)
                countrycode = "";
            if (additionalparams === null)
                additionalparams = "";
            if (!errorcallback)
                errorcallback = this.handleGeneralAjaxError;
            var apiCallURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&verbose=false&output=json&countrycode=" + countrycode + "&longitude=" + lon + "&latitude=" + lat + "&distance=" + distance + "&distanceunit=" + distanceunit + "&includecomments=" + includecomments + "&maxresults=" + maxresults + "&" + additionalparams;
            if (console) {
                console.log("API Call:" + apiCallURL);
            }
            $.ajax({
                type: "GET",
                url: apiCallURL + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            });
        };
        API.prototype.fetchLocationDataListByParam = function (params, callbackname, errorcallback) {
            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + (this.allowMirror ? " &allowmirror=true" : "") + "&verbose=false&output=json";
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
            if (params.locationTitle != null)
                serviceParams += "&locationtitle=" + params.locationTitle;
            if (params.minPowerKW != null)
                serviceParams += "&minpowerkw=" + params.minPowerKW;
            if (params.submissionStatusTypeID != null)
                serviceParams += "&submissionstatustypeid=" + params.submissionStatusTypeID;
            if (params.enableCaching == false)
                serviceParams += "&enablecaching=false";
            if (params.compact != null)
                serviceParams += "&compact=" + params.compact;
            if (params.levelOfDetail > 1)
                serviceParams += "&levelofdetail=" + params.levelOfDetail;
            if (params.polyline != null)
                serviceParams += "&polyline=" + params.polyline;
            if (params.boundingbox != null)
                serviceParams += "&boundingbox=" + params.boundingbox;
            if (params.additionalParams != null)
                serviceParams += "&" + params.additionalParams;
            if (!errorcallback)
                errorcallback = this.handleGeneralAjaxError;
            var apiCallURL = serviceURL + serviceParams;
            if (console) {
                console.log("API Call:" + apiCallURL + "&callback=" + callbackname);
            }
            var ajaxSettings = {
                type: "GET",
                url: apiCallURL + "&callback=" + callbackname,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: errorcallback
            };
            $.ajax(ajaxSettings);
        };
        API.prototype.fetchLocationById = function (id, callbackname, errorcallback, disableCaching) {
            var serviceURL = this.serviceBaseURL + "/poi/?client=" + this.clientName + "&output=json&includecomments=true&chargepointid=" + id;
            if (disableCaching)
                serviceURL += "&enablecaching=false";
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
            }
            else if (result.status == 401) {
                this.hasAuthorizationError = true;
                if (this.authorizationErrorCallback) {
                    this.authorizationErrorCallback();
                }
                else {
                    if (console)
                        console.log("Your session has expired. Please sign in again.");
                }
            }
            else {
                if (this.generalErrorCallback) {
                    this.generalErrorCallback();
                }
                else {
                    if (console)
                        console.log("There was a problem transferring data. Please check your internet connection.");
                }
            }
        };
        API.prototype.fetchCoreReferenceData = function (callbackname, authSessionInfo) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);
            var ajaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/referencedata/?client=" + this.clientName + "&output=json" + (this.allowMirror ? "&allowmirror=true" : "") + "&verbose=false&callback=" + callbackname + "&" + authInfoParams,
                jsonp: "false",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                error: this.handleGeneralAjaxError
            };
            $.ajax(ajaxSettings);
        };
        API.prototype.fetchGeocodeResult = function (address, successCallback, authSessionInfo, errorCallback) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);
            var ajaxSettings = {
                type: "GET",
                url: this.serviceBaseURL + "/geocode/?client=" + this.clientName + "&address=" + address + "&output=json&verbose=false&camelcase=true&" + authInfoParams,
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                success: successCallback,
                error: (errorCallback != null ? errorCallback : this.handleGeneralAjaxError)
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
                complete: function (jqXHR, textStatus) { completedCallback(jqXHR, textStatus); },
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
                success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
                crossDomain: true,
                error: failureCallback
            });
        };
        API.prototype.submitMediaItem = function (data, authSessionInfo, completedCallback, failureCallback, progressCallback) {
            var authInfoParams = this.getAuthParamsFromSessionInfo(authSessionInfo);
            $.ajax({
                url: this.serviceBaseURL + "/?client=" + this.clientName + "&action=mediaitem_submission" + authInfoParams,
                type: 'POST',
                xhr: function () {
                    var myXhr = $.ajaxSettings.xhr();
                    if (myXhr.upload) {
                        myXhr.upload.addEventListener('progress', progressCallback, false);
                    }
                    return myXhr;
                },
                success: function (result, textStatus, jqXHR) { completedCallback(jqXHR, textStatus); },
                error: (failureCallback == null ? this.handleGeneralAjaxError : failureCallback),
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
        API.prototype.hydrateCompactPOI = function (poi) {
            if (poi.DataProviderID != null && poi.DataProvider == null) {
                poi.DataProvider = this.getRefDataByID(this.referenceData.DataProviders, poi.DataProviderID);
            }
            if (poi.OperatorID != null && poi.OperatorInfo == null) {
                poi.OperatorInfo = this.getRefDataByID(this.referenceData.Operators, poi.OperatorID);
            }
            if (poi.UsageTypeID != null && poi.UsageType == null) {
                poi.UsageType = this.getRefDataByID(this.referenceData.UsageTypes, poi.UsageTypeID);
            }
            if (poi.AddressInfo.CountryID != null && poi.AddressInfo.Country == null) {
                poi.AddressInfo.Country = this.getRefDataByID(this.referenceData.Countries, poi.AddressInfo.CountryID);
            }
            if (poi.StatusTypeID != null && poi.StatusType == null) {
                poi.StatusType = this.getRefDataByID(this.referenceData.StatusTypes, poi.StatusTypeID);
            }
            if (poi.SubmissionStatusTypeID != null && poi.SubmissionStatusType == null) {
                poi.SubmissionStatusType = this.getRefDataByID(this.referenceData.SubmissionStatusTypes, poi.SubmissionStatusTypeID);
            }
            if (poi.Connections != null) {
                for (var c = 0; c < poi.Connections.length; c++) {
                    var conn = poi.Connections[c];
                    if (conn.ConnectionTypeID != null && conn.ConnectionType == null) {
                        conn.ConnectionType = this.getRefDataByID(this.referenceData.ConnectionTypes, conn.ConnectionTypeID);
                    }
                    if (conn.LevelID != null && conn.Level == null) {
                        conn.Level = this.getRefDataByID(this.referenceData.ChargerTypes, conn.LevelID);
                    }
                    if (conn.CurrentTypeID != null && conn.CurrentTypeID == null) {
                        conn.CurrentType = this.getRefDataByID(this.referenceData.CurrentTypes, conn.CurrentTypeID);
                    }
                    if (conn.StatusTypeID != null && conn.StatusTypeID == null) {
                        conn.StatusTypeID = this.getRefDataByID(this.referenceData.StatusTypes, conn.StatusTypeID);
                    }
                    poi.Connections[c] = conn;
                }
            }
            if (poi.UserComments != null) {
                for (var c = 0; c < poi.UserComments.length; c++) {
                    var comment = poi.UserComments[c];
                    if (comment.CommentType != null && comment.CommentTypeID == null) {
                        comment.CommentType = this.getRefDataByID(this.referenceData.CommentTypes, conn.CommentTypeID);
                    }
                    if (comment.CheckinStatusType != null && comment.CheckinStatusTypeID == null) {
                        comment.CheckinStatusTypeID = this.getRefDataByID(this.referenceData.CheckinStatusTypes, conn.CheckinStatusTypeID);
                    }
                    poi.UserComments[c] = comment;
                }
            }
            return poi;
        };
        API.prototype.hydrateCompactPOIList = function (poiList) {
            for (var i = 0; i < poiList.length; i++) {
                poiList[i] = this.hydrateCompactPOI(poiList[i]);
            }
            return poiList;
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
                }
                else {
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
    }());
    OCM.API = API;
})(OCM || (OCM = {}));
