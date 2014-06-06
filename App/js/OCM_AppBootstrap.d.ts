/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
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
}
declare var ocm_app: any;
declare var _appBootStrapped: boolean;
declare var gaPlugin: any;
declare function startApp(): void;
declare function onDeviceReady(): void;
declare function bootStrap(): void;
