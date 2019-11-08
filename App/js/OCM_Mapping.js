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
var OCM;
(function (OCM) {
    var GeoLatLng = (function () {
        function GeoLatLng(lat, lng) {
            if (lat === void 0) { lat = null; }
            if (lng === void 0) { lng = null; }
            this.latitude = lat;
            this.longitude = lng;
        }
        return GeoLatLng;
    }());
    OCM.GeoLatLng = GeoLatLng;
    var GeoPosition = (function () {
        function GeoPosition(lat, lng) {
            if (lat === void 0) { lat = null; }
            if (lng === void 0) { lng = null; }
            this.coords = new GeoLatLng();
            this.coords.latitude = lat;
            this.coords.longitude = lng;
        }
        GeoPosition.fromPosition = function (pos) {
            return new GeoPosition(pos.coords.latitude, pos.coords.longitude);
        };
        return GeoPosition;
    }());
    OCM.GeoPosition = GeoPosition;
    var MapOptions = (function () {
        function MapOptions() {
            this.enableClustering = false;
            this.resultBatchID = -1;
            this.useMarkerIcons = true;
            this.useMarkerAnimation = true;
            this.enableTrackingMapCentre = false;
            this.enableSearchByWatchingLocation = false;
            this.mapCentre = null;
            this.mapAPI = MappingAPI.GOOGLE_WEB;
            this.mapType = "ROADMAP";
            this.searchDistanceKM = 1000 * 100;
            this.mapMoveQueryRefreshMS = 300;
            this.enableSearchRadiusIndicator = false;
            this.minZoomLevel = 2;
        }
        return MapOptions;
    }());
    OCM.MapOptions = MapOptions;
    var MappingAPI;
    (function (MappingAPI) {
        MappingAPI[MappingAPI["GOOGLE_WEB"] = 0] = "GOOGLE_WEB";
        MappingAPI[MappingAPI["GOOGLE_NATIVE"] = 1] = "GOOGLE_NATIVE";
        MappingAPI[MappingAPI["LEAFLET"] = 2] = "LEAFLET";
    })(MappingAPI = OCM.MappingAPI || (OCM.MappingAPI = {}));
    var Mapping = (function (_super) {
        __extends(Mapping, _super);
        function Mapping() {
            var _this = _super.call(this) || this;
            _this.mapOptions = new MapOptions();
            _this.mapAPIReady = false;
            _this.mapsInitialised = false;
            _this.mapReady = false;
            _this.setMapAPI(_this.mapOptions.mapAPI);
            var mapManagerContext = _this;
            _this.debouncedMapPositionUpdate = OCM.Utils.debounce(function () {
                mapManagerContext.log("signaling map position change:");
                if (mapManagerContext.mapProvider.mapReady) {
                    var centerPos = mapManagerContext.mapProvider.getMapCenter();
                    mapManagerContext.log("Map centre/zoom changed, updating search position:" + centerPos);
                    mapManagerContext.updateMapCentrePos(centerPos.coords.latitude, centerPos.coords.longitude, false);
                }
            }, 300, false);
            return _this;
        }
        Mapping.prototype.setParentAppContext = function (context) {
            this.parentAppContext = context;
        };
        Mapping.prototype.setMapAPI = function (api) {
            this.mapOptions.mapAPI = api;
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.mapProvider = new OCM.MapProviders.GoogleMapsNative();
            }
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_WEB) {
                this.mapProvider = new OCM.MapProviders.GoogleMapsWeb();
            }
        };
        Mapping.prototype.isMapReady = function () {
            if (this.mapProvider != null) {
                return this.mapProvider.mapReady;
            }
            else {
                return false;
            }
        };
        Mapping.prototype.externalAPILoaded = function (mapAPI) {
            this.mapAPIReady = true;
            this.log("Mapping API Loaded: " + OCM.MappingAPI[mapAPI]);
        };
        Mapping.prototype.initMap = function (mapcanvasID) {
            if (this.mapProvider != null) {
                if (this.mapsInitialised) {
                    this.log("initMap: Map provider already initialised");
                }
                this.log("Mapping Manager: Init " + OCM.MappingAPI[this.mapProvider.mapAPIType]);
                this.mapProvider.initMap(mapcanvasID, this.mapOptions, $.proxy(this.mapManipulationPerformed, this), this);
            }
            else {
                if (this.mapsInitialised) {
                    this.log("initMap: map already initialised");
                    return;
                }
                else {
                    this.log("initMap: " + this.mapOptions.mapAPI);
                }
            }
        };
        Mapping.prototype.updateSearchPosMarker = function (searchPos) {
            if (this.parentAppContext.appConfig.enableLiveMapQuerying)
                return;
            var mapManagerContext = this;
            if (this.mapProvider != null) {
            }
            else {
                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                    var map = this.map;
                    this.log("would add/update search pos marker");
                    if (mapManagerContext.mapCentreMarker != null) {
                        mapManagerContext.log("Updating search marker position");
                        mapManagerContext.mapCentreMarker.setPosition(searchPos);
                        if (this.mapReady)
                            map.refreshLayout();
                    }
                    else {
                        if (this.mapReady) {
                            mapManagerContext.log("Adding search marker position");
                            map.addMarker({
                                'position': searchPos,
                                'draggable': true,
                                title: "Tap to Searching from here, Drag to change position.",
                                content: 'Your search position'
                            }, function (marker) {
                                mapManagerContext.mapCentreMarker = marker;
                                marker.addEventListener(plugin.google.maps.event.MARKER_CLICK, function (marker) {
                                    marker.getPosition(function (pos) {
                                        mapManagerContext.log("Search marker tapped, requesting search from current position.");
                                        mapManagerContext.updateMapCentrePos(pos.lat(), pos.lng(), false);
                                        mapManagerContext.mapOptions.requestSearchUpdate = true;
                                    });
                                });
                                marker.addEventListener(plugin.google.maps.event.MARKER_DRAG_END, function (marker) {
                                    marker.getPosition(function (pos) {
                                        mapManagerContext.updateMapCentrePos(pos.lat(), pos.lng(), false);
                                        mapManagerContext.mapOptions.requestSearchUpdate = true;
                                    });
                                });
                            });
                        }
                    }
                }
                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_WEB) {
                    var map = this.map;
                    if (mapManagerContext.mapCentreMarker != null) {
                        mapManagerContext.log("Updating search marker position");
                        mapManagerContext.mapCentreMarker.setPosition(searchPos);
                        mapManagerContext.mapCentreMarker.setMap(map);
                    }
                    else {
                        mapManagerContext.log("Adding search marker position");
                        mapManagerContext.mapCentreMarker = new google.maps.Marker({
                            position: searchPos,
                            map: map,
                            draggable: true,
                            title: "Tap to Searching from here, Drag to change position.",
                            icon: "images/icons/compass.png"
                        });
                        var infowindow = new google.maps.InfoWindow({
                            content: "Tap marker to search from here, Drag marker to change search position."
                        });
                        infowindow.open(map, mapManagerContext.mapCentreMarker);
                        google.maps.event.addListener(mapManagerContext.mapCentreMarker, 'click', function () {
                            mapManagerContext.log("Search markers tapped, requesting search.");
                            var pos = mapManagerContext.mapCentreMarker.getPosition();
                            mapManagerContext.updateMapCentrePos(pos.lat(), pos.lng(), false);
                            mapManagerContext.mapOptions.requestSearchUpdate = true;
                        });
                        google.maps.event.addListener(mapManagerContext.mapCentreMarker, 'dragend', function () {
                            mapManagerContext.log("Search marker moved, requesting search.");
                            var pos = mapManagerContext.mapCentreMarker.getPosition();
                            mapManagerContext.updateMapCentrePos(pos.lat(), pos.lng(), false);
                            mapManagerContext.mapOptions.requestSearchUpdate = true;
                        });
                    }
                }
            }
        };
        Mapping.prototype.mapManipulationPerformed = function (manipulationType) {
            this.log("map manipulated:" + manipulationType);
            var mapManagerContext = this;
            if (manipulationType == "drag" || manipulationType == "zoom") {
                this.debouncedMapPositionUpdate();
            }
        };
        Mapping.prototype.initMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser) {
            if (this.map == null) {
                this.map = this.createMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser, 13);
                this.mapReady = true;
            }
        };
        Mapping.prototype.showPOIListOnMapViewLeaflet = function (mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
            var map = this.map;
            if (this.mapOptions.resultBatchID != resultBatchID) {
                this.mapOptions.resultBatchID = resultBatchID;
                this.log("Setting up map markers:" + resultBatchID);
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
                                    marker._isClicked = false;
                                    marker.poi = poi;
                                    marker.on('click', function (e) {
                                        if (this._isClicked == false) {
                                            this._isClicked = true;
                                            appcontext.showDetailsView(anchorElement, this.poi);
                                            appcontext.showPage("locationdetails-page");
                                            var mk = this;
                                            setTimeout(function () { mk._isClicked = false; }, 300);
                                        }
                                    });
                                    markerClusterGroup.addLayer(marker);
                                }
                            }
                        }
                    }
                    map.addLayer(markerClusterGroup);
                    map.fitBounds(markerClusterGroup.getBounds());
                    setTimeout(function () { map.invalidateSize(false); }, 300);
                }
            }
        };
        Mapping.prototype.createMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser, zoomLevel) {
            var map = new L.Map(mapcanvasID);
            if (currentLat != null && currentLng != null) {
                map.setView(new L.LatLng(currentLat, currentLng), zoomLevel, true);
            }
            map.setZoom(zoomLevel);
            if (locateUser == true) {
                map.locate({ setView: true, watch: true, enableHighAccuracy: true });
            }
            else {
            }
            new L.TileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://osm.org/copyright">OpenStreetMap</a> contributors'
            }).addTo(map);
            return map;
        };
        ;
        Mapping.prototype.isNativeMapsAvailable = function () {
            if (plugin && plugin.google && plugin.google.maps) {
                return true;
            }
            else {
                return false;
            }
        };
        Mapping.prototype.updateMapSize = function () {
            if (this.mapProvider) {
                if (this.mapProvider.mapReady) {
                    this.mapProvider.refreshMapLayout();
                }
            }
        };
        Mapping.prototype.updateMapCentrePos = function (lat, lng, moveMap) {
            if (moveMap) {
                if (this.mapProvider != null) {
                    this.mapProvider.setMapCenter(new GeoPosition(lat, lng));
                }
            }
            this.mapOptions.mapCentre = new GeoPosition(lat, lng);
        };
        ;
        Mapping.prototype.refreshMapView = function (mapHeight, poiList, searchPos) {
            if (this.mapProvider != null) {
                this.log("Mapping Manager: renderMap " + OCM.MappingAPI[this.mapProvider.mapAPIType]);
                if (this.isMapReady()) {
                    this.mapProvider.renderMap(poiList, mapHeight, this.parentAppContext);
                }
                else {
                    this.log("refreshMapView: map provider not initialised..");
                }
            }
            else {
                this.log("Unsupported Map API: refreshMapView", OCM.LogLevel.ERROR);
            }
            return true;
        };
        Mapping.prototype.setMapType = function (maptype) {
            if (this.mapOptions.mapType == maptype)
                return;
            this.mapOptions.mapType = maptype;
            this.log("Changing map type:" + maptype);
            if (this.isMapReady()) {
                if (this.mapProvider != null) {
                    this.mapProvider.setMapType(maptype);
                }
                else {
                    if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                        try {
                            this.map.setMapTypeId(eval("plugin.google.maps.MapTypeId." + maptype));
                        }
                        catch (exception) {
                            this.log("Failed to set map type:" + maptype + " : " + exception.toString());
                        }
                    }
                }
            }
            else {
                this.log("Map type set, maps not initialised yet.");
            }
        };
        Mapping.prototype.hideMap = function () {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.log("Debug: Hiding Map");
                if (this.map != null && this.mapReady) {
                    this.map.setVisible(false);
                    this.map.setClickable(false);
                }
            }
        };
        Mapping.prototype.showMap = function () {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.log("Debug: Showing Map");
                if (this.map != null && this.mapReady) {
                    this.map.refreshLayout();
                    this.map.setVisible(true);
                    this.map.setClickable(true);
                }
                else {
                    this.log("Map not available - check API?", OCM.LogLevel.ERROR);
                }
            }
        };
        Mapping.prototype.unfocusMap = function () {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                this.map.setClickable(false);
            }
        };
        Mapping.prototype.focusMap = function () {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                this.map.setClickable(true);
            }
        };
        Mapping.prototype.getMapBounds = function () {
            return this.mapProvider.getMapBounds();
        };
        Mapping.prototype.getMapZoom = function () {
            return this.mapProvider.getMapZoom();
        };
        Mapping.prototype.showPOIOnStaticMap = function (mapcanvasID, poi, includeMapLink, isRunningUnderCordova, mapWidth, mapHeight) {
            if (includeMapLink === void 0) { includeMapLink = false; }
            if (isRunningUnderCordova === void 0) { isRunningUnderCordova = false; }
            if (mapWidth === void 0) { mapWidth = 200; }
            if (mapHeight === void 0) { mapHeight = 200; }
            var mapCanvas = document.getElementById(mapcanvasID);
            if (mapCanvas != null) {
                var title = poi.AddressInfo.Title;
                var lat = poi.AddressInfo.Latitude;
                var lon = poi.AddressInfo.Longitude;
                if (mapWidth > 640)
                    mapWidth = 640;
                if (mapHeight > 640)
                    mapHeight = 640;
                var width = mapWidth;
                var height = mapHeight;
                var mapImageURL = "https://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon + "&zoom=14&size=" + width + "x" + height + "&maptype=roadmap&markers=color:blue%7Clabel:A%7C" + lat + "," + lon + "&sensor=false";
                var mapHTML = "";
                if (includeMapLink == true) {
                    mapHTML += "<div>" + OCM.Utils.formatMapLink(poi, "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>", isRunningUnderCordova) + "</div>";
                }
                else {
                    mapHTML += "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>";
                }
                mapCanvas.innerHTML = mapHTML;
            }
        };
        return Mapping;
    }(OCM.Base));
    OCM.Mapping = Mapping;
})(OCM || (OCM = {}));
