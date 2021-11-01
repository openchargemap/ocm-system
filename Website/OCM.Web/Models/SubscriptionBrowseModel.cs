using OCM.API.Common;
using OCM.API.Common.Model;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.MVC.Models
{
    public class SubscriptionBrowseModel
    {
        [DisplayName("Date From"), Required, DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateFrom
        {
            get;
            set;
        }
        public int? SubscriptionID { get; set; }
        public UserSubscription Subscription { get; set; }
        public SubscriptionMatchGroup SubscriptionResults { get; set; }
        public string SummaryHTML { get; set; }
    }
}