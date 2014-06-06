/// <reference path="OCM_Base.d.ts" />
/// <reference path="OCM_CommonUI.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
declare module OCM {
    class GeoLatLng {
        public latitude: number;
        public longitude: number;
    }
    class MapCoords {
        public coords: GeoLatLng;
    }
    class MapOptions {
        public enableClustering: boolean;
        public resultBatchID: number;
        public useMarkerIcons: boolean;
        public useMarkerAnimation: boolean;
        public enableTrackingMapCentre: boolean;
        public mapCentre: MapCoords;
        public iconSet: string;
        public mapAPI: string;
        /** @constructor */
        constructor();
    }
    /** Mapping - provides a way to render to various mapping APIs
    * @module OCM.Mapping
    */
    class Mapping extends Base {
        public map: any;
        public mapsInitialised: boolean;
        public mapAPIReady: boolean;
        public mapOptions: MapOptions;
        public markerClusterer: any;
        public markerList: any[];
        public searchMarker: any;
        private commonUI;
        private enableLogging;
        public errorMessage: string;
        public parentAppContext: any;
        /** @constructor */
        constructor(commonUI: any);
        public setParentAppContext(context: any): void;
        public setMapAPI(api: string): void;
        public initMapGoogleNativeSDK(mapcanvasID: string): boolean;
        public showPOIListOnMapViewGoogleNativeSDK(mapcanvasID: any, poiList: any, appcontext: any, anchorElement: any, resultBatchID: any): void;
        public updateMapCentrePos: (lat: number, lng: number, moveMap: boolean) => void;
        public initMapGoogle(mapcanvasID: any): boolean;
        public showPOIListOnMapViewGoogle(mapcanvasID: any, poiList: any, appcontext: any, anchorElement: any, resultBatchID: any): void;
        public initMapLeaflet(mapcanvasID: any, currentLat: any, currentLng: any, locateUser: any): void;
        public showPOIListOnMapViewLeaflet(mapcanvasID: any, poiList: any, appcontext: any, anchorElement: any, resultBatchID: any): void;
        public createMapLeaflet: (mapcanvasID: any, currentLat: any, currentLng: any, locateUser: any, zoomLevel: any) => L.Map;
        public refreshMapView(mapCanvasID: string, mapHeight: number, poiList: any[], searchPos: any): boolean;
        public hideMap(): void;
        public showMap(): void;
    }
}
