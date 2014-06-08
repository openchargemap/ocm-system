/// <reference path="OCM_CommonUI.d.ts" />
declare var escape: any;
declare var unescape: any;
declare var bootbox: any;
declare module OCM {
    enum AppMode {
        STANDARD = 0,
        LOCALDEV = 1,
        SANDBOXED = 2,
    }
    /** View Model for core functionality */
    class AppViewModel {
        /** The current selected POI */
        public selectedPOI: any;
        /** The current set of POIs from latest search */
        public poiList: any[];
        /** A set of POIs favourited by the user */
        public favouritesList: any[];
        /** track changes in result set, avoiding duplicate processing (maps etc) */
        public resultsBatchID: number;
        public searchPosition: MapCoords;
        constructor();
    }
    /** App configuration settings */
    class AppConfig {
        public launchMapOnStartup: boolean;
        public maxResults: number;
        public baseURL: string;
        public loginProviderRedirectBaseURL: string;
        public loginProviderRedirectURL: string;
        public autoRefreshMapResults: boolean;
        constructor();
    }
    /** App state settings*/
    class AppState {
        public appMode: AppMode;
        public isRunningUnderCordova: boolean;
        public isEmbeddedAppMode: boolean;
        public appInitialised: boolean;
        public languageCode: string;
        public isLocationEditMode: boolean;
        public menuDisplayed: boolean;
        public mapLaunched: boolean;
        public enableCommentSubmit: boolean;
        public isSearchInProgress: boolean;
        public _lastPageId: string;
        constructor();
    }
    /** Base for App Components */
    class AppBase extends Base {
        public ocm_geo: Geolocation;
        public ocm_data: API;
        public mappingManager: Mapping;
        public viewModel: AppViewModel;
        public appState: AppState;
        public appConfig: AppConfig;
        constructor();
        public getLoggedInUserInfo(): {
            "Identifier": any;
            "Username": any;
            "SessionToken": any;
            "Permissions": any;
        };
        public setLoggedInUserInfo(userInfo: any): void;
        public getCookie(c_name: string): any;
        public setCookie(c_name: string, value: any, exdays?: number): void;
        public clearCookie(c_name: string): void;
        public getParameter(name: string): string;
        public setDropdown(id: any, selectedValue: any): void;
        public populateDropdown(id: any, refDataList: any, selectedValue: any, defaultToUnspecified?: boolean, useTitleAsValue?: boolean, unspecifiedText?: string): void;
        public showProgressIndicator(): void;
        public hideProgressIndicator(): void;
        public setElementAction(elementSelector: any, actionHandler: any): void;
        public isUserSignedIn(): boolean;
        public getParameterFromURL(name: any, url: any): string;
        public showMessage(msg: any): void;
    }
}
