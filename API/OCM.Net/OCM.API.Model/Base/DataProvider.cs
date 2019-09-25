using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class DataProvider : SimpleReferenceDataType
    {
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteURL { get; set; }

        public string Comments { get; set; }
        public DataProviderStatusType DataProviderStatusType { get; set; }
        public bool IsRestrictedEdit { get; set; }
        public bool? IsOpenDataLicensed { get; set; }
        public bool? IsApprovedImport { get; set; }
        public string License { get; set; }
        public DateTime? DateLastImported { get; set; }

        public override string ToString()
        {
            return this.Title;
        }
    }
}