/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />


//Set iOS App Splashscreen depending on device size
//var filename = navigator.platform === 'iPad' ? 'images/splashscreen/AppSplashscreen_768x1004.png' : 'images/splashscreen/AppSplashscreen_320x460.png';
//document.write('<link rel="apple-touch-startup-image" href="' + filename + '" />');
//Perform App Init
var ocm_app = new OCM.App();
var _appBootStrapped = false;
var gaPlugin;

function startApp() {
    'use strict';
    if (_appBootStrapped == false) {
        _appBootStrapped = true;
        ocm_app.log("Starting app....");
        ocm_app.initApp();
    }
}

function onDeviceReady() {
    'use strict';
    ocm_app.appState.isRunningUnderCordova = true;
    ocm_app.log("OCM: Cordova Loaded, onDeviceReady Fired.");

    try  {
        if (navigator.connection.type == Connection.NONE) {
            ocm_app.log("OCM: No Network status: " + navigator.connection.type.toString());
            document.getElementById("network-error").style.display = "block";
        }
    } catch (err) {
        ocm_app.log("OCM: error checkin for connection status");
    }

    if (StatusBar) {
        StatusBar.overlaysWebView(false);
    }

    /*
    //phonegap analytics plugin
    
    gaPlugin = window.plugins.gaPlugin;
    gaPlugin.init(successHandler, errorHandler, "UA-76936-12", 10);
    */
    if (window.L) {
        ocm_app.log("Leaflet ready");
    } else {
        ocm_app.log("Leaflet not ready");
    }

    startApp();
}

//startup for non-jquerymobile version
$(function () {
    if (_appBootStrapped == false) {
        ocm_app.log("There can be only one.");

        if (window.cordova) {
            ocm_app.appState.isRunningUnderCordova = true;
            ocm_app.log("Phonegap enabled. Operating as mobile app.");
            document.addEventListener("deviceready", onDeviceReady, false);
        } else {
            ocm_app.log("Phonegap not enabled. Operating as desktop browser.");
            startApp();
        }
    } else {
        ocm_app.log("Ignoring additional bootstrap call.");
    }
});
//# sourceMappingURL=OCM_AppBootstrap.js.map
