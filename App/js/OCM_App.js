var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var Historyjs = History;
var IScroll;
var OCM;
(function (OCM) {
    var App = (function (_super) {
        __extends(App, _super);
        function App() {
            var _this = _super.call(this) || this;
            _this.mappingManager.setParentAppContext(_this);
            _this.appConfig.maxResults = 1000;
            _this.appConfig.baseURL = "https://openchargemap.org/app/";
            _this.appConfig.loginProviderRedirectBaseURL = "https://openchargemap.org/site/loginprovider/?_mode=silent&_forceLogin=true&_redirectURL=";
            _this.appConfig.loginProviderRedirectURL = _this.appConfig.loginProviderRedirectBaseURL + _this.appConfig.baseURL;
            _this.appConfig.newsHomeUrl = null;
            _this.apiClient.clientName = "ocm.app.webapp";
            _this.appState.languageCode = "en";
            _this.appState.menuDisplayed = false;
            _this.apiClient.generalErrorCallback = $.proxy(_this.showConnectionError, _this);
            _this.apiClient.authorizationErrorCallback = $.proxy(_this.showAuthorizationError, _this);
            _this.appState.isEmbeddedAppMode = false;
            _this.appConfig.launchMapOnStartup = true;
            _this.appState.mapLaunched = false;
            _this.appState.appMode = OCM.AppMode.STANDARD;
            _this.appConfig.enableLiveMapQuerying = true;
            _this.appConfig.enablePOIListView = false;
            _this.enableLogging = true;
            if (_this.getParameter("mode") === "embedded") {
                _this.appState.isEmbeddedAppMode = true;
            }
            if (_this.getParameter("languagecode") != "") {
                _this.appState.languageCode = _this.getParameter("languagecode");
            }
            if (_this.appState.isEmbeddedAppMode) {
                _this.appConfig.launchMapOnStartup = true;
            }
            return _this;
        }
        App.prototype.initApp = function () {
            var app = this;
            this.beginObservingAppModelChanges();
            this.appState.appInitialised = true;
            this.initStateTracking();
            this.setupUIActions();
            this.initEditors();
            this.populateDropdown("option-language", languageList, this.appState.languageCode);
            this.loadSettings();
            $('#search-distance').change(function () { app.storeSettings(); });
            $('#search-distance-unit').change(function () { app.storeSettings(); });
            $('#option-language').change(function () {
                app.switchLanguage($("#option-language").val());
            });
            $('#filter-operator, #filter-connectiontype, #filter-connectionlevel,#filter-usagetype,#filter-statustype, #filter-minpowerkw')
                .change(function () {
                app.validateMultiSelect($(this));
                app.storeSettings();
            });
            var app = this;
            this.postLoginInit();
            var cachedResults = this.apiClient.getCachedDataObject("SearchResults");
            var cachedResult_Location = this.apiClient.getCachedDataObject("SearchResults_Location");
            var cachedResult_SearchPos = this.apiClient.getCachedDataObject("SearchResults_SearchPos");
            if (cachedResults !== null) {
                if (cachedResult_Location !== null) {
                    document.getElementById("search-location").value = cachedResult_Location;
                }
                if (cachedResult_SearchPos != null) {
                    app.viewModel.searchPosition = cachedResult_SearchPos;
                }
                app.viewModel.isCachedResults = true;
                app.viewModel.poiList = cachedResults;
                app.viewModel.searchPOIList = cachedResults;
            }
            else {
                app.navigateToStartup();
                app.viewModel.poiList = [];
                app.viewModel.searchPOIList = [];
                app.performSearch(true, false);
            }
            var idParam = app.getParameter("id");
            if (idParam !== null && idParam !== "") {
                var poiId = parseInt(app.getParameter("id"), 10);
                setTimeout(function () {
                    app.showDetailsViewById(poiId, true);
                }, 100);
            }
            this.switchLanguage($("#option-language").val());
            setTimeout(function () { app.syncFavourites(); }, 10000);
            if (app.appState.isRunningUnderCordova) {
                setTimeout(function () {
                    if (navigator.splashscreen)
                        navigator.splashscreen.hide();
                }, 2000);
            }
        };
        App.prototype.beginObservingAppModelChanges = function () {
            var app = this;
            Object.observe(this.viewModel, function (changes) {
                changes.forEach(function (change) {
                    if (change.name == "poiList") {
                        app.log("Handling POI List Change event.");
                        if (app.appConfig.enablePOIListView) {
                            app.renderPOIList(app.viewModel.poiList);
                        }
                        else {
                        }
                        if (app.mappingManager.isMapReady()) {
                            app.log("Auto refreshing map view");
                            app.refreshMapView();
                        }
                        else {
                            app.log("Map not ready, skipping refresh.");
                        }
                    }
                });
            });
            app.log("Observing mappingManager changes");
            Object.observe(this.mappingManager, function (changes) {
                changes.forEach(function (change) {
                    app.log("changed: app.mappingManager." + change.name);
                    if (change.name == "mapAPIReady" && app.mappingManager.mapAPIReady) {
                        app.log("Mapping Step 1:Map API Ready - Initialising Map.", OCM.LogLevel.VERBOSE);
                        app.mappingManager.initMap("map-view");
                    }
                    if (change.name == "mapReady" && app.mappingManager.isMapReady()) {
                        app.log("Mapping Step 2:Map Initialised. Performing First Render:", OCM.LogLevel.VERBOSE);
                        app.refreshMapView();
                        app.navigateToMap();
                    }
                });
            });
            Object.observe(this.mappingManager.mapOptions, function (changes) {
                changes.forEach(function (change) {
                    app.log("changed: app.mappingManager.mapOptions." + change.name);
                    if (change.name == "mapCentre") {
                        if (app.appConfig.enableLiveMapQuerying) {
                            app.log("Live map querying enabled, performing a search from map centre..");
                            app.mappingManager.mapOptions.requestSearchUpdate = true;
                        }
                        if (app.mappingManager.mapOptions.requestSearchUpdate) {
                            app.mappingManager.mapOptions.requestSearchUpdate = false;
                            var pos = app.mappingManager.mapOptions.mapCentre;
                            app.log("Starting new search from map position: " + pos.coords.latitude + "," + pos.coords.longitude);
                            app.viewModel.searchPosition = pos;
                            app.performSearch(false, false);
                        }
                    }
                    if (change.name == "enableSearchByWatchingLocation") {
                        app.refreshLocationWatcherStatus();
                    }
                });
            });
            Object.observe(this.geolocationManager, function (changes) {
                changes.forEach(function (change) {
                    app.log("changed: app.geolocationManager." + change.name + "::" + JSON.stringify(change.oldValue));
                    if (change.name == "clientGeolocationPos") {
                        if (!app.appState.isSearchInProgress && app.mappingManager.mapOptions.enableSearchByWatchingLocation) {
                            app.log("Position watcher update, searching POIs again..");
                            app.performSearch(true, false);
                        }
                        else {
                        }
                    }
                });
            });
        };
        App.prototype.setupUIActions = function () {
            var app = this;
            if (this.appConfig.launchMapOnStartup) {
                if (!app.mappingManager.mapReady) {
                    app.log("Map not ready yet. Waiting for map ready via model observer.");
                    setTimeout(function () {
                        if (!app.mappingManager.mapReady) {
                            app.navigateToHome();
                            app.toggleMapView();
                        }
                    }, 10000);
                }
            }
            else {
                this.navigateToStartup();
            }
            $("[data-role='header']").addClass("ui-header");
            this.fileUploadManager = new OCM.FileUpload();
            app.setElementAction("a[data-rel='back']", function () {
                Historyjs.back();
            });
            app.setElementAction("a[data-rel='menu']", function () {
                app.toggleMenu(true);
            });
            if (app.appState.isRunningUnderCordova) {
                document.addEventListener("menubutton", function () {
                    app.toggleMenu(null);
                }, false);
            }
            if (app.appState.isEmbeddedAppMode) {
                $("#option-expand").show();
                app.setElementAction("#option-expand", function () {
                    var newWindow = window.open(app.appConfig.baseURL, "_blank");
                });
            }
            app.setElementAction("#app-menu-container", function () {
                app.toggleMenu(false);
                if (app.appState.isRunningUnderCordova) {
                    if (app.appState._lastPageId == "map-page") {
                        app.log("menu dismissed, showing map again");
                        app.mappingManager.showMap();
                    }
                    else {
                        app.log("menu dismissed resuming page:" + app.appState._lastPageId);
                    }
                }
            });
            app.setElementAction("a[href='#map-page'],#search-map", function () {
                app.navigateToMap();
            });
            app.setElementAction("a[href='#news-page']", function () {
                app.navigateToNews();
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
            app.setElementAction("a[href='#profile-page']", function () {
                app.navigateToProfile();
            });
            app.setElementAction("#signin-button", function (e) {
                app.log("Signing In");
                if (e)
                    e.preventDefault();
                app.beginLogin();
            });
            app.setElementAction("#signout-button", function (e) {
                app.log("Signing Out");
                if (e)
                    e.preventDefault();
                app.logout(false);
            });
            app.setElementAction("#search-nearby", function () {
                app.performSearch(true, false);
            });
            app.setElementAction("#search-button", function () {
                app.performSearch(false, true);
            });
            app.setElementAction("#map-toggle-list", function () {
                app.toggleMapView();
            });
            app.setElementAction("#map-refresh", function () {
                if (app.mappingManager.mapOptions.mapCentre != null) {
                    app.viewModel.searchPosition = app.mappingManager.mapOptions.mapCentre;
                    app.performSearch(false, false);
                }
            });
            $("#pref-basemap-type").on("change", function () {
                if (app.mappingManager.mapReady) {
                    app.mappingManager.setMapType($("#pref-basemap-type").val());
                }
                app.storeSettings();
            });
            $("#pref-search-behaviour").on("change", function () {
                var selectedVal = $("#pref-search-behaviour").val();
                app.log("Changing search behaviour:" + selectedVal);
                if (selectedVal == "Auto") {
                    app.mappingManager.mapOptions.enableSearchByWatchingLocation = true;
                }
                else {
                    app.mappingManager.mapOptions.enableSearchByWatchingLocation = false;
                }
                app.storeSettings();
            });
            app.setElementAction("#option-favourite", function () {
                app.toggleFavouritePOI(app.viewModel.selectedPOI, null);
            });
            app.setElementAction("#option-edit, #details-edit", function () {
                app.navigateToEditLocation();
            });
            app.setElementAction("#option-checkin, #btn-checkin", function () {
                app.navigateToAddComment();
            });
            app.setElementAction("#submitcomment-button", function () {
                app.performCommentSubmit();
            });
            app.setElementAction("#option-uploadphoto, #btn-uploadphoto", function () {
                app.navigateToAddMediaItem();
            });
            app.setElementAction("#submitmediaitem-button", function () {
                app.performMediaItemSubmit();
            });
            $(window).resize(function () {
                app.adjustMainContentHeight();
            });
        };
        App.prototype.refreshLocationWatcherStatus = function () {
            var app = this;
            if (app.mappingManager.mapOptions.enableSearchByWatchingLocation) {
                app.log("Starting to watch user location");
                app.geolocationManager.startWatchingUserLocation();
            }
            else {
                app.log("Stop watching user location");
                app.geolocationManager.stopWatchingUserLocation();
            }
        };
        App.prototype.checkSelectedAPIMode = function () {
            if ($("#filter-apimode").val() == "standard") {
                this.apiClient.serviceBaseURL = this.apiClient.serviceBaseURL_Standard;
            }
            if ($("#filter-apimode").val() == "sandbox") {
                this.apiClient.serviceBaseURL = this.apiClient.serviceBaseURL_Sandbox;
            }
        };
        App.prototype.toggleMapView = function (forceHideList) {
            if (forceHideList === void 0) { forceHideList = false; }
            var app = this;
            if ($("#mapview-container").hasClass("hidden-xs") || forceHideList == true) {
                $("#listview-container").addClass("hidden-xs");
                $("#listview-container").addClass("hidden-sm");
                $("#mapview-container").removeClass("hidden-xs");
                $("#mapview-container").removeClass("hidden-sm");
                $("#map-toggle-icon").removeClass("fa-map-marker");
                $("#map-toggle-icon").addClass("fa-list");
                app.setMapFocus(true);
            }
            else {
                $("#mapview-container").addClass("hidden-xs");
                $("#mapview-container").addClass("hidden-sm");
                app.setMapFocus(false);
                $("#listview-container").removeClass("hidden-xs");
                $("#listview-container").removeClass("hidden-sm");
                $("#map-toggle-icon").removeClass("fa-list");
                $("#map-toggle-icon").addClass("fa-map-marker");
            }
        };
        App.prototype.postLoginInit = function () {
            var userInfo = this.getLoggedInUserInfo();
            if (!this.isUserSignedIn()) {
                $("#user-profile-info").html("");
                $("#signin-button").show();
                $("#menu-signin").show();
                $("#menu-signout").hide();
            }
            else {
                $("#user-profile-info").html("Signed in as: " + userInfo.Username);
                $("#signin-button").hide();
                $("#menu-signin").hide();
                $("#menu-signout").show();
            }
        };
        App.prototype.beginLogin = function () {
            this.showProgressIndicator();
            this.apiClient.hasAuthorizationError = false;
            var app = this;
            if (this.appState.isRunningUnderCordova) {
                var ref = window.open(this.appConfig.loginProviderRedirectBaseURL + 'AppLogin?redirectWithToken=true', '_blank', 'location=yes');
                try {
                    ref.addEventListener('loaderror', function (event) {
                        app.log('loaderror: ' + event.message, OCM.LogLevel.ERROR);
                    });
                    ref.addEventListener('loadstart', function (event) {
                        app.log('loadstart: ' + event.url);
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
                            app.navigateToMap();
                            app.hideProgressIndicator();
                            ref.close();
                        }
                        else {
                            app.log('OCM: Not got a token ' + event.url);
                        }
                    });
                    ref.addEventListener('exit', function (event) {
                        app.log(event.type);
                    });
                }
                catch (err) {
                    app.log("OCM: error adding inappbrowser events :" + err);
                }
                app.log("OCM: inappbrowser events added..");
            }
            else {
                if (!app.appState.isEmbeddedAppMode) {
                    app.log("OCM: not cordova, redirecting after login..");
                    window.location.href = this.appConfig.loginProviderRedirectURL;
                }
                else {
                    var authWindow = window.open(this.appConfig.loginProviderRedirectBaseURL + 'AppLogin?redirectWithToken=true', "_blank", "width=320, height=400");
                    app.appState._appPollForLogin = setInterval(function () {
                        app.log("Polling for login result..");
                        if (app.isUserSignedIn()) {
                            var userInfo = app.getLoggedInUserInfo();
                            clearInterval(app.appState._appPollForLogin);
                            app.log('Login Completed. ' + userInfo.Username);
                            app.setLoggedInUserInfo(userInfo);
                            app.postLoginInit();
                            app.navigateToHome();
                            app.hideProgressIndicator();
                            authWindow.close();
                        }
                    }, 1000);
                }
            }
        };
        App.prototype.logout = function (navigateToHome) {
            var app = this;
            this.clearCookie("Identifier");
            this.clearCookie("Username");
            this.clearCookie("OCMSessionToken");
            this.clearCookie("AccessPermissions");
            if (navigateToHome == true) {
                app.postLoginInit();
                if (this.appState.isRunningUnderCordova) {
                    app.navigateToMap();
                }
                else {
                    setTimeout(function () { window.location.href = app.appConfig.baseURL; }, 100);
                }
            }
            else {
                app.postLoginInit();
            }
        };
        App.prototype.validateMultiSelect = function ($multiSelect) {
            var options = this.getMultiSelectionAsArray($multiSelect, "");
            if (options.length > 1) {
                var allOptionIndex = options.indexOf("");
                if (allOptionIndex >= 0) {
                    options = options.splice(allOptionIndex, 1);
                    $multiSelect.val(options);
                    this.log("(All) option removed from multi select - other options selected at same time", OCM.LogLevel.VERBOSE);
                }
            }
        };
        App.prototype.storeSettings = function () {
            if (this.appState.suppressSettingsSave == false) {
                this.setCookie("optsearchdist", $('#search-distance').val());
                this.setCookie("optsearchdistunit", $('#search-distance-unit').val());
                this.setCookie("optlanguagecode", $('#option-language').val());
                this.setCookie("filterapimode", $('#filter-apimode').val());
                this.setCookie("optbasemaptype", $('#pref-basemap-type').val());
                this.setCookie("optsearchbehaviour", $('#pref-search-behaviour').val());
                this.setCookie("filterminpowerkw", $('#filter-minpowerkw').val());
                this.setCookie("filteroperator", $('#filter-operator').val());
                this.setCookie("filterconnectiontype", $('#filter-connectiontype').val());
                this.setCookie("filterconnectionlevel", $('#filter-connectionlevel').val());
                this.setCookie("filterusagetype", $('#filter-usagetype').val());
                this.setCookie("filterstatustype", $('#filter-statustype').val());
            }
        };
        App.prototype.loadSettings = function () {
            this.loadPref("optsearchdist", $("#search-distance"), false);
            this.loadPref("optsearchdistunit", $("#search-distance-unit"), false);
            this.loadPref("optlanguagecode", $("#option-language"), false);
            this.loadPref("filterapimode", $("#filter-apimode"), false);
            this.loadPref("optbasemaptype", $("#pref-basemap-type"), false);
            this.loadPref("optsearchbehaviour", $("#pref-search-behaviour"), false);
            this.loadPref("filterminpowerkw", $("#filter-minpowerkw"), true);
            this.loadPref("filteroperator", $("#filter-operator"), true);
            this.loadPref("filterconnectiontype", $("#filter-connectiontype"), true);
            this.loadPref("filterconnectionlevel", $("#filter-connectionlevel"), true);
            this.loadPref("filterusagetype", $("#filter-usagetype"), true);
            this.loadPref("filterstatustype", $("#filter-statustype"), true);
        };
        App.prototype.loadPref = function (settingName, $boundFormElement, isMultiSelect) {
            if (this.getCookie(settingName) != null) {
                this.log("Loading pref " + settingName + " (" + isMultiSelect + "):" + this.getCookie(settingName));
                if (isMultiSelect) {
                    $boundFormElement.val(this.getCookie(settingName).toString().split(","));
                }
                else {
                    $boundFormElement.val(this.getCookie(settingName));
                }
            }
        };
        App.prototype.performCommentSubmit = function () {
            var app = this;
            if (!app.appState.isSubmissionInProgress) {
                app.checkSelectedAPIMode();
                if (app.appState.enableCommentSubmit === true) {
                    app.appState.enableCommentSubmit = false;
                    var refData = this.apiClient.referenceData;
                    var item = this.apiClient.referenceData.UserComment;
                    item.ChargePointID = this.viewModel.selectedPOI.ID;
                    item.CheckinStatusType = this.apiClient.getRefDataByID(refData.CheckinStatusTypes, $("#checkin-type").val());
                    item.CommentType = this.apiClient.getRefDataByID(refData.UserCommentTypes, $("#comment-type").val());
                    item.UserName = $("#comment-username").val();
                    item.Comment = $("#comment-text").val();
                    item.Rating = $("#comment-rating").val();
                    this.showProgressIndicator();
                    this.appState.isSubmissionInProgress = true;
                    this.apiClient.submitUserComment(item, this.getLoggedInUserInfo(), function (jqXHR, textStatus) {
                        app.submissionCompleted(jqXHR, textStatus);
                        if (item.ChargePointID > 0) {
                            app.showDetailsViewById(item.ChargePointID, true);
                            app.navigateToLocationDetails();
                        }
                    }, $.proxy(this.submissionFailed, this));
                }
                else {
                    this.log("Comment submit not enabled");
                }
            }
        };
        App.prototype.performMediaItemSubmit = function () {
            var app = this;
            if (!app.appState.isSubmissionInProgress) {
                this.checkSelectedAPIMode();
                var $fileupload = $(':file');
                var mediafile = $fileupload[0].files[0];
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
                this.showProgressIndicator();
                this.appState.isSubmissionInProgress = true;
                this.apiClient.submitMediaItem(formData, this.getLoggedInUserInfo(), function (jqXHR, textStatus) {
                    app.submissionCompleted(jqXHR, textStatus);
                    if (app.viewModel.selectedPOI.ID > 0) {
                        app.showDetailsViewById(app.viewModel.selectedPOI.ID, true);
                        app.navigateToLocationDetails();
                    }
                }, function () {
                    app.appState.isSubmissionInProgress = false;
                    app.hideProgressIndicator();
                    app.showMessage("Sorry, your submission could not be processed. Please check your network connection or try again later.");
                }, $.proxy(this.mediaSubmissionProgress, this));
            }
        };
        App.prototype.mediaSubmissionProgress = function (progressEvent) {
            if (progressEvent.lengthComputable) {
                var percentComplete = Math.round((progressEvent.loaded / progressEvent.total) * 100);
                $("#file-upload-progress-bar").show();
                document.getElementById("file-upload-progress").style.width = percentComplete + "%";
            }
        };
        App.prototype.submissionCompleted = function (jqXHR, textStatus) {
            this.log("submission::" + textStatus, OCM.LogLevel.VERBOSE);
            this.hideProgressIndicator();
            this.appState.isSubmissionInProgress = false;
            if (textStatus != "error") {
                this.showMessage("Thank you for your contribution, you may need to refresh your search for changes to appear. If approval is required your change may take 1 or more days to show up. (Status Code: " + textStatus + ")");
                if (this.viewModel.selectedPOI != null) {
                    this.showDetailsView(document.getElementById("content-placeholder"), this.viewModel.selectedPOI);
                    this.showPage("locationdetails-page", "Location Details");
                }
                else {
                    this.navigateToMap();
                }
            }
            else {
                this.showMessage("Sorry, there was a problem accepting your submission. Please try again later. (Status Code: " + textStatus + ").");
            }
        };
        App.prototype.clearSearchRequest = function () {
            var app = this;
            if (app.appState._appSearchTimer != null) {
                clearTimeout(app.appState._appSearchTimer);
                app.appState._appSearchTimer = null;
                app.hideProgressIndicator();
            }
            app.appState._appSearchTimestamp = null;
            app.appState.isSearchInProgress = false;
            app.appState.isSearchRequested = false;
        };
        App.prototype.submissionFailed = function () {
            this.hideProgressIndicator();
            this.appState.isSubmissionInProgress = false;
            this.showMessage("Sorry, there was an unexpected problem accepting your contribution. Please check your internet connection and try again later.");
        };
        App.prototype.performSearch = function (useClientLocation, useManualLocation) {
            if (useClientLocation === void 0) { useClientLocation = false; }
            if (useManualLocation === void 0) { useManualLocation = false; }
            var app = this;
            if (this.appState._appSearchTimer == null) {
                if (this.appState._appSearchTimestamp != null) {
                    var timeSinceLastSearchMS = new Date() - this.appState._appSearchTimestamp;
                    if (timeSinceLastSearchMS < this.appConfig.searchFrequencyMinMS) {
                        this.log("Search too soon since last results. Skipping.");
                        return;
                    }
                }
                this.appState._appSearchTimestamp = new Date();
                this.appState._appSearchTimer = setTimeout(function () {
                    if (app.appState._appSearchTimer != null) {
                        clearTimeout(app.appState._appSearchTimer);
                        app.appState._appSearchTimer = null;
                        app.log("performSearch: previous search has timed out.");
                        app.hideProgressIndicator();
                        app.showMessage("Search timed out. Please check your network connection.");
                        app.logAnalyticsEvent("Search", "Timeout");
                    }
                }, this.appConfig.searchTimeoutMS);
                this.showProgressIndicator();
                this.appState.isSearchRequested = true;
                this.viewModel.isCachedResults = false;
                if (useClientLocation == true) {
                    if (this.geolocationManager.clientGeolocationPos == null) {
                        this.geolocationManager.determineUserLocation($.proxy(this.determineUserLocationCompleted, this), $.proxy(this.determineUserLocationFailed, this));
                        return;
                    }
                    else {
                        this.viewModel.searchPosition = this.geolocationManager.clientGeolocationPos;
                    }
                }
                var distance = parseInt(document.getElementById("search-distance").value);
                var distance_unit = document.getElementById("search-distance-unit").value;
                if (this.viewModel.searchPosition == null || useManualLocation == true) {
                    var locationText = document.getElementById("search-location").value;
                    if (locationText === null || locationText == "") {
                        app.logAnalyticsEvent("Search", "Geolocation");
                        this.geolocationManager.determineUserLocation($.proxy(this.determineUserLocationCompleted, this), $.proxy(this.determineUserLocationFailed, this));
                        return;
                    }
                    else {
                        app.logAnalyticsEvent("Search", "Geocode", locationText);
                        var lookupAttempted = this.geolocationManager.determineGeocodedLocation(locationText, $.proxy(this.determineGeocodedLocationCompleted, this), $.proxy(this.determineGeocodedLocationFailed, this));
                        if (lookupAttempted == true) {
                            return;
                        }
                    }
                }
                if (this.viewModel.searchPosition != null && this.viewModel.searchPosition.coords != null && !this.appState.isSearchInProgress) {
                    this.appState.isSearchInProgress = true;
                    var params = new OCM.POI_SearchParams();
                    params.latitude = this.viewModel.searchPosition.coords.latitude;
                    params.longitude = this.viewModel.searchPosition.coords.longitude;
                    params.distance = distance;
                    params.distanceUnit = distance_unit;
                    params.maxResults = this.appConfig.maxResults;
                    params.includeComments = true;
                    params.enableCaching = true;
                    if (this.appConfig.enableLiveMapQuerying) {
                        if (this.mappingManager.isMapReady()) {
                            var bounds = this.mappingManager.getMapBounds();
                            if (bounds != null) {
                                params.boundingbox = "(" + bounds[0].latitude + "," + bounds[0].longitude + "),(" + bounds[1].latitude + "," + bounds[1].longitude + ")";
                            }
                            var zoomLevel = this.mappingManager.getMapZoom();
                            if (zoomLevel > 10) {
                                params.levelOfDetail = 1;
                            }
                            else if (zoomLevel > 6) {
                                params.levelOfDetail = 3;
                            }
                            else if (zoomLevel > 4) {
                                params.levelOfDetail = 5;
                            }
                            else if (zoomLevel > 3) {
                                params.levelOfDetail = 10;
                            }
                            else {
                                params.levelOfDetail = 20;
                            }
                            this.log("zoomLevel:" + zoomLevel + "  :Level of detail:" + params.levelOfDetail);
                        }
                    }
                    if ($("#filter-submissionstatus").val() != 200)
                        params.submissionStatusTypeID = $("#filter-submissionstatus").val();
                    if ($("#filter-connectiontype").val() != "")
                        params.connectionTypeID = $("#filter-connectiontype").val();
                    if ($("#filter-minpowerkw").val() != "")
                        params.minPowerKW = $("#filter-minpowerkw").val();
                    if ($("#filter-operator").val() != "")
                        params.operatorID = $("#filter-operator").val();
                    if ($("#filter-connectionlevel").val() != "")
                        params.levelID = $("#filter-connectionlevel").val();
                    if ($("#filter-usagetype").val() != "")
                        params.usageTypeID = $("#filter-usagetype").val();
                    if ($("#filter-statustype").val() != "")
                        params.statusTypeID = $("#filter-statustype").val();
                    this.checkSelectedAPIMode();
                    this.log("Performing search..");
                    app.logAnalyticsEvent("Search", "Perform Search");
                    this.apiClient.fetchLocationDataListByParam(params, "ocm_app.handleSearchCompleted", "ocm_app.handleSearchError");
                }
            }
            else {
                this.log("Search still in progress, ignoring search request..");
            }
        };
        App.prototype.handleSearchError = function (result) {
            this.clearSearchRequest();
            if (result.status == 200) {
            }
            else {
                this.showMessage("There was a problem performing your search. Please check your internet connection.");
            }
        };
        App.prototype.determineUserLocationCompleted = function (pos) {
            this.clearSearchRequest();
            this.viewModel.searchPosition = pos;
            this.performSearch();
        };
        App.prototype.determineUserLocationFailed = function () {
            this.clearSearchRequest();
            this.showMessage("Could not automatically determine your location. Search by location name instead.");
        };
        App.prototype.determineGeocodedLocationCompleted = function (pos) {
            this.viewModel.searchPosition = pos;
            this.clearSearchRequest();
            this.mappingManager.updateMapCentrePos(pos.coords.latitude, pos.coords.longitude, true);
        };
        App.prototype.determineGeocodedLocationFailed = function () {
            this.clearSearchRequest();
            this.logAnalyticsEvent("Search", "Geocoding Failed");
            this.showMessage("The position of this address could not be determined. You may wish to try starting with a simpler address.");
        };
        App.prototype.handleSearchCompleted = function (poiList) {
            this.appState._appSearchTimestamp = new Date();
            this.clearSearchRequest();
            poiList = this.apiClient.hydrateCompactPOIList(poiList);
            this.viewModel.resultsBatchID++;
            this.viewModel.poiList = poiList;
            this.viewModel.searchPOIList = poiList;
            if (poiList != null && poiList.length > 0) {
                this.logAnalyticsEvent("Search", "Completed", "Results", poiList.length);
                this.log("Caching search results..");
                if (poiList.length <= 101) {
                    this.apiClient.setCachedDataObject("SearchResults", poiList);
                }
                else {
                    var cacheList = poiList.splice(0, 100);
                    this.apiClient.setCachedDataObject("SearchResults", cacheList);
                }
                this.apiClient.setCachedDataObject("SearchResults_Location", document.getElementById("search-location").value);
                this.apiClient.setCachedDataObject("SearchResults_SearchPos", this.viewModel.searchPosition);
            }
            else {
                this.log("No search results, will not overwrite cached search results.");
            }
        };
        App.prototype.renderPOIList = function (locationList) {
            this.log("Rendering " + locationList.length + " search results..");
            $("#search-no-data").hide();
            this.hideProgressIndicator();
            this.appState.isSearchInProgress = false;
            var appContext = this;
            var $listContent = $('<ul id="results-list" class="results-list"></ul>');
            if (this.resultItemTemplate == null)
                this.resultItemTemplate = $("#results-item-template").html();
            $listContent.children().remove();
            if (this.viewModel.poiList == null || this.viewModel.poiList.length == 0) {
                var $content = $("<li class=\"section-heading\"><p><span class=\"ui-li-count\">0 Results match your search</span></p></li>");
                $listContent.append($content);
            }
            else {
                var distUnitPref = document.getElementById("search-distance-unit");
                var distance_unit = "Miles";
                if (distUnitPref != null)
                    distance_unit = distUnitPref.value;
                var introText = "<h3 style='margin-top:0'>The following " + locationList.length + " locations match your search</h3>";
                if (this.viewModel.isCachedResults) {
                    introText = "<p class='alert alert-warning'>You are viewing old search results. Search again to see the latest information.</p>";
                }
                var $resultCount = $(introText);
                $listContent.append($resultCount);
                var isAlternate = false;
                for (var i = 0; i < this.viewModel.poiList.length; i++) {
                    var poi = this.viewModel.poiList[i];
                    var distance = poi.AddressInfo.Distance;
                    if (distance == null)
                        distance = 0;
                    var addressHTML = OCM.Utils.formatPOIAddress(poi);
                    var contactHTML = "";
                    contactHTML += OCM.Utils.formatPhone(poi.AddressInfo.ContactTelephone1, "Tel.");
                    var itemTemplate = "<li class='result'>" + this.resultItemTemplate + "</li>";
                    var locTitle = poi.AddressInfo.Title;
                    locTitle += "<div class='pull-right'>";
                    if (poi.UserComments && poi.UserComments != null) {
                        locTitle += "<span class='badge'><i class=\"fa fa-comment-o\" ></i> " + poi.UserComments.length + "</span>";
                    }
                    if (poi.MediaItems && poi.MediaItems != null) {
                        locTitle += "<span class='badge'><i class=\"fa fa-camera\" ></i> " + poi.MediaItems.length + "</span>";
                    }
                    locTitle += "</div>";
                    if (poi.AddressInfo.Town != null && poi.AddressInfo.Town != "")
                        locTitle = poi.AddressInfo.Town + " - " + locTitle;
                    itemTemplate = itemTemplate.replace("{locationtitle}", locTitle);
                    itemTemplate = itemTemplate.replace("{location}", addressHTML);
                    itemTemplate = itemTemplate.replace("{comments}", "");
                    var direction = "";
                    if (this.viewModel.searchPosition != null && this.viewModel.searchPosition.coords != null)
                        direction = this.geolocationManager.getCardinalDirectionFromBearing(this.geolocationManager.getBearing(this.viewModel.searchPosition.coords.latitude, this.viewModel.searchPosition.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));
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
                                        if (con.Level.ID > maxLevel.ID)
                                            maxLevel = con.Level;
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
                    if (isAlternate)
                        $item.addClass("alternate");
                    $item.on('click', { poi: poi }, function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                        try {
                            appContext.showDetailsView(this, e.data.poi);
                        }
                        catch (err) {
                        }
                        appContext.showPage("locationdetails-page", "Location Details");
                    });
                    $listContent.append($item);
                    isAlternate = !isAlternate;
                }
            }
            $('#results-list').replaceWith($listContent);
            $("#results-list").css("display", "block");
        };
        App.prototype.showDetailsViewById = function (id, forceRefresh) {
            var itemShown = false;
            if (this.viewModel.poiList != null) {
                for (var i = 0; i < this.viewModel.poiList.length; i++) {
                    if (this.viewModel.poiList[i].ID == id) {
                        this.showDetailsView(document.getElementById("content-placeholder"), this.viewModel.poiList[i]);
                        itemShown = true;
                    }
                    if (itemShown)
                        break;
                }
            }
            if (!itemShown || forceRefresh == true) {
                this.log("Location not cached or forced refresh, fetching details:" + id);
                this.apiClient.fetchLocationById(id, "ocm_app.showDetailsFromList", null, true);
            }
        };
        App.prototype.showDetailsFromList = function (results) {
            var app = this;
            if (results.length > 0) {
                app.showDetailsView(document.getElementById("content-placeholder"), results[0]);
            }
            else {
                this.showMessage("The location you are attempting to view does not exist or has been removed.");
            }
        };
        App.prototype.showDetailsView = function (element, poi) {
            var mapSideViewMode = true;
            var templatePrefix = "";
            this.viewModel.selectedPOI = poi;
            this.log("Showing OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
            this.logAnalyticsEvent("POI", "View", "Ref", poi.ID);
            if (this.isFavouritePOI(poi, null)) {
                $("#option-favourite-icon").removeClass("fa-heart-o");
                $("#option-favourite-icon").addClass("fa-heart");
            }
            else {
                $("#option-favourite-icon").removeClass("fa-heart");
                $("#option-favourite-icon").addClass("fa-heart-o");
            }
            var $element = $(element);
            var $poiTemplate;
            var $detailsView = null;
            if (mapSideViewMode == true) {
                $detailsView = $("#list-scroll-container");
            }
            else {
                $detailsView = $("#locationdetails-view");
                $detailsView.css("width", "90%");
                $detailsView.css("display", "block");
            }
            var poiDetails = OCM.Utils.formatPOIDetails(poi, false);
            $("#details-locationtitle").html(poi.AddressInfo.Title);
            $("#details-address").html(poiDetails.address);
            $("#details-driving").html(poiDetails.drivingInfo);
            $("#details-contact").html(poiDetails.contactInfo);
            $("#details-additional").html(poiDetails.additionalInfo);
            $("#details-advanced").html(poiDetails.advancedInfo);
            var viewWidth = $(window).width();
            this.mappingManager.showPOIOnStaticMap("details-map", poi, true, this.appState.isRunningUnderCordova, viewWidth, 200);
            var streetviewUrl = "https://maps.googleapis.com/maps/api/streetview?size=192x96&location=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&fov=90&heading=0&pitch=0&sensor=false";
            var streetViewLink = "https://maps.google.com/maps?q=&layer=c&cbll=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "&cbp=11,0,0,0,0";
            $("#details-streetview").html("<a href='#' onclick=\"window.open('" + streetViewLink + "', '_system');\"><img src=\"" + streetviewUrl + "\" width=\"192\" height=\"96\" title=\"Approximate Streetview (if available): " + poi.AddressInfo.Title + "\" /></a>");
            if (poi.UserComments != null) {
                var commentOutput = "<div class='comments'>";
                for (var c = 0; c < poi.UserComments.length; c++) {
                    var comment = poi.UserComments[c];
                    var commentDate = OCM.Utils.fixJSONDate(comment.DateCreated);
                    var fragment = "<div class='card comment'>" +
                        "<div class='row'>" +
                        "<div class='col-sm-3'>" + (comment.User != null ? "<img class='user' src='https://www.gravatar.com/avatar/" + comment.User.EmailHash + "?d=mm' />" : "<img class='user' src='https://www.gravatar.com/avatar/00?f=y&d=mm'/>") + "<br/>" +
                        ((comment.UserName != null && comment.UserName != "") ? comment.UserName : "(Anonymous)") +
                        "</div>" +
                        "<div class='col-sm-7'>" +
                        "<h3>" + (comment.CommentType != null ? comment.CommentType.Title + " - " : "") +
                        (comment.CheckinStatusType != null ? " " + comment.CheckinStatusType.Title : "") +
                        "</h3>" +
                        "<p>" + (comment.Comment != null ? comment.Comment : "(No Comment)") + "</p> " +
                        "<small>" + commentDate.toLocaleDateString() +
                        "<em>" +
                        "</em></small></div>" +
                        "<div class='col-sm-2'>" + (comment.Rating != null ? "" + comment.Rating + " out of 5" : " Not Rated") + "</div>" +
                        "</div></div>";
                    commentOutput += fragment;
                }
                commentOutput += "</div>";
                $("#details-usercomments").html(commentOutput);
            }
            else {
                $("#details-usercomments").html("<span class='prompt'>No comments/check-ins submitted yet. Help others by adding your comment.</span>");
            }
            if (poi.MediaItems != null) {
                var mediaItemOutput = "<div class='comments'>";
                for (var c = 0; c < poi.MediaItems.length; c++) {
                    var mediaItem = poi.MediaItems[c];
                    if (mediaItem.IsEnabled == true) {
                        var itemDate = OCM.Utils.fixJSONDate(mediaItem.DateCreated);
                        var mediaItemThumbnail = mediaItem.ItemThumbnailURL;
                        var mediumSizeThumbnail = mediaItemThumbnail.replace(".thmb.", ".medi.");
                        mediaItemOutput += "<div class='card comment'><div class='row'>" +
                            "<div class='col-xs-6'><a class='swipebox' href='" + mediumSizeThumbnail + "' target='_blank' title='" + ((mediaItem.Comment != null && mediaItem.Comment != "") ? mediaItem.Comment : poi.AddressInfo.Title) + "'><img class='img-responsive img-thumbnail' src='" + mediaItem.ItemThumbnailURL + "'/></a></div>" +
                            "<div class='col-xs-6'><p>" + (mediaItem.Comment != null ? mediaItem.Comment : "(No Comment)") + "</p> " +
                            "<small><cite>" + ((mediaItem.User != null) ? mediaItem.User.Username : "(Anonymous)") + " " + itemDate.toLocaleDateString() + "</cite></small>" +
                            "</div>" +
                            "</div></div>";
                    }
                }
                mediaItemOutput += "</div>";
                $("#details-mediaitems").html("");
                $("#details-mediaitems-gallery").html(mediaItemOutput);
                $('.swipebox').swipebox();
            }
            else {
                $("#details-mediaitems").html("<span class='prompt'>No photos submitted yet. Can you add one?</span>");
                $("#details-mediaitems-gallery").html("");
            }
            if (mapSideViewMode) {
                $poiTemplate = $("#details-content").detach();
                $poiTemplate.appendTo($detailsView);
                $("#results-list").hide();
                $(".details-body").css("margin-top", "0");
                $("#details-locationtitle").css("font-size", "1em");
                $("#details-map").hide();
            }
            else {
            }
            if (this.viewModel.searchPosition != null) {
                this.geolocationManager.getDrivingDistanceBetweenPoints(this.viewModel.searchPosition.coords.latitude, this.viewModel.searchPosition.coords.longitude, poi.AddressInfo.Latitude, poi.AddressInfo.Longitude, $("#search-distance-unit").val(), this.updatePOIDistanceDetails);
            }
            if (this.appState.languageCode != null) {
                OCM.Utils.applyLocalisation(false);
            }
        };
        App.prototype.adjustMainContentHeight = function () {
            var preferredContentHeight = $(window).height() - 90;
            if ($("#map-view").height() != preferredContentHeight) {
                this.log("adjusting content height:" + preferredContentHeight, OCM.LogLevel.VERBOSE);
                document.getElementById("map-view").style.height = preferredContentHeight + "px";
                document.getElementById("listview-container").style.height = preferredContentHeight + "px";
                document.getElementById("listview-container").style.maxHeight = preferredContentHeight + "px";
                $(".fit-to-viewport").height(preferredContentHeight);
                this.mappingManager.updateMapSize();
            }
            return preferredContentHeight;
        };
        App.prototype.refreshMapView = function () {
            if (!this.mappingManager.mapAPIReady) {
                this.log("app.refreshMapView: API Not Ready");
                return;
            }
            var mapHeight = this.adjustMainContentHeight();
            this.mappingManager.refreshMapView(mapHeight, this.viewModel.poiList, this.viewModel.searchPosition);
            this.mappingManager.setMapType($("#pref-basemap-type").val());
            this.appConfig.autoRefreshMapResults = true;
        };
        App.prototype.setMapFocus = function (hasFocus) {
            if (hasFocus) {
                this.mappingManager.focusMap();
            }
            else {
                this.mappingManager.unfocusMap();
            }
        };
        App.prototype.updatePOIDistanceDetails = function (response, status) {
            if (response != null) {
                var result = response.rows[0].elements[0];
                if (result.status == "OK") {
                    var origin = response.originAddresses[0];
                    $("#addr_distance").after(" - driving distance: " + result.distance.text + " (" + result.duration.text + ") from " + origin);
                }
            }
        };
        App.prototype.isFavouritePOI = function (poi, itineraryName) {
            if (itineraryName === void 0) { itineraryName = null; }
            if (poi != null) {
                var favouriteLocations = this.apiClient.getCachedDataObject("favouritePOIList");
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
        App.prototype.addFavouritePOI = function (poi, itineraryName) {
            if (itineraryName === void 0) { itineraryName = null; }
            if (poi != null) {
                if (!this.isFavouritePOI(poi, itineraryName)) {
                    var favouriteLocations = this.apiClient.getCachedDataObject("favouritePOIList");
                    if (favouriteLocations == null) {
                        favouriteLocations = new Array();
                    }
                    if (itineraryName != null) {
                        favouriteLocations.push({ "poi": poi, "itineraryName": itineraryName });
                    }
                    favouriteLocations.push({ "poi": poi, "itineraryName": null });
                    this.log("Added Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                    this.apiClient.setCachedDataObject("favouritePOIList", favouriteLocations);
                }
                else {
                    var favouriteLocations = this.apiClient.getCachedDataObject("favouritePOIList");
                    if (favouriteLocations != null) {
                        for (var i = 0; i < favouriteLocations.length; i++) {
                            if (favouriteLocations[i].poi.ID == poi.ID) {
                                favouriteLocations[i] = poi;
                                this.log("Updated Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                            }
                        }
                    }
                }
            }
        };
        App.prototype.removeFavouritePOI = function (poi, itineraryName) {
            if (itineraryName === void 0) { itineraryName = null; }
            if (poi != null) {
                if (this.isFavouritePOI(poi, itineraryName)) {
                    var favouriteLocations = this.apiClient.getCachedDataObject("favouritePOIList");
                    if (favouriteLocations == null) {
                        favouriteLocations = new Array();
                    }
                    var newFavList = new Array();
                    for (var i = 0; i < favouriteLocations.length; i++) {
                        if (favouriteLocations[i].poi.ID == poi.ID && favouriteLocations[i].itineraryName == itineraryName) {
                        }
                        else {
                            newFavList.push(favouriteLocations[i]);
                        }
                    }
                    this.apiClient.setCachedDataObject("favouritePOIList", newFavList);
                    this.log("Removed Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                }
                else {
                    this.log("Cannot remove: Not a Favourite POI OCM-" + poi.ID + ": " + poi.AddressInfo.Title);
                }
            }
        };
        App.prototype.toggleFavouritePOI = function (poi, itineraryName) {
            if (itineraryName === void 0) { itineraryName = null; }
            if (poi != null) {
                var $favIcon = $("#option-favourite-icon");
                if (this.isFavouritePOI(poi, itineraryName)) {
                    this.removeFavouritePOI(poi, itineraryName);
                    $favIcon.removeClass("fa-heart");
                    $favIcon.addClass("fa-heart-o");
                    this.logAnalyticsEvent("Favourite", "Remove", "Ref", poi.ID);
                }
                else {
                    this.addFavouritePOI(poi, itineraryName);
                    $favIcon.removeClass("fa-heart-o");
                    $favIcon.addClass("fa-heart");
                    this.logAnalyticsEvent("Favourite", "Add", "Ref", poi.ID);
                }
            }
        };
        App.prototype.getFavouritePOIList = function (itineraryName) {
            if (itineraryName === void 0) { itineraryName = null; }
            var favouriteLocations = this.apiClient.getCachedDataObject("favouritePOIList");
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
        App.prototype.syncFavourites = function () {
            var app = this;
            var favourites = this.getFavouritePOIList();
            if (favourites != null) {
                for (var i = 0; i < favourites.length; i++) {
                    var poi = favourites[i];
                    this.log("Refreshing data for favourite POI: OCM-" + poi.ID);
                    this.apiClient.fetchLocationById(poi.ID, "ocm_app.updateCachedFavourites", function (error) {
                        if (error.status != 200) {
                            app.log("Failed to refresh favourite POI." + JSON.stringify(error));
                        }
                    }, true);
                }
            }
        };
        App.prototype.updateCachedFavourites = function (poiList) {
            if (poiList != null) {
                for (var i = 0; i < poiList.length; i++) {
                    this.addFavouritePOI(poiList[i]);
                }
            }
        };
        App.prototype.switchLanguage = function (languageCode) {
            this.log("Switching UI language: " + languageCode);
            this.appState.languageCode = languageCode;
            localisation_dictionary = eval("localisation_dictionary_" + languageCode);
            OCM.Utils.applyLocalisation(false);
            this.storeSettings();
        };
        App.prototype.hidePage = function (pageId) {
            $("#" + pageId).hide();
        };
        App.prototype.showPage = function (pageId, pageTitle, skipState) {
            if (skipState === void 0) { skipState = false; }
            if (!pageTitle)
                pageTitle = pageId;
            this.log("app.showPage:" + pageId, OCM.LogLevel.VERBOSE);
            this.logAnalyticsView(pageId);
            if (pageId != "startup-page")
                document.getElementById("startup-page").style.display = 'none';
            if (this.appState._lastPageId && this.appState._lastPageId != null) {
                if (this.appState._lastPageId == "map-page" && pageId == "map-page") {
                    this.appState._appQuitRequestCount++;
                    if (this.appState._appQuitRequestCount >= 3) {
                        this.log("Multiple Home Requests, Time to Quit");
                        if (this.appState.isRunningUnderCordova) {
                            if (confirm("Quit Open Charge Map?")) {
                                navigator.app.exitApp();
                            }
                        }
                    }
                }
                else {
                    this.appState._appQuitRequestCount = 0;
                }
                this.hidePage(this.appState._lastPageId);
            }
            document.getElementById(pageId).style.display = "block";
            if (!this.appState.isEmbeddedAppMode) {
                setTimeout(function () { document.documentElement.scrollIntoView(); }, 100);
            }
            this.appState._lastPageId = pageId;
            if (skipState) {
            }
            else {
                Historyjs.pushState({ view: pageId, title: pageTitle }, pageTitle, "?view=" + pageId);
            }
            this.toggleMenu(false);
        };
        App.prototype.initStateTracking = function () {
            var app = this;
            if (document.location.protocol === 'file:') {
            }
            var State = Historyjs.getState();
            State.data.view = "map-page";
            Historyjs.log('initial:', State.data, State.title, State.url);
            Historyjs.Adapter.bind(window, 'statechange', function () {
                var State = Historyjs.getState();
                if (State.data.view) {
                    if (app.appState._lastPageId && app.appState._lastPageId != null) {
                        if (State.data.view == app.appState._lastPageId)
                            return;
                    }
                    app.showPage(State.data.view, State.Title, true);
                }
                else {
                    app.navigateToHome();
                    app.log("pageid:" + app.appState._lastPageId);
                }
                if ($.swipebox && $.swipebox.isOpen) {
                    $('html').removeClass('swipebox-html');
                    $('html').removeClass('swipebox-touch');
                    $("#swipebox-overlay").remove();
                    $(window).trigger('resize');
                }
            });
        };
        App.prototype.navigateToHome = function () {
            this.log("Navigate To: Start Page (Map)", OCM.LogLevel.VERBOSE);
            this.navigateToMap();
        };
        App.prototype.navigateToMap = function (showLatestSearchResults) {
            if (showLatestSearchResults === void 0) { showLatestSearchResults = true; }
            this.log("Navigate To: Map Page", OCM.LogLevel.VERBOSE);
            this.showPage("map-page", "Map");
            var app = this;
            $("#search-title-favourites").hide();
            $("#search-title-main").show();
            if (showLatestSearchResults) {
                app.viewModel.poiList = app.viewModel.searchPOIList;
            }
            this.setMapFocus(true);
            setTimeout(function () { app.refreshMapView(); }, 250);
        };
        App.prototype.navigateToFavourites = function () {
            this.log("Navigate To: Favourites", OCM.LogLevel.VERBOSE);
            var app = this;
            var favouritesList = app.getFavouritePOIList();
            if (favouritesList === null || favouritesList.length === 0) {
                $("#favourites-list").html("<p>You have no favourite locations set. To add or remove a favourite, select the <i title=\"Toggle Favourite\" class=\"fa fa-heart-o\"></i> icon when viewing a location.</p>");
                this.showPage("favourites-page", "Favourites");
            }
            else {
                app.viewModel.poiList = favouritesList;
                app.navigateToMap(false);
                $("#search-title-main").hide();
                $("#search-title-favourites").show();
            }
        };
        App.prototype.navigateToAddLocation = function () {
            this.log("Navigate To: Add Location", OCM.LogLevel.VERBOSE);
            var app = this;
            if (!app.isUserSignedIn()) {
                if (app.appConfig.allowAnonymousSubmissions) {
                    app.showMessage("You are not signed in. You should sign in unless you wish to submit your edit anonymously.");
                }
                else {
                    app.showMessage("You need to Sign In before you can add a location.");
                    return;
                }
                ;
            }
            app.isLocationEditMode = false;
            app.viewModel.selectedPOI = null;
            app.showLocationEditor();
            this.showPage("editlocation-page", "Add Location");
        };
        App.prototype.navigateToEditLocation = function () {
            this.log("Navigate To: Edit Location", OCM.LogLevel.VERBOSE);
            var app = this;
            if (!app.isUserSignedIn()) {
                if (app.appConfig.allowAnonymousSubmissions) {
                    app.showMessage("You are not signed in. You should sign in unless you wish to submit your edit anonymously.");
                }
                else {
                    app.showMessage("You need to Sign In before you can edit locations.");
                    return;
                }
                ;
            }
            app.showLocationEditor();
            app.showPage("editlocation-page", "Edit Location", true);
        };
        App.prototype.navigateToLocationDetails = function () {
            this.log("Navigate To: Location Details", OCM.LogLevel.VERBOSE);
            this.showPage("locationdetails-page", "Charging Location");
        };
        App.prototype.navigateToProfile = function () {
            this.log("Navigate To: Profile", OCM.LogLevel.VERBOSE);
            this.showPage("profile-page", "Sign In");
        };
        App.prototype.navigateToSettings = function () {
            this.log("Navigate To: Settings", OCM.LogLevel.VERBOSE);
            this.showPage("settings-page", "Settings");
        };
        App.prototype.navigateToStartup = function () {
            this.log("Navigate To: Startup", OCM.LogLevel.VERBOSE);
            this.showPage("startup-page", "Open Charge Map", true);
        };
        App.prototype.navigateToAbout = function () {
            this.log("Navigate To: About", OCM.LogLevel.VERBOSE);
            try {
                var dataProviders = this.apiClient.referenceData.DataProviders;
                var summary = "<ul>";
                for (var i = 0; i < dataProviders.length; i++) {
                    if (dataProviders[i].IsApprovedImport == true || dataProviders[i].IsOpenDataLicensed == true) {
                        summary += "<li>" + dataProviders[i].Title + (dataProviders[i].License != null ? ": <small class='subtle'>" + dataProviders[i].License : "") + "</small></li>";
                    }
                }
                summary += "</ul>";
                $("#about-data").html(summary);
            }
            catch (exception) {
                ;
                ;
            }
            this.showPage("about-page", "About");
        };
        App.prototype.navigateToNews = function () {
            this.log("Navigate To: News", OCM.LogLevel.VERBOSE);
            this.showPage("news-page", "News");
            document.getElementById("news-frame").src = this.appConfig.newsHomeUrl;
        };
        App.prototype.navigateToAddComment = function () {
            this.log("Navigate To: Add Comment", OCM.LogLevel.VERBOSE);
            var app = this;
            if (!app.isUserSignedIn()) {
                if (app.appConfig.allowAnonymousSubmissions) {
                    app.showMessage("You are not signed in. You should sign in unless you wish to submit your edit anonymously.");
                }
                else {
                    app.showMessage("You need to Sign In before adding a comment.");
                    return;
                }
                ;
            }
            document.getElementById("comment-form").reset();
            app.appState.enableCommentSubmit = true;
            this.showPage("submitcomment-page", "Add Comment", false);
        };
        App.prototype.navigateToAddMediaItem = function () {
            this.log("Navigate To: Add Media Item", OCM.LogLevel.VERBOSE);
            var app = this;
            if (!app.isUserSignedIn()) {
                app.showMessage("You need to Sign In before you can add photos.");
            }
            else {
                this.showPage("submitmediaitem-page", "Add Media", false);
            }
        };
        App.prototype.showConnectionError = function () {
            $("#progress-indicator").hide();
            $("#network-error").show();
        };
        App.prototype.showAuthorizationError = function () {
            this.showMessage("Your session has expired, please sign in again.");
        };
        App.prototype.toggleMenu = function (showMenu) {
            if (showMenu != null)
                this.appState.menuDisplayed = !showMenu;
            if (this.appState.menuDisplayed) {
                this.setMapFocus(true);
                this.appState.menuDisplayed = false;
                $("#app-menu-container").hide();
            }
            else {
                this.appState.menuDisplayed = true;
                this.setMapFocus(false);
                $("#app-menu-container").show();
            }
        };
        App.prototype.jumpToPageTop = function () {
            this.log("Status tapped, scrolling to page top.");
            window.scrollTo(0, 0);
        };
        App.prototype.initPlacesAutocomplete = function () {
            var input = document.getElementById('search-location');
            this._autocomplete = new google.maps.places.Autocomplete(input);
            this._autocomplete.addListener('place_changed', $.proxy(this.searchFromSelectedPlace, this));
        };
        App.prototype.searchFromSelectedPlace = function () {
            var place = this._autocomplete.getPlace();
            if (place != null) {
                this.determineGeocodedLocationCompleted(new OCM.GeoPosition(place.geometry.location.lat(), place.geometry.location.lng()));
            }
        };
        return App;
    }(OCM.LocationEditor));
    OCM.App = App;
})(OCM || (OCM = {}));
