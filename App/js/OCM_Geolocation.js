//"use strict";

function OCM_Geolocation() {
    //result for latest client gelocation attempt
    this.clientGeolocationPos = null;

    //input/results for latest text geocoding attempt
    this.geocodingTextInput = null;
    this.geocodingResultPos = null;
    this.ocm_data = null;
}

OCM_Geolocation.prototype.determineUserLocation = function (successCallback, failureCallback) {

    var appContext = this;

    //determine user location automatically if enabled & supported
    if (navigator.geolocation) {

        navigator.geolocation.getCurrentPosition(
	        function (position) {
	            this.clientGeolocationPos = position;
	            successCallback(this.clientGeolocationPos);
	        },
	        function () {
	            // could not geolocate
	            appContext.determineUserLocationFailed(failureCallback);
	        }
	    );

    } else {
        appContext.determineUserLocationFailed(failureCallback);
    }
};

OCM_Geolocation.prototype.determineUserLocationFailed = function (failureCallback) {
    //failed
    failureCallback();
};

OCM_Geolocation.prototype.determineGeocodedLocation = function (locationText, successCallback) {

    //caller is searching for same (previously geocoded) text again, return last result
    if (locationText == this.geocodingTextInput) {
        if (this.geocodingResultPos != null) {
            successCallback(this.geocodingResultPos);
            return false;
        }
    }

    this.geocodingTextInput = locationText;
    this.geocodingResultPos = null;

    var geocoder = this.ocm_data;
    var appContext = this;

    geocoder.fetchGeocodeResult(locationText, 
        function (results) {
            var locationPos = {
                'lat': results.latitude,
                'lng': results.longitude
            };
            appContext.determineGeocodedLocationCompleted(locationPos, successCallback);
        }
        , null);

    /*
    geocoder.geocode({ 'address': locationText }, function (results, status) {
        if (status == google.maps.GeocoderStatus.OK) {
            var locationPos = results[0].geometry.location;
           
            appContext.determineGeocodedLocationCompleted(locationPos, successCallback);
        } else {
            alert("Sorry, we could not identify this location: " + status);
        }
    });
    */
    return true;
};

OCM_Geolocation.prototype.determineGeocodedLocationCompleted = function (pos, successCallback) {

    //cinvert google lt/lng result into browser coords
    var geoPosition = {
        coords: {
            latitude: pos.lat,
            longitude: pos.lng
        }
    };

    this.geocodingResultPos = geoPosition;
    successCallback(geoPosition);
};

OCM_Geolocation.prototype.getBearing = function (lat1, lon1, lat2, lon2) {
    //from http://stackoverflow.com/questions/1971585/mapping-math-and-javascript

    //convert degrees to radians
    lat1 = lat1 * Math.PI / 180;
    lat2 = lat2 * Math.PI / 180;
    var dLon = (lon2 - lon1) * Math.PI / 180;
    var y = Math.sin(dLon) * Math.cos(lat2);
    var x = Math.cos(lat1) * Math.sin(lat2) - Math.sin(lat1) * Math.cos(lat2) * Math.cos(dLon);

    var bearing = Math.atan2(y, x) * 180 / Math.PI;
    if (bearing < 0) {
        bearing = bearing + 360;
    }

    bearing = Math.floor(bearing);
    return bearing;
};

OCM_Geolocation.prototype.getCardinalDirectionFromBearing = function (bearing) {
    //partly inspired by http://bryan.reynoldslive.com/post/Latitude2c-Longitude2c-Bearing2c-Cardinal-Direction2c-Distance2c-and-C.aspx

    if (bearing >= 0 && bearing <= 22.5) return "N";
    if (bearing >= 22.5 && bearing <= 67.5) return "NE";
    if (bearing >= 67.5 && bearing <= 112.5) return "E";
    if (bearing >= 112.5 && bearing <= 157.5) return "SE";
    if (bearing >= 157.5 && bearing <= 202.5) return "S";
    if (bearing >= 202.5 && bearing <= 247.5) return "SW";
    if (bearing >= 247.5 && bearing <= 292.5) return "W";
    if (bearing >= 292.5 && bearing <= 337.5) return "NW";
    if (bearing >= 337.5 && bearing <= 360.1) return "N";

    return "?";
};

OCM_Geolocation.prototype.getDrivingDistanceBetweenPoints = function (startLat, startLng, endLat, endLng, distanceUnit, completedCallback) {
    var unitSystem = google.maps.UnitSystem.IMPERIAL;
    if (distanceUnit == "KM") unitSystem = google.maps.UnitSystem.METRIC;

    var startPos = new google.maps.LatLng(startLat, startLng);
    var endPos = new google.maps.LatLng(endLat, endLng);

    var service = new google.maps.DistanceMatrixService();
    service.getDistanceMatrix(
      {
          origins: [startPos],
          destinations: [endPos],
          travelMode: google.maps.TravelMode.DRIVING,
          unitSystem: unitSystem
      }, completedCallback);

}
