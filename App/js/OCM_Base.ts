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
        public enableLogging: boolean;
        constructor() {
            this.enableLogging = false;
        }

        public log(msg: string, level: LogLevel = LogLevel.VERBOSE) {
            if (this.enableLogging && console) {
                console.log("[" + LogLevel[level] + "] " + msg);
            }
        }
    }
} 