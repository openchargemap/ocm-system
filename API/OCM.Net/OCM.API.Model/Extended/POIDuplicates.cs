using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model.Extended
{
    /// <summary>
    /// A single duplicate POI with text reasons why they item is considered a duplicate
    /// </summary>
    public class DuplicatePOIItem
    {
        public ChargePoint DuplicatePOI { get; set; }
        public ChargePoint DuplicateOfPOI { get; set; }
        public List<string> Reasons { get; set; }
        public int Confidence { get; set; }
    }

    /// <summary>
    /// A group of duplicate POIs with suggested best POI to merge/resolve to
    /// </summary>
    public class DuplicatePOIGroup
    {
        public ChargePoint SuggestedBestPOI { get; set; }
        public List<DuplicatePOIItem> DuplicatePOIList {get;set;}
        public List<ChargePoint> AllPOI { get; set; }
    }

    /// <summary>
    /// summary of all groups of duplicate POIs
    /// </summary>
    public class POIDuplicates {
        public List<DuplicatePOIGroup> DuplicateSummaryList { get; set; }
    }

}
