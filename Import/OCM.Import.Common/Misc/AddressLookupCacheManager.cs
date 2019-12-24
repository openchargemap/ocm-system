using Newtonsoft.Json;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.Import.Misc
{

    public class AddressLookupCacheItem
    {
        public DateTime CacheDate { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public AddressInfo AddressResult { get; set; }
    }

    public class AddressLookupCacheManager
    {
        public string AddressCacheDataFile = "AddressCache.json";
        public List<AddressLookupCacheItem> AddressCache = null;

        private string tempFolder = "";
        private API.Client.OCMClient _client;

        public AddressLookupCacheManager(string tmpPath, API.Client.OCMClient apiClient)
        {
            AddressCache = new List<AddressLookupCacheItem>();
            tempFolder = tmpPath;
            _client = apiClient;
        }

        public async Task<bool> LoadCache()
        {
            if (System.IO.File.Exists(tempFolder + "\\" + AddressCacheDataFile))
            {
                string cacheJSON = await System.IO.File.ReadAllTextAsync(tempFolder + "\\" + AddressCacheDataFile);
                AddressCache = JsonConvert.DeserializeObject<List<AddressLookupCacheItem>>(cacheJSON);
                return true;
            }

            return false;
        }

        public async Task<bool> SaveCache()
        {
            try
            {
                var output = AddressCache.OrderBy(c => c.Latitude).ThenBy(c => c.Longitude);
                string json = JsonConvert.SerializeObject(output);
                await System.IO.File.WriteAllTextAsync(tempFolder + "\\" + AddressCacheDataFile, json);

                //refresh in memory cache
                LoadCache();
                return true;
            }
            catch (Exception exp)
            {
                //failed
                LogHelper.Log("Failed to save address cache:" + exp.ToString());
            }

            return false;
        }

        public async Task<AddressLookupCacheItem> PerformLocationLookup(double latitude, double longitude)
        {

            try
            {

                //lookup item in existing cache, if not present create new item

                var cacheHit = AddressCache.FirstOrDefault(g => g.Latitude == latitude && g.Longitude == longitude);
                if (cacheHit != null)
                {
                    return cacheHit;
                }
                else
                {
                    var result = await _client.Geocode(latitude, longitude);
                    if (result?.ResultsAvailable == true)
                    {
                        var cacheItem = new AddressLookupCacheItem { AddressResult = result.AddressInfo, CacheDate = DateTime.Now, Latitude = latitude, Longitude = longitude };
                        AddressCache.Add(cacheItem);
                        return cacheItem;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                //failed
                return null;
            }
        }
    }
}