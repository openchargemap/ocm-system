using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task GetOutput(HttpContext context, System.IO.Stream outputStream, IEnumerable<Common.Model.ChargePoint> dataList, Common.APIRequestParams settings)
        {
            if (settings.APIVersion >= 2)
            {
                var featureCollection = new GeoJSONFeatureCollection();
                foreach (var poi in dataList)
                {
                    var feature = new GeoJSONFeature();
                    feature.ID = poi.ID.ToString();
                    feature.Geometry.Coordinates = new double[] { poi.AddressInfo.Latitude, poi.AddressInfo.Longitude };

                    Common.Model.ConnectionInfo maxConnection = null;
                    if (poi.Connections != null)
                    {
                        maxConnection = poi.Connections.OrderByDescending(p => p.LevelID).FirstOrDefault();
                    }

                    var featureProperties = new Dictionary<string, object> {
                        { "Name", poi.AddressInfo.Title},
                        { "Description", poi.AddressInfo.ToString()},
                        { "URL", "https://openchargemap.org/site/poi/details/"+poi.ID},
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
                    await s.WriteAsync(settings.Callback + "(");
                }

                var serializerSettings = GetSerializerSettings(settings);

                //enforce camelcasing
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializerSettings.NullValueHandling = NullValueHandling.Ignore;

                string json = JsonConvert.SerializeObject(featureCollection, serializerSettings);
                await s.WriteAsync(json);

                if (settings.Callback != null)
                {
                    await s.WriteAsync(")");
                }

                await s.FlushAsync();
            }
        }

        private JsonSerializerSettings GetSerializerSettings(Common.APIRequestParams settings)
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

        public Task GetOutput(HttpContext context, Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestParams settings)
        {
            throw new NotImplementedException();
        }

        public Task GetOutput(HttpContext context, Stream outputStream, object data, Common.APIRequestParams settings)
        {
            throw new NotImplementedException();
        }
    }
}