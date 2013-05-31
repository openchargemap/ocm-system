using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class DataProvider : SimpleReferenceDataType
    {
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }
        public DataProviderStatusType DataProviderStatusType { get; set; }
    }
}