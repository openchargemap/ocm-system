/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="TypeScriptReferences/history/history.d.ts" />
/// <reference path="OCM_Data.d.ts" />
/// <reference path="OCM_CommonUI.d.ts" />
/// <reference path="OCM_Geolocation.d.ts" />
declare var localisation_dictionary: any;
interface JQuery {
    fastClick: any;
    swipebox: any;
    closeSlide: any;
}
interface JQueryStatic {
    swipebox: any;
}
interface HTMLFormElement {
    files: any;
}
declare var Historyjs: Historyjs;
declare var bootbox: any;
declare function OCM_App(): void;
