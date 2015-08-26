/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
function initGoogleMapsCompleted() {
    if (ocm_app.mappingManager.mapOptions.mapAPI == OCM.MappingAPI.GOOGLE_WEB) {
        ocm_app.mappingManager.mapAPIReady = true;
    }
    if (console)
        console.log("Google Maps Web API Loaded.");
}
;
function loadGoogleMaps() {
    //load google maps script async, if google API is selected
    if (ocm_app.mappingManager.mapOptions.mapAPI != OCM.MappingAPI.GOOGLE_WEB) {
        if (console)
            console.log("Google Maps Web API not selected. Loading API anyway.");
    }
    if (console)
        console.log("Starting load of Google Maps Web API");
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = 'https://maps.googleapis.com/maps/api/js?v=3&sensor=false&callback=initGoogleMapsCompleted';
    document.body.appendChild(script);
}
//if we are not running under cordova then we use Google Maps Web API, otherwise we still use API for distance etc
window.onload = loadGoogleMaps;
//# sourceMappingURL=OCM_MapInit.js.map