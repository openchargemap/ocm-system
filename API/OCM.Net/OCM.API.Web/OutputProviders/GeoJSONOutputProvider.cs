using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using OCM.API.Common.Model;

namespace OCM.API.OutputProviders
{
    public class GeoJSONOutputProvider : OutputProviderBase, IOutputProvider
    {
        public GeoJSONOutputProvider()
        {
            ContentType = "application/json";
        }

        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            if (settings.APIVersion >= 2)
            {
                var featureList = new List<GeoJSON.Net.Feature.Feature>();
                foreach (var poi in dataList)
                {
                    var point = new GeoJSON.Net.Geometry.Point(new GeoJSON.Net.Geometry.GeographicPosition(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));
                    ConnectionInfo maxConnection = null;
                    if (poi.Connections != null)
                    {
                        maxConnection = poi.Connections.OrderByDescending(p => p.LevelID).FirstOrDefault();
                    }

                    var featureProperties = new Dictionary<string, object> { 
                        { "Name", poi.AddressInfo.Title},
                        { "Description", poi.AddressInfo.ToString()},
                        { "URL", "http://openchargemap.org/site/poi/details/"+poi.ID},
                        { "Level", (maxConnection!=null && maxConnection.LevelID!=null?maxConnection.LevelID.ToString():null)},
                        { "ConnectionType", (maxConnection!=null && maxConnection.ConnectionType!=null?maxConnection.ConnectionType.Title:null)},
                    };

                    if (settings.IsVerboseOutput) featureProperties.Add("POI", poi);

                    var feature = new GeoJSON.Net.Feature.Feature(point, featureProperties, poi.ID.ToString());
                    featureList.Add(feature);
                }

                //convert list ot GeoJSON FeatureCollection

                GeoJSON.Net.Feature.FeatureCollection collection = new GeoJSON.Net.Feature.FeatureCollection(featureList);

                System.IO.StreamWriter s = new StreamWriter(outputStream);

                if (settings.Callback != null)
                {
                    s.Write(settings.Callback + "(");
                }

                var serializerSettings = GetSerializerSettings(settings);

                //enforce camelcasing
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializerSettings.NullValueHandling = NullValueHandling.Ignore;

                string json = JsonConvert.SerializeObject(collection, serializerSettings);
                s.Write(json);

                if (settings.Callback != null)
                {
                    s.Write(")");
                }

                s.Flush();
            }
        }

        private JsonSerializerSettings GetSerializerSettings(Common.APIRequestSettings settings)
        {
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings();

            if (!settings.IsVerboseOutput)
            {
                jsonSettings.NullValueHandling = NullValueHandling.Ignore;
            }

            jsonSettings.Formatting = Formatting.Indented;

            if (settings.IsCamelCaseOutput)
            {
                jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            return jsonSettings;
        }


        public void GetOutput(Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }

        public void GetOutput(Stream outputStream, object data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}