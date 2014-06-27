/**
* @overview OCM charging location browser/editor Mobile App
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com

http://openchargemap.org
*/

/*jslint browser: true, devel: true, eqeq: true, white: true */
/*global define, $, window, document, OCM_CommonUI, OCM_Geolocation, OCM_Data, OCM_LocationSearchParams */

/*typescript*/
/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="TypeScriptReferences/history/history.d.ts" />

/// <reference path="OCM_Data.ts" />
/// <reference path="OCM_CommonUI.ts" />
/// <reference path="OCM_Geolocation.ts" />

//typescript declarations
declare var localisation_dictionary: any;
declare var languageList: Array<any>;

interface JQuery {
    fastClick: any;
    swipebox: any;
    closeSlide: any;
}

interface JQueryStatic {
    swipebox: any;
}

interface HTMLFormElement {
    files: any;
}

var Historyjs: Historyjs = <any>History;

////////////////////////////////////////////////////////////////

module OCM {

    export class App extends OCM.LocationEditor {

        private resultItemTemplate: any;

        constructor() {
            super();

            this.mappingManager.setParentAppContext(this);

            this.appConfig.maxResults = 100;

            this.appConfig.baseURL = "http://openchargemap.org/app/";
            this.appConfig.loginProviderRedirectBaseURL = "http://openchargemap.org/site/loginprovider/?_mode=silent&_forceLogin=true&_redirectURL=";
            this.appConfig.loginProviderRedirectURL = this.appConfig.loginProviderRedirectBaseURL + this.appConfig.baseURL;

            this.ocm_data.clientName = "ocm.app.webapp";
            this.appState.languageCode = "en";
            this.appState.menuDisplayed = false;

            this.ocm_data.generalErrorCallback = $.proxy(this.showConnectionError, this);
            this.ocm_data.authorizationErrorCallback = $.proxy(this.showAuthorizationError, this);

            this.appState.isEmbeddedAppMode = false; //used when app is embedded in another site

            this.appConfig.launchMapOnStartup = true;
            this.appState.mapLaunched = false;

            this.appState.appMode = AppMode.STANDARD;

            if (this.appState.appMode == AppMode.LOCALDEV) {
                this.appConfig.baseURL = "http://localhost:81/app";
                this.appConfig.loginProviderRedirectBaseURL = "http://localhost:81/site/loginprovider/?_mode=silent&_forceLogin=true&_redirectURL=";
                this.ocm_data.serviceBaseURL = "http://localhost:8080/v2";
                //this.ocm_data.serviceBaseURL = "http://localhost:81/api/v2";
                this.appConfig.loginProviderRedirectURL = this.appConfig.loginProviderRedirectBaseURL + this.appConfig.baseURL;
            }

            if (this.getParameter("mode") === "embedded") this.appState.isEmbeddedAppMode = true;

            if (this.appState.isEmbeddedAppMode) {
                this.appConfig.launchMapOnStartup = true;
            }
        }

        initApp() {

            var app = this;

            this.appState.appInitialised = true;

            //wire up state tracking
            this.initStateTracking();

            //wire up button events
            this.setupUIActions();

            this.initEditors();

            //populate language options

            this.populateDropdown("option-language", languageList, this.appState.languageCode);

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
        }

        setupUIActions() {

            var app = this;

            if (this.appConfig.launchMapOnStartup) {

                //pages are hidden by default, start by show map screen (once map api ready)

                //Observe OCM.Mapping.mapAPIReady and render map
                if (this.mappingManager.mapAPIReady) {
                    //default to map screen and begin loading closest data to centre of map
                    app.navigateToMap();
                    //search nearby on startup
                    app.performSearch(true, false);

                    app.appState.mapLaunched = true;
                    if (app.appState.isRunningUnderCordova) {
                        if (navigator.splashscreen) navigator.splashscreen.hide();
                    }
                } else {
                    (<any>Object).observe(this.mappingManager, function (changes) {

                        // This asynchronous callback runs
                        changes.forEach(function (change) {

                            // console.log(change.type, change.name, change.oldValue);

                            if (!app.appState.mapLaunched && change.name == "mapAPIReady" && app.mappingManager.mapAPIReady) {
                                //default to map screen and begin loading closest data to centre of map
                                app.navigateToMap();
                                //search nearby on startup
                                app.performSearch(true, false);

                                app.appState.mapLaunched = true;
                                //TODO prevent this reloading when api == true again - remove observer

                                if (app.appState.isRunningUnderCordova) {
                                    setTimeout(function () {
                                        if (navigator.splashscreen) navigator.splashscreen.hide();
                                    }, 300);
                                }
                            }
                        });
                    });
                }
            } else {
                //pages are hidden by default, show home screen
                this.navigateToHome();
            }

            //add header classes to header elements
            $("[data-role='header']").addClass("ui-header");

            //set default back ui buttons handler
            app.setElementAction("a[data-rel='back']", function () {
                Historyjs.back();
            });

            app.setElementAction("a[data-rel='menu']", function () {
                //show menu when menu icon activated
                app.toggleMenu(true);
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

            app.setElementAction("#map-toggle-list", function () {
                //show list, hide map
                $("#mapview-container").addClass("hidden-xs");
                $("#mapview-container").addClass("hidden-sm");
                //hide map
                app.setMapFocus(false);

                $("#listview-container").removeClass("hidden-xs");
                $("#listview-container").removeClass("hidden-sm");
            });

            app.setElementAction("#map-toggle-map", function () {
                //show list, hide map
                $("#listview-container").addClass("hidden-xs");
                $("#listview-container").addClass("hidden-sm");



                $("#mapview-container").removeClass("hidden-xs");
                $("#lmapview-container").removeClass("hidden-sm");

                //hide map
                app.setMapFocus(true);
                app.refreshMapView();
            });

            app.setElementAction("#map-refresh", function () {
                //refresh search based on map centre
                if (app.mappingManager.mapOptions.mapCentre != null) {
                    app.viewModel.searchPosition = app.mappingManager.mapOptions.mapCentre;
                    app.performSearch(false, false);
                }
            });

            //details page ui actions
            app.setElementAction("#option-favourite", function () {
                app.toggleFavouritePOI(app.viewModel.selectedPOI, null);
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

            //HACK: adjust height of content based on browser window size
            $(window).resize(function () {
                app.adjustMainContentHeight();
            });
        }

        postLoginInit() {
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
        }

        initDeferredUI() {
            //TODO: deferred UI loaded based on observed property
            this.log("Init of deferred UI..", LogLevel.VERBOSE);

            var app = this;

            //check if user signed in etc
            this.postLoginInit();

            //if cached results exist, render them
            var cachedResults = this.ocm_data.getCachedDataObject("SearchResults");
            var cachedResult_Location = this.ocm_data.getCachedDataObject("SearchResults_Location");

            if (cachedResults !== null) {
                if (cachedResult_Location !== null) {
                    (<HTMLInputElement>document.getElementById("search-location")).value = cachedResult_Location;
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
                    app.showDetailsViewById(poiId, true);
                }, 100);
            }

            this.switchLanguage($("#option-language").val());

        }

        beginLogin() {
            this.showProgressIndicator();

            //reset previous authorization warnings
            this.ocm_data.hasAuthorizationError = false;
            var app = this;

            if (this.appState.isRunningUnderCordova) {
                //do phonegapped login using InAppBrowser

                var ref: any = window.open(this.appConfig.loginProviderRedirectBaseURL + 'AppLogin?redirectWithToken=true', '_blank', 'location=yes');

                //attempt attach event listeners
                try {

                    ref.addEventListener('loaderror', function (event) {
                        app.log('loaderror: ' + event.message, LogLevel.ERROR);
                    });

                    ref.addEventListener('loadstart', function (event) {
                        app.log('loadstart: ' + event.url);
                        //attempt to fetch from url
                        var url = event.url;
                        var token = app.getParameterFromURL("OCMSessionToken", url);
                        if (token.length > 0) {
                            app.log('OCM: Got a token ' + event.url);
                            var userInfo = {
                                "Identifier": app.getParameterFromURL("Identifier", url),
                                "Username": app.getParameterFromURL("Identifier", url),
                                "SessionToken": app.getParameterFromURL("OCMSessionToken", url),
                                "AccessToken": "",
                                "Permissions": app.getParameterFromURL("Permissions", url)
                            };

                            app.log('got login: ' + userInfo.Username);

                            app.setLoggedInUserInfo(userInfo);
                            app.postLoginInit();

                            //return to default 
                            app.navigateToMap();
                            app.hideProgressIndicator();
                            ref.close();
                        } else {
                            app.log('OCM: Not got a token ' + event.url);
                        }
                    });

                    ref.addEventListener('exit', function (event) {
                        app.log(event.type);
                    });
                } catch (err) {
                    app.log("OCM: error adding inappbrowser events :" + err);
                }

                app.log("OCM: inappbrowser events added..");
            }
            else {
                //do normal web login
                app.log("OCM: not cordova, redirecting after login..");
                window.location.href = this.appConfig.loginProviderRedirectURL;
            }
        }

        logout(navigateToHome: boolean) {
            var app = this;

            this.clearCookie("Identifier");
            this.clearCookie("Username");
            this.clearCookie("OCMSessionToken");
            this.clearCookie("AccessPermissions");

            if (navigateToHome == true) {
                app.postLoginInit(); //refresh signed in/out ui
                if (this.appState.isRunningUnderCordova) {
                    app.navigateToMap();
                }
                else {
                    setTimeout(function () { window.location.href = app.appConfig.baseURL; }, 100);
                }
            } else {
                app.postLoginInit(); //refresh signed in/out ui
            }

        }

        storeSettings() {
            //save option settings to cookies
            this.setCookie("optsearchdist", $('#search-distance').val());
            this.setCookie("optsearchdistunit", $('#search-distance-unit').val());
            this.setCookie("optenableexperiments", $('#option-enable-experiments').val());

            this.setCookie("optlanguagecode", $('#option-language').val());
        }

        loadSettings() {
            if (this.getCookie("optsearchdist") != null) $('#search-distance').val(this.getCookie("optsearchdist"));
            if (this.getCookie("optsearchdistunit") != null) $('#search-distance-unit').val(this.getCookie("optsearchdistunit"));
            if (this.getCookie("optenableexperiments") != null) $('#option-enable-experiments').val(this.getCookie("optenableexperiments"));
            if (this.getCookie("optlanguagecode") != null) $('#option-language').val(this.getCookie("optlanguagecode"));
        }

        performCommentSubmit() {

            var app = this;
            if (app.appState.enableCommentSubmit === true) {
                app.appState.enableCommentSubmit = false;
                var refData = this.ocm_data.referenceData;
                var item = this.ocm_data.referenceData.UserComment;

                //collect form values
                item.ChargePointID = this.viewModel.selectedPOI.ID;
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
        }

        performMediaItemSubmit() {

            var $fileupload = $(':file');
            var mediafile = (<HTMLFormElement>$fileupload[0]).files[0];
            var name, size, type;
            if (mediafile) {
                name = mediafile.name;
                size = mediafile.size;
                type = mediafile.type;
            }

            var formData = new FormData();

            formData.append("id", this.viewModel.selectedPOI.ID);
            formData.append("comment", $("#comment").val());
            formData.append("mediafile", mediafile);

            //show progress
            this.showProgressIndicator();

            //submit
            this.ocm_data.submitMediaItem(formData, this.getLoggedInUserInfo(), $.proxy(this.submissionCompleted, this), $.proxy(this.submissionFailed, this));
        }

        submissionCompleted(jqXHR, textStatus) {
            this.log("submission::" + textStatus, LogLevel.VERBOSE);

            this.hideProgressIndicator();
            if (textStatus != "error") {
                this.showMessage("Thank you for your contribution, you may need to refresh your search for changes to appear. If approval is required your change may take 1 or more days to show up. (Status Code: " + textStatus + ")");

                if (this.viewModel.selectedPOI != null) {
                    //navigate to last viewed location
                    this.showDetailsView(document.getElementById("content-placeholder"), this.viewModel.selectedPOI);
                } else {
                    //navigate back to search page
                    this.navigateToMap();
                }

            } else {
                this.showMessage("Sorry, there was a problem accepting your submission. Please try again later. (Status Code: " + textStatus + ").");
            }
        }

        submissionFailed() {
            this.hideProgressIndicator();
            this.showMessage("Sorry, there was an unexpected problem accepting your contribution. Please check your internet connection and try again later.");
        }

        performSearch(useClientLocation: boolean= false, useManualLocation: boolean= false) {

            this.showProgressIndicator();

            //detect if mapping/geolocation available
            if (useClientLocation == true) {
                //initiate client geolocation (if not already determined)
                if (this.ocm_geo.clientGeolocationPos == null) {
                    this.ocm_geo.determineUserLocation($.proxy(
                        this.determineUserLocationCompleted, this), $.proxy(
                            this.determineUserLocationFailed, this));
                    return;
                } else {
                    this.viewModel.searchPosition = this.ocm_geo.clientGeolocationPos;
                }
            }

            var distance = parseInt((<HTMLInputElement>document.getElementById("search-distance")).value);
            var distance_unit = (<HTMLInputElement>document.getElementById("search-distance-unit")).value;

            if (this.viewModel.searchPosition == null || useManualLocation == true) {
                // search position not set, attempt fetch from location input and return for now
                var locationText = (<HTMLInputElement>document.getElementById("search-location")).value;
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

            if (this.viewModel.searchPosition != null && !this.appState.isSearchInProgress) {
                this.appState.isSearchInProgress = true;

                var params = new OCM.POI_SearchParams();
                params.latitude = this.viewModel.searchPosition.coords.latitude;
                params.longitude = this.viewModel.searchPosition.coords.longitude;
                params.distance = distance;
                params.distanceUnit = distance_unit;
                params.maxResults = this.appConfig.maxResults;
                params.includeComments = true;
                params.enableCaching = true;

                //apply filter settings from UI
                if ($("#filter-submissionstatus").val() != 200) params.submissionStatusTypeID = $("#filter-submissionstatus").val();
                if ($("#filter-connectiontype").val() != "") params.connectionTypeID = $("#filter-connectiontype").val();
                if ($("#filter-operator").val() != "") params.operatorID = $("#filter-operator").val();
                if ($("#filter-connectionlevel").val() != "") params.levelID = $("#filter-connectionlevel").val();
                if ($("#filter-usagetype").val() != "") params.usageTypeID = $("#filter-usagetype").val();
                if ($("#filter-statustype").val() != "") params.statusTypeID = $("#filter-statustype").val();

                this.log("Performing search..");
                this.ocm_data.fetchLocationDataListByParam(params, "ocm_app.renderPOIList", this.handleSearchError);
            }
        }

        handleSearchError(result) {
            if (result.status == 200) {
                //all ok
            } else {
                this.showMessage("There was a problem performing your search. Please check your internet connection.");
            }
        }

        determineUserLocationCompleted(pos) {
            this.viewModel.searchPosition = pos;
            this.ocm_geo.clientGeolocationPos = pos;
            this.performSearch();
        }

        determineUserLocationFailed() {
            this.hideProgressIndicator();
            this.showMessage("Could not automatically determine your location. Search by location name instead.");
        }

        determineGeocodedLocationCompleted(pos: MapCoords) {
            this.viewModel.searchPosition = pos;
            this.performSearch();
        }

        renderPOIList(locationList: Array<any>) {

            this.viewModel.resultsBatchID++; //indicates that results have changed and need reprocessed (maps etc)
            this.viewModel.poiList = locationList;


            if (locationList != null && locationList.length > 0) {
                this.log("Caching search results..");
                this.ocm_data.setCachedDataObject("SearchResults", locationList);
                this.ocm_data.setCachedDataObject("SearchResults_Location", (<HTMLInputElement>document.getElementById("search-location")).value);
            } else {
                this.log("No search results, will not overwrite cached search results.");
            }

            this.log("Rendering search results..");
            $("#search-no-data").hide();

            this.hideProgressIndicator();
            this.appState.isSearchInProgress = false;

            var appContext = this;

            //var $listContent = $('#results-list');
            var $listContent = $('<div id="results-list" class="results-list"></div>');
            if (this.resultItemTemplate == null) this.resultItemTemplate = $("#results-item-template").html();

            $listContent.children().remove();

            if (this.viewModel.poiList == null || this.viewModel.poiList.length == 0) {
                var $content = $("<div class=\"section-heading\"><p><span class=\"ui-li-count\">0 Results match your search</span></p></div>");
                $listContent.append($content);
            }
            else {

                var distUnitPref = <HTMLInputElement>document.getElementById("search-distance-unit");
                var distance_unit = "Miles";
                if (distUnitPref != null) distance_unit = distUnitPref.value;

                var $resultCount = $("<div class=\"section-heading\">The following " + locationList.length + " locations match your search</div>");

                $listContent.append($resultCount);

                var isAlternate = false;
                for (var i = 0; i < this.viewModel.poiList.length; i++) {
                    var poi = this.viewModel.poiList[i];
                    var distance = poi.AddressInfo.Distance;
                    if (distance == null) distance = 0;
                    var addressHTML = OCM.Utils.formatPOIAddress(poi);

                    var contactHTML = "";
                    contactHTML += OCM.Utils.formatPhone(poi.AddressInfo.ContactTelephone1, "Tel.");

                    var itemTemplate = "<div class='result'>" + this.resultItemTemplate + "</div>";
                    var locTitle = poi.AddressInfo.Title;
                    if (poi.AddressInfo.Town != null && poi.AddressInfo.Town != "") locTitle = poi.AddressInfo.Town + " - " + locTitle;

                    itemTemplate = itemTemplate.replace("{locationtitle}", locTitle);
                    itemTemplate = itemTemplate.replace("{location}", addressHTML);
                    itemTemplate = itemTemplate.replace("{comments}", "");


                    var direction = "";

                    if (this.viewModel.searchPosition != null) direction = this.ocm_geo.getCardinalDirectionFromBearing(this.ocm_geo.getBearing(this.viewModel.searchPosition.coords.latitude, this.viewModel.searchPosition.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));

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

                    $item.on('click', <any>{ poi: poi }, function (e) {
                        e.preventDefault();
                        e.stopPropagation();

                        //todo: rename as prepareDetailsView
                        try {
                            appContext.showDetailsView(this, e.data.poi);
                        } catch (err) {
                        }
                        appContext.showPage("locationdetails-page", "Location Details");
                    });

                    $listContent.append($item);

                    isAlternate = !isAlternate;
                }
            }

            //show hidden results ui
            $('#results-list').replaceWith($listContent);
            $("#results-list").css("display", "block");

            //after initial load subsequent queries auto refresh the map markers
            if (this.appConfig.autoRefreshMapResults) {
                this.log("Auto refreshing map view");
                this.refreshMapView();
            }
        }

        showDetailsViewById(id, forceRefresh) {
            var itemShown = false;
            //if id in current result list, show
            if (this.viewModel.poiList != null) {
                for (var i = 0; i < this.viewModel.poiList.length; i++) {
                    if (this.viewModel.poiList[i].ID == id) {
                        this.showDetailsView(document.getElementById("content-placeholder"), this.viewModel.poiList[i]);
                        itemShown = true;
                    }
                    if (itemShown) break;
                }
            }

            if (!itemShown || forceRefresh == true) {
                //load poi details, then show
                this.log("Location not cached, fetching details:" + id);
                this.ocm_data.fetchLocationById(id, "ocm_app.showDetailsFromList", null);
            }
        }

        showDetailsFromList(results) {
            var app = this;
            if (results.length > 0) {
                app.showDetailsView(document.getElementById("content-placeholder"), results[0]);
            } else {
                this.showMessage("The location you are attempting to view does not exist or has been removed.");
            }
        }

        showDetailsView(element, poi) {

            this.viewModel.selectedPOI = poi;

            this.log("Showing OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

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
            var poiDetails = OCM.Utils.formatPOIDetails(poi, false);

            $("#details-locationtitle").html(poi.AddressInfo.Title);
            $("#details-address").html(poiDetails.address);
            $("#details-contact").html(poiDetails.contactInfo);
            $("#details-additional").html(poiDetails.additionalInfo);
            $("#details-advanced").html(poiDetails.advancedInfo);

            this.mappingManager.showPOIOnStaticMap("details-map", poi, true, this.appState.isRunningUnderCordova);

            var streetviewUrl = "http://maps.googleapis.com/maps/api/streetview?size=192x96&location=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&fov=90&heading=0&pitch=0&sensor=false";
            var streetViewLink = "https://maps.google.com/maps?q=&layer=c&cbll=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&cbp=11,0,0,0,0";
            $("#details-streetview").html("<a href='#' onclick=\"window.open('" + streetViewLink + "', '_system');\"><img src=\"" + streetviewUrl + "\" width=\"192\" height=\"96\" title=\"Approximate Streetview (if available): " + poi.AddressInfo.Title + "\" /></a>");

            if (poi.UserComments != null) {
                var commentOutput = "<div class='comments'>";

                for (var c = 0; c < poi.UserComments.length; c++) {
                    var comment = poi.UserComments[c];
                    var commentDate = OCM.Utils.fixJSONDate(comment.DateCreated);
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

                //gallery
                var mediaItemOutput = "<div class='comments'>";

                for (var c = 0; c < poi.MediaItems.length; c++) {
                    var mediaItem = poi.MediaItems[c];
                    if (mediaItem.IsEnabled == true) {
                        var itemDate = OCM.Utils.fixJSONDate(mediaItem.DateCreated);
                        mediaItemOutput += "<blockquote><div style='float:left;padding-right:0.3em;'><a class='swipebox' href='" + mediaItem.ItemURL + "' target='_blank' title='" + ((mediaItem.Comment != null && mediaItem.Comment != "") ? mediaItem.Comment : poi.AddressInfo.Title) + "'><img src='" + mediaItem.ItemThumbnailURL + "'/></a></div><p>" + (mediaItem.Comment != null ? mediaItem.Comment : "(No Comment)") + "</p> " +
                        "<small><cite>" + ((mediaItem.User != null) ? mediaItem.User.Username : "(Anonymous)") + " : " + itemDate.toLocaleDateString() + "</cite></small>" +
                        "</blockquote>";
                    }
                }

                mediaItemOutput += "</div>";

                $("#details-mediaitems-gallery").html(mediaItemOutput);

                //activate swipebox gallery
                $('.swipebox').swipebox();

            } else {
                $("#details-mediaitems").html("No photos submitted.");
            }

            var leftPos = $element.position().left;
            var topPos = $element.position().top;
            $detailsView.css("left", leftPos);
            $detailsView.css("top", topPos);

            //once displayed, try fetching a more accurate distance estimate
            if (this.viewModel.searchPosition != null) {
                //TODO: observe property to update UI
                this.ocm_geo.getDrivingDistanceBetweenPoints(this.viewModel.searchPosition.coords.latitude, this.viewModel.searchPosition.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude, $("#search-distance-unit").val(), this.updatePOIDistanceDetails);
            }

            //apply translations (if required)
            if (this.appState.languageCode != null) {
                OCM.Utils.applyLocalisation(false);
            }
        }

        adjustMainContentHeight() {
            //HACK: adjust map/list content to main viewport
            var preferredMapHeight = $(window).height() - 90;
            if ($("#map-view").height() != preferredMapHeight);
            this.log("adjusting map height:" + preferredMapHeight, LogLevel.VERBOSE);
            document.getElementById("map-view").style.height = preferredMapHeight + "px";
            document.getElementById("listview-container").style.height = preferredMapHeight + "px";
            document.getElementById("listview-container").style.maxHeight = preferredMapHeight + "px";

            return preferredMapHeight;
        }

        refreshMapView() {

            var $resultcount = $("#map-view-resultcount");

            if (this.viewModel.poiList != null && this.viewModel.poiList.length > 0) {
                $resultcount.html(this.viewModel.poiList.length + " Results");
            } else {
                $resultcount.html("0 Results");
            }

            if (this.appState.isRunningUnderCordova) {
                //for cordova, switch over to native google maps
                this.mappingManager.setMapAPI("googlenativesdk");
            }

            var appContext = this;
          
            //if (!this.appState.mapLaunched) {
            //on first showing map, adjust map container height to match page
            var mapHeight = this.adjustMainContentHeight();

            //}
            this.mappingManager.refreshMapView("map-view", mapHeight, this.viewModel.poiList, this.viewModel.searchPosition);

            this.appConfig.autoRefreshMapResults = true;
        }

        setMapFocus(hasFocus: boolean) {
            if (hasFocus) {
                this.mappingManager.showMap();
            } else {
                this.mappingManager.hideMap();
            }
        }

        updatePOIDistanceDetails(response, status) {
            if (response != null) {
                var result = response.rows[0].elements[0];

                if (result.status == "OK") {
                    var origin = response.originAddresses[0];
                    $("#addr_distance").after(" - driving distance: " + result.distance.text + " (" + result.duration.text + ") from " + origin);
                }
            }
        }

        isFavouritePOI(poi, itineraryName: string= null) {
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
        }

        addFavouritePOI(poi, itineraryName: string= null) {
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

                    this.log("Added Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

                    this.ocm_data.setCachedDataObject("favouritePOIList", favouriteLocations);
                } else {
                    this.log("Already exists: Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                }
            }
        }

        removeFavouritePOI(poi, itineraryName: string= null) {
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
                    this.log("Removed Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);

                } else {
                    this.log("Cannot remove: Not a Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                }
            }
        }

        toggleFavouritePOI(poi, itineraryName: string= null) {
            if (poi != null) {
                if (this.isFavouritePOI(poi, itineraryName)) {
                    this.removeFavouritePOI(poi, itineraryName);
                    $("#option-favourite").removeClass("fa-heart");
                    $("#option-favourite").addClass("fa-heart-o");
                } else {
                    this.addFavouritePOI(poi, itineraryName);
                    $("#option-favourite").removeClass("fa-heart-o");
                    $("#option-favourite").addClass("fa-heart");
                }
            }
        }

        getFavouritePOIList(itineraryName: string= null) {
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
        }

        switchLanguage(languageCode: string) {

            this.log("Switching UI language: " + languageCode);

            this.appState.languageCode = languageCode;

            localisation_dictionary = eval("localisation_dictionary_" + languageCode);

            //apply translations
            OCM.Utils.applyLocalisation(false);

            //store language preference
            this.storeSettings();
        }

        hidePage(pageId: string) {
            $("#" + pageId).hide();
        }

        showPage(pageId: string, pageTitle: string, skipState: boolean = false) {
            if (!pageTitle) pageTitle = pageId;

            this.log("app.showPage:" + pageId, LogLevel.VERBOSE);

            //hide last shown page
            if (this.appState._lastPageId && this.appState._lastPageId != null) {

                if (this.appState._lastPageId == "map-page" && pageId == "map-page") {
                    this.log("Time to QUIT");

                    //double home page request, time to exit on android etc
                    if (this.appState.isRunningUnderCordova) {
                        (<any>navigator).app.exitApp();
                    }
                }
                this.hidePage(this.appState._lastPageId);
            }

            //hide home page
            document.getElementById("home-page").style.display = "none";

            //show new page
            document.getElementById(pageId).style.display = "block";

            if (!this.appState.isEmbeddedAppMode) {
                //hack: reset scroll position for new page once page has had a chance to render
                setTimeout(function () { (<HTMLElement>document.documentElement).scrollIntoView(); }, 100);
            }

            if (pageId !== "map-page") {
                //native map needs to be hidden or offscreen
                this.setMapFocus(false);
            } else {
                this.setMapFocus(true);
            }

            this.appState._lastPageId = pageId;

            if (skipState) {
                //skip storage of current state
            } else {
                Historyjs.pushState({ view: pageId, title: pageTitle }, pageTitle, "?view=" + pageId);
            }

            this.log("leaving app.showPage:" + pageId, LogLevel.VERBOSE);

            //hide menu when menu item activated
            this.toggleMenu(false);
        }

        initStateTracking() {
            var app = this;
            // Check Location
            if (document.location.protocol === 'file:') {
                //state not supported
            }

            // Establish Variables
            //this.Historyjs = History; // Note: We are using a capital H instead of a lower h

            var State = Historyjs.getState();

            // Log Initial State
            State.data.view = "map-page";
            Historyjs.log('initial:', State.data, State.title, State.url);

            // Bind to State Change
            Historyjs.Adapter.bind(window, 'statechange', function () { // Note: We are using statechange instead of popstate
                // Log the State
                var State = Historyjs.getState(); // Note: We are using History.getState() instead of event.state
                //History.log('statechange:', State.data, State.title, State.url);

                // app.logEvent("state switch to :" + State.data.view);
                if (State.data.view) {
                    if (app.appState._lastPageId && app.appState._lastPageId != null) {
                        if (State.data.view == app.appState._lastPageId) return;//dont show same page twice
                    }
                    app.showPage(State.data.view, State.Title, true);
                } else {
                    app.navigateToMap();
                    //app.showPage("home-page", "Home");
                    app.log("pageid:" + app.appState._lastPageId);

                }

                //if swipebox is open, need to close it:
                if ($.swipebox && $.swipebox.isOpen) {
                    $('html').removeClass('swipebox-html');
                    $('html').removeClass('swipebox-touch');
                    $("#swipebox-overlay").remove();
                    $(window).trigger('resize');
                }
            });
        }

        //methods for use by native build of app to enable navigation via native UI/phonegap
        navigateToSearch() {
            this.log("Navigate To: Search Page", LogLevel.VERBOSE);
            this.showPage("search-page", "Search");
        }

        navigateToHome() {
            //this.log("Navigate To: Home Page", LogLevel.VERBOSE);
            //this.showPage("home-page", "Home");
            this.navigateToMap();
        }

        navigateToMap() {
            this.log("Navigate To: Map Page", LogLevel.VERBOSE);

            this.showPage("map-page", "Map");
            var app = this;

            //change title of map page to be Search
            $("#search-title-favourites").hide();
            $("#search-title-main").show();

            setTimeout(function () { app.refreshMapView(); }, 250);
        }

        navigateToFavourites() {
            this.log("Navigate To: Favourites", LogLevel.VERBOSE);
            var app = this;
            //get list of favourites as POI list and render in standard search page
            var favouritesList = app.getFavouritePOIList();
            if (favouritesList === null || favouritesList.length === 0) {
                $("#favourites-list").html("<p>You have no favourite locations set. To add or remove a favourite, tap the <i title=\"Toggle Favourite\" class=\"icon-heart-empty\"></i> icon when viewing a location.</p>");
                this.showPage("favourites-page", "Favourites");
            } else {

                app.renderPOIList(favouritesList);

                //show favourites on search page
                app.navigateToMap();

                //change title of map page to be favourites
                $("#search-title-main").hide();
                $("#search-title-favourites").show();
            }
        }

        navigateToAddLocation() {
            this.log("Navigate To: Add Location", LogLevel.VERBOSE);
            var app = this;

            app.isLocationEditMode = false;
            app.viewModel.selectedPOI = null;
            app.showLocationEditor();
            this.showPage("editlocation-page", "Add Location");

            if (!app.isUserSignedIn()) {
                app.showMessage("You are not signed in. You should sign in unless you wish to submit your edit anonymously.");
            }
        }

        navigateToEditLocation() {
            this.log("Navigate To: Edit Location", LogLevel.VERBOSE);
            var app = this;

            //show editor
            app.showLocationEditor();
            app.showPage("editlocation-page", "Edit Location");

            if (!app.isUserSignedIn()) {
                app.showMessage("You are not signed in. You should sign in unless you wish to submit your edit anonymously.");
            }
        }

        navigateToLogin() {
            this.log("Navigate To: Login", LogLevel.VERBOSE);
            this.showPage("login-page", "Sign In");
        }

        navigateToSettings() {
            this.log("Navigate To: Settings", LogLevel.VERBOSE);
            this.showPage("settings-page", "Settings");
        }

        navigateToAbout() {
            this.log("Navigate To: About", LogLevel.VERBOSE);
            this.showPage("about-page", "About");
        }

        navigateToAddComment() {
            this.log("Navigate To: Add Comment", LogLevel.VERBOSE);

            var app = this;

            //reset comment form on show
            (<HTMLFormElement>document.getElementById("comment-form")).reset();
            app.appState.enableCommentSubmit = true;

            //show checkin/comment page
            this.showPage("submitcomment-page", "Add Comment");

            if (!app.isUserSignedIn()) {
                app.showMessage("You are not signed in. You should sign in unless you wish to submit an anonymous comment.");
            }
        }

        navigateToAddMediaItem() {
            this.log("Navigate To: Add Media Item", LogLevel.VERBOSE);

            var app = this;

            //show upload page
            this.showPage("submitmediaitem-page", "Add Media");

            if (!app.isUserSignedIn()) {
                app.showMessage("You are not signed in. You should sign in unless you wish to submit anonymously.");
            }
        }

        showConnectionError() {
            $("#progress-indicator").hide();
            $("#network-error").show();
        }

        showAuthorizationError() {
            this.showMessage("Your session has expired, please sign in again.");
        }

        toggleMenu(showMenu: boolean) {

            if (showMenu != null) this.appState.menuDisplayed = !showMenu;

            if (this.appState.menuDisplayed) {
                //hide app menu
                this.appState.menuDisplayed = false;
                $("#app-menu-container").hide();
                //$("#menu").hide();
            }
            else {
                //show app menu
                this.appState.menuDisplayed = true;
                this.setMapFocus(false);
                $("#app-menu-container").show();

                //TODO: handle back button from open menu
                //Historyjs.pushState({ view: "menu", title: "Menu" }, "Menu", "?view=menu");
            }
        }

    }
}