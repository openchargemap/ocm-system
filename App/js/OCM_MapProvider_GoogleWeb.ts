/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

module OCM {
    /**Map Provider for Google Maps Web API
     * @module OCM.Mapping
     */
    export module MapProviders {
        export class GoogleMapsWeb extends OCM.Base implements IMapProvider {
            mapAPIType: OCM.MappingAPI;
            mapReady: boolean;
            providerError: string;

            private map: any;
            private markerList: Array<google.maps.Marker>;

            /** @constructor */
            constructor() {
                super();
                this.mapAPIType = MappingAPI.GOOGLE_WEB;
                this.mapReady = false;
            }

            /*setMapAPI(api: OCM.MappingAPI) {
                this.mapOptions.mapAPI = api;

                if (this.mapOptions.mapAPI == MappingAPI.GOOGLE_NATIVE) {
                    if (plugin.google.maps) {
                        this.mapAPIReady = true;
                    }
                }
            }*/

            initMap(mapCanvasID, mapConfig: MapOptions) {
                this.log("Mapping Manager: Init Google Web");
                /*if (this.mapsInitialised) {
                    this.log("google web maps: map already initialised");
                    return false;
                }

                if (!this.mapAPIReady) {
                    this.log("init google maps web - API not ready, cannot proceed");
                    return false;
                }*/

                if (this.map == null && google.maps) {
                    var mapCanvas = document.getElementById(mapCanvasID);

                    if (mapCanvas != null) {
                        (<any>google.maps).visualRefresh = true;

                        mapCanvas.style.width = '99.5%';
                        mapCanvas.style.height = $(document).height().toString();

                        //create map
                        var mapOptions = {
                            zoom: 10,
                            minZoom: mapConfig.minZoomLevel,
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
                    }
                }
            }

            showPOIListOnMap(poiList: Array<any>, parentContext: OCM.App) {
                {
                    var map = this.map;
                    var bounds = new google.maps.LatLngBounds();

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

                                    iconURL = "images/icons/map/set2_level" + poiLevel + ".png";
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

                                    var markerTooltip = "OCM-" + poi.ID + ": " + poi.AddressInfo.Title + ":";
                                    if (poi.UsageType != null) markerTooltip += " " + poi.UsageType.Title;
                                    if (poiLevel > 0) markerTooltip += " Level " + poiLevel;
                                    if (poi.StatusType != null) markerTooltip += " " + poi.StatusType.Title;

                                    var newMarker = <any>new google.maps.Marker({
                                        position: new google.maps.LatLng(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                                        map: map,
                                        icon: markerImg != null ? markerImg : iconURL,
                                        shadow: shadow,
                                        title: markerTooltip
                                    });

                                    newMarker.poi = poi;

                                    var anchorElement = document.getElementsByTagName("body");
                                    google.maps.event.addListener(newMarker, 'click', function () {
                                        parentContext.showDetailsView(anchorElement, this.poi);
                                        parentContext.showPage("locationdetails-page", "Location Details");
                                    });

                                    bounds.extend(newMarker.getPosition());
                                    this.markerList.push(newMarker);
                                }
                            }
                        }
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
                }
                google.maps.event.trigger(this.map, 'resize');
            }

            updateMapSize() {
                if (this.map != null) {
                    google.maps.event.trigger(this.map, 'resize');
                }
            }

            setMapCenter = function (lat: number, lng: number) {
                if (this.mapReady) {
                    this.map.setCenter(new google.maps.LatLng(lat, lng));
                }
            };

            refreshMapView(mapCanvasID: string, mapHeight: number, poiList: Array<any>, parentContext: App): boolean {
                document.getElementById(mapCanvasID).style.height = mapHeight + "px";

                if (typeof (google) == "undefined") {
                    //no google maps currently available
                    this.providerError = "Google maps cannot be loaded. Please check your data connection.";
                    return false;
                }

                this.log("Refreshing map using API provider: google web");

                //setup map view if not already initialised
                this.initMap(mapCanvasID, parentContext.mappingManager.mapOptions);

                if (this.mapReady) this.showPOIListOnMap(poiList, parentContext);

                return true;
            }
        }
    }
}