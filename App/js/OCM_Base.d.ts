/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
declare module OCM {
    enum LogLevel {
        VERBOSE = 0,
        INFO = 1,
        WARNING = 2,
        ERROR = 3,
    }
    class Base {
        constructor();
        public log(msg: string, level?: LogLevel): void;
    }
}
