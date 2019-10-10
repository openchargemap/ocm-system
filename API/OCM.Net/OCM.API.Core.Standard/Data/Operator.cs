using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class Operator
    {
        public Operator()
        {
            ChargePoints = new HashSet<ChargePoint>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string WebsiteUrl { get; set; }
        public string Comments { get; set; }
        public string PhonePrimaryContact { get; set; }
        public string PhoneSecondaryContact { get; set; }
        public bool? IsPrivateIndividual { get; set; }
        public int? AddressInfoId { get; set; }
        public string BookingUrl { get; set; }
        public string ContactEmail { get; set; }
        public string FaultReportEmail { get; set; }
        public bool? IsRestrictedEdit { get; set; }

        public virtual AddressInfo AddressInfo { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
    }
}
