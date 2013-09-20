using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataProviderStatusType
    {
        public DataProviderStatusType()
        {
            this.DataProviders = new List<DataProvider>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public bool IsProviderEnabled { get; set; }
        public virtual ICollection<DataProvider> DataProviders { get; set; }
    }
}
