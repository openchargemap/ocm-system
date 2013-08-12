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

    public partial class AddressInfo
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
        public Country Country { get; set; }

        [DisplayName("Latitude"), Range(-90, 90), Required]
        public double? Latitude { get; set; }
        [DisplayName("Longitude"), Range(-180, 180), Required]
        public double? Longitude { get; set; }

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

        public string ToString()
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
    }
}