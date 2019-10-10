using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace OCM.Core.Data
{
    public partial class AddressInfo
    {
        public AddressInfo()
        {
            ChargePoints = new HashSet<ChargePoint>();
            Operators = new HashSet<Operator>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string StateOrProvince { get; set; }
        public string Postcode { get; set; }
        public int CountryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactEmail { get; set; }
        public string AccessComments { get; set; }
        public string GeneralComments { get; set; }
        public string RelatedUrl { get; set; }
        public Geometry SpatialPosition { get; set; }

        public virtual Country Country { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<Operator> Operators { get; set; }
    }
}
