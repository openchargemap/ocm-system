using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OCM.API.Common
{
    public class DataProviderManager : ManagerBase
    {
        public void UpdateDateLastImport(int dataProviderID)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderID);
            dataProvider.DateLastImported = DateTime.UtcNow;
            dataModel.SaveChanges();
        }

        /// <summary>
        /// Creates a new DataProvider for a user-submitted OCPI feed.
        /// The provider is created with IsApprovedImport=false pending admin review.
        /// OCPI configuration JSON is stored in the ImportConfig field.
        /// </summary>
        public Model.DataProvider CreateOCPIDataProvider(string title, string websiteUrl, string license, bool isOpenDataLicensed, string ocpiConfigJson, int submittedByUserId, int? dataSharingAgreementId = null)
        {
            var dp = new Core.Data.DataProvider
            {
                Title = title,
                WebsiteUrl = websiteUrl,
                License = license,
                IsOpenDataLicensed = isOpenDataLicensed,
                IsApprovedImport = false, // pending admin approval
                IsRestrictedEdit = true,
                ImportConfig = ocpiConfigJson,
                Comments = "OCPI with Data Sharing Agreement",
                DataSharingAgreementId = dataSharingAgreementId,
                DataProviderStatusTypeId = 1 // manual entry
            };

            dataModel.DataProviders.Add(dp);
            dataModel.SaveChanges();

            var user = new UserManager().GetUser(submittedByUserId);
            AuditLogManager.Log(user, AuditEventType.CreatedItem,
                "{EntityType:\"DataProvider\",EntityID:" + dp.Id + "}",
                $"User submitted OCPI data provider: {title}");

            return Model.Extensions.DataProvider.FromDataModel(dp);
        }

        public Model.DataProvider GetDataProviderByAgreementId(int agreementId)
        {
            var dataProvider = dataModel.DataProviders
                .Include(dp => dp.DataProviderStatusType)
                .FirstOrDefault(dp => dp.DataSharingAgreementId == agreementId);
            return Model.Extensions.DataProvider.FromDataModel(dataProvider);
        }

        public System.Collections.Generic.List<Model.DataProviderStatusType> GetDataProviderStatusTypes()
        {
            return dataModel.DataProviderStatusTypes
                .OrderBy(status => status.Title)
                .ToList()
                .Select(Model.Extensions.DataProviderStatusType.FromDataModel)
                .ToList();
        }

        public string GetImportConfigByAgreementId(int agreementId)
        {
            return dataModel.DataProviders
                .Where(dp => dp.DataSharingAgreementId == agreementId)
                .Select(dp => dp.ImportConfig)
                .FirstOrDefault();
        }

        public System.Collections.Generic.List<int> GetApprovedImportAgreementIds()
        {
            var minimumNextImportUtc = DateTime.UtcNow.AddHours(-1);

            return dataModel.DataProviders
                .Where(dp => dp.IsApprovedImport == true
                    && dp.DataSharingAgreementId.HasValue
                    && (!dp.DateLastImported.HasValue || dp.DateLastImported.Value <= minimumNextImportUtc)
                    && !string.IsNullOrWhiteSpace(dp.ImportConfig))
                .Select(dp => dp.DataSharingAgreementId.Value)
                .Distinct()
                .ToList();
        }

        public void SetImportApprovalStatus(int dataProviderId, bool isApproved)
        {
            var dataProvider = dataModel.DataProviders
                .Include(dp => dp.DataProviderStatusType)
                .FirstOrDefault(dp => dp.Id == dataProviderId);
            if (dataProvider == null)
            {
                return;
            }

            dataProvider.IsApprovedImport = isApproved;
            dataModel.SaveChanges();
        }

        public Model.DataProvider UpdateOCPIDataProvider(int dataProviderId, string title, string websiteUrl, string license, bool isOpenDataLicensed, string ocpiConfigJson, int? dataProviderStatusTypeId, int updatedByUserId)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderId);
            if (dataProvider == null)
            {
                return null;
            }

            dataProvider.Title = title;
            dataProvider.WebsiteUrl = websiteUrl;
            dataProvider.License = license;
            dataProvider.IsOpenDataLicensed = isOpenDataLicensed;
            dataProvider.ImportConfig = ocpiConfigJson;
            dataProvider.Comments = "OCPI with Data Sharing Agreement";
            dataProvider.DataProviderStatusTypeId = dataProviderStatusTypeId;

            dataModel.SaveChanges();

            var user = new UserManager().GetUser(updatedByUserId);
            AuditLogManager.Log(user, AuditEventType.UpdatedItem,
                "{EntityType:\"DataProvider\",EntityID:" + dataProvider.Id + "}",
                $"Updated OCPI data provider configuration: {title}");

            return Model.Extensions.DataProvider.FromDataModel(dataProvider);
        }

        public System.Collections.Generic.List<string> GetApprovedImportConfigs()
        {
            return dataModel.DataProviders
                .Where(dp => dp.IsApprovedImport == true && !string.IsNullOrWhiteSpace(dp.ImportConfig))
                .Select(dp => dp.ImportConfig)
                .ToList();
        }
    }
}
