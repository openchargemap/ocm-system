
var ocm_app = new OCM_App();
var _appBootStrapped = false;
var gaPlugin;

function startApp() {
    'use strict';
    if (_appBootStrapped == false) {
        _appBootStrapped = true;
        ocm_app.logEvent("Starting app....");
        ocm_app.initApp();
    }
}

function onDeviceReady() {
    'use strict';
    ocm_app.isRunningUnderCordova = true;
    ocm_app.logEvent("OCM: Cordova Loaded, onDeviceReady Fired.");

    try  {
        if (navigator.connection.type == Connection.NONE) {
            ocm_app.logEvent("OCM: No Network status: " + navigator.connection.type.toString());
            document.getElementById("network-error").style.display = "block";
        }
    } catch (err) {
        ocm_app.logEvent("OCM: error checkin for connection status");
    }

    if (window.L) {
        ocm_app.logEvent("Leaflet ready");
    } else {
        ocm_app.logEvent("Leaflet not ready");
    }

    startApp();
}

function bootStrap() {
    if (_appBootStrapped == false) {
        ocm_app.logEvent("There can be only one.");

        if ($.mobile) {
            ocm_app.logEvent("Running under JQM");
        } else {
            ocm_app.logEvent("No JQM Running");
        }

        if (window.cordova) {
            ocm_app.isRunningUnderCordova = true;
            ocm_app.logEvent("Phonegap enabled. Operating as mobile app.");
            document.addEventListener("deviceready", onDeviceReady, false);
        } else {
            ocm_app.logEvent("Phonegap not enabled. Operating as desktop browser.");
            startApp();
        }
    } else {
        ocm_app.logEvent("Ignoring additional bootstrap call.");
    }
}

$(document).bind('pageinit', function () {
    bootStrap();
});

$(function () {
    if (!$.mobile) {
        bootStrap();
    }
});
