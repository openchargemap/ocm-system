
/*
OCM charging point map widget
Christopher Cook 2011 
http://openchargemap.org
 	
See http://www.openchargemap.org/ for more details.
 
The following snippet is a minimal example, showing a 640x480 map, selecting 'London,UK' as default starting location: 
 
<!--begin example map-->
<div id="ocm_map_container"></div>
<script type="text/javascript">
ocm_map_initialize("London, UK","ocm_map_container",640,480);
</script>
<!--end example map-->
*/

// Note that using Google Gears (if necessary for geolocation of users) requires loading the Javascript at http://code.google.com/apis/gears/gears_init.js

var ocm_enableAutomaticUserLocation = true;

var ocm_initialLocation = null;
var ocm_reverseGecodedLocation = null;
var ocm_distance = 500;
var ocm_distance_unit = "Miles";
var ocm_browserSupportsLocation = new Boolean();
var ocm_map = null;
var ocm_layer = null;
var ocm_map_bounds = new google.maps.LatLngBounds();
var ocm_useKMLLayer = false;
var ocm_maxresults = 5000;
var ocm_markers = null;
var ocm_searchmarker = null;
var ocm_currentinfowindow = null;
var ocm_markerClusterer = null;
var ocm_enableClustering = false;
var ocm_enableExtendedControls = false;
var ocm_fixedserviceparams = "&";
var ocm_serviceurl = "http://www.openchargemap.org/api/service.ashx?";
var ocm_widgeturl = "http://www.openchargemap.org/api/widgets/map/";

function ocm_map_initialize(defaultlocation, mapcontainerid, width, height, geolocateByDefault) {

    //if a preference is given for automatic geolocation, use that instead of default setting
    if (geolocateByDefault != null) ocm_enableAutomaticUserLocation = geolocateByDefault;

    //create search form
    ocm_populateForm(mapcontainerid);

    //create map canvas
    var mapcanvas = document.getElementById("ocm_map_canvas");
    mapcanvas.style.width = width + "px";
    mapcanvas.style.height = height + "px";

    var myOptions = {
        zoom: 3,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    ocm_map = new google.maps.Map(mapcanvas, myOptions);
    ocm_map_bounds = new google.maps.LatLngBounds();

    document.getElementById("ocm_location").value = defaultlocation;

    //optionally enable clustering
    try {
        if (typeof (MarkerClusterer)) {
            if (ocm_enableClustering != false) ocm_enableClustering = true;
        }
    }
    catch (err) {
        ocm_enableClustering = false;
    }

    if (ocm_enableExtendedControls) ocm_displayExtendedControls();

    ocm_performLocationLookup();
}

function ocm_performLocationLookup() {

    //determine user location automatically if enabled & supported
    if (navigator.geolocation && ocm_enableAutomaticUserLocation) {

        // Try W3C Geolocation (Preferred)
        ocm_browserSupportsLocation = true;

        navigator.geolocation.getCurrentPosition(
	        function (position) {

	            ocm_initialLocation = new google.maps.LatLng(position.coords.latitude, position.coords.longitude);
	            ocm_setSearchLocationFromPos();
	            ocm_lookupLocationOCMData();
	        },
	        function () {
	            // could not geolocate
	            ocm_geolocationFallback();
	        }
	    );

    } else if (google.gears && ocm_enableAutomaticUserLocation) {

        // Try Google Gears Geolocation
        ocm_browserSupportsLocation = true;

        var geo = google.gears.factory.create('beta.geolocation');
        geo.getCurrentPosition(
	            function (position) {
	                ocm_initialLocation = new google.maps.LatLng(position.latitude, position.longitude);
	                ocm_setSearchLocationFromPos();
	                ocm_lookupLocationOCMData();
	            },
	            function () {
	                //could not geolocate
	                ocm_geolocationFallback();
	            }
	        );

    } else {
        ocm_geolocationFallback();
    }
}

function ocm_geolocationFallback() {
    // Browser doesn't support Geolocation or automatic location not supported or enabled 
    ocm_lookupLocationOCMData();
}

function ocm_setSearchLocationFromPos() {
    ocm_reverseGeocodeLocation(ocm_initialLocation);

    //if location name not found from lat/long, show lat/lon in search box, otherwise use location name
    if (ocm_reverseGecodedLocation == null)
        document.getElementById("ocm_location").value = ocm_initialLocation.lat() + "," + ocm_initialLocation.lng();
    else
        document.getElementById("ocm_location").value = ocm_reverseGecodedLocation;
}

function ocm_refreshMarkers() {
    if (ocm_layer != null) {
        if (ocm_layer.setMap) ocm_layer.setMap(null); //remove previous layer
    }

    if (ocm_markerClusterer) {
        ocm_markerClusterer.clearMarkers();
    }

    //get variables from form
    ocm_distance = parseInt(document.getElementById("ocm_distance").value);
    ocm_distance_unit = document.getElementById("ocm_distance_unit").value;
    if (ocm_useKMLLayer) {
        //fetch and display marker data
        var dataURL = ocm_serviceurl + '?output=kml&maxresults=' + ocm_maxresults + '&distance=' + ocm_distance + '&distanceunit=' + ocm_distance_unit + '&latitude=' + ocm_initialLocation.lat() + '&longitude=' + ocm_initialLocation.lng();

        var layerOpts = new Object();
        layerOpts.preserveViewport = true;
        ocm_layer = new google.maps.KmlLayer(dataURL, layerOpts);
        ocm_layer.setMap(ocm_map);

        ocm_displayProgress("");
        /*var bounds = ocm_layer.getDefaultViewport();
        bounds.extend(ocm_initialLocation);
        ocm_map.fitBounds(bounds);*/
    } else {
        if (!jQuery) {
            alert("jQuery 1.5 or higher is required to use the non-kml version of map widget");
        }
        else {
            //load data as JSON and display as markers
            var dataURL = ocm_serviceurl + 'output=json&callback=ocm_renderMarkersFromJSON&maxresults=' + ocm_maxresults + '&distance=' + ocm_distance + '&distanceunit=' + ocm_distance_unit + '&latitude=' + ocm_initialLocation.lat() + '&longitude=' + ocm_initialLocation.lng() + ocm_fixedserviceparams;

            //ocm_renderMarkersFromJSON(null);

            $.ajax({
                type: "GET",
                url: dataURL,
                jsonp: false,
                jsonpCallback: "ocm_renderMarkersFromJSON",
                contentType: "application/json;",
                dataType: "jsonp",
                crossDomain: true,
                success: function () { },
                error: function (msg) {
                    alert("There was a problem loading map data.");
                }
            });
        }
    }
}

function getTestLocations() {
    var msg = new Object();
    //msg = [{ "AddressInfo": ...
    return msg;
}

function ocm_renderMarkersFromJSON(msg) {
    //msg = getTestLocations();
    if (msg != null) {

        if (ocm_markers != null) {
            //clear existing markers
            for (var i = 0; i < ocm_markers.length; i++) {
                ocm_markers[i].setMap(null);
            }
        }

        ocm_markers = new Array();
        var bounds = new google.maps.LatLngBounds();

        var locationList = msg;
        for (var i = 0; i < locationList.length; i++) {
            if (locationList[i].AddressInfo != null) {
                if (locationList[i].AddressInfo.Latitude != null && locationList[i].AddressInfo.Longitude != null) {

                    var poi = locationList[i];
                    ocm_markers[i] = new google.maps.Marker({
                        position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                        map: ocm_map,
                        icon: ocm_widgeturl + "icons/green-circle.png",
                        title: poi.AddressInfo.Title
                    });

                    ocm_markers[i].poi = poi;

                    google.maps.event.addListener(ocm_markers[i], 'click', function () {
                        ocm_showinfowindow(ocm_map, this);
                    });

                    bounds.extend(ocm_markers[i].position);

                }
            }
        }

        //include centre search location in bounds of map zoom
        bounds.extend(ocm_searchmarker.position);
        ocm_map.fitBounds(bounds);
    }

    if (ocm_enableClustering) {
        markerClusterer = new MarkerClusterer(ocm_map, ocm_markers);
    }

    ocm_displayProgress("");
}

function ocm_fixJSONDate(val) {
    if (val == null) return null;
    if (val.indexOf("/") == 0) {
        var pattern = /Date\(([^)]+)\)/;
        var results = pattern.exec(val);
        val = new Date(parseFloat(results[1]));
    }
    return val;
}

function ocm_formatline(val, label, newlineAfterLabel) {
    if (val == null || val == "") return "";
    return (label != null ? "<strong>" + label + "</strong>: " : "") + (newlineAfterLabel ? "<br/>" : "") + val + "<br/>";
}

function ocm_showinfowindow(ocm_map, marker) {
    var poi = marker.poi;
    var isFastCharge = false;
    var dayInMilliseconds = 86400000;
    var currentDate = new Date();

    //close last opened window
    if (ocm_currentinfowindow != null) ocm_currentinfowindow.close();

    var addressInfo = ocm_formatline(poi.AddressInfo.AddressLine1) +
                    ocm_formatline(poi.AddressInfo.AddressLine2) +
                    ocm_formatline(poi.AddressInfo.Town) +
                    ocm_formatline(poi.AddressInfo.StateOrProvince) +
                    ocm_formatline(poi.AddressInfo.Postcode);

    var contactInfo = ocm_formatline(poi.AddressInfo.ContactTelephone1, "Phone") +
                    ocm_formatline(poi.AddressInfo.ContactEmail, "Email");

    var comments = ocm_formatline(poi.GeneralComments, "Comments", true) +
                  ocm_formatline(poi.AddressInfo.AccessComments, "Access");


    if (poi.NumberOfPoints != null) {
        comments += ocm_formatline(poi.NumberOfPoints, "Number Of Points");
    }

    if (poi.UsageType != null) {
        comments += ocm_formatline(poi.UsageType.Title, "Usage");
    }

    if (poi.OperatorInfo != null) {
        if (poi.OperatorInfo.ID != 1) { //skip unknown operators
            comments += ocm_formatline(poi.OperatorInfo.Title, "Operator");
        }
    }

    var equipmentInfo = "";
    if (poi.Chargers != null) {
        if (poi.Chargers.length > 0) {
            for (var c = 0; c < poi.Chargers.length; c++) {
                equipmentInfo += ocm_formatline(poi.Chargers[c].ChargerType.Title, "Type");
                if (!isFastCharge) isFastCharge = poi.Chargers[c].ChargerType.IsFastChargeCapable;
            }
        }
    }
    
    if (poi.Connections != null) {
	        if (poi.Connections.length > 0) {
	            for (var c = 0; c < poi.Connections.length; c++) {
	                equipmentInfo += ocm_formatline(poi.Connections[c].ConnectionType.Title, "Connection");
	            }
	        }
    }

    if (poi.StatusType != null) {
        equipmentInfo += ocm_formatline(poi.StatusType.Title, "Status");
        if (poi.DateLastStatusUpdate != null) {
            equipmentInfo += ocm_formatline(Math.round(((currentDate - ocm_fixJSONDate(poi.DateLastStatusUpdate)) / dayInMilliseconds)) + " days ago", "Last Updated");
        }
    }
    equipmentInfo += ocm_formatline("OCM-" + poi.ID, "OpenChargeMap Ref");

    var contentString = '<div id="ocm_infowindow" style="font-family:arial;font-size:9pt;max-width:350px;">' +
                        '<h1 style="font-size:12pt;">' + poi.AddressInfo.Title + '</h1>' +
                        '<div id="ocm_streetview"></div>' +
                        '<div><table><tr><td style="vertical-align:top;width:50%;"><strong>Location:</strong></br>' + addressInfo + contactInfo + '</td><td style="vertical-align:top;">' + comments + equipmentInfo + '</td></tr></table></div>' +
                    '</div>';

    var infowindow = new google.maps.InfoWindow({
        content: contentString
    });

    var pano = null;
    google.maps.event.addListener(infowindow, 'domready', function () {
        if (pano != null) {
            pano.unbind("position");
            pano.setVisible(false);
        }
        pano = new google.maps.StreetViewPanorama(document.getElementById("ocm_streetview"), {
            navigationControl: true,
            enableCloseButton: false,
            addressControl: false,
            linksControl: false
        });
        pano.bindTo("position", marker);
        pano.setVisible(true);
    });

    google.maps.event.addListener(infowindow, 'closeclick', function () {
        pano.unbind("position");
        pano.setVisible(false);
        pano = null;
    });
    infowindow.open(ocm_map, marker);

    ocm_currentinfowindow = infowindow;
}

function ocm_displayProgress(msg) {
    var ocm_lookup_progress = document.getElementById("ocm_lookup_progress");
    ocm_lookup_progress.innerHTML = msg;
}

function ocm_lookupLocationOCMData() {

    var locationText = document.getElementById("ocm_location").value;

    ocm_displayProgress("Looking up location: " + locationText);

    var geocoder = new google.maps.Geocoder();

    geocoder.geocode({ 'address': locationText }, function (results, status) {
        if (status == google.maps.GeocoderStatus.OK) {

            var locationPos = results[0].geometry.location;
            ocm_map.setCenter(locationPos);

            if (ocm_searchmarker != null) ocm_searchmarker.setMap(null);

            ocm_searchmarker = new google.maps.Marker({
                map: ocm_map,
                position: locationPos,
                title: "Search position: " + locationText
            });

            ocm_initialLocation = locationPos;
            ocm_displayProgress("Fetching charge locations: " + locationText);
            ocm_refreshMarkers();


        } else {
            alert("Sorry, we could not identify this location: " + status);
        }
    });
}

function ocm_reverseGeocodeLocation() {

    if (ocm_initialLocation != null) {
        var geocoder = new google.maps.Geocoder();
        geocoder.geocode({ 'latLng': ocm_initialLocation }, function (results, status) {
            if (status == google.maps.GeocoderStatus.OK) {
                if (results[1]) {
                    ocm_reverseGecodedLocation = results[1].formatted_address;
                    document.getElementById("ocm_location").value = ocm_reverseGecodedLocation;
                }
            } else {
                ocm_reverseGecodedLocation = null;
            }
        });
    }
}

function ocm_displayExtendedControls() {
    var controlDiv = document.createElement('DIV');

    // Set CSS styles for the DIV containing the control
    // Setting padding to 5 px will offset the control
    // from the edge of the map
    controlDiv.style.padding = '5px';

    // Set CSS for the control border
    var controlUI = document.createElement('DIV');
    controlUI.style.backgroundColor = 'white';
    controlUI.style.borderStyle = 'solid';
    controlUI.style.borderWidth = '1px';
    controlUI.style.padding = "4px;";
    controlUI.style.cursor = 'pointer';
    controlUI.style.textAlign = 'center';
    controlUI.title = 'Click to add a new charging point location';
    controlDiv.appendChild(controlUI);

    // Set CSS for the control interior
    var controlText = document.createElement('DIV');
    controlText.style.fontFamily = 'Arial,sans-serif';
    controlText.style.fontSize = '12px';
    controlText.style.paddingLeft = '4px';
    controlText.style.paddingRight = '4px';
    controlText.innerHTML = 'Add New Location';
    controlUI.appendChild(controlText);

    // Setup the click event listeners: simply set the map to Chicago
    google.maps.event.addDomListener(controlUI, 'click', function () {
        alert("clicked add new location.");
    });

    ocm_map.controls[google.maps.ControlPosition.TOP_RIGHT].push(controlDiv);
}

function ocm_populateForm(containerid) {
    var html = '<input type="text" id="ocm_location" name="ocm_location" value="London, UK"/> ' +
	    '<select id="ocm_distance" name="ocm_distance">' +
	    '<option value="5">5</option>' +
	    '<option value="10" selected="selected">10</option>' +
	    '<option value="20">20</option>' +
	    '<option value="30">30</option>' +
	    '<option value="50">50</option>' +
	    '<option value="100">100</option>' +
	    '<option value="500">500</option>' +
	    '<option value="2000">2000</option>' +
	    '<option value="10000">10000</option>' +
	    '</select>' +
	    '<select id="ocm_distance_unit" name="ocm_distance_unit">' +
		'<option value="Miles" selected="selected">Miles</option>' +
		'<option value="KM">Kilometers</option>' +
	    '</select>' +
	    '<input type="button" id="ocm_lookup" name="ocm_lookup" value="Go" onclick="ocm_lookupLocationOCMData();" />' +
	    '<span id ="ocm_lookup_progress"></span>' +
	    '<div id="ocm_map_canvas">' +
	    '</div><div style="font-size:x-small;">powered by the <a href=\"http://openchargemap.org\" target="_blank">open charge map</a> project.</div>';

    document.getElementById(containerid).innerHTML = html;

    //attempt to set distance dropdown based on current ocm_distance setting
    document.getElementById("ocm_distance").value = ocm_distance;
}