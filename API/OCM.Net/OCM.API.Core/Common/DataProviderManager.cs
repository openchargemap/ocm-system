using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class DataProviderManager: ManagerBase
    {
        public void UpdateDateLastImport(int dataProviderID)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp=>dp.Id==dataProviderID);
            dataProvider.DateLastImported = DateTime.UtcNow;
            dataModel.SaveChanges();
        }
    }
}
