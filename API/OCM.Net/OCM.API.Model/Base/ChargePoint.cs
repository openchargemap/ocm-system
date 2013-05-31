using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class ChargePoint
    {
        public int ID { get; set; }
        public string UUID { get; set; }
        public DataProvider DataProvider { get; set; }
        public string DataProvidersReference { get; set; }
        public OperatorInfo OperatorInfo { get; set; }
        public string OperatorsReference { get; set; }
        public UsageType UsageType { get; set; }
        public string UsageCost { get; set; }
        public AddressInfo AddressInfo { get; set; }
        public int? NumberOfPoints { get; set; }
        public string GeneralComments { get; set; }
        public DateTime? DatePlanned { get; set; }

        [DisplayName("Date Last Confirmed")]
        public DateTime? DateLastConfirmed { get; set; }

        [DisplayName("Operational Status")]
        public StatusType StatusType { get; set; }

        [DisplayName("Date Status Last Updated")]
        public DateTime? DateLastStatusUpdate { get; set; }

        public int? DataQualityLevel { get; set; }
        public DateTime? DateCreated { get; set; }
        public User Contributor { get; set; }

        public SubmissionStatusType SubmissionStatus { get; set; }
        public List<UserComment> UserComments { get; set; }
        public int? PercentageSimilarity { get; set; }
        public List<ConnectionInfo> Connections { get; set; }
        public List<MediaItem> MediaItems { get; set; }

        #region deprecated properties
        [JsonIgnore]
        public List<ChargerInfo> Chargers { get; set; }
        [JsonIgnore]
        public string MetadataTags { get; set; }
        #endregion

        /// <summary>
        /// Get a simple summary description of the Charge Point including address/access info
        /// </summary>
        /// <param name="UseHTML"></param>
        /// <returns></returns>
        public string GetSummaryDescription(bool UseHTML)
        {
            string description = "";
            string address = "";
            string newline = "\r\n";
            if (UseHTML) newline = "<br/>";

            address = GetAddressSummary(UseHTML);

            if (!String.IsNullOrEmpty(address)) description = (!String.IsNullOrEmpty(description) ? description + newline : "") + address;

            if (!String.IsNullOrEmpty(GeneralComments)) description += newline + "<em>" + this.GeneralComments + "</em>";

            if (this.AddressInfo != null)
            {
                if (this.AddressInfo.AccessComments != null) description += newline + this.AddressInfo.AccessComments;
            }

            if (this.Connections != null)
            {
                if (this.Connections.Count > 0)
                {
                    description += newline+"Equipment:";
                    foreach (var c in this.Connections)
                    {
                        description += newline;
                        if (c.Level != null) description += "Level: " + c.Level.Title;
                        if (c.ConnectionType != null) description += " Connection Type:" + c.ConnectionType.Title;
                    }
                }
            }

            if (this.StatusType != null)
            {
                description += newline + "Status: " + this.StatusType.Title + " Last Updated " + (this.DateLastStatusUpdate.HasValue ? this.DateLastStatusUpdate.Value.ToShortDateString() : "");
            }

            description += newline + "<a href=\"http://openchargemap.org/site/poi/details/" + this.ID +
                           "\">View Details (OCM-" + this.ID + ")</a>";
            return description;
        }

        public string GetAddressSummary(bool UseHTML)
        {
            string address = "";

            if (this.AddressInfo != null)
            {
                if (this.AddressInfo.AddressLine1 != null) address += "\r\n" + this.AddressInfo.AddressLine1;
                if (this.AddressInfo.AddressLine2 != null) address += "\r\n" + this.AddressInfo.AddressLine2;
                if (this.AddressInfo.Town != null) address += "\r\n" + this.AddressInfo.Town;
                if (this.AddressInfo.StateOrProvince != null) address += "\r\n" + this.AddressInfo.StateOrProvince;
                if (this.AddressInfo.Postcode != null) address += "\r\n" + this.AddressInfo.Postcode;
            }

            return address;
        }
    }
}