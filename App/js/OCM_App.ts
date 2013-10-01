
/*
OCM charging location browser/editor Mobile App
Christopher Cook
http://openchargemap.org

See http://www.openchargemap.org/ for more details
*/

/*jslint browser: true, devel: true, eqeq: true, white: true */
/*global define, $, window, document, OCM_CommonUI, OCM_Geolocation, OCM_Data, OCM_LocationSearchParams */

/*typescript*/
/// <reference path="TypeScriptReferences/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap.d.ts" />

//typescript declarations
declare var OCM_CommonUI;
declare var OCM_Geolocation;
declare var OCM_Data;
declare var localisation_dictionary;

function OCM_App() {
    this.ocm_ui = new OCM_CommonUI();
    this.ocm_geo = new OCM_Geolocation();
    this.ocm_data = new OCM_Data();
    this.numConnectionEditors = 5;
    this.maxResults = 100;
    this.locationList = null;
    this.resultBatchID = 1; //used to track changes in result sets, avoiding duplicate processing (maps etc)
    this.ocm_app_searchPos = null;

    this.currentInfoWindow = null;
    this.resultItemTemplate = null;

    this.selectedPOI = null;
    this.isLocationEditMode = false;

    this.baseURL = "http://openchargemap.org/app/";
    this.loginProviderRedirectBaseURL = "http://openchargemap.org/site/loginprovider/?_mode=silent&_forceLogin=true&_redirectURL=";
    this.loginProviderRedirectURL = this.loginProviderRedirectBaseURL + this.baseURL;
    this.enableExperimental = false;
    this.searchInProgress = false;
    this.enableCommentSubmit = true;
    this.appInitialised = false;
    this.renderingMode = "jqm";
    this.isRunningUnderCordova = false;
    this.mapAPI = "leaflet";
    this.ocm_app_context = this;
    this.ocm_data.clientName = "ocm.app.webapp";
    this.languageCode = "en";

    this.ocm_data.generalErrorCallback = $.proxy(this.showConnectionError);
    this.ocm_data.authorizationErrorCallback = $.proxy(this.showAuthorizationError);

    this.isLocalDevMode = false;
    if (this.isLocalDevMode == true) {
        this.baseURL = "http://localhost:81";
        this.loginProviderRedirectBaseURL = "http://localhost:81/site/loginprovider/?_mode=silent&_forceLogin=true&_redirectURL=";
        //this.ocm_data.serviceBaseURL = "http://localhost:8080/v2";
        this.ocm_data.serviceBaseURL = "http://localhost:81/api/v2";
        this.loginProviderRedirectURL = this.loginProviderRedirectBaseURL + this.baseURL;
    }
    this.ocm_geo.ocm_data = this.ocm_data;

}

OCM_App.prototype.logEvent = function (logMsg) {
    if (window.console) {
        window.console.log(logMsg);
    }
};

OCM_App.prototype.setElementAction = function (elementSelector, actionHandler) {

    $(elementSelector).unbind("click");
    $(elementSelector).unbind("touchstart");
    $(elementSelector).fastClick(function (event) {
        event.preventDefault();
        actionHandler();
    });
};

OCM_App.prototype.initApp = function () {

    var app = this.ocm_app_context;

    this.showProgressIndicator();
    this.appInitialised = true;

    this.ocm_ui.isRunningUnderCordova = this.isRunningUnderCordova;

    //wire up state tracking
    this.initStateTracking();

    //wire up button events

    this.setupUIActions();

    $('#intro-section').show();

    this.initEditors();

    //populate language options

    this.populateDropdown("option-language", this.ocm_data.languageList, this.languageCode);

    //load options settings from storage/cookies
    this.loadSettings();

    //when options change, store settings
    $('#search-distance').change(function () { app.storeSettings(); });
    $('#search-distance-unit').change(function () { app.storeSettings(); });
    $('#option-enable-experiments').change(function () { app.storeSettings(); });
    $('#option-language').change(function () {
        app.switchLanguage($("#option-language").val());
    });

    app.initDeferredUI();
};

OCM_App.prototype.setupUIActions = function () {

    var app = this;
    //running straight jquery, pages are hidden by default, show home screen
    $("#home-page").show();

    //add header classes to header elements
    $("[data-role='header']").addClass("ui-header");

    //set default back ui buttons handler
    app.setElementAction("a[data-rel='back']", function () {
        History.back();
    });
    //set home page ui link actions
    app.setElementAction("a[href='#search-page'],#map-listview", function () {
        app.navigateToSearch();
    });

    app.setElementAction("a[href='#map-page'],#search-map", function () {
        app.navigateToMap();
    });

    app.setElementAction("a[href='#addlocation-page'],#search-addlocation", function () {
        app.navigateToAddLocation();
    });

    app.setElementAction("a[href='#favourites-page'],#search-favourites", function () {
        app.navigateToFavourites();
    });

    app.setElementAction("a[href='#settings-page'],#search-settings", function () {
        app.navigateToSettings();
    });

    app.setElementAction("a[href='#about-page']", function () {
        app.navigateToAbout();
    });

    app.setElementAction("a[href='#login-page']", function () {
        app.navigateToLogin();
    });

    //set all back/home button ui actions
    app.setElementAction("a[href='#home-page']", function () {
        app.navigateToHome();
    });

    //search page button actions
    app.setElementAction("#search-nearby", function () {
        app.performSearch(true, false);
    });

    app.setElementAction("#search-button", function () {
        app.performSearch(false, true);
    });

    //details page ui actions
    app.setElementAction("#option-favourite", function () {
        app.toggleFavouritePOI(app.selectedPOI, null);
    });

    app.setElementAction("#option-edit, #details-edit", function () {
        app.navigateToEditLocation();
    });

    //comment/checkin ui actions
    app.setElementAction("#option-checkin, #btn-checkin", function () {
        app.navigateToAddComment();
    });

    app.setElementAction("#submitcomment-button", function () {
        app.performCommentSubmit();
    });

    //media item uploads
    app.setElementAction("#option-uploadphoto, #btn-uploadphoto", function () {
        app.navigateToAddMediaItem();
    });

    app.setElementAction("#submitmediaitem-button", function () {
        app.performMediaItemSubmit();
    });

};

OCM_App.prototype.postLoginInit = function () {
    var userInfo = this.getLoggedInUserInfo();

    //if user logged in, enable features
    if (!this.isUserSignedIn()) {
        //user is not signed in
        //$("#login-summary").html("Register and login (via your Twitter account, if you have one): <input type=\"button\" data-inline='true' value=\"Register or Sign In\" onclick='ocm_app.beginLogin();' />").trigger("create");
        $("#login-summary").html("<input type=\"button\" id=\"login-button\" class='btn btn-primary' data-mini=\"true\" data-icon=\"arrow-r\" value=\"Sign In\" onclick='ocm_app.beginLogin();'/>").trigger("create");
        $("#user-profile-info").html("You are not currently signed in.");
        $("#login-button ").show();
    } else {
        //user is signed in
        $("#user-profile-info").html("You are signed in as: " + userInfo.Username + " <input type=\"button\" data-mini=\"true\" class='btn btn-primary' value=\"Sign Out\" onclick='ocm_app.logout(true);' />").trigger("create");
        $("#login-button").hide();

    }

};

OCM_App.prototype.initDeferredUI = function () {
    this.logEvent("Init of deferred UI..");

    var app = this;

    //check if user sign in
    this.postLoginInit();

    if (this.isRunningUnderCordova) {
        navigator.splashscreen.hide();
    }

    if ($("#option-enable-experiments").val() == "on") {
        this.enableExperimental = true;
        $("#nav-items").append("<li><a href=\"#experiment-page\" data-icon=\"info\">Kapow!</a></li>").trigger("create");
        if ($.mobile) {
            $("#navbar").trigger("create");
            $("#navbar").navbar("refresh");


            $("#experiment-page").delegate('pageshow', function (event, ui) {
                app.initExperimentalContent();
            });
        }
    } else {
        this.enableExperimental = false;
    }

    //if cached results exist, render them
    var cachedResults = this.ocm_data.getCachedDataObject("SearchResults");
    var cachedResult_Location = this.ocm_data.getCachedDataObject("SearchResults_Location");

    if (cachedResults !== null) {
        if (cachedResult_Location !== null) {
            document.getElementById("search-location").value = cachedResult_Location;
        }
        setTimeout(function () {
            app.renderPOIList(cachedResults);
        }, 50);
    }

    //if ID of location passed in, show details view
    var idParam = app.getParameter("id");
    if (idParam !== null && idParam !== "") {

        var poiId = parseInt(app.getParameter("id"), 10);
        setTimeout(function () {
            app.showDetailsViewById(poiId);
        }, 100);
    }

    this.switchLanguage($("#option-language").val());
};

OCM_App.prototype.isUserSignedIn = function () {
    if (this.ocm_data.hasAuthorizationError == true) {
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

OCM_App.prototype.getParameterFromURL = function (name, url) {
    var sval = "";

    if (url.indexOf("?") >= 0) {
        var params = url.substr(url.indexOf("?") + 1);
        
        params = params.split("&");
        var temp = "";
        // split param and value into individual pieces
        for (var i = 0; i < params.length; i++) {
            temp = params[i].split("=");
            console.log("parse param:"+params[i]);
            if ([temp[0]] == name) {
                sval = temp[1];
                console.log("parse val:" + temp[1]);
            }
        }
    }
    return sval;
};

OCM_App.prototype.beginLogin = function () {
    this.showProgressIndicator();

    //reset previous authorization warnings
    this.ocm_data.isAuthorizationError = false;
    var app = this;

    if (this.isRunningUnderCordova) {
        //do phonegapped login using InAppBrowser
       
        var ref: any = window.open(this.loginProviderRedirectBaseURL + 'AppLogin?redirectWithToken=true', '_blank', 'location=yes');

        app.logEvent("OCM: adding loadstop events..");
        app.logEvent("OCM: "+ref);
        //attempt attach event listeners
        try {

            /*
            ref.addEventListener('loadstop', function (event) {
                
                app.logEvent('in loadstop: ' + event.url);

                app.hideProgressIndicator();

              
                
                //attempt to fetch from js injection
                setTimeout(function () {

                    app.logEvent('checking for login result: ' + event.url);
                        try {

                            ref.executeScript({
                                code: "if (getSignInResult) getSignInResult();"
                            }, function (result) {
                                    if (result != null) {
                                        ref.close();
                                        var userInfo = result[0];

                                        app.logEvent('got login: ' + userInfo.Username);

                                        app.setLoggedInUserInfo(userInfo);
                                        app.postLoginInit();

                                        //return to home
                                        app.navigateToHome();
                                    }
                                    else {
                                        app.logEvent('no sign in result received');
                                    }



                                });

                        } catch (err) {
                            app.logEvent("Ignoring phonegap error evaluation login script");
                        }
                    }, 1000);
                
            });
*/

            ref.addEventListener('loaderror', function (event) {
                app.logEvent('error: ' + event.message);
            });

            ref.addEventListener('loadstart', function (event) {
                app.logEvent('loadstart: ' + event.url);
                //attempt to fetch from url
                var url = event.url;
                var token = app.getParameterFromURL("OCMSessionToken", url);
                if (token.length>0) {
                    app.logEvent('OCM: Got a token ' + event.url);
                    var userInfo = {
                        "Identifier": app.getParameterFromURL("Identifier", url),
                        "Username": app.getParameterFromURL("Identifier", url),
                        "SessionToken": app.getParameterFromURL("OCMSessionToken", url),
                        "AccessToken": "",
                        "Permissions": app.getParameterFromURL("Permissions", url)
                    };

                    app.logEvent('got login: ' + userInfo.Username);

                    app.setLoggedInUserInfo(userInfo);
                    app.postLoginInit();

                    //return to home
                    app.navigateToHome();
                } else {
                    app.logEvent('OCM: Not got a token ' + event.url);
                }
            });

            ref.addEventListener('exit', function (event) {
                app.logEvent(event.type);
            });
        } catch (err) {
            app.logEvent("OCM: error adding inappbrowser events :" + err);
        }

        app.logEvent("OCM: events added..");
    }
    else {
        //do normal web login
        app.logEvent("OCM: not cordova, redirecting after login..");
        window.location = this.loginProviderRedirectURL;
    }
};

OCM_App.prototype.logout = function (navigateToHome) {
    var app = this;

    this.clearCookie("Identifier");
    this.clearCookie("Username");
    this.clearCookie("OCMSessionToken");
    this.clearCookie("AccessPermissions");

    if (navigateToHome == true) {
        app.postLoginInit(); //refresh signed in/out ui
        if (this.isRunningUnderCordova) {
            app.navigateToHome();
        }
        else {
            setTimeout(function () { window.location = app.baseURL; }, 100);
        }
    } else {
        app.postLoginInit(); //refresh signed in/out ui
    }

};

OCM_App.prototype.getLoggedInUserInfo = function () {
    var userInfo = {
        "Identifier": this.getCookie("Identifier"),
        "Username": this.getCookie("Username"),
        "SessionToken": this.getCookie("OCMSessionToken"),
        "Permissions": this.getCookie("AccessPermissions")
    };
    return userInfo;
};

OCM_App.prototype.setLoggedInUserInfo = function (userInfo) {

    this.setCookie("Identifier", userInfo.Identifier);
    this.setCookie("Username", userInfo.Username);
    this.setCookie("OCMSessionToken", userInfo.SessionToken);
    this.setCookie("AccessPermissions", userInfo.AccessPermissions);

};
OCM_App.prototype.storeSettings = function () {
    //save option settings to cookies
    this.setCookie("optsearchdist", $('#search-distance').val());
    this.setCookie("optsearchdistunit", $('#search-distance-unit').val());
    this.setCookie("optenableexperiments", $('#option-enable-experiments').val());

    //console.log("Saving language code:" + $('#option-language').val());
    this.setCookie("optlanguagecode", $('#option-language').val());
};

OCM_App.prototype.loadSettings = function () {
    if (this.getCookie("optsearchdist") != null) $('#search-distance').val(this.getCookie("optsearchdist"));
    if (this.getCookie("optsearchdistunit") != null) $('#search-distance-unit').val(this.getCookie("optsearchdistunit"));
    if (this.getCookie("optenableexperiments") != null) $('#option-enable-experiments').val(this.getCookie("optenableexperiments"));
    if (this.getCookie("optlanguagecode") != null) $('#option-language').val(this.getCookie("optlanguagecode"));
};

OCM_App.prototype.performCommentSubmit = function () {

    var app = this;
    if (app.enableCommentSubmit === true) {
        app.enableCommentSubmit = false;
        var refData = this.ocm_data.referenceData;
        var item = this.ocm_data.referenceData.UserComment;

        //collect form values
        item.ChargePointID = this.selectedPOI.ID;
        item.CheckinStatusType = this.ocm_data.getRefDataByID(refData.CheckinStatusTypes, $("#checkin-type").val());
        item.CommentType = this.ocm_data.getRefDataByID(refData.UserCommentTypes, $("#comment-type").val());
        item.UserName = $("#comment-username").val();
        item.Comment = $("#comment-text").val();
        item.Rating = $("#comment-rating").val();

        //show progress
        this.showProgressIndicator();

        //submit
        this.ocm_data.submitUserComment(item, this.getLoggedInUserInfo(), $.proxy(this.submissionCompleted, this), $.proxy(this.submissionFailed, this));
    }
};

OCM_App.prototype.performMediaItemSubmit = function () {

    var $fileupload = $(':file');
    var mediafile = $fileupload[0].files[0];
    var name, size, type;
    if (mediafile) {
        name = mediafile.name;
        size = mediafile.size;
        type = mediafile.type;
    }

    var formData = new FormData();

    formData.append("id", this.selectedPOI.ID);
    formData.append("comment", $("#comment").val());
    formData.append("mediafile", mediafile);

    //show progress
    this.showProgressIndicator();

    //submit
    this.ocm_data.submitMediaItem(formData, this.getLoggedInUserInfo(), $.proxy(this.submissionCompleted, this), $.proxy(this.submissionFailed, this));
};

OCM_App.prototype.submissionCompleted = function (jqXHR, textStatus) {
    this.logEvent("submission::" + textStatus);

    this.hideProgressIndicator();
    if (textStatus != "error") {
        this.showMessage("Thank you for your contribution, you may need to refresh your browser page for changes to appear. If approval is required your change may take 24hrs or more to show up. (Status Code: " + textStatus + ")");
        //navigate back to search page
        this.navigateToHome();

    } else {
        this.showMessage("Sorry, there was a problem accepting your submission. Please try again later. (Status Code: " + textStatus + "). If the problem persists try clearing your web browser cookies/cache.");
    }
};

OCM_App.prototype.submissionFailed = function () {
    this.hideProgressIndicator();
    this.showMessage("Sorry, there was an unexpected problem accepting your contribution. Please check your internet connection and try again later.");
};

OCM_App.prototype.performSearch = function (useClientLocation, useManualLocation) {

    //hide intro section if still displayed
    $("#intro-section").hide(); //("display", "none");

    //detect if mapping/geolocation available

    this.showProgressIndicator();

    if (useClientLocation == true) {
        //initiate client geolocation (if not already determined)
        if (this.ocm_geo.clientGeolocationPos == null) {
            this.ocm_geo.determineUserLocation($.proxy(
                this.determineUserLocationCompleted, this), $.proxy(
                    this.determineUserLocationFailed, this));
            return;
        } else {
            this.ocm_app_searchPos = this.ocm_geo.clientGeolocationPos;
        }
    }

    var distance = parseInt(document.getElementById("search-distance").value);
    var distance_unit = document.getElementById("search-distance-unit").value;

    if (this.ocm_app_searchPos == null || useManualLocation == true) {
        // search position not set, attempt fetch from location input and
        // return for now
        var locationText = document.getElementById("search-location").value;
        if (locationText === null || locationText == "") {
            //try to geolocate via browser location API
            this.ocm_geo.determineUserLocation($.proxy(
                this.determineUserLocationCompleted, this), $.proxy(
                    this.determineUserLocationFailed, this));
            return;
        } else {
            // try to gecode text location name, if new lookup not
            // attempted, continue to rendering
            var lookupAttempted = this.ocm_geo.determineGeocodedLocation(locationText, $.proxy(this.determineGeocodedLocationCompleted, this));
            if (lookupAttempted == true) {
                return;
            }
        }
    }

    if (this.ocm_app_searchPos != null && !this.searchInProgress) {
        this.searchInProgress = true;

        var params = new OCM_LocationSearchParams();
        params.latitude = this.ocm_app_searchPos.coords.latitude;
        params.longitude = this.ocm_app_searchPos.coords.longitude;
        params.distance = distance;
        params.distanceUnit = distance_unit;
        params.maxResults = this.maxResults;
        params.includeComments = true;

        //apply filter settings from UI
        if ($("#filter-submissionstatus").val() != 200)
            params.submissionStatusTypeID = $("#filter-submissionstatus")
                .val();
        if ($("#filter-connectiontype").val() != "")
            params.connectionTypeID = $("#filter-connectiontype").val();

        if ($("#filter-operator").val() != "")
            params.operatorID = $("#filter-operator").val();

        if ($("#filter-connectionlevel").val() != "")
            params.levelID = $("#filter-connectionlevel").val();

        if ($("#filter-usagetype").val() != "")
            params.usageTypeID = $("#filter-usagetype").val();

        if ($("#filter-statustype").val() != "")
            params.statusTypeID = $("#filter-statustype").val();

        this.logEvent("Performing search..");
        this.ocm_data.fetchLocationDataListByParam(params,
            "ocm_app.renderPOIList", this.handleSearchError);
    }

};

OCM_App.prototype.handleSearchError = function (result) {
    if (result.status == 200) {
        //all ok
    } else {
        this.showMessage("There was a problem performing your search. Please check your internet connection.");
    }
};

OCM_App.prototype.determineUserLocationCompleted = function (pos) {
    this.ocm_app_searchPos = pos;
    this.ocm_geo.clientGeolocationPos = pos;
    this.performSearch();
};

OCM_App.prototype.determineUserLocationFailed = function () {
    this.hideProgressIndicator();
    this.showMessage("Could not automatically determine your location. Search by location name instead.");
};

OCM_App.prototype.determineGeocodedLocationCompleted = function (pos) {
    this.ocm_app_searchPos = pos;
    this.performSearch();
};

OCM_App.prototype.renderPOIList = function (locationList) {

    this.resultBatchID++; //indicates that results have changed and need reprocessed (maps etc)

    if (locationList != null && locationList.length > 0) {
        this.logEvent("Caching search results..");
        this.ocm_data.setCachedDataObject("SearchResults", locationList);
        this.ocm_data.setCachedDataObject("SearchResults_Location", document.getElementById("search-location").value);
    } else {
        this.logEvent("No search results, will not overwrite cached search results.");
    }

    this.logEvent("Rendering search results..");
    $("#search-no-data").hide();

    this.hideProgressIndicator();
    this.searchInProgress = false;

    var appContext = this;
    this.locationList = locationList;

    //var $listContent = $('#results-list');
    var $listContent = $('<div id="results-list" class="results-list"></div>');
    if (this.resultItemTemplate == null) this.resultItemTemplate = $("#results-item-template").html();

    $listContent.children().remove();

    if (this.locationList == null || this.locationList.length == 0) {
        var $content = $("<div class=\"section-heading\"><p><span class=\"ui-li-count\">0 Results match your search</span></p></div>");
        $listContent.append($content);
    }
    else {

        var distUnitPref = document.getElementById("search-distance-unit");
        var distance_unit = "Miles";
        if (distUnitPref != null) distance_unit = distUnitPref.value;

        var $resultCount = $("<div class=\"section-heading\">The following " + locationList.length + " locations match your search</div>");

        $listContent.append($resultCount);

        var isAlternate = false;
        for (var i = 0; i < this.locationList.length; i++) {
            var poi = this.locationList[i];
            var distance = poi.AddressInfo.Distance;

            var addressHTML = this.ocm_ui.formatPOIAddress(poi);

            var contactHTML = "";
            contactHTML += this.ocm_ui.formatPhone(poi.AddressInfo.ContactTelephone1, "Tel.");

            var itemTemplate = "<div class='result'>" + this.resultItemTemplate + "</div>";
            var locTitle = poi.AddressInfo.Title;
            if (poi.AddressInfo.Town != null && poi.AddressInfo.Town != "") locTitle = poi.AddressInfo.Town + " - " + locTitle;

            itemTemplate = itemTemplate.replace("{locationtitle}", locTitle);
            itemTemplate = itemTemplate.replace("{location}", addressHTML);
            itemTemplate = itemTemplate.replace("{comments}", "");


            var direction = "";

            if (this.ocm_app_searchPos != null) direction = this.ocm_geo.getCardinalDirectionFromBearing(this.ocm_geo.getBearing(this.ocm_app_searchPos.coords.latitude, this.ocm_app_searchPos.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));

            itemTemplate = itemTemplate.replace("{distance}", distance.toFixed(1));
            itemTemplate = itemTemplate.replace("{distanceunit}", distance_unit + (direction != "" ? " <span title='" + direction + "' class='direction-" + direction + "'>&nbsp;&nbsp;</span>" : ""));

            var statusInfo = "";
            if (poi.UsageType != null) {
                statusInfo += "<strong>" + poi.UsageType.Title + "</strong><br/>";
            }

            if (poi.StatusType != null) {
                statusInfo += poi.StatusType.Title;
            }

            var maxLevel = null;
            if (poi.Connections != null) {
                if (poi.Connections.length > 0) {

                    for (var c = 0; c < poi.Connections.length; c++) {
                        var con = poi.Connections[c];
                        if (con.Level != null) {
                            if (maxLevel == null) {
                                maxLevel = con.Level;
                            }
                            else {
                                if (con.Level.ID > maxLevel.ID) maxLevel = con.Level;
                            }
                        }
                    }
                }
            }

            if (maxLevel != null) {
                statusInfo += "<br/>" + maxLevel.Title + "";
            }

            itemTemplate = itemTemplate.replace("{status}", statusInfo);

            var $item = $(itemTemplate);
            if (isAlternate) $item.addClass("alternate");

            $item.on('click', { poi: poi }, function (e) {
                e.preventDefault();
                e.stopPropagation();

                //todo: rename as prepareDetailsView
                try {
                    appContext.showDetailsView(this, e.data.poi);
                } catch (err) {
                }
                appContext.showPage("locationdetails-page")
            });

            $listContent.append($item);

            isAlternate = !isAlternate;
        }
    }

    //show hidden results ui

    $('#results-list').replaceWith($listContent);
    $("#results-list").css("display", "block");
};

OCM_App.prototype.showDetailsViewById = function (id) {
    var itemShown = false;
    //if id in current result list, show
    if (this.locationList != null) {
        for (var i = 0; i < this.locationList.length; i++) {
            if (this.locationList[i].ID == id) {
                this.showDetailsView(document.getElementById("content-placeholder"), this.locationList[i]);
                //if ($.mobile) $.mobile.changePage("#locationdetails-page");
                itemShown = true;
            }

            if (itemShown) break;
        }
    }

    if (!itemShown) {
        //load poi details, then show
        this.logEvent("Location not cached, fetching details:" + id);
        this.ocm_data.fetchLocationById(id, "ocm_app.showDetailsFromList", null);
    }
};

OCM_App.prototype.showDetailsFromList = function (results) {
    var app = this;
    if (results.length > 0) {
        setTimeout(function () {
            app.showDetailsView(document.getElementById("content-placeholder"), results[0]);
            //if ($.mobile) $.mobile.changePage("#locationdetails-page");
        }, 50);
    } else {
        this.showMessage("The location you are attempting to view does not exist or has been removed.");
    }
};

OCM_App.prototype.showDetailsView = function (element, poi) {

    this.selectedPOI = poi;

    this.logEvent("Showing OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

    if (this.isFavouritePOI(poi, null)) {
        $("#option-favourite").removeClass("icon-heart-empty");
        $("#option-favourite").addClass("icon-heart");

    } else {
        $("#option-favourite").removeClass("icon-heart");
        $("#option-favourite").addClass("icon-heart-empty");
    }

    //TODO: bug/ref data load when editor opens clears settings

    var $element = $(element);
    var $detailsView = $("#locationdetails-view");
    $detailsView.css("width", "90%");
    $detailsView.css("display", "block");

    //populate details view
    var poiDetails = this.ocm_ui.formatPOIDetails(poi);

    $("#details-locationtitle").html(poi.AddressInfo.Title);
    $("#details-address").html(poiDetails.address);
    $("#details-contact").html(poiDetails.contactInfo);
    $("#details-additional").html(poiDetails.additionalInfo);
    $("#details-advanced").html(poiDetails.advancedInfo);

    this.ocm_ui.showPOIOnStaticMap("details-map", poi, true, true);

    var streetviewUrl = "http://maps.googleapis.com/maps/api/streetview?size=192x96&location=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&fov=90&heading=0&pitch=0&sensor=false";
    var streetViewLink = "https://maps.google.com/maps?q=&layer=c&cbll=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&cbp=11,0,0,0,0";
    $("#details-streetview").html("<a href='#' onclick=\"window.open('" + streetViewLink + "', '_system');\"><img src=\"" + streetviewUrl + "\" width=\"192\" height=\"96\" title=\"Approximate Streetview (if available): " + poi.AddressInfo.Title + "\" /></a>");

    if (poi.UserComments != null) {
        var commentOutput = "<div class='comments'>";

        for (var c = 0; c < poi.UserComments.length; c++) {
            var comment = poi.UserComments[c];
            var commentDate = this.ocm_ui.fixJSONDate(comment.DateCreated);
            commentOutput +=
            "<blockquote>" +
            "<p>" + (comment.Rating != null ? "<strong>Rating: " + comment.Rating + " out of 5</strong> : " : "") + (comment.Comment != null ? comment.Comment : "(No Comment)") + "</p> " +
            "<small><cite>" + (comment.CommentType != null ? "[" + comment.CommentType.Title + "] " : "") + ((comment.UserName != null && comment.UserName != "") ? comment.UserName : "(Anonymous)") + " : " + commentDate.toLocaleDateString() +
            "<em>" +

            (comment.CheckinStatusType != null ? " " + comment.CheckinStatusType.Title : "") +
            "</em> </cite></small></blockquote>";
        }
        commentOutput += "</div>";

        $("#details-usercomments").html(commentOutput);

    } else {
        $("#details-usercomments").html("No comments submitted.");
    }

    if (poi.MediaItems != null) {
        var mediaItemOutput = "<div class='comments'>";

        for (var c = 0; c < poi.MediaItems.length; c++) {
            var mediaItem = poi.MediaItems[c];
            if (mediaItem.IsEnabled == true) {
                var itemDate = this.ocm_ui.fixJSONDate(mediaItem.DateCreated);
                mediaItemOutput += "<blockquote><div style='float:left;padding-right:0.3em;'><a href='" + mediaItem.ItemURL + "' target='_blank'><img src='" + mediaItem.ItemThumbnailURL + "'/></a></div><p>" + (mediaItem.Comment != null ? mediaItem.Comment : "(No Comment)") + "</p> " +
                "<small><cite>" + ((mediaItem.User != null) ? mediaItem.User.Username : "(Anonymous)") + " : " + itemDate.toLocaleDateString() + "</cite></small>" +
                "</blockquote>";
            }
        }

        mediaItemOutput += "</div>";

        $("#details-mediaitems").html(mediaItemOutput);

    } else {
        $("#details-mediaitems").html("No photos submitted.");
    }

    var leftPos = $element.position().left;
    var topPos = $element.position().top;
    $detailsView.css("left", leftPos);
    $detailsView.css("top", topPos);

    //general mobile related refresh/recreate widget events
    if ($.mobile) {
        try {
            $("#details-locationtitle").trigger("create");
            $("#details-content").trigger("create");
            $("#details-usercomments").trigger("create");
        } catch (exp) { }

        try {
            $("#details-usercomments").listview("refresh");
            $("#details-general").trigger("create");
        } catch (exp) { }
    }

    //once displayed, try fetching a more accurate distance estimate
    if (this.ocm_app_searchPos != null) {
        this.ocm_geo.getDrivingDistanceBetweenPoints(this.ocm_app_searchPos.coords.latitude, this.ocm_app_searchPos.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude, $("#search-distance-unit").val(), this.updateDistanceDetails);
    }

    //apply translations (if required)
    if (this.languageCode != null) {
        this.ocm_ui.applyLocalisation(false);
    }
};

OCM_App.prototype.updateDistanceDetails = function (response, status) {
    if (response != null) {
        var result = response.rows[0].elements[0];

        if (result.status == "OK") {
            var origin = response.originAddresses[0];
            $("#addr_distance").after(" - driving distance: " + result.distance.text + " (" + result.duration.text + ") from " + origin);
        }
    }
};

OCM_App.prototype.isFavouritePOI = function (poi, itineraryName) {
    if (poi != null) {
        var favouriteLocations = this.ocm_data.getCachedDataObject("favouritePOIList");
        if (favouriteLocations != null) {
            for (var i = 0; i < favouriteLocations.length; i++) {
                if (favouriteLocations[i].poi.ID == poi.ID && (favouriteLocations[i].itineraryName == itineraryName || (itineraryName == null && favouriteLocations[i].itineraryName == null))) {
                    return true;
                }
            }
        }
    }
    return false;
};

OCM_App.prototype.addFavouritePOI = function (poi, itineraryName) {
    if (poi != null) {
        if (!this.isFavouritePOI(poi, itineraryName)) {
            var favouriteLocations = this.ocm_data.getCachedDataObject("favouritePOIList");
            if (favouriteLocations == null) {
                favouriteLocations = new Array();
            }

            if (itineraryName != null) {
                favouriteLocations.push({ "poi": poi, "itineraryName": itineraryName }); //add to specific itinerary
            }
            favouriteLocations.push({ "poi": poi, "itineraryName": null }); // add to 'all' list

            this.logEvent("Added Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

            this.ocm_data.setCachedDataObject("favouritePOIList", favouriteLocations);
        } else {
            this.logEvent("Already exists: Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
        }
    }
};

OCM_App.prototype.removeFavouritePOI = function (poi, itineraryName) {
    if (poi != null) {
        if (this.isFavouritePOI(poi, itineraryName)) {
            var favouriteLocations = this.ocm_data.getCachedDataObject("favouritePOIList");
            if (favouriteLocations == null) {
                favouriteLocations = new Array();
            }

            var newFavList = new Array();
            for (var i = 0; i < favouriteLocations.length; i++) {
                if (favouriteLocations[i].poi.ID == poi.ID && favouriteLocations[i].itineraryName == itineraryName) {
                    //skip item
                } else {
                    newFavList.push(favouriteLocations[i]);
                }
            }
            this.ocm_data.setCachedDataObject("favouritePOIList", newFavList);
            this.logEvent("Removed Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

        } else {
            this.logEvent("Cannot remove: Not a Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
        }
    }
};

OCM_App.prototype.toggleFavouritePOI = function (poi, itineraryName) {
    if (poi != null) {
        if (this.isFavouritePOI(poi, itineraryName)) {
            this.removeFavouritePOI(poi, itineraryName);
            $("#option-favourite").removeClass("icon-heart");
            $("#option-favourite").addClass("icon-heart-empty");
        } else {
            this.addFavouritePOI(poi, itineraryName);
            $("#option-favourite").removeClass("icon-heart-empty");
            $("#option-favourite").addClass("icon-heart");
        }
    }
};

OCM_App.prototype.getFavouritePOIList = function (itineraryName) {
    var favouriteLocations = this.ocm_data.getCachedDataObject("favouritePOIList");
    var poiList = new Array();
    if (favouriteLocations != null) {
        for (var i = 0; i < favouriteLocations.length; i++) {
            if (favouriteLocations[i].itineraryName == itineraryName) {
                poiList.push(favouriteLocations[i].poi);
            }
        }
    }
    return poiList;
};

OCM_App.prototype.refreshMapView = function () {

    this.ocm_ui.mapOptions.enableClustering = true;

    var $resultcount = $("#map-view-resultcount");

    if (this.locationList != null && this.locationList.length > 0) {
        $resultcount.html(this.locationList.length + " Results");
    } else {
        $resultcount.html("No Search Results");
    }

    if (this.mapAPI == "google") {
        if (typeof (google) == "undefined") {
            //no google maps currently available
            $("#map-view").html("Google maps cannot be loaded. Please check your data connection.");
            return;
        }

        //setup map view if not already initialised
        this.ocm_ui.initMapGoogle("map-view");

        this.ocm_ui.showPOIListOnMapViewGoogle("map-view", this.locationList, this, $resultcount, this.resultBatchID);
    }

    if (this.mapAPI == "leaflet") {

        //setup map view, tracking user pos, if not already initialised
        //TODO: use last search pos as lat/lng, or first item in locationList
        var centreLat = 50;
        var centreLng = 50;
        if (this.locationList != null && this.locationList.length > 0) {
            centreLat = this.locationList[0].AddressInfo.Latitude;
            centreLng = this.locationList[0].AddressInfo.Longitude;
        }
        this.ocm_ui.initMapLeaflet("map-view", centreLat, centreLng, false);

        this.ocm_ui.showPOIListOnMapViewLeaflet("map-view", this.locationList, this, $resultcount, this.resultBatchID);
    }
};

OCM_App.prototype.hasUserPermissionForPOI = function (poi, permissionLevel) {

    var userInfo = this.getLoggedInUserInfo();
    if (userInfo.Permissions != null) {
        if (userInfo.Permissions.indexOf("[Administrator=true]") != -1) {
            return true;
        }

        if (permissionLevel == "Edit") {
            //check if user has country level or all countries edit permission
            if (userInfo.Permissions.indexOf("[CountryLevel_Editor=All]") != -1) {
                return true;
            }
            if (poi.AddressInfo.Country != null) {
                if (userInfo.Permissions.indexOf("[CountryLevel_Editor=" + poi.AddressInfo.Country.ID + "]") != -1) {
                    return true;
                }
            }
        }
    }
    return false;
};

OCM_App.prototype.initExperimentalContent = function () {
    //render/init current experiments
    var output = "";
    if (this.locationList != null) {
        for (var l = 0; l < this.locationList.length; l++) {
            var poi = this.locationList[l];
            var streeviewUrl = "http://maps.googleapis.com/maps/api/streetview?size=256x128&location=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&fov=90&heading=0&pitch=0&sensor=false";

            output += "<img alt=\"" + poi.AddressInfo.Title + "\" title=\"" + poi.AddressInfo.Title + "\" src=\"" + streeviewUrl + "\"/>";
        }
    }
    $("#experiment-output").html(output).trigger("create");

};

OCM_App.prototype.switchLanguage = function (languageCode) {

    this.logEvent("Switching UI language: " + languageCode);

    this.languageCode = languageCode;

    localisation_dictionary = eval("localisation_dictionary_" + languageCode);

    //apply translations
    this.ocm_ui.applyLocalisation(false);

    //store language preference
    this.storeSettings();
};

OCM_App.prototype.hidePage = function (pageId) {
    $("#" + pageId).hide();
}

OCM_App.prototype.showPage = function (pageId, pageTitle, skipState) {
    if (!pageTitle) pageTitle = pageId;

    this.logEvent("app.showPage:"+pageId);
    //show new page

    //hide last shown page
    if (this._lastPageId && this._lastPageId != null) {
        this.hidePage(this._lastPageId);
    }
    //hide home page
    document.getElementById("home-page").style.display = "none";
    document.getElementById(pageId).style.display = "block";
    //$("#home-page").hide();

    //show new page
    //$("#" + pageId).show();

    //hack: reset scroll position for new page once page has had a chance to render
    setTimeout(function () { document.documentElement.scrollIntoView(); }, 100);


    this._lastPageId = pageId;

    if (skipState) {
        //skip storage of current state
    } else {
        this.History.pushState({ view: pageId, title: pageTitle }, pageTitle, "?view=" + pageId);
    }

    this.logEvent("leaving app.showPage:" + pageId);
}

OCM_App.prototype.initStateTracking = function () {
    var app = this;
    // Check Location
    if (document.location.protocol === 'file:') {
        //state not supported
    }

    // Establish Variables
    this.History = window.History; // Note: We are using a capital H instead of a lower h

    var History = this.History;
    var State = History.getState();

    // Log Initial State
    State.data.view = "home-page";
    History.log('initial:', State.data, State.title, State.url);

    // Bind to State Change
    History.Adapter.bind(window, 'statechange', function () { // Note: We are using statechange instead of popstate
        // Log the State
        var State = History.getState(); // Note: We are using History.getState() instead of event.state
        //History.log('statechange:', State.data, State.title, State.url);

        // app.logEvent("state switch to :" + State.data.view);
        if (State.data.view) {
            if (app._lastPageId && app._lastPageId != null) {
                if (State.data.view == app._lastPageId) return;//dont show same page twice
            }
            app.showPage(State.data.view, State.Title, true);
        } else {
            app.showPage("home-page", "Home");
        }
    });
};

//methods for use by native build of app to enable navigation via native UI/phonegap
OCM_App.prototype.navigateToSearch = function () {
    this.logEvent("Navigate To: Search Page");
    this.hidePage("home-page");
    this.showPage("search-page", "Search");
};

OCM_App.prototype.navigateToHome = function () {
    this.logEvent("Navigate To: Home Page");
    this.showPage("home-page", "Home");
};

OCM_App.prototype.navigateToMap = function () {
    this.logEvent("Navigate To: Map Page");

    this.showPage("map-page", "Map");
    var app = this;
    setTimeout(function () { app.refreshMapView(); }, 250);

};

OCM_App.prototype.navigateToFavourites = function () {
    this.logEvent("Navigate To: Favourites");
    var app = this;
    //get list of favourites as POI list and render in standard search page
    var favouritesList = app.getFavouritePOIList();
    if (favouritesList === null || favouritesList.length === 0) {
        $("#favourites-list").html("<p>You have no favourite locations set. To add or remove a favourite, tap the <i title=\"Toggle Favourite\" class=\"icon-heart-empty\"></i> icon when viewing a location.</p>");
        this.showPage("favourites-page", "Favourites");
    } else {

        app.renderPOIList(favouritesList);

        //TODO: manage favourites in single page?

        //show favourites on search page
        this.navigateToSearch();
    }
};

OCM_App.prototype.navigateToAddLocation = function () {
    this.logEvent("Navigate To: Add Location");
    var app = this;
    if (app.isUserSignedIn()) {
        app.isLocationEditMode = false;
        app.selectedPOI = null;
        app.showLocationEditor();
        this.showPage("editlocation-page", "Add Location");

    } else {
        app.showMessage("Please Sign In before adding a location.");
    }
};

OCM_App.prototype.navigateToEditLocation = function () {
    this.logEvent("Navigate To: Edit Location");
    var app = this;

    if (app.isUserSignedIn()) {
        //show editor
        app.showLocationEditor();
        app.showPage("editlocation-page", "Edit Location");
    } else {
        app.showMessage("Please Sign In before editing.");
    }
};

OCM_App.prototype.navigateToLogin = function () {
    this.logEvent("Navigate To: Login");
    this.showPage("login-page", "Sign In");
};

OCM_App.prototype.navigateToSettings = function () {
    this.logEvent("Navigate To: Settings");
    this.showPage("settings-page", "Settings");
};

OCM_App.prototype.navigateToAbout = function () {
    this.logEvent("Navigate To: About");
    this.showPage("about-page", "About");
};

OCM_App.prototype.navigateToAddComment = function () {
    this.logEvent("Navigate To: Add Comment");

    var app = this;

    //reset comment form on show
    document.getElementById("comment-form").reset();
    app.enableCommentSubmit = true;

    if (app.isUserSignedIn()) {
        //show checkin/comment page
        this.showPage("submitcomment-page", "Add Comment");
    } else {
        app.showMessage("Please Sign In");
    }
}

OCM_App.prototype.navigateToAddMediaItem = function () {
    this.logEvent("Navigate To: Add Media Item");

    var app = this;

    if (app.isUserSignedIn()) {
        //show upload page
        this.showPage("submitmediaitem-page", "Add Media");
    } else {
        app.showMessage("Please Sign In");
    }
}

OCM_App.prototype.showConnectionError = function () {
    $("#network-error").show();
    this.ocm_ui.hideProgressIndicator();
    //app.showMessage("An error occurred transferring data. Please check your data connection.");
};

OCM_App.prototype.showAuthorizationError = function () {
    this.showMessage("Your session has expired, please sign in again.");
};

OCM_App.prototype.showMessage = function (msg) {
    if (this.isRunningUnderCordova) {
        navigator.notification.alert(
            msg,  // message
            function () { ;; },         // callback
            'Open Charge Map',            // title
            'OK'                  // buttonName
            );
    } else {
        alert(msg);
    }

};