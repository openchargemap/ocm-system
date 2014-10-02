using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model.Extended
{
    public class POIDetailsCache
    {
        public DateTime DateCached { get; set; }
        public ChargePoint POI { get; set; }
        public List<ChargePoint> POIListNearby { get; set; }
    }
}
