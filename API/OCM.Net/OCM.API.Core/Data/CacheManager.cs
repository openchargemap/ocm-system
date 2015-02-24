using OCM.API.Common;
using OCM.API.Common.Model.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Core.Data
{
    public enum CacheUpdateStrategy
    {
        All,
        Modified,
        Incremental
    }

    public class CacheManager
    {
        public static OCM.API.Common.Model.ChargePoint GetPOI(int id)
        {
            try
            {
                return new CacheProviderMongoDB().GetPOI(id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static OCM.API.Common.Model.CoreReferenceData GetCoreReferenceData()
        {
            try
            {
                return new CacheProviderMongoDB().GetCoreReferenceData();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<OCM.API.Common.Model.ChargePoint> GetPOIList(APIRequestParams filter)
        {
            try
            {
                return new CacheProviderMongoDB().GetPOIList(filter);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async static Task<MirrorStatus> RefreshCachedData(CacheUpdateStrategy updateStrategy = CacheUpdateStrategy.Modified)
        {
            try
            {
                return await new CacheProviderMongoDB().PopulatePOIMirror(updateStrategy);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async static Task<MirrorStatus> GetCacheStatus(bool includeDupeCheck =false)
        {
            try
            {
                return await Task.Run<MirrorStatus>(() =>
                {
                    return new CacheProviderMongoDB().GetMirrorStatus(includeDupeCheck, true);
                });
                
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async static Task<MirrorStatus> RefreshCachedPOI(int poiId)
        {
            if (poiId == 0) {
                return await RefreshCachedData();
            }

            try
            {
                return await new CacheProviderMongoDB().PopulatePOIMirror(CacheUpdateStrategy.Modified);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static List<CountryExtendedInfo> GetExtendedCountryInfo()
        {
            return new CacheProviderMongoDB().GetExtendedCountryInfo();
        }
    }
}
