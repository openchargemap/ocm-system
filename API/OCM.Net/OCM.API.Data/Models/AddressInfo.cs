using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class AddressInfo
    {
        public AddressInfo()
        {
            this.ChargePoints = new List<ChargePoint>();
            this.Operators = new List<Operator>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string StateOrProvince { get; set; }
        public string Postcode { get; set; }
        public Nullable<int> CountryID { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactEmail { get; set; }
        public string AccessComments { get; set; }
        public string GeneralComments { get; set; }
        public string RelatedURL { get; set; }
        public virtual Country Country { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        public virtual ICollection<Operator> Operators { get; set; }
    }
}
