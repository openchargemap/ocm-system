using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace OCM.Import.Misc
{
    public class GelocationCacheItem
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int CountryID { get; set; }

        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string LanguageCodes { get; set; }
    }

    public class GeolocationCacheManager
    {
        public string CountryCodeLookupServiceURL = "http://api.geonames.org/countryCodeJSON?formatted=true&lat={latitude}&lng={longitude}&username={userid}&style=full";
        public string NearbyPlacesLookupServiceURL = "http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat={latitude}&lng={longitude}&username={userid}&style=full";
        public string GeonamesAPIUserName = "demo";
        public string GeolocationCacheDataFile = "GeolocationCache.json";
        public List<GelocationCacheItem> GeolocationCache = null;

        private string tempFolder = "";

        public GeolocationCacheManager(string tmpPath)
        {
            GeolocationCache = new List<GelocationCacheItem>();
            tempFolder = tmpPath;
        }

        public bool LoadCache()
        {
            var path = Path.Combine(tempFolder, GeolocationCacheDataFile);
            if (System.IO.File.Exists(path))
            {
                string cacheJSON = System.IO.File.ReadAllText(path);
                GeolocationCache = JsonConvert.DeserializeObject<List<GelocationCacheItem>>(cacheJSON);
                return true;
            }

            return false;
        }

        public bool SaveCache()
        {
            try
            {
                var output = GeolocationCache.OrderBy(c => c.CountryName).ThenBy(c => c.Latitude).ThenBy(c => c.Longitude);
                string json = JsonConvert.SerializeObject(output);

                var path = Path.Combine(tempFolder, GeolocationCacheDataFile);
                System.IO.File.WriteAllText(path, json);

                //refresh in memory cache
                LoadCache();
                return true;
            }
            catch (Exception exp)
            {
                //failed
                LogHelper.Log("Failed to save geolocation cache:" + exp.ToString());
            }

            return false;
        }

        public GelocationCacheItem PerformLocationLookup(double latitude, double longitude, List<Country> ocmCountryList)
        {
            //http://api.geonames.org/countryCodeJSON?formatted=true&lat=41.55111&lng=10.2&username<userid>&style=full
            //http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat=47.3&lng=9&username=<userid>&style=full

            try
            {
                //round position to 3 decimal places, or 111 meters accuracy
                latitude = Math.Round(latitude, 3);
                longitude = Math.Round(longitude, 3);

                //lookup item in existing cache, if not present create new item

                var cacheHit = GeolocationCache.FirstOrDefault(g => g.Latitude == latitude && g.Longitude == longitude);
                if (cacheHit != null)
                {
                    return cacheHit;
                }
                else
                {
                    GelocationCacheItem cacheItem = new GelocationCacheItem();
                    string serviceURL = CountryCodeLookupServiceURL.Replace("{latitude}", latitude.ToString()).Replace("{longitude}", longitude.ToString()).Replace("{userid}", GeonamesAPIUserName);

                    WebRequest request = WebRequest.Create(serviceURL);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseText = streamReader.ReadToEnd();

                        if (responseText.Contains("countryName"))
                        {
                            //example response
                            /*{
                              "languages": "de-AT,hr,hu,sl",
                              "distance": 0,
                              "countryName": "Austria",
                              "countryCode": "AT"
                            }*/
                            JObject responseObject = JObject.Parse(responseText);
                            cacheItem.CountryName = responseObject["countryName"].ToString();
                            cacheItem.CountryCode = responseObject["countryCode"].ToString();
                            cacheItem.LanguageCodes = responseObject["languages"].ToString();
                            cacheItem.Longitude = longitude;
                            cacheItem.Latitude = latitude;

                            var countryInfo = ocmCountryList.FirstOrDefault(o => o.ISOCode == cacheItem.CountryCode || o.Title == cacheItem.CountryName);
                            if (countryInfo != null)
                            {
                                cacheItem.CountryID = countryInfo.ID;
                            }
                            else
                            {
                                LogHelper.Log("Country not found in OCM list:" + cacheItem.CountryName);
                            }

                            LogHelper.Log("Geocache item created:" + cacheItem.CountryName);
                            GeolocationCache.Add(cacheItem);
                            SaveCache();
                        }
                    }
                    return cacheItem;
                }
            }
            catch (Exception)
            {
                //failed
                return null;
            }
        }
    }
}