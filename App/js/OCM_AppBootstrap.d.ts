/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
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
