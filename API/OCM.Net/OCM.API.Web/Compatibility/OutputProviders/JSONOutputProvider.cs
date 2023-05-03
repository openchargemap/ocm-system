using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace OCM.API.OutputProviders
{
    public class POINonComputedContractResolver : DefaultContractResolver
    {
        public static readonly POINonComputedContractResolver Instance = new POINonComputedContractResolver();

        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            // don't seralize computed properties
            if (property.DeclaringType == typeof(ChargePoint) && 
                property.PropertyName == "IsRecentlyVerified"
                ||
                property.PropertyName == "DataQualityLevel"
                )
            {
                property.ShouldSerialize = instance => false;
            }

            return property;
        }
    }

    public class JSONOutputProvider : OutputProviderBase, IOutputProvider
    {
        public JSONOutputProvider()
        {
            ContentType = "application/json";

        }

        public string PerformSerialisationToString(object graph, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings != null)
            {
                return JsonConvert.SerializeObject(graph, serializerSettings);
            }
            else
            {
                return JsonConvert.SerializeObject(graph);
            }
        }

        public async Task PerformSerialisationV2(System.IO.Stream outputStream, object graph, string jsCallbackName)
        {
            await PerformSerialisationV2(outputStream, graph, jsCallbackName, null);
        }

        [Obsolete]
        public void PerformBinarySerialisation(System.IO.Stream outputStream, object graph, string jsCallbackName)
        {
            //MemoryStream ms = new MemoryStream();
            JsonSerializer serializer = new JsonSerializer();

            // serialize product to BSON
            BsonWriter writer = new BsonWriter(outputStream);
            serializer.Serialize(writer, graph);
        }

        /// <summary>
        /// Serialise object using Json.NET
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="graph"></param>
        /// <param name="jsCallbackName"></param>
        public async Task PerformSerialisationV2(System.IO.Stream outputStream, object graph, string jsCallbackName, JsonSerializerSettings serializerSettings)
        {
            System.IO.StreamWriter s = new StreamWriter(outputStream);

            if (jsCallbackName != null)
            {
                await s.WriteAsync(jsCallbackName + "(");
            }

            string json = PerformSerialisationToString(graph, serializerSettings);
            await s.WriteAsync(json);

            if (jsCallbackName != null)
            {
                await s.WriteAsync(")");
            }

            await s.FlushAsync();
        }

        /// <summary>
        /// Serialise using DataContractSerializer
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="graph"></param>
        /// <param name="jsCallbackName"></param>
        /// <param name="jsonSerializer"></param>
        public async Task PerformSerialisationV1(System.IO.Stream outputStream, object graph, string jsCallbackName, DataContractJsonSerializer jsonSerializer)
        {
            System.IO.StreamWriter s = new StreamWriter(outputStream);

            if (jsCallbackName != null)
            {
                await s.WriteAsync(jsCallbackName + "(");
            }

            string json = "";

            using (var ms = new MemoryStream())
            {
                jsonSerializer.WriteObject(ms, graph);
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                json = sr.ReadToEnd();
                ms.Close();
            }

            await s.WriteAsync(json);

            if (jsCallbackName != null)
            {
                await s.WriteAsync(")");
            }

            await s.FlushAsync();
        }

        public async Task GetOutput(HttpContext context, System.IO.Stream outputStream, IEnumerable<Common.Model.ChargePoint> dataList, Common.APIRequestParams settings)
        {
            if (settings.APIVersion >= 2)
            {
                await PerformSerialisationV2(outputStream, dataList, settings.Callback, GetSerializerSettings(settings));
            }
            else
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<Common.Model.ChargePoint>));
                await PerformSerialisationV1(outputStream, dataList, settings.Callback, jsonSerializer);
            }
        }

        public async Task GetOutput(HttpContext context, Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestParams settings)
        {
            if (settings.APIVersion >= 2)
            {
                await PerformSerialisationV2(outputStream, data, settings.Callback, GetSerializerSettings(settings));
            }
            else
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Common.Model.CoreReferenceData));
                await PerformSerialisationV1(outputStream, data, settings.Callback, jsonSerializer);
            }
        }

        public async Task GetOutput(HttpContext context, Stream outputStream, Object data, Common.APIRequestParams settings)
        {
            await PerformSerialisationV2(outputStream, data, settings.Callback, GetSerializerSettings(settings));
        }

        private JsonSerializerSettings GetSerializerSettings(Common.APIRequestParams settings)
        {
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings();

            if (!settings.IsVerboseOutput)
            {
                jsonSettings.NullValueHandling = NullValueHandling.Ignore;
            }

            jsonSettings.Formatting = Formatting.Indented;

            if (settings.IsCompactOutput)
            {
                jsonSettings.Formatting = Formatting.None;
            }
            if (settings.IsCamelCaseOutput)
            {
                jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            if (settings.ExcludeComputedProperties)
            {
                jsonSettings.ContractResolver = new POINonComputedContractResolver();
            }

            return jsonSettings;
        }
    }
}