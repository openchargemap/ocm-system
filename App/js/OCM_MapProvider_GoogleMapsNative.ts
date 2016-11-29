/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

module OCM {
    /**Map Provider for Google Maps Web API
     * @module OCM.Mapping
     */
    export module MapProviders {
        export class GoogleMapsNative extends OCM.Base implements IMapProvider {
            mapAPIType: OCM.MappingAPI;
            mapReady: boolean;
            providerError: string;
            mapCanvasID: string;

            private map: any;
            private markerList: collections.Dictionary<number, google.maps.Marker>;
            private mapManipulationCallback: any;

            /** @constructor */
            constructor() {
                super();
                this.mapAPIType = MappingAPI.GOOGLE_NATIVE;
                this.mapReady = false;
                this.markerList = new collections.Dictionary<number, google.maps.Marker>();
                this.mapCanvasID = "map-view";
            }

            /**
            * Performs one-time init of map object for this map provider
            * @param mapcanvasID  dom element for map canvas
            * @param mapConfig  general map config/options
            * @param mapManipulationCallback  custom handler for map zoom/drag events
            */
            initMap(mapCanvasID, mapConfig: MapOptions, mapManipulationCallback: any, parentMapManager: Mapping) {
                this.log("GoogleMapsNative: initMap");
                this.mapCanvasID = mapCanvasID;
                this.mapManipulationCallback = mapManipulationCallback;

                var apiAvailable = true;
                if ((<any>window).plugin && plugin.google && plugin.google.maps) {
                    apiAvailable = true;

                    this.log("Native maps plugin is available.");

                    if (this.map == null) {
                        var mapCanvas = document.getElementById(mapCanvasID);
                        this.map = plugin.google.maps.Map.getMap();

                        var mapManagerContext = this;

                        //setup map manipulation events
                        this.map.addEventListener(plugin.google.maps.event.CAMERA_CHANGE, function () {
                            mapManipulationCallback("drag");
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
                            };

                            mapManagerContext.map.setOptions(mapOptions);
                            mapManagerContext.map.setDiv(mapCanvas);
                            mapManagerContext.map.setVisible(true);
                            mapManagerContext.mapReady = true;
                            parentMapManager.mapReady = true;
                        });
                    }
                } else {
                    this.log("No native maps plugin available.");
                    this.mapReady = false;
                }
            }

            /**
            * Renders the given array of POIs as map markers
            * @param poiList  array of POI objects
            * @param parentContext  parent app context
            */
            showPOIListOnMap(poiList: Array<any>, parentContext: OCM.App) {
                var clearMarkersOnRefresh: boolean = false;
                var map = this.map;

                map.setVisible(true);
                map.clear();

                var bounds = new plugin.google.maps.LatLngBounds();
                var markersAdded = 0;

                //clear existing markers (if enabled)
                if (clearMarkersOnRefresh) {
                    if (this.markerList != null) {
                        for (var i = 0; i < this.markerList.size(); i++) {
                            if (this.markerList[i]) {
                                this.markerList[i].setMap(null);
                            }
                        }
                    }
                    this.markerList = new collections.Dictionary<number, any>();
                }

                if (poiList != null) {
                    //render poi markers
                    var poiCount = poiList.length;
                    for (var i = 0; i < poiList.length; i++) {
                        if (poiList[i].AddressInfo != null) {
                            if (poiList[i].AddressInfo.Latitude != null && poiList[i].AddressInfo.Longitude != null) {
                                var poi = poiList[i];

                                var addMarker = true;
                                if (!clearMarkersOnRefresh && this.markerList != null) {
                                    //find if this poi already exists in the marker list
                                    if (this.markerList.containsKey(poi.ID)) {
                                        addMarker = false;
                                    }
                                }

                                if (addMarker) {
                                    var poiLevel = OCM.Utils.getMaxLevelOfPOI(poi);

                                    var iconURL = null;
                                    var animation = null;
                                    var shadow = null;
                                    var markerImg = null;

                                    iconURL = "images/icons/map/set4_level" + poiLevel;
                                    if (poi.UsageType != null && poi.UsageType.Title.indexOf("Private") > -1) {
                                        iconURL += "_private";
                                    }

                                    iconURL += ".png";

                                    var markerTooltip = "OCM-" + poi.ID + ": " + poi.AddressInfo.Title + ":";
                                    if (poi.UsageType != null) markerTooltip += " " + poi.UsageType.Title;
                                    if (poiLevel > 0) markerTooltip += " Level " + poiLevel;
                                    if (poi.StatusType != null) markerTooltip += " " + poi.StatusType.Title;

                                    /*
                                    markerImg = new google.maps.MarkerImage(
                                        iconURL,
                                        new google.maps.Size(68, 100.0),
                                        null,
                                        new google.maps.Point(15, 45),
                                        new google.maps.Size(34, 50)
                                    );

                                    var newMarker = <any>new google.maps.Marker({
                                        position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                                        map: map,
                                        icon: markerImg != null ? markerImg : iconURL,
                                        title: markerTooltip
                                    });

                                    newMarker.poi = poi;

                                    var anchorElement = document.getElementById("body");
                                    google.maps.event.addListener(newMarker, 'click', function () {
                                        //TODO: move to parent callback, probably based on poi.ID
                                        parentContext.showDetailsView(anchorElement, this.poi);
                                        //parentContext.showPage("locationdetails-page", "Location Details");
                                    });
                                    */

                                    var markerPos = new plugin.google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude);

                                    var newMarker = map.addMarker({
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

                                    this.markerList.setValue(poi.ID, newMarker);
                                    markersAdded++;
                                }
                            }
                        }
                    }

                    this.log(markersAdded + " new map markers added out of a total " + this.markerList.size());
                }

                var uiContext = this;
                //zoom to bounds of markers
                if (poiList != null && poiList.length > 0) {
                    if (!parentContext.appConfig.enableLiveMapQuerying) {
                        this.log("Fitting to marker bounds:" + bounds);
                        map.setCenter(bounds.getCenter());
                        this.log("zoom before fit bounds:" + map.getZoom());

                        map.fitBounds(bounds);

                        //fix incorrect zoom level when fitBounds guesses a zooom level of 0 etc.
                        var zoom = map.getZoom();
                        map.setZoom(zoom < 6 ? 6 : zoom);
                    } else {
                        if (map.getCenter() == undefined) {
                            map.setCenter(bounds.getCenter());
                        }
                    }
                }

                // this.log("Moving camera to map centre:" + this.mapOptions.mapCentre);

                //move camera insteaad of animating
                /*setTimeout(function () {
                    map.moveCamera({
                        'target': gmMapCentre,
                        'tilt': 60,
                        'zoom': 12,
                        'bearing': 0
                    });
                }, 500);*/

                this.refreshMapLayout();
            }

            refreshMapLayout() {
                if (this.map != null) {
                    this.map.refreshLayout();
                }
            }

            setMapCenter(pos: GeoPosition) {
                if (this.mapReady) {
                    this.map.setCenter(new plugin.google.maps.LatLng(pos.coords.latitude, pos.coords.longitude));
                }
            }

            getMapCenter(): GeoPosition {
                var pos = this.map.getCenter();
                return new GeoPosition(pos.lat(), pos.lng());
            }

            setMapZoom(zoomLevel: number) {
                this.map.setZoom(zoomLevel);
            }

            getMapZoom(): number {
                return this.map.getZoom();
            }

            setMapType(mapType: string) {
                try {
                    this.map.setMapTypeId(eval("google.maps.MapTypeId." + mapType));
                } catch (exception) {
                    this.log("Failed to set map type:" + mapType + " : " + exception.toString());
                }
            }

            getMapBounds(): Array<GeoLatLng> {
                var bounds = new Array<GeoLatLng>();

                var mapBounds = this.map.getBounds();
                bounds.push(new GeoLatLng(mapBounds.getNorthEast().lat(), mapBounds.getNorthEast().lng()));
                bounds.push(new GeoLatLng(mapBounds.getSouthWest().lat(), mapBounds.getSouthWest().lng()));

                return bounds;
            }

            renderMap(poiList: Array<any>, mapHeight: number, parentContext: App): boolean {
                if (!this.mapReady) {
                    this.log("renderMap: skipping render, map not ready yet");
                }

                if (this.map == null) this.log("Native map not initialised");
                if (this.mapCanvasID == null) this.log("mapcanvasid not set!!");
                document.getElementById(this.mapCanvasID).style.height = mapHeight + "px";

                var mapManagerContext = this;
                var isAvailable = plugin.google.maps.Map.isAvailable(function (isAvailable, message) {
                    if (isAvailable) {
                        //setup map view if not already initialised
                        //mapManagerContext.initMap(this.mapCanvasID, parentContext.mappingManager.mapOptions, this.mapManipulationCallback, parentContext.mappingManager);

                        /*if (parentContext != null && parentContext.viewModel.searchPosition != null) {
                            var searchPos = parentContext.viewModel.searchPosition;
                            if (searchPos != null) {
                                mapManagerContext.(searchPos.coords.latitude, searchPos.coords.longitude, false);
                            }
                        } else {
                            mapManagerContext.log("Map centre cannot be updated");
                        }*/

                        mapManagerContext.showPOIListOnMap(poiList, parentContext);
                    } else {
                        mapManagerContext.log("Native Maps not available");
                    }
                    return isAvailable;
                });

                return true;
            }
        }
    }
}