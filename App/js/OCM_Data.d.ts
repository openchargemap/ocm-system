/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
declare module OCM {
    class POI_SearchParams {
        constructor();
        public countryCode: string;
        public latitude: number;
        public longitude: number;
        public distance: number;
        public distanceUnit: string;
        public connectionTypeID: number;
        public operatorID: number;
        public levelID: number;
        public countryID: number;
        public usageTypeID: number;
        public statusTypeID: number;
        public submissionStatusTypeID: number;
        public maxResults: number;
        public additionalParams: string;
        public includeComments: boolean;
        public enableCaching: boolean;
    }
    interface ConnectionInfo {
        ID: number;
        Reference: string;
        ConnectionType: any;
        StatusType: any;
        Level: any;
        CurrentType: any;
        Amps: number;
        Voltage: number;
        PowerKW: number;
        Quantity: number;
        Comments?: string;
    }
    class API {
        public serviceBaseURL: string;
        public hasAuthorizationError: boolean;
        public ATTRIBUTION_METADATAFIELDID: number;
        public referenceData: any;
        public clientName: string;
        public authorizationErrorCallback: any;
        public generalErrorCallback: any;
        public fetchLocationDataList(countrycode: any, lat: any, lon: any, distance: any, distanceunit: any, maxresults: any, includecomments: any, callbackname: any, additionalparams: any, errorcallback: any): void;
        public fetchLocationDataListByParam(params: POI_SearchParams, callbackname: any, errorcallback: any): void;
        public fetchLocationById(id: any, callbackname: any, errorcallback: any): void;
        public handleGeneralAjaxError(result: any, ajaxOptions: any, thrownError: any): void;
        public fetchCoreReferenceData(callbackname: any, authSessionInfo: any): void;
        public fetchGeocodeResult(address: any, successCallback: any, authSessionInfo: any): void;
        public getAuthParamsFromSessionInfo(authSessionInfo: any): string;
        public submitLocation(data: any, authSessionInfo: any, completedCallback: any, failureCallback: any): void;
        public submitUserComment(data: any, authSessionInfo: any, completedCallback: any, failureCallback: any): void;
        public submitMediaItem(data: any, authSessionInfo: any, completedCallback: any, failureCallback: any): void;
        public getRefDataByID(refDataList: any, id: any): any;
        public sortCoreReferenceData(): void;
        public sortReferenceData(sourceList: any): void;
        public getMetadataValueByMetadataFieldID(metadataValues: any, id: any): any;
        public sortListByTitle(a: any, b: any): number;
        public isLocalStorageAvailable(): boolean;
        public setCachedDataObject(itemName: any, itemValue: any): void;
        public getCachedDataObject(itemName: any): any;
    }
}
