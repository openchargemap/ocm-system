using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model.Extended
{
    public class GeocodingResult
    {
        public int AddressInfoID { get; set; }
        public bool ResultsAvailable { get; set; }
        public string Service { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string QueryURL { get; set; }
        public string ExtendedData { get; set; }
        public string Address { get; set; }
        public string Attribution { get; set; }

        public AddressInfo AddressInfo { get; set; }
    }

}
