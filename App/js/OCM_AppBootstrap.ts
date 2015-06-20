/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />

//declare var OCM_App: any;
declare var StatusBar: any;
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

interface JQueryStatic {
    mobile: any;
}

interface Window {
    L: any;
    cordova: any;
    device: any;
    analytics: any;
}

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

    if (device) {

        //if running under iOS 7or higher , add iOS specific css adjustments
        var iOS7 =
            device.platform
            && device.platform.toLowerCase() == "ios"
            && parseFloat(device.version) >= 7.0;

        if (iOS7) {
            $("body").addClass("iOS");
        }
    }

    try {
        if (navigator.connection) {
            if (navigator.connection.type == Connection.NONE) {
                ocm_app.log("OCM: No Network status: " + navigator.connection.type.toString());
                var networkErrorElement = document.getElementById("network-error");
                if (networkErrorElement) {
                    networkErrorElement.style.display = "block";
                }
            }
        } else {
            ocm_app.log("Cordova connection status not available");
        }
    } catch (err) {
        ocm_app.log("OCM: error checkin for connection status");
    }

    //if (StatusBar) {
    //StatusBar.overlaysWebView(false);
    //}

    /*
    //phonegap analytics plugin
    */
    window.analytics.startTrackerWithId('UA-76936-12');

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

        //apply safari specific styling adjustments
        var isSafari = navigator.vendor.indexOf("Apple")==0 && /\sSafari\//.test(navigator.userAgent); // true or false
        if (isSafari) {
            ocm_app.log("Adjusting for Safari browser.");
             $("body").addClass("safari");
        }
        
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