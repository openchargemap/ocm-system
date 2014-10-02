using OCM.API.Common;
using OCM.API.Common.Model.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.Core.Data
{
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

        public static List<OCM.API.Common.Model.ChargePoint> GetPOIList(APIRequestSettings filter)
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

        public async static Task<MirrorStatus> RefreshCachedPOIList()
        {
            try
            {
                return await new CacheProviderMongoDB().PopulatePOIMirror(CacheProviderMongoDB.CacheUpdateStrategy.Modified);
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
