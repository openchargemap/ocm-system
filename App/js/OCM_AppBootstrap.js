/*global OCM_App:true*/

//Set iOS App Splashscreen depending on device size
var filename = navigator.platform === 'iPad' ? 'images/splashscreen/AppSplashscreen_768x1004.png' : 'images/splashscreen/AppSplashscreen_320x460.png';
document.write('<link rel="apple-touch-startup-image" href="' + filename + '" />');

//Perform App Init
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
	ocm_app.logEvent("Cordova Loaded, onDeviceReady Fired.");
	if (navigator.network.connection.type == Connection.NONE) {
		$("#network-error").show();
	}
	
	/*
	//phonegap analytics plugin
	
	gaPlugin = window.plugins.gaPlugin;
	gaPlugin.init(successHandler, errorHandler, "UA-76936-12", 10);
	*/
	
	startApp();
}

function bootStrap() {
    if (window.cordova) {
        ocm_app.isRunningUnderCordova = true;
        ocm_app.logEvent("Phonegap enabled. Operating as mobile app.");
		document.addEventListener("deviceready", onDeviceReady, false);
	} else {
		ocm_app.logEvent("Phonegap not enabled. Operating as desktop browser.");
		startApp();
	}
}


$(document).bind('pageinit', function () {
	if (_appBootStrapped == false) {
	    ocm_app.logEvent("There can be only one.");
		bootStrap();
	}
});

$(document).bind('pagebeforeshow', function () {

});
