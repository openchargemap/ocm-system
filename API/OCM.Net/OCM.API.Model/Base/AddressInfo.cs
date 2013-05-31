using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public enum DistanceUnit
    {
        KM = 1,
        Miles = 2
    }

    public partial class AddressInfo
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string StateOrProvince { get; set; }
        public string Postcode { get; set; }
        public Country Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactEmail { get; set; }
        public string AccessComments { get; set; }
        public string GeneralComments { get; set; }
        public string RelatedURL { get; set; }
        public double? Distance { get; set; }
        public DistanceUnit DistanceUnit { get; set; }
    }
}