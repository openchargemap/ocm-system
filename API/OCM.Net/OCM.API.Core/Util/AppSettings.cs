using System;
using System.Collections.Generic;
using System.Text;

namespace OCM.Core.Settings
{
    public class CoreSettings
    {
        public string CachePath { get; set; }
        public bool EnableDataWrites { get; set; }
        public bool IsCacheOnlyMode { get; set; }
        public bool RefreshCacheOnLoad { get; set; }
        public string DataSourceAPIBaseUrl { get; set; }
        public ApiKeys ApiKeys { get; set; }
        public MongoDbSettings MongoDBSettings { get; set; }
    }

    public class ApiKeys
    {
        public string MapQuestOpenAPIKey { get; set; }
        public string OSMApiKey { get; set; }
        public string OCMApiKey { get; set; }
    }
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }

        public int MaxCacheAgeMinutes { get; set; }
        public int CacheSyncBatchSize { get; set; } = 10000;
    }
}
