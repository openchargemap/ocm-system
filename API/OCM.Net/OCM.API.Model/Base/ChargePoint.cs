﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OCM.API.Common.Model
{
    /// <summary>
    /// Core POI details (without expanded object properties)
    /// </summary>
    public class POIDetails
    {
        [DisplayName("OCM Ref")]
        public int ID { get; set; }

        [DisplayName("Unique ID")]
        [StringLength(100)]
        public string UUID { get; set; }

        [DisplayName("Parent POI")]
        public int? ParentChargePointID { get; set; }

        public int? DataProviderID { get; set; }

        [DisplayName("Data Providers Reference")]
        [StringLength(100)]
        public string DataProvidersReference { get; set; }

        public int? OperatorID { get; set; }

        [DisplayName("Operators Own Ref")]
        [StringLength(100)]
        public string OperatorsReference { get; set; }

        public int? UsageTypeID { get; set; }

        [DisplayName("Usage Cost")]
        [StringLength(200)]
        public string UsageCost { get; set; }

        [DisplayName("Nearest Address")]
        public AddressInfo AddressInfo { get; set; }

        [DisplayName("Equipment Info")]
        public List<ConnectionInfo> Connections { get; set; }

        [DisplayName("Number Of Stations/Bays")]
        [Range(0, 500)]
        public int? NumberOfPoints { get; set; }

        [DisplayName("General Comments"), DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        public string GeneralComments { get; set; }

        [DisplayName("Date Planned")]
        public DateTime? DatePlanned { get; set; }

        [DisplayName("Date Last Confirmed")]
        public DateTime? DateLastConfirmed { get; set; }

        public int? StatusTypeID { get; set; }

        [DisplayName("Date Status Last Updated")]
        public DateTime? DateLastStatusUpdate { get; set; }

        [DisplayName("Metadata")]
        public List<MetadataValue> MetadataValues { get; set; }

        [DisplayName("Data Quality Level")]
        [Range(1, 5)]
        public int? DataQualityLevel { get; set; }

        [DisplayName("Data Added")]
        public DateTime? DateCreated { get; set; }

        public int? SubmissionStatusTypeID { get; set; }

        [JsonIgnoreAttribute]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool AddressCleaningRequired { get; set; }

    }

    public class ChargePoint : POIDetails
    {
        public ChargePoint() { }
        public ChargePoint(POIDetails poi)
        {
            ID = poi.ID;
            UUID = poi.UUID;
            ParentChargePointID = poi.ParentChargePointID;
            DataProviderID = poi.DataProviderID;
            DataProvidersReference = poi.DataProvidersReference;
            OperatorID = poi.OperatorID;
            OperatorsReference = poi.OperatorsReference;
            UsageTypeID = poi.UsageTypeID;
            UsageCost = poi.UsageCost;
            AddressInfo = poi.AddressInfo;
            Connections = poi.Connections;
            NumberOfPoints = poi.NumberOfPoints;
            GeneralComments = poi.GeneralComments;
            DatePlanned = poi.DatePlanned;
            DateLastConfirmed = poi.DateLastConfirmed;
            StatusTypeID = poi.StatusTypeID;
            DateLastStatusUpdate = poi.DateLastStatusUpdate;
            MetadataValues = poi.MetadataValues;
            DataQualityLevel = poi.DataQualityLevel;
            DateCreated = poi.DateCreated;
            SubmissionStatusTypeID = poi.SubmissionStatusTypeID;
            AddressCleaningRequired = poi.AddressCleaningRequired;
        }

        [DisplayName("Data Provider")]
        public DataProvider DataProvider { get; set; }

        [DisplayName("Network/Operator")]
        public OperatorInfo OperatorInfo { get; set; }

        [DisplayName("Usage Type")]
        public UsageType UsageType { get; set; }

        [DisplayName("Operational Status")]
        public StatusType StatusType { get; set; }

        [DisplayName("Submission Status")]
        public SubmissionStatusType SubmissionStatus { get; set; }

        [DisplayName("Comments/Checkins")]
        public List<UserComment> UserComments { get; set; }

        [DisplayName("% Similarity")]
        public int? PercentageSimilarity { get; set; }

        [DisplayName("Media Items")]
        public List<MediaItem> MediaItems { get; set; }

        /// <summary>
        /// Level of detail (map priority) for internal use only
        /// </summary>
        [JsonIgnoreAttribute]
        [System.Text.Json.Serialization.JsonIgnore]
        public int? LevelOfDetail { get; set; }

        #region deprecated properties

        [Obsolete, JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<ChargerInfo> Chargers { get; set; }

        [Obsolete, JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string MetadataTags { get; set; }

        #endregion deprecated properties

#if !PORTABLE

        /// <summary>
        /// Get a simple summary description of the Charge Point including address/access info
        /// </summary>
        /// <param name="UseHTML"></param>
        /// <returns></returns>
        public string GetSummaryDescription(bool UseHTML)
        {
            string description = "";
            string address = "";

            address = GetAddressSummary(UseHTML);

            if (!String.IsNullOrEmpty(address)) description = (!String.IsNullOrEmpty(description) ? description : "") + "<p>" + address + "</p>";

            if (!String.IsNullOrEmpty(GeneralComments)) description += "<p><em>" + this.GeneralComments + "</em></p>";

            if (this.AddressInfo != null)
            {
                if (this.AddressInfo.AccessComments != null && this.AddressInfo.AccessComments != this.GeneralComments) description += "<p>" + this.AddressInfo.AccessComments + "</p>";
            }

            if (this.Connections != null)
            {
                if (this.Connections.Count > 0)
                {
                    description += "<ul>";
                    foreach (var c in this.Connections)
                    {
                        description += "<li>" + c.ToString() + "</li>";
                    }
                    description += "</ul>";
                }
            }

            if (this.StatusType != null)
            {
                description += "<p>Status: " + this.StatusType.Title + "</p>";
            }

            if (this.UsageType != null)
            {
                description += "<p>Usage: " + this.UsageType.Title + "</p>";
            }

            description += "<a href=\"https://openchargemap.org/site/poi/details/" + this.ID + "\">View More Details (OCM-" + this.ID + ")</a>";

            if (this.DataProvider != null)
            {
                var dataProviderText = this.DataProvider.Title;
                if (!String.IsNullOrEmpty(this.DataProvider.WebsiteURL)) dataProviderText = "<a href\"" + this.DataProvider.WebsiteURL + "\">" + this.DataProvider.Title + "</a>";
                description += "<p><small>Data Provider: " + dataProviderText + (!String.IsNullOrEmpty(this.DataProvider.License) ? " - " + this.DataProvider.License : "") + "</small></p>";
            }

            return description;
        }

#endif

        public string GetAddressSummary(bool UseHTML, bool fullDetails = false)
        {
            string address = "";

            if (this.AddressInfo != null)
            {
                if (this.AddressInfo.AddressLine1 != null) address += "\r\n" + this.AddressInfo.AddressLine1;
                if (this.AddressInfo.AddressLine2 != null) address += "\r\n" + this.AddressInfo.AddressLine2;
                if (this.AddressInfo.Town != null) address += "\r\n" + this.AddressInfo.Town;
                if (this.AddressInfo.StateOrProvince != null) address += "\r\n" + this.AddressInfo.StateOrProvince;
                if (this.AddressInfo.Postcode != null) address += "\r\n" + this.AddressInfo.Postcode;
                if (this.AddressInfo.Country != null) address += "\r\n" + this.AddressInfo.Country.Title;
                if (fullDetails)
                {
                    if (this.AddressInfo.Title != null) address = this.AddressInfo.Title + "\r\n" + address;
                    if (this.AddressInfo.AccessComments != null) address += "\r\n" + this.AddressInfo.AccessComments;
                    if (this.AddressInfo.ContactEmail != null) address += "\r\n" + this.AddressInfo.ContactEmail;
                    if (this.AddressInfo.ContactTelephone1 != null) address += "\r\n" + this.AddressInfo.ContactTelephone1;
                    if (this.AddressInfo.ContactTelephone2 != null) address += "\r\n" + this.AddressInfo.ContactTelephone2;
                    if (this.AddressInfo.RelatedURL != null) address += "\r\n" + this.AddressInfo.RelatedURL;
                    if (this.AddressInfo.GeneralComments != null) address += "\r\n" + this.AddressInfo.GeneralComments;
                }
            }

            return address;
        }

        public bool IsRecentlyVerified
        {
            get
            {
                if (DateLastVerified > DateTime.UtcNow.AddMonths(-6)) return true;
                else return false;
            }
        }

        public DateTime? DateLastVerified
        {
            get
            {
                // get max date where data was last reviewed or had a contribution. Date Created is considered up to 6 months from creation otherwise it is not counted.

                var keyDates = new DateTime?[] { this.DateCreated > DateTime.UtcNow.AddMonths(-6) ? this.DateCreated : null, this.DateLastStatusUpdate, this.DateLastConfirmed };
                return keyDates.Max();

                //positive comments within last 6 months
                /*if (this.UserComments != null && this.UserComments.Any(u => u.CheckinStatusType != null && u.CheckinStatusType.IsPositive == true && u.DateCreated > DateTime.UtcNow.AddMonths(-6)))
                {
                    return this.UserComments.Where(u => u.CheckinStatusType != null && u.CheckinStatusType.IsPositive == true).Max(u => u.DateCreated);
                }*/
            }
        }
    }
}