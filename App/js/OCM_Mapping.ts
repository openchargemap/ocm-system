/// <reference path="OCM_Base.ts" />
/// <reference path="OCM_CommonUI.ts" />

/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

module OCM {
    export class GeoLatLng implements Coordinates {
        //based on HTML Geolocation "Coordinates"
        public altitudeAccuracy: number;
        public longitude: number;
        public latitude: number;
        public speed: number;
        public heading: number;
        public altitude: number;
        public accuracy: number;

        constructor(lat: number = null, lng: number = null) {
            this.latitude = lat;
            this.longitude = lng;
        }
    }

    export class GeoPosition {
        //based on HTML Geolocation "Position"
        public coords: GeoLatLng;
        public timestamp: number;
        public attribution: string;

        constructor(lat: number = null, lng: number = null) {
            this.coords = new GeoLatLng();
            this.coords.latitude = lat;
            this.coords.longitude = lng;
        }

        static fromPosition(pos: Position): GeoPosition {
            return new GeoPosition(pos.coords.latitude, pos.coords.longitude);
        }
    }

    export class MapOptions {
        public enableClustering: boolean;
        public resultBatchID: number;

        public useMarkerIcons: boolean;
        public useMarkerAnimation: boolean;
        public enableTrackingMapCentre: boolean;
        public enableSearchByWatchingLocation: boolean;
        public mapCentre: GeoPosition;
        public searchDistanceKM: number;
        public iconSet: string;
        public mapAPI: MappingAPI;
        public mapMoveQueryRefreshMS: number; //time to wait before recognising map centre has changed
        public requestSearchUpdate: boolean;
        public enableSearchRadiusIndicator: boolean;
        public mapType: string;
        public minZoomLevel: number;

        /** @constructor */
        constructor() {
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
    }

    export enum MappingAPI {
        GOOGLE_WEB,
        GOOGLE_NATIVE,
        LEAFLET
    }

    export interface IMapProvider {
        mapAPIType: OCM.MappingAPI;
        mapReady: boolean;
        providerError: string;

        initMap(mapCanvasID: string, mapConfig: MapOptions, mapManipulationCallback: any);
        refreshMapLayout();
        renderMap(poiList: Array<any>, mapHeight: number, parentContext: App);
        getMapZoom(): number;
        setMapZoom(zoomLevel: number);
        getMapCenter(): GeoPosition;
        setMapCenter(pos: GeoPosition);
        setMapType(mapType: string);
        getMapBounds(): Array<GeoLatLng>;
    }

    /** Mapping - provides a way to render to various mapping APIs
     * @module OCM.Mapping
     */
    export class Mapping extends Base {
        public map: any;
        public mapCentreMarker: any;
        public mapsInitialised: boolean; //initial map setup initiated
        public mapAPIReady: boolean; //api loaded
        public mapReady: boolean; //map ready for any api calls (initialisation completed)
        public mapOptions: MapOptions;
        public markerClusterer: any;
        public markerList: Array<any>;
        public searchMarker: any;

        public errorMessage: string;
        public parentAppContext: OCM.App;
        private _mapMoveTimer: any;
        private mapProvider: IMapProvider;

        /** @constructor */
        constructor() {
            super();

            this.mapOptions = new MapOptions();

            this.mapAPIReady = false;
            this.mapsInitialised = false;
            this.mapReady = false;

            this.setMapAPI(this.mapOptions.mapAPI);
        }

        setParentAppContext(context: any) {
            this.parentAppContext = context;
        }

        setMapAPI(api: OCM.MappingAPI) {
            this.mapOptions.mapAPI = api;

            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                if (plugin.google.maps) {
                    this.mapAPIReady = true;
                }
            }

            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_WEB) {
                this.mapProvider = new OCM.MapProviders.GoogleMapsWeb();
            }
        }

        isMapReady() {
            if (this.mapProvider != null) {
                return this.mapProvider.mapReady;
            } else {
                //TODO: remove when all map providers implemented
                if (this.map != null && this.isMapReady) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        externalAPILoaded(mapAPI: OCM.MappingAPI) {
            this.mapAPIReady = true;
            this.log("Mapping API Loaded: " + mapAPI);
        }

        /**
        * Used by map provider as callback when a zoom or pan/drag has been performed
        * @param manipulationType  string name for event "zoom", "drag" etc
        */
        mapManipulationPerformed(manipulationType: string) {
            this.log("map manipulated:" + manipulationType);
            var mapManagerContext = this;
            if (manipulationType == "drag" || manipulationType == "zoom") {
                if (mapManagerContext._mapMoveTimer != null) {
                    clearTimeout(mapManagerContext._mapMoveTimer);
                    mapManagerContext._mapMoveTimer = null;
                }

                //after the center of the map centre has stopped changing, update search centre pos
                mapManagerContext._mapMoveTimer = window.setTimeout(function () {
                    try {
                        //create new latlng from map centre so that values get normalised to 180/-180

                        var centerPos: GeoPosition = mapManagerContext.mapProvider.getMapCenter();
                        mapManagerContext.log("Map centre/zoom changed, updating search position:" + centerPos);
                        mapManagerContext.updateMapCentrePos(centerPos.coords.latitude, centerPos.coords.longitude, false);
                        //mapManagerContext.updateSearchPosMarker(centerPos);
                    } catch (exc) {
                        //failed to update map centre pos
                    }

                    clearTimeout(mapManagerContext._mapMoveTimer);
                    mapManagerContext._mapMoveTimer = null;

                    setTimeout(function () {
                        mapManagerContext.mapOptions.enableTrackingMapCentre = true;
                    }, mapManagerContext.mapOptions.mapMoveQueryRefreshMS - 200);
                }, mapManagerContext.mapOptions.mapMoveQueryRefreshMS);
            }
        }

        /**
         * Performs one-time init of map object, detects cordova and chooses map api as appropriate
         * @param mapcanvasID  dom element for map canvas
         */
        initMap(mapcanvasID: string) {
            if (this.mapProvider != null) {
                this.log("Mapping Manager: Init " + OCM.MappingAPI[this.mapProvider.mapAPIType]);
                this.mapProvider.initMap(mapcanvasID, this.mapOptions, $.proxy(this.mapManipulationPerformed, this));
                this.mapsInitialised = true;
            } else {
                if (this.mapsInitialised) {
                    this.log("initMap: map already initialised");
                    return;
                } else {
                    this.log("initMap: " + this.mapOptions.mapAPI)
                }
                //detect if running under cordova and if google maps plugin is available
                if (this.parentAppContext.appState.isRunningUnderCordova) {
                    var mappingManager = this;

                    if (mappingManager.mapOptions.mapAPI != OCM.MappingAPI.GOOGLE_NATIVE) {
                        //for cordova, switch over to native google maps, if available
                        if ((<any>window).plugin && plugin.google && plugin.google.maps) {
                            plugin.google.maps.Map.isAvailable(function (isAvailable, message) {
                                if (isAvailable) {
                                    mappingManager.log("Native maps available, switching API.");
                                    mappingManager.setMapAPI(OCM.MappingAPI.GOOGLE_NATIVE);
                                } else {
                                    mappingManager.log("Google Play Services not available, fallback to web maps API");
                                }
                            });
                        } else {
                            mappingManager.log("Running under cordova but no native maps plugin available.");
                        }
                    }
                }

                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                    this.initMapGoogleNativeSDK(mapcanvasID);
                }
            }
        }

        initMapGoogleNativeSDK(mapcanvasID: string) {
            if (this.mapsInitialised) return false;

            this.log("Using Google Maps Native", LogLevel.INFO);

            if (plugin.google && plugin.google.maps) {
                var mapManagerContext = this;
                var mapCanvas = document.getElementById(mapcanvasID);

                this.map = plugin.google.maps.Map.getMap();
                var map = this.map;

                if (this.mapOptions.enableClustering) {
                    this.markerClusterer = new MarkerClusterer(this.map, this.markerList);
                }

                this.map.addEventListener(plugin.google.maps.event.MY_LOCATION_BUTTON_CLICK, function () {
                    map.getMyLocation(function (location) {
                        var msg = JSON.stringify(location);
                        mapManagerContext.log("My Location Clicked, updating search position: " + msg);
                        mapManagerContext.updateMapCentrePos(location.latLng.lat, location.latLng.lng, false);
                        mapManagerContext.updateSearchPosMarker(new plugin.google.maps.LatLng(location.latLng.lat, location.latLng.lng));
                        mapManagerContext.mapOptions.requestSearchUpdate = true;
                    }
                    );
                });

                this.map.addEventListener(plugin.google.maps.event.CAMERA_CHANGE, function () {
                    if (mapManagerContext._mapMoveTimer != null) {
                        clearTimeout(mapManagerContext._mapMoveTimer);
                        mapManagerContext._mapMoveTimer = null;
                    }

                    var map = this.map;

                    // after the center of the map centre has stopped changing, update search centre pos
                    mapManagerContext._mapMoveTimer = window.setTimeout(function () {
                        try {
                            //create new latlng from map centre so that values get normalised to 180/-180
                            var centrePos = new plugin.google.maps.LatLng(map.getCenter().lat(), map.getCenter().lng());
                            mapManagerContext.log("Map centre changed, updating search position:" + centrePos);

                            mapManagerContext.updateMapCentrePos(centrePos.lat(), centrePos.lng(), false);
                            mapManagerContext.updateSearchPosMarker(centrePos);
                        } catch (exc) {
                            //failed to update map centre pos
                        }

                        clearTimeout(mapManagerContext._mapMoveTimer);
                        mapManagerContext._mapMoveTimer = null;
                    }, mapManagerContext.mapOptions.mapMoveQueryRefreshMS);
                });

                this.map.on(plugin.google.maps.event.MAP_READY, function () {
                    mapManagerContext.log("Native Mapping Ready.", LogLevel.INFO);

                    var mapOptions = {
                        mapType: plugin.google.maps.MapTypeId.ROADMAP,
                        controls: {
                            compass: true,
                            myLocationButton: true,
                            zoom: true
                        },
                        gestures: {
                            scroll: true,
                            tilt: true,
                            rotate: true,
                            zoom: true
                        }
                        /*,
                        camera: {
                            zoom: 15,
                            bearing: 50
                        }*/
                    };
                    map.setOptions(mapOptions);
                    map.setDiv(mapCanvas);

                    map.setVisible(true);

                    mapManagerContext.mapReady = true;
                });
            }

            //map init has completed, map not actually ready until mapReady==true
            mapManagerContext.mapAPIReady = true;
            mapManagerContext.mapsInitialised = true;

            return true;
        }

        showPOIListOnMapViewGoogleNativeSDK(mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
            var app = this;
            if (!this.mapReady) {
                return;
            }
            //if list has changes to results render new markers etc

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
                                }
                            }

                            var markerTooltip = "OCM-" + poi.ID + ": " + poi.AddressInfo.Title + ":";
                            if (poi.UsageType != null) markerTooltip += " " + poi.UsageType.Title;
                            if (poiLevel > 0) markerTooltip += " Level " + poiLevel;
                            if (poi.StatusType != null) markerTooltip += " " + poi.StatusType.Title;

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
                                //show full details when info window tapped
                                marker.addEventListener(plugin.google.maps.event.INFO_CLICK, function () {
                                    var markerTitle = marker.getTitle();
                                    var poiId = markerTitle.substr(4, markerTitle.indexOf(":") - 4);
                                    //app.hideMap();

                                    parentContext.showDetailsViewById(poiId, false);
                                    parentContext.showPage("locationdetails-page", "Location Details");
                                });
                            });

                            bounds.extend(markerPos);
                        }
                    }
                }
            }

            if (this.mapOptions.enableClustering && this.markerClusterer) {
                this.markerClusterer.addMarkers(this.markerList);
            }

            //include centre search location in bounds of map zoom
            if (this.searchMarker != null) bounds.extend(this.searchMarker.position);

            var uiContext = this;

            this.log("Native Maps: refreshing map layout");
            this.map.refreshLayout();

            if (this.mapOptions.mapCentre != null) {
                var gmMapCentre = new plugin.google.maps.LatLng(this.mapOptions.mapCentre.coords.latitude, this.mapOptions.mapCentre.coords.longitude);

                if (this.mapOptions.searchDistanceKM != null && this.mapOptions.enableSearchRadiusIndicator) {
                    map.addCircle({
                        'center': gmMapCentre,
                        'radius': this.mapOptions.searchDistanceKM,
                        'strokeColor': '#AA00FF',
                        'strokeWidth': 5,
                        'fillColor': '#009DFF33'
                    });
                }

                this.log("Moving camera to map centre:" + this.mapOptions.mapCentre);

                //move camera insteaad of animating
                setTimeout(function () {
                    map.moveCamera({
                        'target': gmMapCentre,
                        'tilt': 60,
                        'zoom': 12,
                        'bearing': 0
                    });
                }, 500);
            } else {
                this.log("map centre not set, not setting camera");
            }
        }

        updateSearchPosMarker(searchPos) {
            //skip search marker if using live map viewport bounds querying
            if (this.parentAppContext.appConfig.enableLiveMapQuerying) return;

            var mapManagerContext = this;

            if (this.mapProvider != null) {
                //TODO:?
            } else {
                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                    var map = this.map;
                    this.log("would add/update search pos marker");
                    if (mapManagerContext.mapCentreMarker != null) {
                        mapManagerContext.log("Updating search marker position");
                        mapManagerContext.mapCentreMarker.setPosition(searchPos);
                        if (this.mapReady) map.refreshLayout();
                        //mapManagerContext.mapCentreMarker.setMap(map);
                    } else {
                        if (this.mapReady) {
                            mapManagerContext.log("Adding search marker position");

                            map.addMarker({
                                'position': searchPos,
                                'draggable': true,
                                title: "Tap to Searching from here, Drag to change position.",
                                content: 'Your search position'
                                // icon: "images/icons/compass.png"
                            }, function (marker) {
                                mapManagerContext.mapCentreMarker = marker;

                                //marker click
                                marker.addEventListener(plugin.google.maps.event.MARKER_CLICK, function (marker) {
                                    marker.getPosition(function (pos) {
                                        mapManagerContext.log("Search marker tapped, requesting search from current position.");

                                        mapManagerContext.updateMapCentrePos(pos.lat(), pos.lng(), false);
                                        mapManagerContext.mapOptions.requestSearchUpdate = true;
                                    });
                                });

                                //marker drag
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
                    } else {
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
        }

        initMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser) {
            if (this.map == null) {
                this.map = this.createMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser, 13);
                this.mapReady = true;
            }
        }

        showPOIListOnMapViewLeaflet(mapcanvasID, poiList, appcontext, anchorElement, resultBatchID) {
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

                                    var marker = <any>new L.Marker(new L.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude), <L.MarkerOptions>{ icon: markerIcon, title: markerTitle, draggable: false, clickable: true });
                                    marker._isClicked = false; //workaround for double click event
                                    marker.poi = poi;
                                    marker.on('click',
                                        function (e) {
                                            if (this._isClicked == false) {
                                                this._isClicked = true;
                                                appcontext.showDetailsView(anchorElement, this.poi);
                                                appcontext.showPage("locationdetails-page");

                                                //workaround double click event by clearing clicked state after short time
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

                    //refresh map view
                    setTimeout(function () { map.invalidateSize(false); }, 300);
                }
            }
        }

        createMapLeaflet = function (mapcanvasID, currentLat, currentLng, locateUser, zoomLevel) {
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

        isNativeMapsAvailable(): boolean {
            if (plugin && plugin.google && plugin.google.maps) {
                return true;
            } else {
                return false;
            }
        }

        updateMapSize() {
            if (this.mapProvider) {
                if (this.mapProvider.mapReady) {
                    this.mapProvider.refreshMapLayout();
                }
            } else {
                if (this.map != null) {
                    if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                        this.map.refreshLayout();
                    }
                }
            }
        }

        updateMapCentrePos = function (lat: number, lng: number, moveMap: boolean) {
            //update record of map centre so search results can optionally be refreshed
            if (moveMap) {
                if (this.mapProvider != null) {
                    this.mapProvider.setMapCenter(lat, lng);
                } else {
                    if (this.map && this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                        this.map.setCenter(new plugin.google.maps.LatLng(lat, lng));
                    }
                }
            }

            this.mapOptions.mapCentre = new GeoPosition(lat, lng);
        };

        refreshMapView(mapCanvasID: string, mapHeight: number, poiList: Array<any>, searchPos: any): boolean {
            if (this.mapProvider != null) {
                this.log("Mapping Manager: renderMap " + OCM.MappingAPI[this.mapProvider.mapAPIType]);
                this.mapProvider.renderMap(poiList, mapHeight, this.parentAppContext);
            } else {
                document.getElementById(mapCanvasID).style.height = mapHeight + "px";
                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                    this.log("Refreshing map using API: googlenativesdk");

                    var appcontext = this;

                    var isAvailable = plugin.google.maps.Map.isAvailable(function (isAvailable, message) {
                        if (isAvailable) {
                            //setup map view if not already initialised
                            appcontext.initMap(mapCanvasID);

                            if (appcontext.parentAppContext != null && appcontext.parentAppContext.viewModel.searchPosition != null) {
                                var searchPos = appcontext.parentAppContext.viewModel.searchPosition;
                                if (searchPos != null) {
                                    appcontext.updateMapCentrePos(searchPos.coords.latitude, searchPos.coords.longitude, false);
                                }
                            } else {
                                appcontext.log("Map centre cannot be updated");
                            }

                            if (appcontext.mapReady) appcontext.showPOIListOnMapViewGoogleNativeSDK(mapCanvasID, poiList, this.parentAppContext, document.getElementById(mapCanvasID), appcontext.mapOptions.resultBatchID);
                        } else {
                            appcontext.log("Native Maps not available");
                        }
                        return isAvailable;
                    });

                    if (!isAvailable) {
                        this.errorMessage = "Google Maps not installed";
                        return false;
                    }
                }

                if (this.mapOptions.mapAPI == MappingAPI.LEAFLET) {
                    //setup map view, tracking user pos, if not already initialised
                    //TODO: use last search pos as lat/lng, or first item in locationList
                    var centreLat = 50;
                    var centreLng = 50;
                    if (poiList != null && poiList.length > 0) {
                        centreLat = poiList[0].AddressInfo.Latitude;
                        centreLng = poiList[0].AddressInfo.Longitude;
                    }
                    this.initMapLeaflet(mapCanvasID, centreLat, centreLng, false);

                    if (this.mapReady) this.showPOIListOnMapViewLeaflet(mapCanvasID, poiList, this, document.getElementById(mapCanvasID), this.mapOptions.resultBatchID);
                }
            }
            return true;
        }

        setMapType(maptype: string) {
            if (this.mapOptions.mapType == maptype) return;

            this.mapOptions.mapType = maptype;
            this.log("Changing map type:" + maptype);

            if (this.isMapReady()) {
                if (this.mapProvider != null) {
                    this.mapProvider.setMapType(maptype);
                } else {
                    if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                        try {
                            this.map.setMapTypeId(eval("plugin.google.maps.MapTypeId." + maptype));
                        } catch (exception) {
                            this.log("Failed to set map type:" + maptype + " : " + exception.toString());
                        }
                    }
                }
            } else {
                this.log("Map type set, maps not initialised yet.");
            }
        }

        hideMap() {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.log("Debug: Hiding Map");
                if (this.map != null && this.mapReady) {
                    this.map.setVisible(false);
                    this.map.setClickable(false);
                }
            }
        }

        showMap() {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.log("Debug: Showing Map");

                if (this.map != null && this.mapReady) {
                    //show/reposition map

                    this.map.refreshLayout();
                    this.map.setVisible(true);
                    this.map.setClickable(true);
                } else {
                    this.log("Map not available - check API?", LogLevel.ERROR);
                }
            }
        }

        unfocusMap() {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                this.map.setClickable(false);
            }
        }

        focusMap() {
            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE && this.mapReady) {
                this.map.setClickable(true);
            }
        }

        getMapBounds(): Array<GeoLatLng> {
            return this.mapProvider.getMapBounds();
        }
        getMapZoom(): number {
            //TODO: normalize zoom between providers?
            return this.mapProvider.getMapZoom();
        }

        showPOIOnStaticMap(mapcanvasID: string, poi, includeMapLink: boolean = false, isRunningUnderCordova: boolean = false, mapWidth: number = 200, mapHeight: number = 200) {
            var mapCanvas = document.getElementById(mapcanvasID);
            if (mapCanvas != null) {
                var title = poi.AddressInfo.Title;
                var lat = poi.AddressInfo.Latitude;
                var lon = poi.AddressInfo.Longitude;

                if (mapWidth > 640) mapWidth = 640;
                if (mapHeight > 640) mapHeight = 640;
                var width = mapWidth;
                var height = mapHeight;

                var mapImageURL = "http://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon + "&zoom=14&size=" + width + "x" + height + "&maptype=roadmap&markers=color:blue%7Clabel:A%7C" + lat + "," + lon + "&sensor=false";
                var mapHTML = "";
                if (includeMapLink == true) {
                    mapHTML += "<div>" + OCM.Utils.formatMapLink(poi, "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>", isRunningUnderCordova) + "</div>";
                } else {
                    mapHTML += "<div><img width=\"" + width + "\" height=\"" + height + "\" src=\"" + mapImageURL + "\" /></div>";
                }

                mapCanvas.innerHTML = mapHTML;
            }
        }
    }
} 