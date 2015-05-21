/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

function initGoogleMapsCompleted() {
    ocm_app.mappingManager.mapAPIReady = true;
    if (console) console.log("Google Maps Web API Loaded:" + ocm_app.mappingManager.mapAPIReady);
};

function loadGoogleMapsScript() {
    //load google maps script async, if google API is selected
    if (ocm_app.mappingManager.mapOptions.mapAPI != OCM.MappingAPI.GOOGLE_WEB) {
        if (console) console.log("Google Maps Web API not selected.");
        setTimeout(
            function () {
                if (!ocm_app.mappingManager.mapAPIReady) ocm_app.mappingManager.mapAPIReady = true;
            }, 1000);

        return false;
    }

    if (console) console.log("Starting load of Google Maps Web API");

    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'https://maps.googleapis.com/maps/api/js?v=3&sensor=false&callback=initGoogleMapsCompleted';
    document.body.appendChild(script);

    /*var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'js/ThirdParty/google.maps/markerclusterer_compiled.js';
    document.body.appendChild(script);*/

    /*var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'js/ThirdParty/google.maps/markerwithlabel_packed.js';
    document.body.appendChild(script);*/
}

if (!window.cordova) {
    //if we are not running under cordova then we use Google Maps Web API
    window.onload = loadGoogleMapsScript;
}