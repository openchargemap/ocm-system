/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/**
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com
*/

//typescript declarations
declare var localisation_dictionary;

module OCM {
    /*
        UI Localisation/translation utils
    */
    export class i18n {
        getTranslation(resourceKey, defaultValue, params, targetElement: HTMLElement) {
            try {
                var translatedText: string = eval("localisation_dictionary." + resourceKey);
                if (translatedText == null) translatedText = defaultValue;

                //TODO: if resource key value translates a link, find links in default Value
                if (targetElement != null) {
                    if (translatedText.indexOf("{") >= 0) {
                        var re = /\{(.*?)\}/gmi; //match text between {..}
                        var found = translatedText.match(re);
                        for (var i = 0; i < found.length; i++) {
                            var translationTemplate = found[i];
                            //alert(found[i]);
                            var matchingNodes: NodeList = targetElement.querySelectorAll('[data-localize-id]');
                            for (var m = 0; m < matchingNodes.length; m++) {
                                var origElement = <Element>matchingNodes[m];

                                var elementId = origElement.getAttribute('data-localize-id');
                                if (translationTemplate.indexOf(elementId) >= 0) {
                                    var startPos = (translationTemplate.indexOf(":")) + 1;
                                    var newText = translationTemplate.substr(startPos, translationTemplate.indexOf("}") - startPos);
                                    (<HTMLElement>origElement).innerHTML = newText;
                                    translatedText = translatedText.replace(found[i], (<HTMLElement>origElement).outerHTML);
                                }
                            }
                        }
                    }
                }

                //if we have parameters to apply, replace their values in the translation
                if (params != null) {
                    for (var propertyName in params) {
                        translatedText = translatedText.replace("{" + propertyName + "}", params[propertyName].toString());
                    }
                }

                return translatedText;
            } catch (exp) {
                if (console) console.log("OCM: could not translate resource key: " + resourceKey);
            }

            //no translation available
            return null;
        }

        applyLocalisation(isTestMode) {
            try {
                if (isTestMode == true || localisation_dictionary != null) {
                    var elementList = $("[data-localize]");

                    for (var i = 0; i < elementList.length; i++) {
                        var $element = $(elementList[i]);
                        var resourceKey = $element.attr("data-localize");

                        if (isTestMode == true || eval("localisation_dictionary." + resourceKey) != undefined) {
                            var localisedText;

                            if ($element.is("select")) {
                                var optionsList = (<HTMLSelectElement>$element[0]).options;

                                //enumerate and translate each list item
                                for (var opt = 0; opt < optionsList.length; opt++) {
                                    var optValue = optionsList[opt].value;
                                    if (optValue.indexOf("-") != -1) {
                                        //can't have '-' in property name from resource dictionary, replace with "minus"
                                        optValue = optValue.replace("-", "minus");
                                    }
                                    var optResourceKey = resourceKey + ".value_" + optValue;
                                    if (isTestMode == true) {
                                        //in test mode the resource key is displayed as the localised text
                                        localisedText = "[" + optResourceKey + "]";
                                    } else {
                                        localisedText = this.getTranslation(optResourceKey, null, null, elementList[i]);
                                    }
                                    if (localisedText != null) {
                                        optionsList[opt].text = localisedText;
                                    }
                                }
                            } else {
                                if (isTestMode == true) {
                                    //in test mode the resource key is displayed as the localised text
                                    localisedText = "[" + resourceKey + "]";
                                } else {
                                    localisedText = this.getTranslation(resourceKey, null, null, elementList[i]);
                                }

                                if (localisedText != null) {
                                    if ($element.is("input")) {
                                        if ($element.attr("type") == "button") {
                                            //set input button value
                                            $element.val(localisedText);
                                        }
                                    } else {
                                        if ($element.attr("data-localize-opt") == "title") {
                                            //set title of element only
                                            $(elementList[i]).attr("title", localisedText);
                                        } else {
                                            //standard localisation method is to replace inner text of element
                                            $(elementList[i]).html(localisedText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (exp) {
                //localisation attempt failed
                if (console) console.log(exp.toString());
            } finally {
            }
        }
    }
}