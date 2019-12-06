using System;
using System.Collections.Generic;
using System.Text;

namespace OCM.Core.Settings
{
    public class CoreSettings
    {
        public string CachePath { get; set; }
        public bool EnableDataWrites { get; set; }
        public ApiKeys ApiKeys { get; set; }
        public MongoDbSettings MongoDBSettings { get; set; }
    }

    public class ApiKeys
    {
        public string MapQuestOpenAPIKey { get; set; }

        public string OSMApiKey { get; set; }
    }
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }

        public int MaxCacheAgeMinutes { get; set; }
    }
}
