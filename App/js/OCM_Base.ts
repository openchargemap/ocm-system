/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

module OCM {
    export enum LogLevel {
        VERBOSE,
        INFO,
        WARNING,
        ERROR
    }
    export class Base {

        constructor() {
        }

        public log(msg: string, level: LogLevel = LogLevel.VERBOSE) {
            if (console) {
                console.log("[" + LogLevel[level] + "] " + msg);
            }
        }
    }
} 