/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/
var OCM;
(function (OCM) {
    (function (LogLevel) {
        LogLevel[LogLevel["VERBOSE"] = 0] = "VERBOSE";
        LogLevel[LogLevel["INFO"] = 1] = "INFO";
        LogLevel[LogLevel["WARNING"] = 2] = "WARNING";
        LogLevel[LogLevel["ERROR"] = 3] = "ERROR";
    })(OCM.LogLevel || (OCM.LogLevel = {}));
    var LogLevel = OCM.LogLevel;
    var Base = (function () {
        function Base() {
        }
        Base.prototype.log = function (msg, level) {
            if (typeof level === "undefined") { level = 0 /* VERBOSE */; }
            if (console) {
                console.log("[" + LogLevel[level] + "] " + msg);
            }
        };
        return Base;
    })();
    OCM.Base = Base;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_Base.js.map
