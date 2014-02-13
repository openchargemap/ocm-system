using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OCM.API.Common.Model
{
    public enum DistanceUnit
    {
        KM = 1,
        Miles = 2
    }

    public partial class AddressInfo : ICloneable
    {
        public int ID { get; set; }

        [DisplayName("Location Title"), StringLength(100), Required]
        public string Title { get; set; }

        [DisplayName("Address Line 1"), StringLength(1000), Required]
        public string AddressLine1 { get; set; }

        [DisplayName("Address Line 2"), StringLength(1000)]
        public string AddressLine2 { get; set; }

        [DisplayName("City/Town"), StringLength(100)]
        public string Town { get; set; }

        [DisplayName("State or Province"), StringLength(100)]
        public string StateOrProvince { get; set; }

        [DisplayName("Zip/Postcode"), StringLength(100), DataType(System.ComponentModel.DataAnnotations.DataType.PostalCode)]
        public string Postcode { get; set; }
        
        public int? CountryID { get; set; }
        [Required]
        public Country Country { get; set; }

        [DisplayName("Latitude"), Range(-90, 90), Required]
        public double Latitude { get; set; }
        [DisplayName("Longitude"), Range(-180, 180), Required]
        public double Longitude { get; set; }

        [DisplayName("Main Contact Phone"), StringLength(200), DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        public string ContactTelephone1 { get; set; }
        [DisplayName("Additional Contact Phone"), StringLength(200), DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        public string ContactTelephone2 { get; set; }

        [DisplayName("Public Email for Enquiries"), StringLength(500), DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string ContactEmail { get; set; }

        [DisplayName("Comments for Access/Directions")]
        public string AccessComments { get; set; }

        [DisplayName("Related Website"), StringLength(500), DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string RelatedURL { get; set; }

        public double? Distance { get; set; }
        public DistanceUnit DistanceUnit { get; set; }

        #region deprecated properties
        [Obsolete, JsonIgnore]
        public string GeneralComments { get; set; }
        #endregion

        public override string ToString()
        {
            string output = "";

            if (!String.IsNullOrWhiteSpace(this.AddressLine1)) output += this.AddressLine1+",";
            if (!String.IsNullOrWhiteSpace(this.AddressLine2)) output += this.AddressLine2 + ",";
            if (!String.IsNullOrWhiteSpace(this.Town)) output += this.Town + ",";
            if (!String.IsNullOrWhiteSpace(this.StateOrProvince)) output += this.StateOrProvince + ",";
            if (!String.IsNullOrWhiteSpace(this.Postcode)) output += this.Postcode + ",";
            if (this.Country!=null) output += this.Country.Title;
            return output;
        }

        public object Clone()
        {
            var address = new AddressInfo();
            address.ID = this.ID;
            address.Title = this.Title;
            address.AddressLine1 = this.AddressLine1;
            address.AddressLine2 = this.AddressLine2;
            address.Town = this.Town;
            address.StateOrProvince = this.StateOrProvince;
            address.Postcode = this.Postcode;
            address.CountryID = this.CountryID;
            address.Country = this.Country;
            address.Latitude = this.Latitude;
            address.Longitude = this.Longitude;
            address.ContactTelephone1 = this.ContactTelephone1;
            address.ContactTelephone2 = this.ContactTelephone2;
            address.ContactEmail = this.ContactEmail;
            address.AccessComments = this.AccessComments;
            address.RelatedURL = this.RelatedURL;
            address.Distance = this.Distance;
            address.DistanceUnit = this.DistanceUnit;
#pragma warning disable 0612
            address.GeneralComments = this.GeneralComments;
#pragma warning restore 0612

            return address;
        }
    }
}