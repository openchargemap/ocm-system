/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="TypeScriptReferences/history/history.d.ts" />
/// <reference path="OCM_Data.d.ts" />
/// <reference path="OCM_CommonUI.d.ts" />
/// <reference path="OCM_Geolocation.d.ts" />
declare var localisation_dictionary: any;
declare var languageList: any[];
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
declare module OCM {
    class App extends LocationEditor {
        private resultItemTemplate;
        constructor();
        public initApp(): void;
        public setupUIActions(): void;
        public postLoginInit(): void;
        public initDeferredUI(): void;
        public beginLogin(): void;
        public logout(navigateToHome: boolean): void;
        public storeSettings(): void;
        public loadSettings(): void;
        public performCommentSubmit(): void;
        public performMediaItemSubmit(): void;
        public submissionCompleted(jqXHR: any, textStatus: any): void;
        public submissionFailed(): void;
        public performSearch(useClientLocation?: boolean, useManualLocation?: boolean): void;
        public handleSearchError(result: any): void;
        public determineUserLocationCompleted(pos: any): void;
        public determineUserLocationFailed(): void;
        public determineGeocodedLocationCompleted(pos: MapCoords): void;
        public renderPOIList(locationList: any[]): void;
        public showDetailsViewById(id: any, forceRefresh: any): void;
        public showDetailsFromList(results: any): void;
        public showDetailsView(element: any, poi: any): void;
        public refreshMapView(): void;
        public setMapFocus(hasFocus: boolean): void;
        public updatePOIDistanceDetails(response: any, status: any): void;
        public isFavouritePOI(poi: any, itineraryName?: string): boolean;
        public addFavouritePOI(poi: any, itineraryName?: string): void;
        public removeFavouritePOI(poi: any, itineraryName?: string): void;
        public toggleFavouritePOI(poi: any, itineraryName?: string): void;
        public getFavouritePOIList(itineraryName?: string): any[];
        public switchLanguage(languageCode: string): void;
        public hidePage(pageId: string): void;
        public showPage(pageId: string, pageTitle: string, skipState?: boolean): void;
        public initStateTracking(): void;
        public navigateToSearch(): void;
        public navigateToHome(): void;
        public navigateToMap(): void;
        public navigateToFavourites(): void;
        public navigateToAddLocation(): void;
        public navigateToEditLocation(): void;
        public navigateToLogin(): void;
        public navigateToSettings(): void;
        public navigateToAbout(): void;
        public navigateToAddComment(): void;
        public navigateToAddMediaItem(): void;
        public showConnectionError(): void;
        public showAuthorizationError(): void;
        public toggleMenu(showMenu: boolean): void;
    }
}
