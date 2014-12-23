using OCM.API.Common.Model;
using OCM.API.Common.Model.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.MVC.Models
{
    public class POIViewModel
    {
        public ChargePoint POI {get;set;}
        public DataQualityReport DataQualityReport { get; set; }
        public UserComment NewComment { get; set; }
        public List<ChargePoint> POIListNearby { get; set; }
    }

}