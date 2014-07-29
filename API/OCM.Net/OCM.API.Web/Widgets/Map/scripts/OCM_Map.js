function OCM_MapOptions() {
    this.enableClustering = true;
    this.pauseOnLoad = false;
    this.pausedMessage = "tap or click to view map";
    this.showFilter_NearLocation = true;
    this.showFilter_Distance = true;
    this.showFilter_DistanceUnit = true;
    this.showFilter_Operator = false;
    this.showFilter_ConnectionTypes = false;
    this.showFilter_Levels = true;
    this.showFilter_Country = false;
    this.showFilter_SubmissionStatus = false;
    this.showFilter_UsageType = false;
    this.showFilter_StatusType = false;

    this.geolocateUserOnLoad = false;

    this.searchParams = new OCM.POI_SearchParams();

    this.useMarkerIcons = true;
    this.useMarkerAnimation = true;
    this.isTestLocalisationMode = false;
    this.iconSet = "LargeMarkers";

}


function OCM_Map() {
    this.ocm_ui = new OCM_CommonUI();
    this.ocm_data = new OCM.API();
    this.ocm_geo = new OCM.Geolocation(this.ocm_data);
    this.ocm_geo.ocm_data = this.ocm_data;
    this.mapOptions = new OCM_MapOptions();

    this.ocm_ui.ocm_markers = null;
    this.ocm_markerClusterer = null;

    this.searchNearMePrompt = "(My Location)";
    this.ocm_data.clientName = "ocm.mapWidget";

    //optionally enable clustering
    try {
        if (typeof (MarkerClusterer)) {
            this.ocm_markerClusterer = new MarkerClusterer();
        }
    }
    catch (err) {
        this.mapOptions.enableClustering = false;
    }

}

//TODO: move to common
OCM_Map.prototype.getParameter = function (name) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(window.location.href);
    if (results == null)
        return "";
    else
        return results[1];
};

OCM_Map.prototype.renderLocations = function (locationlist) {
    this.showPOIListOnMap("map-canvas", locationlist, this, $("#map-canvas"));
};

//TODO: move to common app util
OCM_Map.prototype.hasUserPermissionForPOI = function () {
    return false;
};

OCM_Map.prototype.getLocalisation = function (resourceKey, defaultValue, isTestMode) {
    return this.ocm_ui.getLocalisation(resourceKey, defaultValue, isTestMode);
};

//TODO: remove references and redirect to common ui
OCM_Map.prototype.applyLocalisation = function (isTestMode) {
    return this.ocm_ui.applyLocalisation(isTestMode);
};

OCM_Map.prototype.showDetailsView = function (element, poi) {
    var $element = $(element);
    var $detailsView = $("#details-content");

    $detailsView.html("<iframe style='width:100%;height:100%;border:none;' frameborder=0 src='http://openchargemap.org/site/poi/details/" + poi.ID + "?layout=simple&languagecode=" + this.language_code + "'></iframe>");

    var leftPos = $element.position().left;
    var topPos = $element.position().top;
    var maxDialogHeight = $(window).height() - 128;
    if (maxDialogHeight < 300) maxDialogHeight = 300; //workaround firefox differences
    if (maxDialogHeight > 800) maxDialogHeight = 800;
    $detailsView.css("left", leftPos);
    $detailsView.css("top", topPos);

    //if edit optional available, enable edit controls
    var $editControl = $("#details-edit");
    if (!this.hasUserPermissionForPOI(poi, "Edit")) {
        $editControl.hide();
    } else {
        $editControl.show();
    }

    this.applyLocalisation(ocm_map.mapOptions.isTestLocalisationMode);

    var dialogWidth = $(window).width() * 0.66;
    if (dialogWidth < 300) dialogWidth = 300;
    $("#details-content").dialog({ autoOpen: true, width: dialogWidth, height: maxDialogHeight, title: this.getLocalisation("ocm.details.locationDetails", "Location Details", ocm_map.mapOptions.isTestLocalisationMode) });
};

OCM_Map.prototype.toggleFilter = function (filterID, filterEnabled) {
    if (filterEnabled == true) {
        $(filterID).show();
        return 1;
    } else {
        $(filterID).hide();
        return 0;
    }
};

OCM_Map.prototype.initMap = function () {
    this.mapOptions.searchParams.additionalParams = "";
    this.mapOptions.searchParams.includeComments = true;
    this.mapOptions.mapTitle = "Charging Location Map";

    //$("#map-heading").css("width",200);
    if (this.getParameter("maptitle") != "") {
        this.mapOptions.mapTitle = unescape(this.getParameter("maptitle"));
    }

    if (this.getParameter("iconset") != "") {
        this.mapOptions.iconSet = unescape(this.getParameter("iconset"));
    }

    if (this.getParameter("clustering") != "") {
        var val = unescape(this.getParameter("clustering"));
        if (val == "false")
        {
            this.mapOptions.enableClustering = false;
        } else {
            //default to true
            this.mapOptions.enableClustering = true;
        }
    }

    if (this.getParameter("maxresults") != "") {
        this.mapOptions.searchParams.maxResults = unescape(this.getParameter("maxresults"));
    } else {
        this.mapOptions.searchParams.maxResults = 5000;
    }

    if (this.getParameter("countrycode") != "") {
        this.mapOptions.searchParams.countryCode = unescape(this.getParameter("countrycode"));
    }

    if (this.getParameter("latitude") != "") {
        this.mapOptions.searchParams.latitude = unescape(this.getParameter("latitude"));
    }

    if (this.getParameter("longitude") != "") {
        this.mapOptions.searchParams.longitude = unescape(this.getParameter("longitude"));
    }

    if (this.getParameter("distance") != "") {
        this.mapOptions.searchParams.distance = unescape(this.getParameter("distance"));
        $("#search-distance").val(this.mapOptions.searchParams.distance);
    }

    if (this.getParameter("distanceunit") != "") {
        this.mapOptions.searchParams.distanceUnit = unescape(this.getParameter("distanceunit"));
        $("#search-distance-unit").val(this.mapOptions.searchParams.distanceUnit);
    }

    if (this.getParameter("operatorname") != "") {
        this.mapOptions.searchParams.additionalParams += "&operatorname=" + unescape(this.getParameter("operatorname"));
    }

    if (this.getParameter("connectiontype") != "") {
        this.mapOptions.searchParams.additionalParams += "&connectiontype=" + unescape(this.getParameter("connectiontype"));
    }

    if (this.getParameter("connectiontypeid") != "") {
        this.mapOptions.searchParams.connectionTypeID = this.getParameter("connectiontypeid");
        $("#connectiontypeid").val(this.mapOptions.searchParams.connectionTypeID);
    }

    if (this.getParameter("operatorid") != "") {
        this.mapOptions.searchParams.operatorID = this.getParameter("operatorid");
        $("#operatorid").val(this.mapOptions.searchParams.operatorID);
    }

    if (this.getParameter("countryid") != "") {
        this.mapOptions.searchParams.countryID = this.getParameter("countryid");
        $("#countryid").val(this.mapOptions.searchParams.countryID);
    }

    if (this.getParameter("levelid") != "") {
        this.mapOptions.searchParams.levelID = this.getParameter("levelid");
        $("#levelid").val(this.mapOptions.searchParams.levelID);
    }

    if (this.getParameter("usagetypeid") != "") {
        this.mapOptions.searchParams.usageTypeID = this.getParameter("usagetypeid");
        $("#usagetypeid").val(this.mapOptions.searchParams.usageTypeID);
    }

    if (this.getParameter("statustypeid") != "") {
        this.mapOptions.searchParams.statusTypeID = this.getParameter("statustypeid");
        $("#statustypeid").val(this.mapOptions.searchParams.statusTypeID);
    }

    if (this.getParameter("submissionstatustypeid") != "") {
        this.mapOptions.searchParams.submissionStatusTypeID = this.getParameter("submissionstatustypeid");
        $("#submissionstatustypeid").val(this.mapOptions.searchParams.submissionStatusTypeID);
    }

    var enabledFilterList = this.getParameter("filtercontrols");
    this.mapOptions.showFilter_NearLocation = (enabledFilterList.indexOf("nearlocation") > 0);
    this.mapOptions.showFilter_Distance = (enabledFilterList.indexOf("distance") > 0);
    this.mapOptions.showFilter_Operator = (enabledFilterList.indexOf("operator") > 0);
    this.mapOptions.showFilter_ConnectionTypes = (enabledFilterList.indexOf("connectiontype") > 0);
    this.mapOptions.showFilter_Levels = (enabledFilterList.indexOf("level") > 0);
    this.mapOptions.showFilter_Country = (enabledFilterList.indexOf("country") > 0);
    this.mapOptions.showFilter_SubmissionStatus = (enabledFilterList.indexOf("submissionstatus") > 0);
    this.mapOptions.showFilter_UsageType = (enabledFilterList.indexOf("usage") > 0);
    this.mapOptions.showFilter_StatusType = (enabledFilterList.indexOf("status") > 0);

    var filterCount = 0;

    //enable/disable visible filter controls
    filterCount += this.toggleFilter("#filter-nearlocation", this.mapOptions.showFilter_NearLocation);
    filterCount += this.toggleFilter("#filter-distance", this.mapOptions.showFilter_Distance);
    filterCount += this.toggleFilter("#filter-operators", this.mapOptions.showFilter_Operator);
    filterCount += this.toggleFilter("#filter-connectiontypes", this.mapOptions.showFilter_ConnectionTypes);
    filterCount += this.toggleFilter("#filter-level", this.mapOptions.showFilter_Levels);
    filterCount += this.toggleFilter("#filter-country", this.mapOptions.showFilter_Country);
    filterCount += this.toggleFilter("#filter-submissionstatus", this.mapOptions.showFilter_SubmissionStatus);
    filterCount += this.toggleFilter("#filter-usagetype", this.mapOptions.showFilter_UsageType);
    filterCount += this.toggleFilter("#filter-statustype", this.mapOptions.showFilter_StatusType);

    if (filterCount > 0) {
        //controls enabled, show control overlay
        this.showFilterControls();
    } else {
        this.hideFilterControls();
    }

    $("#map-heading").html(this.mapOptions.mapTitle + "<div class='map-branding'>&nbsp;</div>");

    var promptVal = this.searchNearMePrompt;
    $("#search-nearme").click(function () {
        $("#search-location").val(promptVal);
    });

    this.applyLocalisation(this.mapOptions.isTestLocalisationMode);

    //perform initial search --if set to geolocate user, user that, otherwise use manual default location or no filter
    this.performSearch(this.mapOptions.geolocateUserOnLoad, !this.mapOptions.geolocateUserOnLoad);
};

OCM_Map.prototype.showFilterControls = function () {
    var leftPos = $("#map-canvas").width() - 250;

    $("#map-filter-controls").css("left", leftPos);
    $("#map-filter-controls").fadeIn();
};

OCM_Map.prototype.hideFilterControls = function () {
    $("#map-filter-controls").fadeOut();
};

//TODO: use OCM_Data.js
OCM_Map.prototype.sortReferenceData = function (sourceList) {
    sourceList.sort(this.sortListByTitle);
};

//TODO: use OCM_Data.js
OCM_Map.prototype.sortListByTitle = function (a, b) {
    if (a.Title < b.Title) return -1;
    if (a.Title > b.Title) return 1;
    if (a.Title == b.Title) return 0;
};

//TODO: dedupe
OCM_Map.prototype.populateDropdown = function (id, refDataList, selectedValue, defaultToUnspecified, useTitleAsValue, unspecifiedText) {
    var $dropdown = $("#" + id);
    $('option', $dropdown).remove();


    if (defaultToUnspecified == true) {
        if (unspecifiedText == null) unspecifiedText = "Unknown";
        $dropdown.append($('<option value=\"\"> </option>').val("").html(unspecifiedText));
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

    //refresh control
    try {
        if ($.mobile) {
            $dropdown.trigger("create");
            $dropdown.selectmenu("refresh");
        }
    } catch (err) {
    }
};

OCM_Map.prototype.getReferenceData_Completed = function (refData) {
    this.ocm_data.referenceData = refData;

    //sort reference data lists
    this.sortReferenceData(refData.ConnectionTypes);
    this.sortReferenceData(refData.Countries);
    this.sortReferenceData(refData.Operators);
    this.sortReferenceData(refData.UsageTypes);
    this.sortReferenceData(refData.StatusTypes);

    //setup dropdown options
    this.populateDropdown("connectiontypeid", refData.ConnectionTypes, null, true, false, "(All)");
    this.populateDropdown("countryid", refData.Countries, null, true, false, "(All)");
    this.populateDropdown("operatorid", refData.Operators, null, true, false, "(All)");
    this.populateDropdown("submissionstatustypeid", refData.SubmissionStatusTypes, null, true, false, "(All)");
    this.populateDropdown("usagetypeid", refData.UsageTypes, null, true, false, "(All)");
    this.populateDropdown("statustypeid", refData.StatusTypes, null, true, false, "(All)");

    //dynamically load localisation (if any required) and load map
    var language_code = ocm_map.getParameter("languagecode");
    if (language_code == "" || language_code == null) language_code = "en";
    ocm_map.language_code = language_code;
    localisation_dictionary=null;

    if (language_code != "test") {
        $LAB.script("scripts/Localisation/languagePack.min.js").wait(function () {
			localisation_dictionary = eval("localisation_dictionary_"+language_code);
            ocm_map.initMap();
        });
    } else {
        if (language_code == "test") ocm_map.mapOptions.isTestLocalisationMode = true;
        ocm_map.initMap();
    }
};

OCM_Map.prototype.showProgressIndicator = function () {
    $("#progress-indicator").fadeIn('slow');
};

OCM_Map.prototype.hideProgressIndicator = function () {
    $("#progress-indicator").fadeOut('slow');
};

OCM_Map.prototype.newSearch = function () {
    if ($("#search-location").val() == this.searchNearMePrompt) {
        //search by geolocating user
        this.performSearch(true, false);
    } else {
        //search based on text location (if specified)
        this.performSearch(false, true);
    }
}

OCM_Map.prototype.performSearch = function (useClientLocation, useManualLocation) {

    this.showProgressIndicator();
    var searchParams = this.mapOptions.searchParams;
    var useDistanceSearch = true;

    if (useClientLocation == true) {
        //initiate client geolocation (if not already determined)
        if (this.ocm_geo.clientGeolocationPos == null) {
            this.ocm_geo.determineUserLocation($.proxy(this.determineUserLocationCompleted, this), $.proxy(this.determineUserLocationFailed, this));
            return;
        } else {
            this.ocm_app_searchPos = this.ocm_geo.clientGeolocationPos;
        }
    }

    searchParams.distance = parseInt(document.getElementById("search-distance").value);
    searchParams.distanceUnit = document.getElementById("search-distance-unit").value;

    if (searchParams.latitude != null && searchParams.longitude != null) {
        this.ocm_app_searchPos = new google.maps.LatLng(parseFloat(searchParams.latitude), parseFloat(searchParams.longitude));
    }

    if (this.ocm_app_searchPos == null || useManualLocation == true) {
        //search position not set, attempt fetch from location input and return for now
        var locationText = document.getElementById("search-location").value;
        if (locationText === null || locationText == "") {
            //alert("no location set, skipping distance search");
            useDistanceSearch = false;
            //try to geolocate via browser location API
            //this.ocm_geo.determineUserLocation($.proxy(this.determineUserLocationCompleted, this), $.proxy(this.determineUserLocationFailed, this));
            // return;
        } else {
            //try to gecode text location name, if new lookup not attempted, continue to rendering
            var lookupAttempted = this.ocm_geo.determineGeocodedLocation(locationText, $.proxy(this.determineGeocodedLocationCompleted, this));
            if (lookupAttempted == true) return;
        }
    }

    if (this.ocm_app_searchPos != null || useDistanceSearch == false) { // && !this.searchInProgress) {
        this.searchInProgress = true;

        var additionalparams = "";

        $("#levelid").val() == "" ? searchParams.levelID = null : searchParams.levelID = $("#levelid").val();
        $("#connectiontypeid").val() != "" ? searchParams.connectionTypeID = $("#connectiontypeid").val() : searchParams.connectionTypeID = null;
        $("#operatorid").val() != "" ? searchParams.operatorID = $("#operatorid").val() : searchParams.operatorID = null;
        $("#countryid").val() != "" ? searchParams.countryID = $("#countryid").val() : searchParams.countryID = null;
        $("#usagetypeid").val() != "" ? searchParams.usageTypeID = $("#usagetypeid").val() : searchParams.usageTypeID = null;
        $("#statustypeid").val() != "" ? searchParams.statusTypeID = $("#statustypeid").val() : searchParams.statusTypeID = null;
        $("#submissionstatustypeid").val() != "" ? searchParams.submissionStatusTypeID = $("#submissionstatustypeid").val() : searchParams.submissionStatusTypeID = null;

        
        if (this.ocm_app_searchPos != null && useDistanceSearch == true) {

            //convert from google lookup coords
            if (this.ocm_app_searchPos.coords == undefined) this.ocm_app_searchPos.coords = {
                latitude: this.ocm_app_searchPos.lat(),
                longitude: this.ocm_app_searchPos.lng()
            };

            //searchParams.latitude = this.ocm_app_searchPos.lat();
            //searchParams.longitude = this.ocm_app_searchPos.lng();
            searchParams.latitude = this.ocm_app_searchPos.coords.latitude;
            searchParams.longitude = this.ocm_app_searchPos.coords.longitude;

            //country not required if searching on position
            searchParams.countryID = null;
        } else {
            //TODO: check if this still required
            //searchParams.latitude = null;
            //searchParams.longitude = null;
        }

        searchParams.includeComments = false;

        //perform data lookup via OCM API
        this.ocm_data.fetchLocationDataListByParam(searchParams, "ocm_map.renderLocations");
    }
};

OCM_Map.prototype.determineUserLocationCompleted = function (pos) {
    this.ocm_app_searchPos = pos;
    this.ocm_geo.clientGeolocationPos = pos;
    this.performSearch();
};

OCM_Map.prototype.determineUserLocationFailed = function (pos) {
    this.hideProgressIndicator();
    $("#search-location").val("");
    this.showUserMessage("Could not automatically determine your location. Search by location name instead.");
};

OCM_Map.prototype.determineGeocodedLocationCompleted = function (pos) {
    this.ocm_app_searchPos = pos;
    this.performSearch();
};

OCM_Map.prototype.getMaxLevelOfPOI = function (poi) {
    var level = 0;

    if (poi.Connections != null) {
        for (var c = 0; c < poi.Connections.length; c++) {
            if (poi.Connections[c].Level != null && poi.Connections[c].Level.ID > level) {
                level = poi.Connections[c].Level.ID;
            }
        }
    }

    if (level == 4) level = 2; //lvl 1&2
    if (level > 4) level = 3; //lvl 2&3 etc
    return level;
};

OCM_Map.prototype.showPOIListOnMap = function (mapcanvasID, poiList, appcontext, anchorElement) {

    var mapCanvas = document.getElementById(mapcanvasID);
    if (mapCanvas != null) {

        /*if (this.isMobileBrowser()) {
		mapCanvas.style.width = '100%';
		mapCanvas.style.height = '100%';
		}*/

        if (this.ocm_markerClusterer && this.mapOptions.enableClustering) {
            this.ocm_markerClusterer.clearMarkers();
        }

        //create map
        var mapOptions = {
            zoom: 3,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            mapTypeControl: true,
            mapTypeControlOptions: {
                style: google.maps.MapTypeControlStyle.DROPDOWN_MENU,
                position: google.maps.ControlPosition.BOTTOM_RIGHT
            },
            panControl: true,
            panControlOptions: {
                position: google.maps.ControlPosition.TOP_LEFT
            },
            zoomControl: true,
            zoomControlOptions: {
                style: google.maps.ZoomControlStyle.LARGE,
                position: google.maps.ControlPosition.TOP_LEFT
            },
            scaleControl: true,
            scaleControlOptions: {
                position: google.maps.ControlPosition.BOTTOM_LEFT
            },
            streetViewControl: true,
            streetViewControlOptions: {
                position: google.maps.ControlPosition.TOP_LEFT
            }
        };
        var map = new google.maps.Map(mapCanvas, mapOptions);

        var bounds = new google.maps.LatLngBounds();

        //clear existing markers
        if (this.ocm_markers != null) {
            for (var i = 0; i < this.ocm_markers.length; i++) {
                if (this.ocm_markers[i]) {
                    this.ocm_markers[i].setMap(null);
                }
            }
        }

        this.ocm_markers = new Array();
        if (poiList != null) {
            //render poi markers
            var poiCount = poiList.length;
            for (var i = 0; i < poiList.length; i++) {
                if (poiList[i].AddressInfo != null) {
                    if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {

                        var poi = poiList[i];

                        var poiLevel = this.getMaxLevelOfPOI(poi);

                        var iconURL = null;
                        var animation = null;
                        if (this.mapOptions.useMarkerIcons) {
                            if (this.mapOptions.iconSet == "SmallPins") {
                                iconURL = "icons/sm_pin_level" + poiLevel + ".png"
                            } else {
                                iconURL = "icons/level" + poiLevel + ".png"
                            }
                        }

                        if (poiCount < 100 && this.mapOptions.useMarkerAnimation == true) {
                            animation = google.maps.Animation.DROP;
                        }

                        var newMarker = new google.maps.Marker({
                            position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                            map: this.mapOptions.enableClustering ? null : map,
                            icon: iconURL,
                            animation: animation,
                            title: poi.AddressInfo.Title
                        });

                        newMarker.poi = poi;

                        google.maps.event.addListener(newMarker, 'click', function () {
                            appcontext.showDetailsView(anchorElement, this.poi);
                            $.mobile.changePage("#locationdetails-page");
                        });

                        bounds.extend(newMarker.position);
                        this.ocm_markers.push(newMarker);
                    }
                }
            }
        }

        //include centre search location in bounds of map zoom
        if (this.ocm_searchmarker != null) bounds.extend(this.ocm_searchmarker.position);
        map.fitBounds(bounds);

        if (this.mapOptions.enableClustering) {
            var mcOptions = { gridSize: 50, maxZoom: 12 };
            this.ocm_markerClusterer = new MarkerClusterer(map, this.ocm_markers);
        }
    }

    if (poiList.length == 0) {
        //this.showUserMessage("There are no charging locations which match your search criteria.");
    }
    this.hideProgressIndicator();
};

OCM_Map.prototype.showUserMessage = function (msg) {
    alert(msg);
};


var ocm_map = new OCM_Map();
if (ocm_map.getParameter("paused") == "true") {
    ocm_map.mapOptions.pauseOnLoad = true;

    if (ocm_map.getParameter("pausedmessage") != "") {
        ocm_map.mapOptions.pausedMessage = ocm_map.getParameter("pausedmessage");
    }
}

$(document).ready(function () {

    //init localisation and map
    if ($.mobile == null) {
        $.mobile = new Object();
        $.mobile.changePage = function () { };
    }

    if (ocm_map.mapOptions.pauseOnLoad)
    {
        //wait for user to activate map
        
        var $pausedMap = $("#map-paused");
        $pausedMap.html("<div class='map-branding'>&nbsp;</div>");
        $pausedMap.append(ocm_map.mapOptions.pausedMessage);
        $pausedMap.fadeIn();

        $pausedMap.click(function () {
            $(this).fadeOut();
            ocm_map.ocm_data.fetchCoreReferenceData("ocm_map.getReferenceData_Completed");
        });
    } else {
        setTimeout(function () {
            ocm_map.ocm_data.fetchCoreReferenceData("ocm_map.getReferenceData_Completed");
        }, 500);
    }


});
