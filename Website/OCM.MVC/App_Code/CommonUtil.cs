using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC
{
    public class CommonUtil
    {
        /// <summary>
        /// this language list corresponds to the list of supported OCM_UI_LocalisationResources
        /// </summary>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> SupportedLanguages
        {
            get
            {
                var languages = new List
                <KeyValuePair<string, string>>();

                languages.Add(new KeyValuePair<string, string>("en", "English"));
                languages.Add(new KeyValuePair<string, string>("fr", "French/Français"));
                languages.Add(new KeyValuePair<string, string>("ja", "Japanese/日本語"));
                languages.Add(new KeyValuePair<string, string>("nl", "Dutch/Nederlands"));
                languages.Add(new KeyValuePair<string, string>("de", "German/Deutsch"));
                languages.Add(new KeyValuePair<string, string>("lt", "Lithuanian/Lietuvių"));
                languages.Add(new KeyValuePair<string, string>("zh", "Chinese/中国的"));
                languages.Add(new KeyValuePair<string, string>("ru", "Russian/Pусский"));
                languages.Add(new KeyValuePair<string, string>("el", "Greek/Ελληνική"));
                languages.Add(new KeyValuePair<string, string>("ar", "العربية / Arabic"));
                languages.Add(new KeyValuePair<string, string>("hu", "Hungarian/Magyar Nyelv"));
                languages.Add(new KeyValuePair<string, string>("pt", "Portuguese/português"));
                languages.Add(new KeyValuePair<string, string>("sk", "Slovak/slovenčina"));
                languages.Add(new KeyValuePair<string, string>("it", "Italian/Italiano"));
                languages.Add(new KeyValuePair<string, string>("es", "Spanish/Español"));
                languages.Add(new KeyValuePair<string, string>("et", "Estonian/Eesti"));
                languages.Add(new KeyValuePair<string, string>("fy", "Frisian/Frysk"));
                languages.Add(new KeyValuePair<string, string>("fi", "Finnish/Suomi"));

                //note: es,is,pl,ro,tr lang code are blank resources and should be removed

                languages.Add(new KeyValuePair<string, string>("test", "Test (For Translators)"));

                return languages;
            }
        }

        public static string DetermineLanguageCode(bool allowTestMode = false, string routeLanguageCode = null)
        {
            var Request = HttpContext.Current.Request;
            var Session = HttpContext.Current.Session;

            string selectedLanguageCode = null;

            if (!String.IsNullOrEmpty(routeLanguageCode)) selectedLanguageCode = routeLanguageCode;
            if (!String.IsNullOrEmpty(Request["languagecode"])) selectedLanguageCode = Request["languagecode"];

            if (!String.IsNullOrEmpty(selectedLanguageCode))
            {
                Session["languageCode"] = selectedLanguageCode;
            }
            else
            {
                if (Session["languageCode"] == null)
                {
                    //attempt to automatically determine users language from info passed in request
                    var languages = Request.UserLanguages;
                    var supportedLanguages = SupportedLanguages;

                    if (languages != null)
                    {
                        foreach (var language in languages)
                        {
                            var langCode = language.Split(';')[0];
                            if (SupportedLanguages.Any(l => l.Key == langCode))
                            {
                                var chosenSupportedLanguage = SupportedLanguages.First(l => l.Key == langCode);
                                Session["languageCode"] = chosenSupportedLanguage.Key;
                                break;
                            }
                        }
                    }
                }
            }

            if (Session["languageCode"] != null && !String.IsNullOrEmpty(Session["languageCode"].ToString()))
            {
                if (Session["languageCode"].ToString() == "test" && !allowTestMode)
                {
                    return "en";
                }
                else
                {
                    return Session["languageCode"].ToString();
                }
            }
            else
            {
                return "en";
            }
        }

        public static string GetDocumentLanguageCode()
        {
            return DetermineLanguageCode();
        }

        /// <summary>
        /// If user supplies a language code preference we provide a script block to dynamically apply localization
        /// </summary>
        /// <returns></returns>
        public static string GetLocalizationScriptBlock(string urlPrefix, string routeLanguageCode = null)
        {
            string ocm_language_code = DetermineLanguageCode(allowTestMode: true, routeLanguageCode: routeLanguageCode);
            string locEventScript =
                "<script charset =\"UTF-8\" src=\"" + urlPrefix + "/OCM/SharedLibs/OCM_i18n.js\"></script>";

            locEventScript += @"<script>
                _i18nUtils=new OCM.i18n();
                var localisationEvent = new CustomEvent('ocm-i18n-ready',{detail:null, bubble:true, cancelable:true});

                </script>
            ";

            //if (ocm_language_code != "en")
            {
                string output = locEventScript + "<script charset=\"UTF-8\" src=\"" + urlPrefix + "/OCM/Localisation/languagePack.min.js\"></script>";

                if (ocm_language_code != "test")
                {
                    output += "<script>localisation_dictionary = localisation_dictionary_" + ocm_language_code + "; _i18nUtils.applyLocalisation(false);</script>";
                }
                else
                {
                    output += "<script>_i18nUtils.applyLocalisation(true);</script>";
                }
                output += "<script>document.dispatchEvent(localisationEvent);</script>";
                return output;
            }
            /*else
            {
                return locEventScript;
            }*/
        }
    }
}