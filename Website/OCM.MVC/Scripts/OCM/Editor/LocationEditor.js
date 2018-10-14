//Note: this class is built using TypeScript, only the .ts file should be edited
//TODO: shadow marker for original marker pos, show duplicates nearby
var LocationEditor = /** @class */ (function () {
    function LocationEditor(startLat, startLng, latControlId, lngControlId) {
        this.map = null;
        this.marker = null;
        this.poiPos = null;
        this.addressResult = null;
        this.geocodeRequested = false;
        this.startLat = startLat;
        this.startLng = startLng;
        this.latControlId = latControlId;
        this.lngControlId = lngControlId;
    }
    LocationEditor.prototype.initializeMap = function () {
        // Enable the visual refresh
        google.maps.visualRefresh = true;
        this.poiPos = new google.maps.LatLng(this.startLat, this.startLng);
        var mapOptions = {
            zoom: 16,
            center: this.poiPos,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            panControl: true,
            zoomControl: true,
            scaleControl: true
        };
        this.map = new google.maps.Map(document.getElementById("map-canvas"), mapOptions);
        this.addMapMarker();
        if (this.marker != null) {
            //begin lookup of nearest address if we have a position
            this.beginReverseGeocode();
        }
        //google map event setup
        var appContext = this;
        google.maps.event.addListenerOnce(this.map, "idle", function () {
            appContext.refreshMap();
        });
        google.maps.event.addListener(this.map, "drag", function () {
            //reset pos of marker to current map centre
            appContext.setNewPOIPos(appContext.map.getCenter(), false);
        });
        google.maps.event.addListener(this.map, "dragend", function () {
            //reset pos of marker to current map centre, including reverse geocode of final position
            appContext.setNewPOIPos(appContext.map.getCenter(), true);
        });
    };
    LocationEditor.prototype.addMapMarker = function () {
        if (!(this.poiPos.lat() === 0 && this.poiPos.lng() === 0)) {
            this.marker = new google.maps.Marker({
                map: this.map,
                draggable: false,
                animation: google.maps.Animation.DROP,
                position: this.poiPos,
                title: "Equipment Location"
            });
            //google.maps.event.addListener(this.marker, 'drag', this.setNewPOIPos);
        }
        else {
            //centre map on a default position
            this.map.setCenter(new google.maps.LatLng(51.6256067484225, -0.505837798118591));
        }
    };
    LocationEditor.prototype.setPosFromGeolocation = function (geoPos) {
        var posLatLng = new google.maps.LatLng(geoPos.coords.latitude, geoPos.coords.longitude);
        this.setNewPOIPos(posLatLng, true);
        this.refreshMap();
        //add the marker if we haven't already
        if (this.marker == null) {
            this.addMapMarker();
            this.beginReverseGeocode();
        }
    };
    LocationEditor.prototype.setNewPOIPos = function (newPos, performReverseGeocode) {
        this.poiPos = newPos;
        //update lat/lng in ui
        $("#" + this.latControlId).val(newPos.lat());
        $("#" + this.lngControlId).val(newPos.lng());
        //move marker to new pos
        if (this.marker != null) {
            this.marker.setPosition(newPos);
            //geocode address
            if (performReverseGeocode === true) {
                this.beginReverseGeocode();
            }
        }
        if (performReverseGeocode) {
            this.getNearbyPOI();
        }
    };
    LocationEditor.prototype.beginReverseGeocode = function () {
        var appContext = this;
        setTimeout(function () {
            appContext.reverseGeocodePosition(appContext.poiPos, $.proxy(appContext.showAddressSuggestions, appContext));
        }, 500);
    };
    LocationEditor.prototype.refreshMap = function () {
        this.logMessage("Refreshing map..");
        var appContext = this;
        setTimeout(function () {
            if (appContext.map && google && google.maps) {
                //TODO: support google or osm
                google.maps.event.trigger(appContext.map, "resize");
                appContext.map.setCenter(appContext.poiPos);
            }
        }, 1500);
    };
    LocationEditor.prototype.getUserLocation = function () {
        var appContext = this;
        if (navigator.geolocation) {
            var completedCallback = $.proxy(appContext.setPosFromGeolocation, appContext);
            //TODO: proxy
            navigator.geolocation.getCurrentPosition(completedCallback);
        }
        else {
            console.log("Geolocation is not supported by this browser.");
        }
    };
    LocationEditor.prototype.showAddressSuggestions = function () {
        if (this.addressResult != null) {
            $("#copyAddress").show();
            //Google result
            if (this.addressResult.address_components) {
                var addressComponents = this.addressResult.address_components;
                $("#nearest-address").html(this.addressResult.formatted_address);
                //find address component in result with type 'postal_code'
                /*for (var i = addressComponents.length - 1; i >= 0; i--) {
                    for (var t = 0; t < addressComponents[i].types.length; t++) {
                      if (addressComponents[i].types[t] === "postal_code") {
                        break;
                        }
                    }
                }
                */
            }
            //OSM Nomanitim results
            if (this.addressResult.address) {
                $("#nearest-address").html(this.addressResult.display_name);
            }
        }
    };
    LocationEditor.prototype.reverseGeocodePosition_OSM = function (pos, completedCallback) {
        var lat = pos.lat();
        var lng = pos.lng();
        var appContext = this;
        $.getJSON("https://nominatim.openstreetmap.org/reverse?format=json&lat=" + lat + "&lon=" + lng + "&zoom=18&addressdetails=1", function (data) {
            if (data != null) {
                appContext.addressResult = data;
            }
            else {
                appContext.addressResult = null;
            }
            completedCallback();
        });
    };
    LocationEditor.prototype.reverseGeocodePosition = function (pos, completedCallback) {
        var serviceprovider = "osm";
        if (serviceprovider === "osm") {
            this.reverseGeocodePosition_OSM(pos, completedCallback);
        }
        if (serviceprovider === "google") {
            var geocoder = new google.maps.Geocoder();
            var appContext = this;
            appContext.geocodeRequested = true;
            geocoder.geocode({ "latLng": pos }, function (results, status) {
                if (status === google.maps.GeocoderStatus.OK) {
                    if (results[1]) {
                        //got an address for this location
                        var addressResult = results[0];
                        appContext.addressResult = addressResult;
                        /*$("#full-address").html(addressResult.formatted_address);

                        //find address component in result with type 'postal_code'
                        for (var i = addressComponents.length - 1; i >= 0; i--) {
                            for (var t = 0; t < addressComponents[i].types.length; t++) {
                                if (addressComponents[i].types[t] == "postal_code") {
                                    break;
                                }
                            }
                        }*/
                    }
                    else {
                        //no result
                        appContext.addressResult = null;
                    }
                }
                appContext.geocodeRequested = false;
                completedCallback();
            });
        }
    };
    LocationEditor.prototype.useSuggestedAddress = function () {
        if (this.addressResult != null) {
            //osm
            if (this.addressResult.address) {
                var address = this.addressResult.address;
                //$("#nearest-address").html(this.addressResult.display_name);
                $("#AddressInfo_AddressLine1").val(address.road);
                if (!address.road && address.bridleway) {
                    $("#AddressInfo_AddressLine1").val(address.bridleway);
                }
                if ($("#AddressInfo_AddressLine1").val() !== "") {
                    //use address line 1 as title if not already set
                    $("#AddressInfo_Title").val($("#AddressInfo_AddressLine1").val());
                }
                $("#AddressInfo_AddressLine2").val(address.suburb);
                if (!address.suburb && address.hamlet) {
                    $("#AddressInfo_AddressLine2").val(address.hamlet);
                }
                $("#AddressInfo_Town").val(address.city);
                if (!address.city && address.county) {
                    $("#AddressInfo_Town").val(address.county);
                }
                if (!address.city && address.village) {
                    $("#AddressInfo_Town").val(address.village);
                }
                $("#AddressInfo_StateOrProvince").val(address.state);
                if (address.county) {
                    $("#AddressInfo_StateOrProvince").val(address.county);
                }
                $("#AddressInfo_Postcode").val(address.postcode);
                //country selection using country name instead of ID
                var optVal = $("#AddressInfo_Country_ID option:contains('" + address.country + "')").attr("value");
                $("#AddressInfo_Country_ID").val(optVal);
            }
        }
    };
    LocationEditor.prototype.lookupAddressPosition = function () {
        //get address from ui and lookup latlon geocode
        var address = "";
        address += this.getFormTextValue("AddressInfo_AddressLine1");
        address += this.getFormTextValue("AddressInfo_AddressLine2");
        address += this.getFormTextValue("AddressInfo_Town");
        address += this.getFormTextValue("AddressInfo_StateOrProvince");
        address += this.getFormTextValue("AddressInfo_Postcode");
        address += $("#AddressInfo_Country_ID option:selected").text();
        console.log("Finding lat/lon of address.." + address);
        this.beginGeocodingFromPlacename(address);
    };
    LocationEditor.prototype.beginGeocodingFromPlacename = function (address) {
        var appContext = this;
        var dataAPI = new OCM.API();
        var geocoding = new OCM.Geolocation(dataAPI);
        geocoding.determineGeocodedLocation(address, $.proxy(appContext.setPosFromGeolocation, appContext));
    };
    LocationEditor.prototype.getFormTextValue = function (elementId) {
        var val = $("#" + elementId).val();
        if (val.length > 0) {
            val += ", ";
        }
        return val;
    };
    LocationEditor.prototype.getNearbyPOI = function () {
        console.log("Fetching nearby POI list");
        if (this.poiPos != null) {
            var ocm_api = new OCM.API();
            var params = new OCM.POI_SearchParams();
            params.latitude = this.poiPos.lat();
            params.longitude = this.poiPos.lng();
            params.distance = 5;
            params.distanceUnit = "Miles";
            params.maxResults = 5;
            params.includeComments = true;
            params.enableCaching = false;
            ocm_api.fetchLocationDataListByParam(params, "locationEditor.renderNearbyPOI", null);
        }
    };
    LocationEditor.prototype.renderNearbyPOI = function (poiList) {
        var output = "<h4>Charging Locations Nearby</h4>";
        if (poiList.length > 0) {
            output += "<p class='alert alert-danger'>The following locations already exist nearby. Please ensure you are not adding a duplicate. You can edit any of these listings if required instead:</p>";
        }
        else {
            output += "<p class='alert alert-info'>There are no locations listed nearby.</p>";
        }
        for (var i = 0; i < poiList.length; i++) {
            var poi = poiList[i];
            var url = "https://openchargemap.org/site/poi/details/" + poi.ID;
            if (poi.ID === poiId) {
                output += "<li>OCM-" + poi.ID + " : " + poi.AddressInfo.Title + " <span class='label label-info'>Being Edited</span></li>";
            }
            else {
                output += "<li><a target='_blank' href=\"" + url + "\">OCM-" + poi.ID + " : " + poi.AddressInfo.Title + "</a> (" + (Math.round(poi.AddressInfo.Distance * 10) / 10) + " Miles)</li>";
            }
        }
        output += "</ul>";
        $("#nearbypoi").html(output);
    };
    LocationEditor.prototype.logMessage = function (msg) {
        if (console) {
            console.log(msg);
        }
    };
    LocationEditor.prototype.positionMarkerAtTextLocation = function (location) {
        if (location.length > 4) {
            this.beginGeocodingFromPlacename(location);
        }
    };
    return LocationEditor;
}());
//# sourceMappingURL=LocationEditor.js.map