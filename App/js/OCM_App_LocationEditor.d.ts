/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="OCM_App.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
interface JQuery {
    validate: any;
    collapse: any;
}
declare module OCM {
    class LocationEditor extends AppBase {
        public editorMapInitialised: boolean;
        public editorMap: any;
        public editMarker: any;
        public positionAttribution: any;
        public numConnectionEditors: number;
        public isLocationEditMode: boolean;
        public initEditors(): void;
        public resetEditorForm(): void;
        public populateEditor(refData: any): void;
        public populateEditorLatLon(result: MapCoords): void;
        public validateLocationEditor(): boolean;
        public performLocationSubmit(): void;
        public showLocationEditor(): void;
        public refreshEditorMap(): void;
        public initEditorMap(currentLat: any, currentLng: any): void;
        public hasUserPermissionForPOI(poi: any, permissionLevel: any): boolean;
    }
}
