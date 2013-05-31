using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class OperatorInfo : SimpleReferenceDataType
    {
        public string WebsiteURL { get; set; }
        public string Comments { get; set; }
        public string PhonePrimaryContact { get; set; }
        public string PhoneSecondaryContact { get; set; }
        public bool? IsPrivateIndividual { get; set; }
        public AddressInfo AddressInfo { get; set; }
        public string BookingURL { get; set; }
        public string ContactEmail { get; set; }
        public string FaultReportEmail { get; set; }
        public bool? IsRestrictedEdit { get; set; }
    }
}