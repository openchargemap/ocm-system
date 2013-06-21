/*
OCM charging location browser/editor Mobile App
Christopher Cook
http://openchargemap.org

See http://www.openchargemap.org/ for more details
*/

function OCM_App() {
    this.ocm_ui = new OCM_CommonUI();
    this.ocm_geo = new OCM_Geolocation();
    this.ocm_data = new OCM_Data();
    this.numConnectionEditors = 5;
    this.maxResults = 1000;
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
    this.isRunningUnderCordova= false;
    this.ocm_app_context = this;
    //this.ocm_data.clientName="ocm.app.android";
    //this.baseURL = "http://localhost:8090/App/";
    //this.loginProviderRedirectURL = "http://localhost:8089/LoginProvider/Default.aspx?_mode=silent&_forceLogin=true&_redirectURL=" + this.baseURL;
    //this.ocm_data.serviceBaseURL = "http://localhost:55883/";
}

OCM_App.prototype.logEvent = function (logMsg) {
    if (window.console) {
        window.console.log(logMsg);
    }
};

OCM_App.prototype.initApp = function () {

    var app = this.ocm_app_context;

    this.showProgressIndicator();
    this.appInitialised = true;

    //wire up button events
    
    $(document).delegate("#search-nearby", "click",
				function () {
				    app.performSearch(true, false);
				    return false;
				}
			);

    $(document).delegate("#search-button", "click",
				function () {
				    app.performSearch(false, true);
				    return false;
				}
			);

    $("#popupPanel").on({
        popupbeforeposition: function () {
            var h = $(window).height();

            $("#popupPanel").css("height", h);
        }
    });

    $(document).delegate("#editlocation-submit", "click",
				function () {
				    if (app.validateLocationEditor() == true) {
				        app.performLocationSubmit();
				    }
				    return false;
				}
			);

    $(document).delegate("#details-edit", "click",
		function () {
		    app.showLocationEditor();
		    return false;
		}
	);

    $(document).delegate("#option-favourite", "click", function() {
         app.toggleFavouritePOI(app.selectedPOI, null);
    });
    
    $(document).delegate("#option-edit", "click", function () {

        if (app.isUserSignedIn()) {
            //show editor
            app.showLocationEditor();
        } else {
            app.showMessage("Please Sign In before editing.");
        }
        return false;
    });

    $(document).delegate("#option-checkin", "click", function () { 
        //show checkin/comment page   
        $.mobile.changePage( "#submitcomment-page");
        return false;
    });

    $(document).delegate("#map-page", "pageshow", function () {
        app.refreshMapView();
    });

    $(document).delegate("#submitcomment-button", "click",
				function () {
				    if (app.enableCommentSubmit == true) {
				        app.enableCommentSubmit = false;
				        app.performCommentSubmit();
				    }
				    return false;
				}
			);

    $(document).delegate("#submitcomment-page", "pageshow", function (event, ui) {
        app.logEvent("Showing submit comment page..");
        //reset comment form on show
        document.getElementById("comment-form").reset();
        app.enableCommentSubmit = true;
    });

    $(document).delegate("#add-location", "click", function (event, ui) {
        app.isLocationEditMode = false;
        app.selectedPOI = null;
        app.showLocationEditor();
        return false;
    });

    $(document).delegate("#favourites-page", "pageshow", function () {
        //get list of favourites as POI list and render in standard search page
        var favouritesList = app.getFavouritePOIList();
        if (favouritesList == null || favouritesList.length == 0) {
            $("#favourites-list").html("<p>You have no favourite locations set. To add or remove a favourite, tap the <i title=\"Toggle Favourite\" class=\"icon-heart-empty\"></i> icon when viewing a location.</p>");
        } else {
            if ($.mobile) $.mobile.changePage("#search-page");
            app.renderLocationList(favouritesList);

            //TODO: manage favourites in single page?

        }
    });

    $('#intro-section').show();

    this.initEditors();

    //load options settings from storage/cookies
    this.loadSettings();

    //when options change, store settings
    $('#search-distance').change(function () { app.storeSettings(); });
    $('#search-distance-unit').change(function () { app.storeSettings(); });
    $('#option-enable-experiments').change(function () { app.storeSettings(); });

    //setTimeout(function(){	
    ocm_app.initDeferredUI();
    //},50);

};

OCM_App.prototype.postLoginInit = function () {
    var userInfo = this.getLoggedInUserInfo();

    //if user logged in, enable features
    if (!this.isUserSignedIn()) {
            //user is not signed in
            //$("#login-summary").html("Register and login (via your Twitter account, if you have one): <input type=\"button\" data-inline='true' value=\"Register or Sign In\" onclick='ocm_app.beginLogin();' />").trigger("create");		
	            $("#login-summary").html("<input type=\"button\" id=\"login-button\" data-inline=\"true\" data-mini=\"true\" data-icon=\"arrow-r\" value=\"Sign In\" onclick='ocm_app.beginLogin();'/>").trigger("create");;
            $("#user-profile-info").html("You are not currently signed in.");
            $("#login-button ").show();
    } else {
            //user is signed in
            $("#user-profile-info").html("You are signed in as: " + userInfo.Username + " <input type=\"button\" data-mini=\"true\" data-inline='true' value=\"Sign Out\" onclick='ocm_app.logout();' />").trigger("create");
            $("#login-button").hide();
            
    }
    
};

OCM_App.prototype.initDeferredUI = function () {
    this.logEvent("Init of deferred UI..");
  
    //check if user sign in
    this.postLoginInit();
    
    if (this.isRunningUnderCordova)
    {
    	navigator.splashscreen.hide();
    }
    
    if ($("#option-enable-experiments").val() == "on") {
        this.enableExperimental = true;
        $("#nav-items").append("<li><a href=\"#experiment-page\" data-icon=\"info\">Kapow!</a></li>").trigger("create");
        $("#navbar").trigger("create");
        $("#navbar").navbar("refresh");
        $("#experiment-page").delegate('pageshow', function (event, ui) {
            app.initExperimentalContent();
        });
    } else {
        this.enableExperimental = false;
    }

    //if cached results exist, render them
    var cachedResults = this.ocm_data.getCachedDataObject("SearchResults");
    var cachedResult_Location = this.ocm_data.getCachedDataObject("SearchResults_Location");
    if (cachedResults != null) {
        if (cachedResult_Location != null) document.getElementById("search-location").value = cachedResult_Location;
        setTimeout(function () { ocm_app.renderLocationList(cachedResults); }, 50);
    }

    //if ID of location passed in, show details view
    var idParam = ocm_app.getParameter("id");
    if (idParam != null && idParam != "") {

        var _id = parseInt(ocm_app.getParameter("id"));
        setTimeout(function () {
            ocm_app.showDetailsViewById(_id);
        }, 100);
    }
};

OCM_App.prototype.isUserSignedIn = function () {
    var userInfo = this.getLoggedInUserInfo();
    if (userInfo.Username != null && userInfo.Username != "") return true;
    else return false;
};

OCM_App.prototype.beginLogin = function () {
    this.showProgressIndicator();

    if (this.isRunningUnderCordova)
    {
        //do phonegapped login using InAppBrowser
        var ref = window.open(this.loginProviderRedirectBaseURL +'AppLogin', '_blank', 'location=yes');
        var _app =this;
        ref.addEventListener('loadstop', function (event) {
        	_app.hideProgressIndicator();

            _app.logEvent('fetching login result: ' + event.url);
        
            		ref.executeScript({
                        code: "getSignInResult();"
                    }, function(result) {
                        if (result!=null) {
                ref.close();
                             var userInfo = result[0];
                             
                             _app.logEvent('got login: ' + userInfo.Username);
                             
                             _app.setLoggedInUserInfo(userInfo);
                             _app.postLoginInit();
            }
                        else {
                        	//no sign in result
                        }
                        
                        //return to home
                        $.mobile.changePage("#home-page");

                    });
            		    
       
        });
        ref.addEventListener('loaderror', function (event) { 
        	_app.logEvent('error: ' + event.message); }
        );
        ref.addEventListener('exit', function (event) { 
        	_app.logEvent(event.type); 
        });
    }
    else {
    	//do normal web login 
        window.location = this.loginProviderRedirectURL;
    }
};

OCM_App.prototype.logout = function () {
    this.clearCookie("Identifier");
    this.clearCookie("Username");
    this.clearCookie("OCMSessionToken");
    this.clearCookie("AccessToken");
    this.clearCookie("AccessPermissions");

    if (this.isRunningUnderCordova){
    	$.mobile.changePage("#home-page");
    }
    else {
    var app = this.ocm_app_context;
    setTimeout(function () { window.location = app.baseURL; }, 100);
    }
   
};

OCM_App.prototype.getLoggedInUserInfo = function () {
    var userInfo = {
        "Identifier": this.getCookie("Identifier"),
        "Username": this.getCookie("Username"),
        "SessionToken": this.getCookie("OCMSessionToken"),
        "AccessToken": this.getCookie("AccessToken"),
        "Permissions": this.getCookie("AccessPermissions")
    };
    return userInfo;
};

OCM_App.prototype.setLoggedInUserInfo = function (userInfo) {
 
    this.setCookie("Identifier", userInfo.Identifier);
    this.setCookie("Username",userInfo.Username);
    this.setCookie("OCMSessionToken", userInfo.OCMSessionToken);
    this.setCookie("AccessToken", userInfo.AccessToken);
    this.setCookie("AccessPermissions", userInfo.AccessPermissions);
  
};
OCM_App.prototype.storeSettings = function () {
    //save option settings to cookies
    this.setCookie("optsearchdist", $('#search-distance').val());
    this.setCookie("optsearchdistunit", $('#search-distance-unit').val());
    this.setCookie("optenableexperiments", $('#option-enable-experiments').val());
};

OCM_App.prototype.loadSettings = function () {
    if (this.getCookie("optsearchdist") != null) $('#search-distance').val(this.getCookie("optsearchdist"));
    if (this.getCookie("optsearchdistunit") != null) $('#search-distance-unit').val(this.getCookie("optsearchdistunit"));
    if (this.getCookie("optenableexperiments") != null) $('#option-enable-experiments').val(this.getCookie("optenableexperiments"));
};

OCM_App.prototype.performCommentSubmit = function () {

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
};

OCM_App.prototype.submissionCompleted = function (jqXHR, textStatus) {
    this.logEvent("submission::" + textStatus);

    this.hideProgressIndicator();
    if (textStatus != "error") {
        this.showMessage("Thank you for your contribution, you may need to refresh your browser page for changes to appear. If approval is required your change may take 24hrs or more to show up. (Status Code: " + textStatus + ")");
        //navigate back to search page
        $.mobile.changePage("#search-page");

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
    if (window.google) {
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
                var lookupAttempted = this.ocm_geo.determineGeocodedLocation(
						locationText, $.proxy(
								this.determineGeocodedLocationCompleted, this));
                if (lookupAttempted == true)
                    return;
            }
        }

        if (this.ocm_app_searchPos != null && !this.searchInProgress) {
            this.searchInProgress = true;

            var params = new OCM_LocationSearchParams();
            params.latitude = this.ocm_app_searchPos.lat();
            params.longitude = this.ocm_app_searchPos.lng();
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
					"ocm_app.renderLocationList", this.handleSearchError);
        }
    } else {
        this.logEvent("No google maps..");
        this.showMessage("Geolocation not available. Search on your position is unavailable.");
    }
};

OCM_App.prototype.handleSearchError = function (result) {
    //self.hideProgressIndicator();

    //self.logEvent("Search status: "+result.status);

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

OCM_App.prototype.renderLocationList = function (locationList) {

    this.resultBatchID++; //indicates that results have changed and need reprocessed (maps etc)

    if (locationList != null && locationList.length > 0) {
        this.logEvent("Caching search results..");
        this.ocm_data.setCachedDataObject("SearchResults", locationList);
        this.ocm_data.setCachedDataObject("SearchResults_Location", document.getElementById("search-location").value);
    } else {
        this.logEvent("No search results, will not overwrite cached search results.");
    }

    this.logEvent("Rendering search results..");
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

            if (this.ocm_app_searchPos != null) direction = this.ocm_geo.getCardinalDirectionFromBearing(this.ocm_geo.getBearing(this.ocm_app_searchPos.lat(), this.ocm_app_searchPos.lng(), poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));

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
            $item.on('click', { poi: poi }, function (event) {
                appContext.showDetailsView(this, event.data.poi);
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
                if ($.mobile) $.mobile.changePage("#locationdetails-page");
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
    if (results.length > 0) {
        setTimeout(function () {
            ocm_app.showDetailsView(document.getElementById("content-placeholder"), results[0]);
            if ($.mobile) $.mobile.changePage("#locationdetails-page");
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
    
    //TODO: all users can initiate edit - requires API change
    //if edit option available to user, enable edit controls
    var $editControl = $("#details-edit");
    if (!this.hasUserPermissionForPOI(poi, "Edit")) {
        $editControl.hide();
    } else {
        $editControl.show();
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
    $("#details-streetview").html("<a target='_blank' href='" + streetViewLink + "'><img src=\"" + streetviewUrl + "\" title=\"Approximate Streetview (if available): " + poi.AddressInfo.Title + "\" /></a>");

    if (poi.UserComments != null) {
        var commentOutput = "<div class='comments'>";

        for (var c = 0; c < poi.UserComments.length; c++) {
            var comment = poi.UserComments[c];
            var commentDate = this.ocm_ui.fixJSONDate(comment.DateCreated);
            commentOutput += "<blockquote><strong>" + (comment.Rating != null ? comment.Rating + " out of 5" : "(Not Rated)") +
                (comment.CommentType != null ? " : " + comment.CommentType.Title : "") +
                (comment.CheckinStatusType != null ? " : " + comment.CheckinStatusType.Title : "") +
                "</strong>" +
                "<p>" + (comment.Comment != null ? comment.Comment : "(No Comment)") + "</p> " +
				"<small><cite>" + ((comment.UserName != null && comment.UserName != "") ? comment.UserName : "(Anonymous)") + " : " + commentDate.toLocaleDateString() + "</cite></small>" +
				"</blockquote>";
        }
        commentOutput += "</div>";

        $("#details-usercomments").html(commentOutput);

    } else {
        $("#details-usercomments").html("No comments submitted.");
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

OCM_App.prototype.addFavouritePOI = function(poi, itineraryName) {
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

OCM_App.prototype.removeFavouritePOI = function(poi, itineraryName) {
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

OCM_App.prototype.toggleFavouritePOI = function(poi, itineraryName) {
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

    //setup map view if not already initialised
    this.ocm_ui.initMap("map-view");

    var $resultcount = $("#map-view-resultcount");

    if (this.locationList != null && this.locationList.length > 0) {
        $resultcount.html("Showing " + this.locationList.length + " Results:");
    } else {
        $resultcount.html("No Results. Perform a search to see results on this map");
    }

    this.ocm_ui.showPOIListOnMap2("map-view", this.locationList, this, $resultcount, this.resultBatchID);

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

//methods for use by native build of app to enable navigation via native UI/phonegap
function navigateToSearch() {
    $.mobile.changePage("#search-page", { transition: "slideup" });
};

function navigateToMap() {
    $.mobile.changePage("#map-page", { transition: "slideup" });
};

function navigateToEditor() {
    $.mobile.changePage("#editlocation-page", { transition: "slideup" });
};

function navigateToLogin() {
    $.mobile.changePage("#login-page", { transition: "slideup" });
};

function navigateToAbout() {
    $.mobile.changePage("#about-page", { transition: "slideup" });
};

function showSearchOptions() {
    navigateToAbout();
}

OCM_App.prototype.showMessage = function (msg) {
    if (this.isRunningUnderCordova)
    {
        navigator.notification.alert(
           msg,  // message
           function () { ;;},         // callback
           'Open Charge Map',            // title
           'OK'                  // buttonName
       );
    } else {
        alert(msg);
    }

};