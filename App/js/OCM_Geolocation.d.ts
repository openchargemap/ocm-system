/// <reference path="TypeScriptReferences/googlemaps/google.maps.d.ts" />
/// <reference path="OCM_Data.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
declare module OCM {
    class Geolocation {
        constructor(api: API);
        public clientGeolocationPos: MapCoords;
        private geocodingTextInput;
        private geocodingResultPos;
        private api;
        public determineUserLocation(successCallback: any, failureCallback: any): void;
        public determineUserLocationFailed(failureCallback: any): void;
        public determineGeocodedLocation(locationText: any, successCallback: any): boolean;
        public determineGeocodedLocationCompleted(pos: any, successCallback: any, failureCallback: any): void;
        public getBearing(lat1: any, lon1: any, lat2: any, lon2: any): number;
        public getCardinalDirectionFromBearing(bearing: any): string;
        public getDrivingDistanceBetweenPoints(startLat: any, startLng: any, endLat: any, endLng: any, distanceUnit: any, completedCallback: any): void;
    }
}
