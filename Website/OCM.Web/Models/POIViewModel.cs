using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using System.Collections.Generic;

namespace OCM.MVC.Models
{
    public class POIViewModel
    {
        public ChargePoint POI { get; set; }
        public DataQualityReport DataQualityReport { get; set; }
        public UserComment NewComment { get; set; }
        public List<ChargePoint> POIListNearby { get; set; }
    }

}