var OCM;
(function (OCM) {
    var LogLevel;
    (function (LogLevel) {
        LogLevel[LogLevel["VERBOSE"] = 0] = "VERBOSE";
        LogLevel[LogLevel["INFO"] = 1] = "INFO";
        LogLevel[LogLevel["WARNING"] = 2] = "WARNING";
        LogLevel[LogLevel["ERROR"] = 3] = "ERROR";
    })(LogLevel = OCM.LogLevel || (OCM.LogLevel = {}));
    var Base = (function () {
        function Base() {
            this.enableLogging = true;
        }
        Base.prototype.log = function (msg, level) {
            if (level === void 0) { level = LogLevel.VERBOSE; }
            if (this.enableLogging && console) {
                console.log("[" + LogLevel[level] + "] {" + (new Date().toLocaleTimeString()) + "} " + msg);
            }
        };
        return Base;
    }());
    OCM.Base = Base;
})(OCM || (OCM = {}));
