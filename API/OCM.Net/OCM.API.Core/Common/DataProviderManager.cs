using System;
using System.Linq;

namespace OCM.API.Common
{
    public class DataProviderManager : ManagerBase
    {
        public void UpdateDateLastImport(int dataProviderID)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderID);
            dataProvider.DateLastImported = DateTime.UtcNow;
            dataModel.SaveChanges();
        }
    }
}
