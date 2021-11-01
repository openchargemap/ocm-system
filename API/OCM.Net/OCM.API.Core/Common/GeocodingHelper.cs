using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using OCM.Core.Settings;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace OCM.API.Common
{
    [Serializable]
    public class LatLon
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public static LatLon Parse(string val)
        {
            var temp = val.ToString().Trim();
            if (temp.StartsWith(",")) temp = temp.Substring(1, temp.Length - 1).Trim();
            var fragments = temp.Split(',');
            var ll = new LatLon
            {
                Latitude = double.Parse(fragments[0].Substring(1, fragments[0].Length - 1)),
                Longitude = double.Parse(fragments[1])
            };

            if (ll.Latitude < -90) ll.Latitude = -90;
            if (ll.Latitude > 90) ll.Latitude = 90;

            if (ll.Longitude < -180) ll.Longitude = -180;
            if (ll.Longitude > 180) ll.Longitude = 180;
            return ll;
        }
    }

    [Serializable]
    public class LocationLookupResult : LatLon
    {
        public string IP { get; set; }

        public string Country_Code { get; set; }

        public string Country_Name { get; set; }

        public string Region_Code { get; set; }

        public string Region_Name { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        public bool SuccessfulLookup { get; set; }

        public int? CountryID { get; set; }

        public int? LocationID { get; set; }
    }

    public class LocationImage
    {
        public string ImageRepositoryID { get; set; }

        public string ImageID { get; set; }

        public string Title { get; set; }

        public string Submitter { get; set; }

        public string SubmitterURL { get; set; }

        public string DetailsURL { get; set; }

        public string ImageURL { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    public class GeocodingHelper
    {
        public bool IncludeExtendedData { get; set; }

        public bool IncludeQueryURL { get; set; }

        private CoreSettings _settings;

        public GeocodingHelper(CoreSettings settings)
        {
            _settings = settings;
        }

        /* public GeocodingResult GeolocateAddressInfo_Google(AddressInfo address)
         {
             var result = GeolocateAddressInfo_Google(address.ToString());
             result.AddressInfoID = address.ID;

             return result;
         }*/

        public GeocodingResult GeolocateAddressInfo_MapquestOSM(AddressInfo address)
        {
            var result = GeolocateAddressInfo_MapquestOSM(address.ToString());
            result.AddressInfoID = address.ID;

            return result;
        }

        public GeocodingResult ReverseGecode_MapquestOSM(double latitude, double longitude, ReferenceDataManager refDataManager)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "MapQuest Open";

            string url = "http://open.mapquestapi.com/geocoding/v1/reverse?location=" + latitude + "," + longitude + "&key=" + _settings.ApiKeys.MapQuestOpenAPIKey;

            if (IncludeQueryURL) result.QueryURL = url;

            string data = "";

            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
                client.Encoding = Encoding.GetEncoding("UTF-8");
                data = client.DownloadString(url);

                if (IncludeExtendedData)
                {
                    result.ExtendedData = data;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(data);
                }

                if (data == "[]")
                {
                    System.Diagnostics.Debug.WriteLine("No geocoding results:" + url);
                    result.ResultsAvailable = false;
                }
                else
                {
                    JObject o = JObject.Parse(data);
                    var locations = o["results"][0]["locations"];
                    if (locations.Any())
                    {
                        var item = o["results"][0]["locations"][0];


                        result.AddressInfo = new AddressInfo();
                        result.AddressInfo.Title = item["street"]?.ToString();
                        result.AddressInfo.Postcode = item["postalCode"]?.ToString();
                        result.AddressInfo.AddressLine1 = item["street"]?.ToString();

                        if (item["adminArea5Type"]?.ToString() == "City")
                        {
                            result.AddressInfo.Town = item["adminArea5"]?.ToString();
                        }

                        if (item["adminArea3Type"]?.ToString() == "State")
                        {
                            result.AddressInfo.StateOrProvince = item["adminArea3"]?.ToString();
                        }

                        if (item["adminArea3Type"]?.ToString() == "State")
                        {
                            result.AddressInfo.StateOrProvince = item["adminArea3"]?.ToString();
                        }
                        if (item["adminArea1Type"]?.ToString() == "Country")
                        {
                            var countryCode = item["adminArea1"]?.ToString();
                            var country = refDataManager.GetCountryByISO(countryCode);
                            if (country != null)
                            {
                                result.AddressInfo.CountryID = country.ID;
                            }
                        }

                        result.Latitude = latitude;
                        result.Longitude = longitude;
                        result.Attribution = "Portions © OpenStreetMap contributors"; // using mapquest open so results are from OSM
                        result.AddressInfo.Latitude = latitude;
                        result.AddressInfo.Longitude = longitude;

                        result.ResultsAvailable = true;
                    }
                    else
                    {
                        result.ResultsAvailable = false;
                    }
                }
            }
            catch (Exception)
            {
                //
            }
            return result;
        }
        public GeocodingResult GeolocateAddressInfo_MapquestOSM(string address)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "MapQuest Open";

            string url = "https://open.mapquestapi.com/geocoding/v1/address?key=" + _settings.ApiKeys.MapQuestOpenAPIKey + "&location=" + Uri.EscapeDataString(address.ToString());
            if (IncludeQueryURL) result.QueryURL = url;

            string data = "";
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
                client.Encoding = Encoding.GetEncoding("UTF-8");
                data = client.DownloadString(url);

                if (IncludeExtendedData) result.ExtendedData = data;

                if (data == "[]")
                {
                    System.Diagnostics.Debug.WriteLine("No geocoding results:" + url);
                    result.ResultsAvailable = false;
                }
                else
                {
                    JObject o = JObject.Parse(data);
                    var locations = o["results"][0]["locations"];
                    if (locations.Any())
                    {
                        var item = o["results"][0]["locations"][0]["latLng"];
                        result.Latitude = double.Parse(item["lat"].ToString());
                        result.Longitude = double.Parse(item["lng"].ToString());

                        result.ResultsAvailable = true;
                    }
                    else
                    {
                        result.ResultsAvailable = false;
                    }
                }
            }
            catch (Exception)
            {
                //
            }

            return result;
        }


        public GeocodingResult ReverseGecode_OSM(double latitude, double longitude, ReferenceDataManager refDataManager)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "Nominatim OSM";


            string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";

            if (IncludeQueryURL) result.QueryURL = url;

            string data = "";

            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
                client.Encoding = Encoding.GetEncoding("UTF-8");
                data = client.DownloadString(url);


                /* e.g.: 
                {
                    "place_id": 101131804,
                    "licence": "Data © OpenStreetMap contributors, ODbL 1.0. https://osm.org/copyright",
                    "osm_type": "way",
                    "osm_id": 61267076,
                    "lat": "-32.1685328505288",
                    "lon": "115.9882328723638",
                    "display_name": "Eleventh Road, Haynes, Armadale, Western Australia, 6112, Australia",
                    "address": {
                        "road": "Eleventh Road",
                        "suburb": "Haynes",
                        "town": "Armadale",
                        "state": "Western Australia",
                        "postcode": "6112",
                        "country": "Australia",
                        "country_code": "au"
                    },
                    "boundingbox": [
                        "-32.1689883",
                        "-32.1619497",
                        "115.9805577",
                        "115.9887699"
                    ]
                }
                 * */
                if (IncludeExtendedData)
                {
                    result.ExtendedData = data;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(data);
                }

                if (data == "{}")
                {
                    System.Diagnostics.Debug.WriteLine("No geocoding results:" + url);
                    result.ResultsAvailable = false;
                }
                else
                {
                    JObject o = JObject.Parse(data);

                    var item = o["address"];


                    result.AddressInfo = new AddressInfo();
                    result.AddressInfo.Title = item["road"]?.ToString();
                    result.AddressInfo.Postcode = item["postcode"]?.ToString();
                    result.AddressInfo.AddressLine1 = item["road"]?.ToString();
                    result.AddressInfo.AddressLine2 = item["suburb"]?.ToString();
                    result.AddressInfo.Town = item["town"]?.ToString();
                    result.AddressInfo.StateOrProvince = item["state"]?.ToString();

                    var countryCode = item["country_code"]?.ToString();
                    var country = refDataManager.GetCountryByISO(countryCode);
                    if (country != null)
                    {
                        result.AddressInfo.CountryID = country.ID;
                    }


                    result.Latitude = latitude;
                    result.Longitude = longitude;
                    result.Attribution = "Data © OpenStreetMap contributors, ODbL 1.0. https://osm.org/copyright";
                    result.AddressInfo.Latitude = latitude;
                    result.AddressInfo.Longitude = longitude;

                    result.ResultsAvailable = true;


                }
            }
            catch (Exception)
            {
                //
            }
            return result;
        }
        public GeocodingResult GeolocateAddressInfo_OSM(AddressInfo address)
        {
            var result = GeolocateAddressInfo_OSM(address.ToString());
            result.AddressInfoID = address.ID;

            return result;
        }



        public GeocodingResult GeolocateAddressInfo_OSM(string address)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "OSM Nominatim";

            if (String.IsNullOrWhiteSpace(address))
            {
                result.ResultsAvailable = false;
            }
            else
            {
                string url = "https://nominatim.openstreetmap.org/search?q=" + address.ToString() + "&format=json&polygon=0&addressdetails=1&email=" + _settings.ApiKeys.OSMApiKey;

                if (IncludeQueryURL) result.QueryURL = url;

                string data = "";
                try
                {
                    //enforce rate limiting
                    System.Threading.Thread.Sleep(1000);

                    //make api request
                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
                    client.Encoding = Encoding.GetEncoding("UTF-8");
                    data = client.DownloadString(url);

                    if (IncludeExtendedData) result.ExtendedData = data;

                    if (data == "[]")
                    {
                        result.ResultsAvailable = false;
                    }
                    else
                    {
                        JArray o = JArray.Parse(data);
                        result.Latitude = double.Parse(o.First["lat"].ToString());
                        result.Longitude = double.Parse(o.First["lon"].ToString());
                        result.Attribution = o.First["licence"].ToString();
                        result.ResultsAvailable = true;
                    }
                }
                catch (Exception)
                {
                    //oops
                }
            }
            return result;
        }

        public static LocationLookupResult GetLocationFromIP_FreegeoIP(string ipAddress)
        {
            if (!ipAddress.StartsWith("::"))
            {
                try
                {
                    WebClient url = new WebClient();

                    //http://freegeoip.net/json/{ip}
                    string urlString = "https://freegeoip.net/json/" + ipAddress;

                    string result = url.DownloadString(urlString);
                    LocationLookupResult lookup = JsonConvert.DeserializeObject<LocationLookupResult>(result);
                    lookup.SuccessfulLookup = true;

                    /* -- example result:
                      {
                       "ip":"116.240.210.146",
                       "country_code":"AU",
                       "country_name":"Australia",
                       "region_code":"08",
                       "region_name":"Western Australia",
                       "city":"Perth",
                       "zipcode":"",
                       "latitude":-31.9522,
                       "longitude":115.8614,
                       "metro_code":"",
                       "area_code":""
                    }
                     */
                    return lookup;
                }
                catch (Exception)
                {
                    ; ;//failed
                }
            }

            return new LocationLookupResult { SuccessfulLookup = false };
        }
    }
}