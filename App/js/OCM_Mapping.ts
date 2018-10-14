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

        initMap(mapCanvasID: string, mapConfig: MapOptions, mapManipulationCallback: any, mapManagerContext: Mapping);
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
        private debouncedMapPositionUpdate: any;

        /** @constructor */
        constructor() {
            super();

            this.mapOptions = new MapOptions();

            this.mapAPIReady = false;
            this.mapsInitialised = false;
            this.mapReady = false;

            this.setMapAPI(this.mapOptions.mapAPI);

            var mapManagerContext = this;
            this.debouncedMapPositionUpdate = OCM.Utils.debounce(function () {
                mapManagerContext.log("signaling map position change:");
                if (mapManagerContext.mapProvider.mapReady) {
                    //create new latlng from map centre so that values get normalised to 180/-180

                    var centerPos: GeoPosition = mapManagerContext.mapProvider.getMapCenter();
                    mapManagerContext.log("Map centre/zoom changed, updating search position:" + centerPos);
                    mapManagerContext.updateMapCentrePos(centerPos.coords.latitude, centerPos.coords.longitude, false);
                }
            }, 300, false);
        }

        setParentAppContext(context: any) {
            this.parentAppContext = context;
        }

        setMapAPI(api: OCM.MappingAPI) {
            this.mapOptions.mapAPI = api;

            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                this.mapProvider = new OCM.MapProviders.GoogleMapsNative();
            }

            if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_WEB) {
                this.mapProvider = new OCM.MapProviders.GoogleMapsWeb();
            }
        }

        isMapReady() {
            if (this.mapProvider != null) {
                return this.mapProvider.mapReady;
            } else {
                return false;
            }
        }

        externalAPILoaded(mapAPI: OCM.MappingAPI) {
            this.mapAPIReady = true;
            this.log("Mapping API Loaded: " + OCM.MappingAPI[mapAPI]);
        }

        /**
         * Performs one-time init of map object, detects cordova and chooses map api as appropriate
         * @param mapcanvasID  dom element for map canvas
         */
        initMap(mapcanvasID: string) {
            if (this.mapProvider != null) {
                if (this.mapsInitialised) {
                    this.log("initMap: Map provider already initialised");
                }

                this.log("Mapping Manager: Init " + OCM.MappingAPI[this.mapProvider.mapAPIType]);

                this.mapProvider.initMap(mapcanvasID, this.mapOptions, $.proxy(this.mapManipulationPerformed, this), this);
            } else {
                if (this.mapsInitialised) {
                    this.log("initMap: map already initialised");
                    return;
                } else {
                    this.log("initMap: " + this.mapOptions.mapAPI)
                }
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

        /**
        * Used by map provider as callback when a zoom or pan/drag has been performed
        * @param manipulationType  string name for event "zoom", "drag" etc
        */
        mapManipulationPerformed(manipulationType: string) {
            this.log("map manipulated:" + manipulationType);
            var mapManagerContext = this;
            if (manipulationType == "drag" || manipulationType == "zoom") {
                //after the center of the map centre has stopped changing, update search centre pos
                this.debouncedMapPositionUpdate();
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

                                    var marker = <any>new (<any>L).Marker(new (<any>L).LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude), { icon: markerIcon, title: markerTitle, draggable: false, clickable: true });
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

        createMapLeaflet(mapcanvasID, currentLat, currentLng, locateUser, zoomLevel) {
            // create a map in the "map" div, set the view to a given place and zoom
            var map = new (<any>L).Map(mapcanvasID);
            if (currentLat != null && currentLng != null) {
                map.setView(new (<any>L).LatLng(currentLat, currentLng), zoomLevel, true);
            }
            map.setZoom(zoomLevel);

            if (locateUser == true) {
                map.locate({ setView: true, watch: true, enableHighAccuracy: true });
            } else {
                //use a default view
            }

            // add an OpenStreetMap tile layer
            new (<any>L).TileLayer('http://{s}.tile.osm.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://osm.org/copyright">OpenStreetMap</a> contributors'
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
            }
        }

        updateMapCentrePos(lat: number, lng: number, moveMap: boolean) {
            //update record of map centre so search results can optionally be refreshed
            if (moveMap) {
                if (this.mapProvider != null) {
                    this.mapProvider.setMapCenter(new GeoPosition(lat, lng));
                }
            }

            this.mapOptions.mapCentre = new GeoPosition(lat, lng);
        };

        refreshMapView(mapHeight: number, poiList: Array<any>, searchPos: any): boolean {
            if (this.mapProvider != null) {
                this.log("Mapping Manager: renderMap " + OCM.MappingAPI[this.mapProvider.mapAPIType]);

                if (this.isMapReady()) {
                    this.mapProvider.renderMap(poiList, mapHeight, this.parentAppContext);
                } else {
                    this.log("refreshMapView: map provider not initialised..");
                }
            } else {
                this.log("Unsupported Map API: refreshMapView", LogLevel.ERROR);
                /*

                document.getElementById(mapCanvasID).style.height = mapHeight + "px";

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
                */
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

                var mapImageURL = "https://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon + "&zoom=14&size=" + width + "x" + height + "&maptype=roadmap&markers=color:blue%7Clabel:A%7C" + lat + "," + lon + "&sensor=false";
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