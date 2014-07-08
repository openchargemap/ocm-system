using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OCM.API.OutputProviders
{
    public class GeoJSONGeometry
    {
        public GeoJSONGeometry()
        {
            this.Coordinates = new double[] { 0, 0 };
            this.Type = "Point";
        }

        public string Type { get; set; }

        public double[] Coordinates { get; set; }
    }

    public class GeoJSONFeature
    {
        public GeoJSONFeature()
        {
            this.Geometry = new GeoJSONGeometry();
            this.Properties = new Dictionary<string, object>();
        }

        public string ID { get; set; }

        public GeoJSONGeometry Geometry { get; set; }

        public Dictionary<string, object> Properties { get; set; }
    }

    public class GeoJSONFeatureCollection
    {
        public GeoJSONFeatureCollection()
        {
            this.Features = new List<GeoJSONFeature>();
        }

        public List<GeoJSONFeature> Features { get; set; }
    }

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
                var featureCollection = new GeoJSONFeatureCollection();
                foreach (var poi in dataList)
                {
                    var feature = new GeoJSONFeature();
                    feature.ID = poi.ID.ToString();
                    feature.Geometry.Coordinates = new double[] { poi.AddressInfo.Latitude, poi.AddressInfo.Longitude };

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
                    feature.Properties = featureProperties;

                    featureCollection.Features.Add(feature);
                }

                System.IO.StreamWriter s = new StreamWriter(outputStream);

                if (settings.Callback != null)
                {
                    s.Write(settings.Callback + "(");
                }

                var serializerSettings = GetSerializerSettings(settings);

                //enforce camelcasing
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializerSettings.NullValueHandling = NullValueHandling.Ignore;

                string json = JsonConvert.SerializeObject(featureCollection, serializerSettings);
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