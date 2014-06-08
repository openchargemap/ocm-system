/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
declare module OCM {
    class Utils {
        static getMaxLevelOfPOI(poi: any): number;
        static applyLocalisation(isTestMode: boolean): void;
        static fixJSONDate(val: any): any;
        static formatMapLinkFromPosition(poi: any, searchLatitude: any, searchLongitude: any, distance: any, distanceunit: any): string;
        static formatSystemWebLink(linkURL: any, linkTitle: any): string;
        static formatMapLink(poi: any, linkContent: any, isRunningUnderCordova: boolean): string;
        static formatURL(url: any, title?: string): string;
        static formatPOIAddress(poi: any): string;
        static formatString(val: any): any;
        static formatTextField(val: any, label?: string, newlineAfterLabel?: boolean, paragraph?: boolean, resourceKey?: string): string;
        static formatEmailAddress(email: string): string;
        static formatPhone(phone: any, labeltitle?: string): string;
        static formatPOIDetails(poi: any, fullDetailsMode: boolean): {
            "address": string;
            "contactInfo": string;
            "additionalInfo": string;
            "advancedInfo": string;
        };
    }
}
