using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace OCM.MVC.App_Code
{

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

        public List<LocationImage> GetGeneralLocationImages(double latitude, double longitude)
        {
            List<LocationImage> imageList = new List<LocationImage>();

            //TODO: proper method of add/subtract a 2.5(?) mile window around the latitude, with caching of results
            double minx = longitude - 0.1;
            double maxx = longitude + 0.1;
            double miny = latitude - 0.1;
            double maxy = latitude + 0.1;
            string url = "http://www.panoramio.com/map/get_panoramas.php?set=full&from=0&to=10&minx=" + minx + "&miny=" + miny + "&maxx=" + maxx + "&maxy=" + maxy + "&size=medium&mapfilter=false";

            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                wc.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                try
                {
                    string result = wc.DownloadString(url);

                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    var parsedResult = jss.Deserialize<dynamic>(result);

                    var list = parsedResult["photos"];
                    foreach (var i in list)
                    {
                        var image = new LocationImage();
                        image.ImageID = i["photo_id"].ToString();
                        image.ImageURL = i["photo_file_url"].ToString();
                        image.DetailsURL = i["photo_url"].ToString();
                        image.Width = (int)double.Parse(i["width"].ToString());
                        image.Height = (int)double.Parse(i["height"].ToString());
                        image.Submitter = i["owner_name"].ToString();
                        image.SubmitterURL = i["owner_url"].ToString();
                        image.ImageRepositoryID = "Panoramio.com";

                        imageList.Add(image);
                    }

                }
                catch (Exception)
                {
                    //failed to get any images
                    return null;
                }
            }
            return imageList;
        }

        public GeocodingResult GeolocateAddressInfo_Google(AddressInfo address)
        {
            var result = GeolocateAddressInfo_Google(address.ToString());
            result.AddressInfoID = address.ID;

            return result;
        }

        public GeocodingResult GeolocateAddressInfo_Google(string address)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "Google Maps";

            if (!String.IsNullOrWhiteSpace(address))
            {

                string url = "http://maps.googleapis.com/maps/api/geocode/json?sensor=true&address=" + address;

                if (IncludeQueryURL) result.QueryURL = url;

                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                    try
                    {
                        string queryResult = wc.DownloadString(url);


                        if (IncludeExtendedData) result.ExtendedData = queryResult;

                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        var parsedResult = jss.Deserialize<dynamic>(queryResult);

                        string lat = parsedResult["results"][0]["geometry"]["location"]["lat"].ToString();
                        string lng = parsedResult["results"][0]["geometry"]["location"]["lng"].ToString();
                        string desc = parsedResult["results"][0]["formatted_address"].ToString();

                        result.Latitude = Double.Parse(lat);
                        result.Longitude = Double.Parse(lng);
                        result.Address = desc;

                        result.ResultsAvailable = true;

                    }
                    catch (Exception)
                    {
                        //failed to geocode
                        result.ResultsAvailable = false;
                    }
                }

            }

            return result;
        }

        public GeocodingResult GeolocateAddressInfo_MapquestOSM(AddressInfo address)
        {
            var result = GeolocateAddressInfo_MapquestOSM(address.ToString());
            result.AddressInfoID = address.ID;

            return result;
        }

        public GeocodingResult GeolocateAddressInfo_MapquestOSM(string address)
        {
            GeocodingResult result = new GeocodingResult();
            result.Service = "MapQuest Open";

            string url = "http://open.mapquestapi.com/geocoding/v1/address?key=" + ConfigurationManager.AppSettings["MapQuestOpen_API_Key"] + "&location=" + Uri.EscapeDataString(address.ToString());
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

                string url = "http://nominatim.openstreetmap.org/search?q=" + address.ToString() + "&format=json&polygon=0&addressdetails=1&email=" + ConfigurationManager.AppSettings["OSM_API_Key"];
                
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

    }
}