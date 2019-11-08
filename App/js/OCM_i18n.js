var OCM;
(function (OCM) {
    var i18n = (function () {
        function i18n() {
        }
        i18n.prototype.getTranslation = function (resourceKey, defaultValue, params, targetElement) {
            try {
                var translatedText = eval("localisation_dictionary." + resourceKey);
                if (translatedText == null)
                    translatedText = defaultValue;
                if (targetElement != null) {
                    if (translatedText.indexOf("{") >= 0) {
                        var re = /\{(.*?)\}/gmi;
                        var found = translatedText.match(re);
                        for (var i = 0; i < found.length; i++) {
                            var translationTemplate = found[i];
                            var matchingNodes = targetElement.querySelectorAll('[data-localize-id]');
                            for (var m = 0; m < matchingNodes.length; m++) {
                                var origElement = matchingNodes[m];
                                var elementId = origElement.getAttribute('data-localize-id');
                                if (translationTemplate.indexOf(elementId) >= 0) {
                                    var startPos = (translationTemplate.indexOf(":")) + 1;
                                    var newText = translationTemplate.substr(startPos, translationTemplate.indexOf("}") - startPos);
                                    origElement.innerHTML = newText;
                                    translatedText = translatedText.replace(found[i], origElement.outerHTML);
                                }
                            }
                        }
                    }
                }
                if (params != null) {
                    for (var propertyName in params) {
                        translatedText = translatedText.replace("{" + propertyName + "}", params[propertyName].toString());
                    }
                }
                return translatedText;
            }
            catch (exp) {
                if (console)
                    console.log("OCM: could not translate resource key: " + resourceKey);
            }
            return null;
        };
        i18n.prototype.applyLocalisation = function (isTestMode) {
            try {
                if (isTestMode == true || localisation_dictionary != null) {
                    var elementList = $("[data-localize]");
                    for (var i = 0; i < elementList.length; i++) {
                        var $element = $(elementList[i]);
                        var resourceKey = $element.attr("data-localize");
                        if (isTestMode == true || eval("localisation_dictionary." + resourceKey) != undefined) {
                            var localisedText;
                            if ($element.is("select")) {
                                var optionsList = $element[0].options;
                                for (var opt = 0; opt < optionsList.length; opt++) {
                                    var optValue = optionsList[opt].value;
                                    if (optValue.indexOf("-") != -1) {
                                        optValue = optValue.replace("-", "minus");
                                    }
                                    var optResourceKey = resourceKey + ".value_" + optValue;
                                    if (isTestMode == true) {
                                        localisedText = "[" + optResourceKey + "]";
                                    }
                                    else {
                                        localisedText = this.getTranslation(optResourceKey, null, null, elementList[i]);
                                    }
                                    if (localisedText != null) {
                                        optionsList[opt].text = localisedText;
                                    }
                                }
                            }
                            else {
                                if (isTestMode == true) {
                                    localisedText = "[" + resourceKey + "]";
                                }
                                else {
                                    localisedText = this.getTranslation(resourceKey, null, null, elementList[i]);
                                }
                                if (localisedText != null) {
                                    if ($element.is("input")) {
                                        if ($element.attr("type") == "button") {
                                            $element.val(localisedText);
                                        }
                                    }
                                    else {
                                        if ($element.attr("data-localize-opt") == "title") {
                                            $(elementList[i]).attr("title", localisedText);
                                        }
                                        else {
                                            $(elementList[i]).html(localisedText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (exp) {
                if (console)
                    console.log(exp.toString());
            }
            finally {
            }
        };
        return i18n;
    }());
    OCM.i18n = i18n;
})(OCM || (OCM = {}));
