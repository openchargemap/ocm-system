using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class DataProviderUser
    {
        public int ID { get; set; }
        public int DataProviderID { get; set; }
        public int UserID { get; set; }
        public bool IsDataProviderAdmin { get; set; }
        public virtual DataProvider DataProvider { get; set; }
        public virtual User User { get; set; }
    }
}
