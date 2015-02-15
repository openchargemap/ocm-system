using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Core.Util
{
    public static class MongoExtensions
    {
        public static dynamic ToDynamic(this BsonDocument doc)
        {
            // Credit: http://mikaelkoskinen.net/mongodb-aggregation-framework-examples-in-c-
            var json = doc.ToJson();
            dynamic obj = JToken.Parse(json);
            return obj;
        }
    }
}
