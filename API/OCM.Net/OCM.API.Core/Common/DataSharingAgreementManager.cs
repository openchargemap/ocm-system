using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public class DataSharingAgreementManager : ManagerBase
    {
        public Model.DataSharingAgreement CreateAgreement(Model.DataSharingAgreement agreement, int userId)
        {
            var item = new Core.Data.DataSharingAgreement
            {
                CompanyName = agreement.CompanyName,
                ContactEmail = agreement.ContactEmail,
                CountryId = agreement.CountryID,
                DataFeedType = agreement.DataFeedType,
                DataFeedUrl = agreement.DataFeedURL,
                DataLicense = agreement.DataLicense,
                DistributionLimitations = agreement.DistributionLimitations,
                RepresentativeName = agreement.RepresentativeName,
                UserId = userId,
                WebsiteUrl = agreement.WebsiteURL,
                DateAgreed = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                Comments = agreement.Comments,
                Credentials = agreement.Credentials
            };

            dataModel.DataSharingAgreements.Add(item);
            dataModel.SaveChanges();

            try
            {
                new SubmissionManager().SendContactUsMessage(item.RepresentativeName, item.ContactEmail, $"A new data sharing agreement has been submitted for {item.CompanyName}");
            }
            catch
            {
            }

            return Model.Extensions.DataSharingAgreement.FromDataModel(item);
        }

        public Model.DataSharingAgreement GetAgreement(int id)
        {
            var item = dataModel.DataSharingAgreements.FirstOrDefault(a => a.Id == id);
            return Model.Extensions.DataSharingAgreement.FromDataModel(item);
        }

        public List<Model.DataSharingAgreement> GetAgreements()
        {
            return dataModel.DataSharingAgreements
                .OrderByDescending(a => a.DateCreated)
                .ToList()
                .Select(Model.Extensions.DataSharingAgreement.FromDataModel)
                .ToList();
        }

        public Model.DataSharingAgreement UpdateAgreement(Model.DataSharingAgreement agreement)
        {
            var item = dataModel.DataSharingAgreements.FirstOrDefault(a => a.Id == agreement.ID);
            if (item == null)
            {
                return null;
            }

            item.CompanyName = agreement.CompanyName;
            item.ContactEmail = agreement.ContactEmail;
            item.CountryId = agreement.CountryID;
            item.DataFeedType = agreement.DataFeedType;
            item.DataFeedUrl = agreement.DataFeedURL;
            item.DataLicense = agreement.DataLicense;
            item.DistributionLimitations = agreement.DistributionLimitations;
            item.RepresentativeName = agreement.RepresentativeName;
            item.WebsiteUrl = agreement.WebsiteURL;
            item.Comments = agreement.Comments;
            item.Credentials = agreement.Credentials;

            dataModel.SaveChanges();

            return Model.Extensions.DataSharingAgreement.FromDataModel(item);
        }

        public bool DeleteAgreement(int id, int deletedByUserId)
        {
            var item = dataModel.DataSharingAgreements.FirstOrDefault(a => a.Id == id);
            if (item == null)
            {
                return false;
            }

            // a provider which was only ever created for this submission goes with it, otherwise a rejected
            // submission would stay listed forever. Anything with imported data is kept and unlinked.
            var removedProviders = new List<string>();
            var linkedProviders = dataModel.DataProviders.Where(dp => dp.DataSharingAgreementId == id).ToList();
            foreach (var provider in linkedProviders)
            {
                if (DataProviderManager.IsUnused(dataModel, provider))
                {
                    removedProviders.Add($"{provider.Title} (#{provider.Id})");
                    dataModel.DataProviders.Remove(provider);
                }
                else
                {
                    provider.DataSharingAgreementId = null;
                }
            }

            dataModel.DataSharingAgreements.Remove(item);
            dataModel.SaveChanges();

            var user = new UserManager().GetUser(deletedByUserId);
            var removedSummary = removedProviders.Count > 0 ? $" and its unused data provider {string.Join(", ", removedProviders)}" : string.Empty;
            AuditLogManager.Log(user, AuditEventType.DeletedItem,
                "{EntityType:\"DataSharingAgreement\",EntityID:" + id + "}",
                $"Deleted data sharing agreement for {item.CompanyName}{removedSummary}");

            return true;
        }
    }
}
