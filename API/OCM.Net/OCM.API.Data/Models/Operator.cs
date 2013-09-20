using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class Operator
    {
        public Operator()
        {
            this.ChargePoints = new List<ChargePoint>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }
        public string PhonePrimaryContact { get; set; }
        public string PhoneSecondaryContact { get; set; }
        public Nullable<bool> IsPrivateIndividual { get; set; }
        public Nullable<int> AddressInfoID { get; set; }
        public string BookingURL { get; set; }
        public string ContactEmail { get; set; }
        public string FaultReportEmail { get; set; }
        public Nullable<bool> IsRestrictedEdit { get; set; }
        public virtual AddressInfo AddressInfo { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
    }
}
