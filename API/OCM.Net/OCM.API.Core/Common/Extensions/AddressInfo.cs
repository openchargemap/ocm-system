using System;

namespace OCM.API.Common.Model.Extensions
{
    public class AddressInfo
    {
        public static Model.AddressInfo FromDataModel(Core.Data.AddressInfo source, bool isVerboseMode)
        {
            if (source == null) return null;

            Model.AddressInfo a = new Model.AddressInfo();
            a.ID = source.Id;
            a.Title = source.Title;
            a.AddressLine1 = source.AddressLine1;
            a.AddressLine2 = source.AddressLine2;
            a.Town = source.Town;
            a.StateOrProvince = source.StateOrProvince;
            a.Postcode = source.Postcode;

            //populate country (full object or id only)
            if (isVerboseMode)
            {
                a.Country = Model.Extensions.Country.FromDataModel(source.Country);
                a.CountryID = source.Country.Id;
            }
            else
            {
                a.CountryID = source.CountryId;
            }

            a.Latitude = source.Latitude;
            a.Longitude = source.Longitude;
            a.ContactTelephone1 = source.ContactTelephone1;
            a.ContactTelephone2 = source.ContactTelephone2;
            a.ContactEmail = source.ContactEmail;
            a.AccessComments = source.AccessComments;
#pragma warning disable 612 //suppress obsolete warning
            a.GeneralComments = source.GeneralComments;
#pragma warning restore 612 //restore warning
            a.RelatedURL = source.RelatedUrl;

            if (!String.IsNullOrEmpty(a.RelatedURL) && !a.RelatedURL.StartsWith("http")) a.RelatedURL = "http://" + a.RelatedURL;
            return a;
        }
    }
}