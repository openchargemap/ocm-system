/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/googlemaps/google.maps.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.awesomemarkers.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
//"use strict";

//typescript declarations
declare var localisation_dictionary;
declare var MarkerClusterer: any;
interface MVCObject {
    visualRefresh: any;
}

module google.maps {
    var visualRefresh: any;
}

function OCM_CommonUI() {
    this.enablePOIMap = true;
    this.map = null;
    this.ocm_markers = [];
    this.ocm_markerClusterer = null;
    this.mapView = null;
    this.mapOptions = new Object();
    this.mapOptions.enableClustering = false;
    this.mapOptions.resultBatchID = 0;
    this.mapOptions.useMarkerIcons = true;
    this.mapOptions.useMarkerAnimation = true;
    this.isRunningUnderCordova = false;

}

OCM_CommonUI.prototype.getLocalisation = function (resourceKey, defaultValue, isTestMode) {
    if (typeof localisation_dictionary != 'undefined' || isTestMode == true) {
        return eval("localisation_dictionary." + resourceKey);
    } else {
        //localisation not in use
        if (isTestMode == true) {
            return "[" + resourceKey + "]";
        } else {
            return defaultValue;
        } 
    }
};

OCM_CommonUI.prototype.applyLocalisation = function (isTestMode) {
    try {
        if (isTestMode == true || localisation_dictionary != null) {
            var elementList = $("[data-localize]");

            for (var i = 0; i < elementList.length; i++) {
                var $element = $(elementList[i]);
                var resourceKey = $element.attr("data-localize");

                if (isTestMode == true || eval("localisation_dictionary." + resourceKey) != undefined) {
                    var localisedText;

                    if (isTestMode == true) {
                        //in test mode the resource key is displayed as the localised text 
                        localisedText = "[" + resourceKey + "] " + eval("localisation_dictionary." + resourceKey);
                    } else {
                        localisedText = eval("localisation_dictionary." + resourceKey);
                    }

                    if ($element.is("input")) {
                        if ($element.attr("type") == "button") {
                            //set input button value
                            $element.val(localisedText);
                        }
                    } else {
                        if ($element.attr("data-localize-opt") == "title") {
                            //set title of element only
                            $(elementList[i]).attr("title", localisedText);
                        } else {
                            //standard localisation method is to replace inner text of element
                            $(elementList[i]).text(localisedText);
                        }
                    }
                }
            }
        }
    } catch (exp) {
        //localisation attempt failed
    } finally {
    }
};


OCM_CommonUI.prototype.fixJSONDate = function (val) {
    if (val == null) return null;
    if (val.indexOf("/") == 0) {
        var pattern = /Date\(([^)]+)\)/;
        var results = pattern.exec(val);
        val = new Date(parseFloat(results[1]));
    } else {
        val = new Date(val);
    }
    return val;
};

OCM_CommonUI.prototype.showPOIOnStaticMap = function (mapcanvasID, poi, includeMapLink) {

    var mapCanvas = document.getElementById(mapcanvasID);
    if (mapCanvas != null) {
        var title = poi.AddressInfo.Title;
        var lat = poi.AddressInfo.Latitude;
        var lon = poi.AddressInfo.Longitude;
        var width = 200;
        var height = 200;

        var mapImageURL = "http://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon + "&zoom=14&size="+width+"x"+height+"&maptype=roadmap&markers=color:blue%7Clabel:A%7C" + lat + "," + lon + "&sensor=false";
        var mapHTML = "";
        if (includeMapLink == true) {
            mapHTML += "<div>" + this.formatMapLink(poi, "<div><img width=\""+width+"\" height=\""+height+"\" src=\"" + mapImageURL + "\" /></div>") + "</div>";
        } else {
            mapHTML += "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>";
        }

        mapCanvas.innerHTML = mapHTML;
    }
};

OCM_CommonUI.prototype.showPOIOnMap = function (mapcanvasID, poi) {

    var mapCanvas = document.getElementById(mapcanvasID);
    if (mapCanvas != null) {
        var title = poi.AddressInfo.Title;
        var lat = poi.AddressInfo.Latitude;
        var lon = poi.AddressInfo.Longitude;

        //map item if possible:
        var markerLatlng = new google.maps.LatLng(lat, lon);
        var myOptions = {
            zoom: 14,
            center: markerLatlng,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };
        var map = new google.maps.Map(mapCanvas, myOptions);

        var marker = new google.maps.Marker({
            position: markerLatlng,
            map: map,
            title: title
        });

        map.setCenter(markerLatlng);
        google.maps.event.trigger(map, 'resize');

    }
};

OCM_CommonUI.prototype.isMobileBrowser = function () {
    var useragent = navigator.userAgent;
    if (useragent.indexOf('iPhone') != -1 || useragent.indexOf('Android') != -1 || useragent.indexOf('Windows Phone') != -1) {
        return true;
    } else {
        return false;
    }
};

OCM_CommonUI.prototype.getMaxLevelOfPOI = function (poi) {
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

OCM_CommonUI.prototype.showPOIListOnMapViewLeaflet = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {

    var map = this.map;
    
    //if list has changes to results render new markers etc
    if (this.mapOptions.resultBatchID != resultBatchID) {

        this.mapOptions.resultBatchID = resultBatchID;

        if (console) console.log("Setting up map markers:"+resultBatchID);
        // Creates a red marker with the coffee icon

        var unknownPowerMarker = L.AwesomeMarkers.icon({
            icon: 'bolt',
            color: 'darkpurple',
            prefix: 'fa'
        });

        var lowPowerMarker = L.AwesomeMarkers.icon({
            icon: 'bolt',
            color: 'darkblue',
            spin: true,
            prefix: 'fa'
        });

        var mediumPowerMarker = L.AwesomeMarkers.icon({
            icon: 'bolt',
            color: 'green',
            spin: true,
            prefix: 'fa'
        });

        var highPowerMarker = L.AwesomeMarkers.icon({
            icon: 'bolt',
            color: 'orange',
            spin: true,
            prefix: 'fa'
        });

      
    
        if (this.mapOptions.enableClustering)
        {
            var markerClusterGroup = new L.MarkerClusterGroup();
      
            if (poiList != null) {
                //render poi markers
                var poiCount = poiList.length;
                for (var i = 0; i < poiList.length; i++) {
                    if (poiList[i].AddressInfo != null) {
                        if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {

                            
                            var poi = poiList[i];

                            var markerTitle = poi.AddressInfo.Title;
                            var powerTitle = "";
                            var usageTitle = "";

                            var poiLevel = this.getMaxLevelOfPOI(poi);
                            var markerIcon = unknownPowerMarker;
                            
                            if (poiLevel == 0) {
                                powerTitle += "Power Level Unknown";
                            }

                            if (poiLevel == 1) {
                                markerIcon = lowPowerMarker;
                                powerTitle += "Low Power";
                            }
                    
                            if (poiLevel == 2) {
                                markerIcon = mediumPowerMarker;
                                powerTitle += "Medium Power";
                            }

                            if (poiLevel == 3) {
                                markerIcon = highPowerMarker;

                                powerTitle += "High Power";
                            }

                            usageTitle = "Unknown Usage Restrictions"

                            if (poi.UsageType != null && poi.UsageType.ID!=0)
                            {
                                usageTitle = poi.UsageType.Title;
                            }
                            markerTitle += " (" + powerTitle + ", " + usageTitle + ")"

                            var marker = <any>new L.Marker(new L.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude), <L.MarkerOptions>{icon:markerIcon,title:markerTitle, draggable:false, clickable:true});
                            marker._isClicked = false; //workaround for double click event
                            marker.poi = poi;
                            marker.on('click', 
                                function (e) {
                                    if (this._isClicked == false)
                                    {
                                     this._isClicked = true;
                                        appcontext.showDetailsView(anchorElement, this.poi);
                                        appcontext.showPage("locationdetails-page");

                                        //workaround double click event by clearing clicked state after short time
                                        var mk = this;
                                        setTimeout(function () {mk._isClicked=false }, 300);
                                    }
                                });
                            
                            markerClusterGroup.addLayer(marker);
                  
                        }
                    }
                }
            }

            map.addLayer(markerClusterGroup);
            map.fitBounds(markerClusterGroup.getBounds());

            //refresh map view
            setTimeout(function () { map.invalidateSize(false); }, 300);
       
        }
    }

};

OCM_CommonUI.prototype.showPOIListOnMapViewGoogle = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {

 
    //if list has changes to results render new markers etc
    if (this.mapOptions.resultBatchID != resultBatchID) {

        this.mapOptions.resultBatchID = resultBatchID;

        var map = this.map;
        var bounds = new google.maps.LatLngBounds();

        if (this.mapOptions.enableClustering && this.ocm_markerClusterer) {
            this.ocm_markerClusterer.clearMarkers();
        }

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
                        var shadow = null;
                        var markerImg = null;
                        if (this.mapOptions.useMarkerIcons) {
                            if (this.mapOptions.iconSet == "SmallPins") {
                                iconURL = "images/icons/map/sm_pin_level" + poiLevel + ".png";
                            } else {
                                iconURL = "images/icons/map/level" + poiLevel + ".png";
                                shadow = new google.maps.MarkerImage("images/icons/map/marker-shadow.png",
								        new google.maps.Size(41.0, 31.0),
								        new google.maps.Point(0, 0),
								        new google.maps.Point(12.0, 15.0)
    							);

                                markerImg = new google.maps.MarkerImage(iconURL,
								        new google.maps.Size(25.0, 31.0),
								        new google.maps.Point(0, 0),
								        new google.maps.Point(12.0, 15.0)
    								);
                            }
                        }

                        if (poiCount < 100 && this.mapOptions.useMarkerAnimation == true) {
                            animation = google.maps.Animation.DROP;
                        }

                        var newMarker = <any>new google.maps.Marker({
                            position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                            map: this.mapOptions.enableClustering ? null : map,
                            icon: markerImg != null ? markerImg : iconURL,
                            shadow: shadow,
                            animation: animation,
                            title: poi.AddressInfo.Title
                        });

                        newMarker.poi = poi;

                        google.maps.event.addListener(newMarker, 'click', function () {
                            appcontext.showDetailsView(anchorElement, this.poi);
                            appcontext.showPage("locationdetails-page");
                        });

                        bounds.extend(newMarker.getPosition());
                        this.ocm_markers.push(newMarker);
                    }
                }
            }
        }

        if (this.mapOptions.enableClustering && this.ocm_markerClusterer) {
            this.ocm_markerClusterer.addMarkers(this.ocm_markers);
        }

        //include centre search location in bounds of map zoom
        if (this.ocm_searchmarker != null) bounds.extend(this.ocm_searchmarker.position);
        map.fitBounds(bounds);
    }

    //map.setCenter(markerLatlng);
    google.maps.event.trigger(this.map, 'resize');

};

OCM_CommonUI.prototype.initMapGoogle = function (mapcanvasID) {
    if (this.map == null) {

        var mapCanvas = document.getElementById(mapcanvasID);
        if (mapCanvas != null) {

            (<any>google.maps).visualRefresh = true;

            mapCanvas.style.width = '99.5%';
            mapCanvas.style.height = $(document).height().toString();

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

            this.map = new google.maps.Map(mapCanvas, mapOptions);

            if (this.mapOptions.enableClustering) {
                this.ocm_markerClusterer = new MarkerClusterer(this.map, this.ocm_markers);
            }
        }
    }
};

OCM_CommonUI.prototype.initMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser)
{
    if (this.map == null) {
        this.map = this.createMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser, 13);
    }
};

OCM_CommonUI.prototype.createMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser, zoomLevel) {
   
    // create a map in the "map" div, set the view to a given place and zoom
    var map = new L.Map(mapcanvasID);
    if (currentLat != null && currentLng != null) {
        map.setView(new L.LatLng(currentLat, currentLng), zoomLevel, true);
    }
    map.setZoom(zoomLevel);

    if (locateUser==true)
    {
        map.locate({setView: true, watch:true, enableHighAccuracy:true});
    } else {
        //use a default view
       
    }
    
    // add an OpenStreetMap tile layer
    new L.TileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    return map;
    
};

OCM_CommonUI.prototype.showPOIListOnMap = function (mapcanvasID, poiList, appcontext, anchorElement) {

    var mapCanvas = document.getElementById(mapcanvasID);
    if (mapCanvas != null) {

        //if (this.isMobileBrowser()) {
        mapCanvas.style.width = '90%';
        mapCanvas.style.height = '80%';
        //}

        //create map
        var mapOptions = {
            zoom: 3,
            mapTypeId: google.maps.MapTypeId.ROADMAP
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
            for (var i = 0; i < poiList.length; i++) {
                if (poiList[i].AddressInfo != null) {
                    if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {

                        var poi = poiList[i];

                        this.ocm_markers[i] = new google.maps.Marker({
                            position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                            map: map,
                            icon: "http://openchargemap.org/api/widgets/map/icons/green-circle.png",
                            title: poi.AddressInfo.Title
                        });

                        this.ocm_markers[i].poi = poi;

                        google.maps.event.addListener(this.ocm_markers[i], 'click', function () {
                            appcontext.showDetailsView(anchorElement, this.poi);
                           // $.mobile.changePage("#locationdetails-page");
                        });

                        bounds.extend(this.ocm_markers[i].position);
                    }
                }
            }
        }

        //include centre search location in bounds of map zoom
        if (this.ocm_searchmarker != null) bounds.extend(this.ocm_searchmarker.position);
        map.fitBounds(bounds);
    }
};

///Begin Standard data formatting methods ///
OCM_CommonUI.prototype.formatMapLinkFromPosition = function (poi, searchLatitude, searchLongitude, distance, distanceunit) {
    return '<a href="http://maps.google.com/maps?saddr=' + searchLatitude + ',' + searchLongitude + '&daddr=' + poi.AddressInfo.Latitude + ',' + poi.AddressInfo.Longitude + '">Map (' + Math.ceil(distance) + ' ' + distanceunit + ')</a>';
};

OCM_CommonUI.prototype.formatSystemWebLink = function (linkURL, linkTitle) {
    return "<a href='#' onclick=\"window.open('" + linkURL + "', '_system');return false;\">"+linkTitle+"</a>";
}

OCM_CommonUI.prototype.formatMapLink = function (poi, linkContent) {

    if (this.isRunningUnderCordova) {
        
        if (device.platform == "WinCE") {
            return "<a target=\"_system\" data-role=\"button\" data-icon=\"grid\" href=\"maps:" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "\">" + linkContent + "</a>";
        } else {
            return this.formatSystemWebLink("http://maps.google.com/maps?q=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude, linkContent);
        }
    }
    //default to google maps online link
    return "<a target=\"_blank\"  href=\"http://maps.google.com/maps?q=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude + "\">" + linkContent+ "</a>";

};

OCM_CommonUI.prototype.formatURL = function (url, title) {
    if (url == null || url == "") return "";
    if (url.indexOf("http") == -1) url = "http://" + url;
    return '<a target="_blank" href="' + url + '">' + (title != null ? title : url) + '</a>';
};

OCM_CommonUI.prototype.formatPOIAddress = function (poi) {
    var output = "" + this.formatTextField(poi.AddressInfo.AddressLine1) +
		this.formatTextField(poi.AddressInfo.AddressLine2) +
		this.formatTextField(poi.AddressInfo.Town) +
		this.formatTextField(poi.AddressInfo.StateOrProvince) +
		this.formatTextField(poi.AddressInfo.Postcode) +
        this.formatTextField(poi.AddressInfo.Country.Title);

    if (this.enablePOIMap == true) {
        output += "<div id='info_map'></div>";
    }
    return output;
};

OCM_CommonUI.prototype.formatString = function (val) {
    if (val == null) return "";
    return val.toString();
};

OCM_CommonUI.prototype.formatTextField = function (val, label, newlineAfterLabel, paragraph, resourceKey) {
    if (val == null || val == "" || val == undefined) return "";
    var output = (label != null ? "<strong class='ocm-label' " + (resourceKey != null ? "data-localize='" + resourceKey + "' " : "") + ">" + label + "</strong>: " : "") + (newlineAfterLabel ? "<br/>" : "") + (val.toString().replace("\n", "<br/>")) + "<br/>";
    if (paragraph == true) output = "<p>" + output + "</p>";
    return output;
};

OCM_CommonUI.prototype.formatEmailAddress = function (email) {
    if (email != undefined && email != null && email != "") {
        return "<i class='icon-envelope'></i> <a href=\"mailto:" + email + "\">" + email + "</a><br/>";
    } else return "";
};

OCM_CommonUI.prototype.formatPhone = function (phone, labeltitle) {
    if (phone != undefined && phone != null && phone != "") {
        if (labeltitle == null) labeltitle = "<i class='icon-phone'></i> ";
        else labeltitle += ": ";
        return labeltitle + "<a href=\"tel:" + phone + "\">" + phone + "</a><br/>";
    } else return "";
};

OCM_CommonUI.prototype.formatPOIDetails = function (poi, fullDetailsMode) {

    var dayInMilliseconds = 86400000;
    var currentDate = new Date();

    if (fullDetailsMode == null) fullDetailsMode = false;

    var addressInfo = this.formatPOIAddress(poi);

    var contactInfo = "";

    if (poi.AddressInfo.Distance != null) {
        var directionsUrl = "http://maps.google.com/maps?saddr=&daddr=" + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude;
        contactInfo += "<strong id='addr_distance'><span data-localize='ocm.details.approxDistance'>Distance</span>: " + poi.AddressInfo.Distance.toFixed(1) + " " + (poi.AddressInfo.DistanceUnit == 2 ? "Miles" : "KM") + "</strong>";
        contactInfo += " <br/><i class='icon-road'></i>  "+this.formatSystemWebLink(directionsUrl, "Get Directions")+"<br/>";
    }

    contactInfo += this.formatPhone(poi.AddressInfo.ContactTelephone1);
    contactInfo += this.formatPhone(poi.AddressInfo.ContactTelephone2);
    contactInfo += this.formatEmailAddress(poi.AddressInfo.ContactEmail);

    if (poi.AddressInfo.RelatedURL != null && poi.AddressInfo.RelatedURL != "") {
        var displayUrl = poi.AddressInfo.RelatedURL;
        //remove protocol from url
        displayUrl = displayUrl.replace(/.*?:\/\//g, "");
        //shorten url if over 40 characters
        if (displayUrl.length > 40) displayUrl = displayUrl.substr(0, 40) + "..";
        contactInfo += "<i class='icon-external-link'></i>  " + this.formatURL(poi.AddressInfo.RelatedURL, "<span data-localize='ocm.details.addressRelatedURL'>" + displayUrl + "</span>");
    }

    var comments = this.formatTextField(poi.GeneralComments, "Comments", true, true, "ocm.details.generalComments") +
				  this.formatTextField(poi.AddressInfo.AccessComments, "Access", true, true, "ocm.details.accessComments");

    var additionalInfo = "";

    if (poi.NumberOfPoints != null) {
        additionalInfo += this.formatTextField(poi.NumberOfPoints, "Number Of Points", false, false, "ocm.details.numberOfPoints");
    }

    if (poi.UsageType != null) {
        additionalInfo += this.formatTextField(poi.UsageType.Title, "Usage", false, false, "ocm.details.usageType");
    }

    if (poi.UsageCost != null) {
        additionalInfo += this.formatTextField(poi.UsageCost, "Usage Cost", false, false, "ocm.details.usageCost");
    }

    if (poi.OperatorInfo != null) {
        if (poi.OperatorInfo.ID != 1) { //skip unknown operators
            additionalInfo += this.formatTextField(poi.OperatorInfo.Title, "Operator", false, false, "ocm.details.operatorTitle");
            if (poi.OperatorInfo.WebsiteURL != null) {
                advancedInfo += this.formatTextField(this.formatURL(poi.OperatorInfo.WebsiteURL), "Operator Website", true, false, "ocm.details.operatorWebsite");
            }
        }
    }

    var equipmentInfo = "";

    if (poi.StatusType != null) {
        equipmentInfo += this.formatTextField(poi.StatusType.Title, "Status", false, false, "ocm.details.operationalStatus");
        if (poi.DateLastStatusUpdate != null) {
            equipmentInfo += this.formatTextField(Math.round(((<any>currentDate - <any>this.fixJSONDate(poi.DateLastStatusUpdate)) / dayInMilliseconds)) + " days ago", "Last Updated", false, false, "ocm.details.lastUpdated");
        }
    }

    //output table of connection info
    if (poi.Connections != null) {
        if (poi.Connections.length > 0) {

            equipmentInfo += "<table class='datatable'>";
            equipmentInfo += "<tr><th data-localize='ocm.details.equipment.connectionType'>Connection</th><th data-localize='ocm.details.equipment.powerLevel'>Power Level</th><th data-localize='ocm.details.operationalStatus'>Status</th><th data-localize='ocm.details.equipment.quantity'>Qty</th></tr>";

            for (var c = 0; c < poi.Connections.length; c++) {
                var con = poi.Connections[c];
                if (con.Amps == "") con.Amps = null;
                if (con.Voltage == "") con.Voltage = null;
                if (con.Quantity == "") con.Quantity = null;
                if (con.PowerKW == "") con.PowerKW = null;

                equipmentInfo += "<tr>" +
					"<td>" + (con.ConnectionType != null ? con.ConnectionType.Title : "") + "</td>" +
					"<td>" + (con.Level != null ? "<strong>" + con.Level.Title + "</strong><br/>" : "") +
					 (con.Amps != null ? this.formatString(con.Amps) + "A/ " : "") +
					 (con.Voltage != null ? this.formatString(con.Voltage) + "V/ " : "") +
					 (con.PowerKW != null ? this.formatString(con.PowerKW) + "kW <br/>" : "") +
                     (con.CurrentType != null ? con.CurrentType.Title : "") +
					"</td>" +
					"<td>" + (con.StatusType != null ? con.StatusType.Title : "-") + "</td>" +
					"<td>" + (con.Quantity != null ? this.formatString(con.Quantity) : "1") + "</td>" +
					"</tr>";
            }
            equipmentInfo += "</table>";
        }
    }

    var advancedInfo = "";
    advancedInfo += this.formatTextField("<a target='_blank' href='http://openchargemap.org/site/poi/details/" + poi.ID + "'>OCM-" + poi.ID + "</a>", "OpenChargeMap Ref", false, false, "ocm.details.refNumber");
    if (poi.DataProvider != null) {
        advancedInfo += this.formatTextField(poi.DataProvider.Title, "Data Provider", false, false, "ocm.details.dataProviderTitle");
        if (poi.DataProvider.WebsiteURL != null) {
            advancedInfo += this.formatTextField(this.formatURL(poi.DataProvider.WebsiteURL), "Website", false, false, "ocm.details.dataProviderWebsite");
        }
    }

    var output = {
        "address": addressInfo,
        "contactInfo": contactInfo,
        "additionalInfo": comments + additionalInfo + equipmentInfo,
        "advancedInfo": advancedInfo
    };
    return output;
};
/// End Standard Data Formatting methods ///