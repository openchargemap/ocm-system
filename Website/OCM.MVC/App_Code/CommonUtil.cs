using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web;

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
                languages.Add(new KeyValuePair<string, string>("zh", "Chinese/中国的"));
                languages.Add(new KeyValuePair<string, string>("ru", "Russian/Pусский"));
                languages.Add(new KeyValuePair<string, string>("el", "Greek/Ελληνική"));
                languages.Add(new KeyValuePair<string, string>("ar", "العربية / Arabic"));
                
                languages.Add(new KeyValuePair<string, string>("test", "Test (For Translators)"));

                return languages;
            }
        }

        private static string DetermineLanguageCode(bool allowTestMode = false)
        {
            var Request = HttpContext.Current.Request;
            var Session = HttpContext.Current.Session;

            if (!String.IsNullOrEmpty(Request["languagecode"]))
            {
                Session["languageCode"] = Request["languagecode"];
            }
            else
            {
                if (Session["languageCode"]==null)
                {
                    //attempt to automatically determine users language
                    var languages = Request.UserLanguages;
                    var supportedLanguages = SupportedLanguages;

                    if (languages != null)
                    {
                        foreach (var language in languages)
                        {
                            var langCode = language.Split(';')[0];
                            if (SupportedLanguages.Any(l=>l.Key==langCode))
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
        public static string GetLocalizationScriptBlock(string urlPrefix)
        {
            string ocm_language_code = DetermineLanguageCode(allowTestMode: true);

            if (ocm_language_code != "en")
            {
                string output = "<script charset=\"UTF-8\" src=\"" + urlPrefix + "/OCM/Localisation/languagePack.min.js\"></script>";
                output += "<script charset=\"UTF-8\" src=\"http://openchargemap.org/app/js/OCM_CommonUI.js?v=3.0\"></script>";

                if (ocm_language_code != "test")
                {
                    output += "<script>localisation_dictionary = localisation_dictionary_"+ocm_language_code+"; new OCM_CommonUI().applyLocalisation(false);</script>";
                }
                else
                {
                    output += "<script>new OCM_CommonUI().applyLocalisation(true);</script>";
                }
                return output;
            }
            else
            {
                return "";
            }
        }
    }
}