using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace OCM.MVC.App_Code
{
    public class GeoPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
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
                        image.Width = int.Parse(i["width"].ToString());
                        image.Height = int.Parse(i["height"].ToString());
                        image.Submitter = i["owner_name"].ToString();
                        image.SubmitterURL = i["owner_url"].ToString();
                        image.ImageRepositoryID = "Panoramio.com";
                        /*"photo_id": 505229,
                              "photo_title": "Etangs près de Dijon",
                              "photo_url": "http://www.panoramio.com/photo/505229",
                              "photo_file_url": "http://static2.bareka.com/photos/medium/505229.jpg",
                              "longitude": 5.168552,
                              "latitude": 47.312642,
                              "width": 350,
                              "height": 500,
                              "upload_date": "20 January 2007",
                              "owner_id": 78506,
                              "owner_name": "Philippe Stoop",
                              "owner_url": "http://www.panoramio.com/user/78506"
                         * */
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

        public GeoPoint GeoCodeFromString(string address)
        {
            if (String.IsNullOrWhiteSpace(address)) return null;

            GeoPoint point = null;
            string url = "http://maps.googleapis.com/maps/api/geocode/json?sensor=true&address=" + address;

            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                wc.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                try
                {
                    string result = wc.DownloadString(url);

                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    var parsedResult = jss.Deserialize<dynamic>(result);

                    string lat = parsedResult["results"][0]["geometry"]["location"]["lat"].ToString();
                    string lng = parsedResult["results"][0]["geometry"]["location"]["lng"].ToString();
                    string desc = parsedResult["results"][0]["formatted_address"].ToString();
                    point = new GeoPoint();
                    point.Latitude = Double.Parse(lat);
                    point.Longitude = Double.Parse(lng);
                    point.Address = desc;

                }
                catch (Exception)
                {
                    //failed to geocode
                    return null;
                }
            }


            /*JToken
            dynamic googleResults = new Uri(url + address).GetDynamicJsonObject();
            foreach (var result in googleResults.results)
            {
                Console.WriteLine("[" + result.geometry.location.lat + "," + result.geometry.location.lng + "] " + result.formatted_address);
            }*/

            return point;
        }
    }
}