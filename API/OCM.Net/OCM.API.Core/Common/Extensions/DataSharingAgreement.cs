namespace OCM.API.Common.Model.Extensions
{
    public class DataSharingAgreement
    {
        public static Model.DataSharingAgreement FromDataModel(Core.Data.DataSharingAgreement source)
        {
            if (source == null) return null;

            return new Model.DataSharingAgreement
            {
                ID = source.Id,
                UserID = source.UserId,
                CompanyName = source.CompanyName,
                CountryID = source.CountryId,
                RepresentativeName = source.RepresentativeName,
                ContactEmail = source.ContactEmail,
                IsEmailVerified = source.IsEmailVerified,
                WebsiteURL = source.WebsiteUrl,
                DataFeedType = source.DataFeedType,
                DataFeedURL = source.DataFeedUrl,
                DistributionLimitations = source.DistributionLimitations,
                DataLicense = source.DataLicense,
                Credentials = source.Credentials,
                DateCreated = source.DateCreated,
                DateAgreed = source.DateAgreed,
                DateAgreementTerminated = source.DateAgreementTerminated ?? default,
                Comments = source.Comments
            };
        }
    }
}
