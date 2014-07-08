using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace OCM.API.OutputProviders
{
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

        public void PerformSerialisationV2(System.IO.Stream outputStream, object graph, string jsCallbackName)
        {
            PerformSerialisationV2(outputStream, graph, jsCallbackName, null);
        }

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
        public void PerformSerialisationV2(System.IO.Stream outputStream, object graph, string jsCallbackName, JsonSerializerSettings serializerSettings)
        {
            System.IO.StreamWriter s = new StreamWriter(outputStream);

            if (jsCallbackName != null)
            {
                s.Write(jsCallbackName + "(");
            }

            string json = PerformSerialisationToString(graph, serializerSettings);
            s.Write(json);

            if (jsCallbackName != null)
            {
                s.Write(")");
            }

            s.Flush();
        }

        /// <summary>
        /// Serialise using DataContractSerializer
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="graph"></param>
        /// <param name="jsCallbackName"></param>
        /// <param name="jsonSerializer"></param>
        public void PerformSerialisationV1(System.IO.Stream outputStream, object graph, string jsCallbackName, DataContractJsonSerializer jsonSerializer)
        {
            System.IO.StreamWriter s = new StreamWriter(outputStream);

            if (jsCallbackName != null)
            {
                s.Write(jsCallbackName + "(");
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

            s.Write(json);

            if (jsCallbackName != null)
            {
                s.Write(")");
            }

            s.Flush();
        }

        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            if (settings.APIVersion >= 2)
            {
                PerformSerialisationV2(outputStream, dataList, settings.Callback, GetSerializerSettings(settings));
            }
            else
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<Common.Model.ChargePoint>));
                PerformSerialisationV1(outputStream, dataList, settings.Callback, jsonSerializer);
            }
        }

        public void GetOutput(Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestSettings settings)
        {
            if (settings.APIVersion >= 2)
            {
                PerformSerialisationV2(outputStream, data, settings.Callback, GetSerializerSettings(settings));
            }
            else
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(Common.Model.CoreReferenceData));
                PerformSerialisationV1(outputStream, data, settings.Callback, jsonSerializer);
            }
        }

        public void GetOutput(Stream outputStream, Object data, Common.APIRequestSettings settings)
        {
            PerformSerialisationV2(outputStream, data, settings.Callback, GetSerializerSettings(settings));
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
    }
}