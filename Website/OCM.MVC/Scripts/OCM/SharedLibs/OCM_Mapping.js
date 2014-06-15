/// <reference path="OCM_Base.ts" />
/// <reference path="OCM_CommonUI.ts" />
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
var OCM;
(function (OCM) {
    var GeoLatLng = (function () {
        function GeoLatLng() {
        }
        return GeoLatLng;
    })();
    OCM.GeoLatLng = GeoLatLng;

    var MapCoords = (function () {
        function MapCoords() {
        }
        return MapCoords;
    })();
    OCM.MapCoords = MapCoords;

    var MapOptions = (function () {
        /** @constructor */
        function MapOptions() {
            this.enableClustering = false;
            this.resultBatchID = -1;
            this.useMarkerIcons = true;
            this.useMarkerAnimation = true;
            this.enableTrackingMapCentre = false;
            this.mapCentre = null;
            this.mapAPI = "google";
            this.searchDistanceKM = 1000 * 100;
        }
        return MapOptions;
    })();
    OCM.MapOptions = MapOptions;

    /** Mapping - provides a way to render to various mapping APIs
    * @module OCM.Mapping
    */
    var Mapping = (function (_super) {
        __extends(Mapping, _super);
        /** @constructor */
        function Mapping() {
            _super.call(this);
            this.updateMapCentrePos = function (lat, lng, moveMap) {
                //update record of map centre so search results can optionally be refreshed
                if (moveMap) {
                    //TODO: re-centre map
                    if (this.map) {
                        this.map.setCenter(new google.maps.LatLng(lat, lng));
                    }
                }

                this.mapOptions.mapCentre = { 'coords': { 'latitude': lat, 'longitude': lng } };
            };
            this.createMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser, zoomLevel) {
                // create a map in the "map" div, set the view to a given place and zoom
                var map = new L.Map(mapcanvasID);
                if (currentLat != null && currentLng != null) {
                    map.setView(new L.LatLng(currentLat, currentLng), zoomLevel, true);
                }
                map.setZoom(zoomLevel);

                if (locateUser == true) {
                    map.locate({ setView: true, watch: true, enableHighAccuracy: true });
                } else {
                    //use a default view
                }

                // add an OpenStreetMap tile layer
                new L.TileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
                    attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
                }).addTo(map);

                return map;
            };

            this.mapOptions = new MapOptions();
            this.mapsInitialised = false;
            this.enableLogging = true;
            this.mapAPIReady = false;
        }
        Mapping.prototype.setParentAppContext = function (context) {
            this.parentAppContext = context;
        };

        Mapping.prototype.setMapAPI = function (api) {
            this.mapOptions.mapAPI = api;
        };

        Mapping.prototype.initMapGoogleNativeSDK = function (mapcanvasID) {
            if (this.mapsInitialised)
                return false;

            this.log("Using Google Maps Native", 1 /* INFO */);

            if (plugin.google && plugin.google.maps) {
                var mapCanvas = document.getElementById(mapcanvasID);

                // mapCanvas.style.width = '100%';
                // mapCanvas.style.height = $(document).height().toString();
                //if available, use a native map
                this.map = plugin.google.maps.Map.getMap(mapCanvas);

                //set map options
                this.map.setOptions({
                    'mapType': plugin.google.maps.MapTypeId.HYBRID,
                    'controls': {
                        'compass': true,
                        'myLocationButton': true,
                        'indoorPicker': true,
                        'zoom': true
                    },
                    /* 'gestures': {
                    'scroll': false,  //Disable scrolling
                    'tilt': false,    //Disable changing the tilt
                    'rotate': false   //Disable changing the bearing
                    },*/
                    'camera': {
                        //'latLng': GORYOKAKU_JAPAN,
                        // 'tilt': 30,
                        'zoom': 15,
                        'bearing': 50
                    }
                });

                if (this.mapOptions.enableClustering) {
                    this.markerClusterer = new MarkerClusterer(this.map, this.markerList);
                }

                this.map.clear();
                this.map.refreshLayout();
                this.map.setVisible(true);
                this.mapsInitialised = true;
            }

            return true;
        };

        Mapping.prototype.showPOIListOnMapViewGoogleNativeSDK = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
            if (!this.mapsInitialised) {
                return;
            }

            //if list has changes to results render new markers etc
            //  if (this.mapOptions.resultBatchID != resultBatchID) {
            //this.mapOptions.resultBatchID = resultBatchID;
            var map = this.map;
            map.setVisible(true);
            map.clear();

            var bounds = new plugin.google.maps.LatLngBounds();

            if (this.mapOptions.enableClustering && this.markerClusterer) {
                this.markerClusterer.clearMarkers();
            }

            //clear existing markers
            if (this.markerList != null) {
                for (var m = 0; m < this.markerList.length; m++) {
                    if (this.markerList[m]) {
                        this.markerList[m].setMap(null);
                    }
                }
            }

            this.markerList = new Array();

            if (poiList != null) {
                //render poi markers
                var poiCount = poiList.length;
                for (var i = 0; i < poiList.length; i++) {
                    if (poiList[i].AddressInfo != null) {
                        if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {
                            var poi = poiList[i];

                            var poiLevel = OCM.Utils.getMaxLevelOfPOI(poi);

                            var iconURL = null;
                            var animation = null;
                            var shadow = null;
                            var markerImg = null;

                            if (this.mapOptions.useMarkerIcons) {
                                if (this.mapOptions.iconSet == "SmallPins") {
                                    iconURL = "images/icons/map/sm_pin_level" + poiLevel + ".png";
                                } else {
                                    iconURL = "images/icons/map/set2_level" + poiLevel + "_60x100.png";
                                    /*shadow = new plugin.google.maps.MarkerImage("images/icons/map/marker-shadow.png",
                                    new google.maps.Size(41.0, 31.0),
                                    new google.maps.Point(0, 0),
                                    new google.maps.Point(12.0, 15.0)
                                    );
                                    
                                    markerImg = new plugin.google.maps.MarkerImage(iconURL,
                                    new google.maps.Size(25.0, 31.0),
                                    new google.maps.Point(0, 0),
                                    new google.maps.Point(12.0, 15.0)
                                    );*/
                                }
                            }

                            //if (poiCount < 100 && this.mapOptions.useMarkerAnimation == true) {
                            //    animation = plugin.google.maps.Animation.DROP;
                            //}
                            var markerTooltip = "OCM-" + poi.ID + ": " + poi.AddressInfo.Title + ":";
                            if (poi.UsageType != null)
                                markerTooltip += " " + poi.UsageType.Title;
                            if (poiLevel > 0)
                                markerTooltip += " Level " + poiLevel;
                            if (poi.StatusType != null)
                                markerTooltip += " " + poi.StatusType.Title;

                            var parentContext = this.parentAppContext;

                            var markerPos = new plugin.google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude);

                            map.addMarker({
                                'position': markerPos,
                                'title': markerTooltip,
                                'snippet': "View details",
                                'icon': {
                                    'url': 'www/' + iconURL,
                                    'size': {
                                        'width': 30,
                                        'height': 50
                                    }
                                }
                            }, function (marker) {
                                marker.addEventListener(plugin.google.maps.event.INFO_CLICK, function () {
                                    var markerTitle = marker.getTitle();
                                    var poiId = markerTitle.substr(4, markerTitle.indexOf(":") - 4);

                                    parentContext.showDetailsViewById(poiId);
                                    parentContext.showPage("locationdetails-page");
                                });
                            });

                            bounds.extend(markerPos);
                            //this.ocm_markers.push(newMarker);
                        }
                    }
                }
            }

            if (this.mapOptions.enableClustering && this.markerClusterer) {
                this.markerClusterer.addMarkers(this.markerList);
            }

            //include centre search location in bounds of map zoom
            if (this.searchMarker != null)
                bounds.extend(this.searchMarker.position);

            var uiContext = this;

            //TODO: add marker for current search position
            /* if (this.mapOptions.enableTrackingMapCentre == false) {
            this.mapOptions.enableTrackingMapCentre = true;
            var uiContext = this;
            
            map.on(plugin.google.maps.event.CAMERA_CHANGE, function () {
            
            // 500ms after the center of the map has changed, update search centre pos
            window.setTimeout(function () {
            
            //create new latlng from map centre so that values get normalised to 180/-180
            var centrePos = new plugin.google.maps.LatLng(map.getCenter().lat(), map.getCenter().lng());
            this.log("Map centre changed, updating search position:" + centrePos);
            
            uiContext.updateMapCentrePos(centrePos.lat(), centrePos.lng(), false);
            }, 500);
            });
            }*/
            //}
            this.log("showPOIList: refreshing map layout");
            this.map.refreshLayout();

            if (this.mapOptions.mapCentre != null) {
                var gmMapCentre = new plugin.google.maps.LatLng(this.mapOptions.mapCentre.coords.latitude, this.mapOptions.mapCentre.coords.longitude);

                if (this.mapOptions.searchDistanceKM != null) {
                    map.addCircle({
                        'center': gmMapCentre,
                        'radius': this.mapOptions.searchDistanceKM,
                        'strokeColor': '#AA00FF',
                        'strokeWidth': 5,
                        'fillColor': '#009DFF33'
                    });
                }

                this.log("Animating camera to map centre:" + this.mapOptions.mapCentre);

                map.animateCamera({
                    'target': gmMapCentre,
                    'tilt': 60,
                    'zoom': 12,
                    'bearing': 0
                });
            }
        };

        Mapping.prototype.initMapGoogle = function (mapcanvasID) {
            if (this.mapsInitialised || !this.mapAPIReady)
                return false;

            if (this.map == null && google.maps) {
                this.mapsInitialised = true;
                var mapCanvas = document.getElementById(mapcanvasID);

                if (mapCanvas != null) {
                    google.maps.visualRefresh = true;

                    mapCanvas.style.width = '99.5%';
                    mapCanvas.style.height = $(document).height().toString();

                    if (this.markerClusterer && this.mapOptions.enableClustering) {
                        this.markerClusterer.clearMarkers();
                    }

                    //create map
                    var mapOptions = {
                        zoom: 10,
                        minZoom: 6,
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
                        this.markerClusterer = new MarkerClusterer(this.map, this.markerList);
                    }
                }
                return true;
            }
        };

        Mapping.prototype.showPOIListOnMapViewGoogle = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
            if (!this.mapsInitialised) {
                this.log("showPOIList; cannot show google map, not initialised.");
                return;
            }

             {
                this.mapOptions.resultBatchID = resultBatchID;

                var map = this.map;
                var bounds = new google.maps.LatLngBounds();

                if (this.mapOptions.enableClustering && this.markerClusterer) {
                    this.markerClusterer.clearMarkers();
                }

                //clear existing markers
                if (this.markerList != null) {
                    for (var i = 0; i < this.markerList.length; i++) {
                        if (this.markerList[i]) {
                            this.markerList[i].setMap(null);
                        }
                    }
                }

                this.markerList = new Array();
                if (poiList != null) {
                    //render poi markers
                    var poiCount = poiList.length;
                    for (var i = 0; i < poiList.length; i++) {
                        if (poiList[i].AddressInfo != null) {
                            if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {
                                var poi = poiList[i];

                                var poiLevel = OCM.Utils.getMaxLevelOfPOI(poi);

                                var iconURL = null;
                                var animation = null;
                                var shadow = null;
                                var markerImg = null;
                                if (this.mapOptions.useMarkerIcons) {
                                    if (this.mapOptions.iconSet == "SmallPins") {
                                        iconURL = "images/icons/map/sm_pin_level" + poiLevel + ".png";
                                    } else {
                                        iconURL = "images/icons/map/set2_level" + poiLevel + ".png";
                                        shadow = new google.maps.MarkerImage("images/icons/map/marker-shadow.png", new google.maps.Size(41.0, 31.0), new google.maps.Point(0, 0), new google.maps.Point(12.0, 15.0));

                                        markerImg = new google.maps.MarkerImage(iconURL, new google.maps.Size(25.0, 31.0), new google.maps.Point(0, 0), new google.maps.Point(12.0, 15.0));
                                    }
                                }

                                if (poiCount < 100 && this.mapOptions.useMarkerAnimation == true) {
                                    animation = google.maps.Animation.DROP;
                                }

                                var markerTooltip = "OCM-" + poi.ID + ": " + poi.AddressInfo.Title + ":";
                                if (poi.UsageType != null)
                                    markerTooltip += " " + poi.UsageType.Title;
                                if (poiLevel > 0)
                                    markerTooltip += " Level " + poiLevel;
                                if (poi.StatusType != null)
                                    markerTooltip += " " + poi.StatusType.Title;

                                var newMarker = new google.maps.Marker({
                                    position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                                    map: this.mapOptions.enableClustering ? null : map,
                                    icon: markerImg != null ? markerImg : iconURL,
                                    shadow: shadow,
                                    animation: animation,
                                    title: markerTooltip
                                });

                                /*var marker1 = new MarkerWithLabel({
                                position: homeLatLng,
                                draggable: true,
                                raiseOnDrag: true,
                                map: map,
                                labelContent: "$425K",
                                labelAnchor: new google.maps.Point(22, 0),
                                labelClass: "labels", // the CSS class for the label
                                labelStyle: { opacity: 0.75 }
                                });*/
                                newMarker.poi = poi;
                                var parentContext = this.parentAppContext;

                                google.maps.event.addListener(newMarker, 'click', function () {
                                    parentContext.showDetailsView(anchorElement, this.poi);
                                    parentContext.showPage("locationdetails-page");
                                });

                                bounds.extend(newMarker.getPosition());
                                this.markerList.push(newMarker);
                            }
                        }
                    }
                }

                if (this.mapOptions.enableClustering && this.markerClusterer) {
                    this.markerClusterer.addMarkers(this.markerList);
                }

                if (this.mapOptions.mapCentre != null) {
                    var marker = new google.maps.Marker({
                        position: new google.maps.LatLng(this.mapOptions.mapCentre.coords.latitude, this.mapOptions.mapCentre.coords.longitude),
                        map: map,
                        title: "Searching From Here",
                        content: 'Your search position'
                    });
                    bounds.extend(marker.getPosition());
                    this.markerList.push(marker);
                }

                //include centre search location in bounds of map zoom
                if (this.searchMarker != null)
                    bounds.extend(this.searchMarker.position);

                var uiContext = this;

                //zoom to bounds of markers
                if (poiList != null && poiList.length > 0) {
                    this.log("Fitting to marker bounds:" + bounds);
                    map.setCenter(bounds.getCenter());
                    this.log("zoom before fit bounds:" + map.getZoom());
                    map.fitBounds(bounds);

                    //fix incorrect zoom level when fitBounds guesses a zooom level of 0 etc.
                    var zoom = map.getZoom();
                    map.setZoom(zoom < 6 ? 6 : zoom);
                }

                //TODO: add marker for current search position
                if (this.mapOptions.enableTrackingMapCentre == false) {
                    this.mapOptions.enableTrackingMapCentre = true;
                    google.maps.event.addListener(map, 'center_changed', function () {
                        // 500ms after the center of the map has changed, update search centre pos
                        window.setTimeout(function () {
                            //create new latlng from map centre so that values get normalised to 180/-180
                            var centrePos = new google.maps.LatLng(map.getCenter().lat(), map.getCenter().lng());
                            uiContext.log("Map centre changed, updating search position:" + centrePos);

                            uiContext.updateMapCentrePos(centrePos.lat(), centrePos.lng(), false);
                        }, 500);
                    });
                }
            }

            /*else {
            if (this.enableLogging && console) console.log("Not rendering markers, batchId is same as last time.");
            }*/
            google.maps.event.trigger(this.map, 'resize');
        };

        Mapping.prototype.initMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser) {
            if (this.map == null) {
                this.map = this.createMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser, 13);
            }
        };

        Mapping.prototype.showPOIListOnMapViewLeaflet = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
            var map = this.map;

            //if list has changes to results render new markers etc
            if (this.mapOptions.resultBatchID != resultBatchID) {
                this.mapOptions.resultBatchID = resultBatchID;

                this.log("Setting up map markers:" + resultBatchID);

                // Creates a red marker with the coffee icon
                var unknownPowerMarker = L.AwesomeMarkers.icon({
                    icon: "bolt",
                    color: "darkpurple",
                    prefix: "fa"
                });

                var lowPowerMarker = L.AwesomeMarkers.icon({
                    icon: "bolt",
                    color: "darkblue",
                    spin: true,
                    prefix: "fa"
                });

                var mediumPowerMarker = L.AwesomeMarkers.icon({
                    icon: "bolt",
                    color: "green",
                    spin: true,
                    prefix: "fa"
                });

                var highPowerMarker = L.AwesomeMarkers.icon({
                    icon: "bolt",
                    color: "orange",
                    spin: true,
                    prefix: "fa"
                });

                if (this.mapOptions.enableClustering) {
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

                                    var poiLevel = OCM.Utils.getMaxLevelOfPOI(poi);
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

                                    usageTitle = "Unknown Usage Restrictions";

                                    if (poi.UsageType != null && poi.UsageType.ID != 0) {
                                        usageTitle = poi.UsageType.Title;
                                    }

                                    markerTitle += " (" + powerTitle + ", " + usageTitle + ")";

                                    var marker = new L.Marker(new L.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude), { icon: markerIcon, title: markerTitle, draggable: false, clickable: true });
                                    marker._isClicked = false; //workaround for double click event
                                    marker.poi = poi;
                                    marker.on('click', function (e) {
                                        if (this._isClicked == false) {
                                            this._isClicked = true;
                                            appcontext.showDetailsView(anchorElement, this.poi);
                                            appcontext.showPage("locationdetails-page");

                                            //workaround double click event by clearing clicked state after short time
                                            var mk = this;
                                            setTimeout(function () {
                                                mk._isClicked = false;
                                            }, 300);
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
                    setTimeout(function () {
                        map.invalidateSize(false);
                    }, 300);
                }
            }
        };

        Mapping.prototype.refreshMapView = function (mapCanvasID, mapHeight, poiList, searchPos) {
            document.getElementById(mapCanvasID).style.height = mapHeight + "px";

            if (this.mapOptions.mapAPI == "googlenativesdk") {
                this.log("Refreshing map using API: googlenativesdk");

                var appcontext = this;

                var isAvailable = plugin.google.maps.Map.isAvailable(function (isAvailable, message) {
                    if (isAvailable) {
                        if (searchPos != null) {
                            appcontext.updateMapCentrePos(searchPos.coords.latitude, searchPos.coords.longitude, false);
                        }

                        //setup map view if not already initialised
                        appcontext.initMapGoogleNativeSDK(mapCanvasID);
                        appcontext.showPOIListOnMapViewGoogleNativeSDK(mapCanvasID, poiList, this.parentAppContext, document.getElementById(mapCanvasID), appcontext.mapOptions.resultBatchID);
                    } else {
                        this.log("Native Maps not available");
                    }
                    return isAvailable;
                });

                if (!isAvailable) {
                    this.errorMessage = "Google Maps not installed";
                    return false;
                }
            }

            if (this.mapOptions.mapAPI == "google") {
                if (typeof (google) == "undefined") {
                    //no google maps currently available
                    this.errorMessage = "Google maps cannot be loaded. Please check your data connection.";
                    return false;
                }

                this.log("Refreshing map using API: google");

                if (searchPos != null) {
                    this.updateMapCentrePos(searchPos.coords.latitude, searchPos.coords.longitude, false);
                }

                //setup map view if not already initialised
                this.initMapGoogle(mapCanvasID);

                this.showPOIListOnMapViewGoogle(mapCanvasID, poiList, this, document.getElementById(mapCanvasID), this.mapOptions.resultBatchID);

                return true;
            }

            if (this.mapOptions.mapAPI == "leaflet") {
                //setup map view, tracking user pos, if not already initialised
                //TODO: use last search pos as lat/lng, or first item in locationList
                var centreLat = 50;
                var centreLng = 50;
                if (poiList != null && poiList.length > 0) {
                    centreLat = poiList[0].AddressInfo.Latitude;
                    centreLng = poiList[0].AddressInfo.Longitude;
                }
                this.initMapLeaflet(mapCanvasID, centreLat, centreLng, false);

                this.showPOIListOnMapViewLeaflet(mapCanvasID, poiList, this, document.getElementById(mapCanvasID), this.mapOptions.resultBatchID);
            }

            return true;
        };

        Mapping.prototype.hideMap = function () {
            if (this.mapOptions.mapAPI == "googlenativesdk") {
                this.log("Debug: Hiding Map");

                //hide map otherwise it will stay on top
                this.map.setVisible(false);
                this.map.refreshLayout();
            }
        };

        Mapping.prototype.showMap = function () {
            if (this.mapOptions.mapAPI == "googlenativesdk") {
                this.log("Debug: Showing Map");

                //show/reposition map
                this.map.setVisible(true);
                this.map.refreshLayout();
            }
        };

        Mapping.prototype.showPOIOnStaticMap = function (mapcanvasID, poi, includeMapLink, isRunningUnderCordova) {
            if (typeof includeMapLink === "undefined") { includeMapLink = false; }
            if (typeof isRunningUnderCordova === "undefined") { isRunningUnderCordova = false; }
            var mapCanvas = document.getElementById(mapcanvasID);
            if (mapCanvas != null) {
                var title = poi.AddressInfo.Title;
                var lat = poi.AddressInfo.Latitude;
                var lon = poi.AddressInfo.Longitude;
                var width = 200;
                var height = 200;

                var mapImageURL = "http://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon + "&zoom=14&size=" + width + "x" + height + "&maptype=roadmap&markers=color:blue%7Clabel:A%7C" + lat + "," + lon + "&sensor=false";
                var mapHTML = "";
                if (includeMapLink == true) {
                    mapHTML += "<div>" + OCM.Utils.formatMapLink(poi, "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>", isRunningUnderCordova) + "</div>";
                } else {
                    mapHTML += "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>";
                }

                mapCanvas.innerHTML = mapHTML;
            }
        };
        return Mapping;
    })(OCM.Base);
    OCM.Mapping = Mapping;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_Mapping.js.map
