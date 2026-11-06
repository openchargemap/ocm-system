using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace OCM.API.Common
{
    /// <summary>
    /// An existing data provider which could be the same organisation as a data sharing agreement.
    /// </summary>
    public class DataProviderMatch
    {
        public int DataProviderId { get; set; }
        public string Title { get; set; }
        public string WebsiteUrl { get; set; }
        public string Reason { get; set; }

        /// <summary>
        /// The agreement this provider already belongs to, if any.
        /// </summary>
        public int? LinkedAgreementId { get; set; }

        /// <summary>
        /// False when linking would take the provider from another agreement or replace its import configuration.
        /// </summary>
        public bool CanLink { get; set; }
    }

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
        /// The provider is created with IsApprovedImport=false pending admin review, and is not marked as
        /// open data until it is approved because that flag lists it publicly as a data source.
        /// OCPI configuration JSON is stored in the ImportConfig field.
        /// </summary>
        public Model.DataProvider CreateOCPIDataProvider(string title, string websiteUrl, string license, string ocpiConfigJson, int submittedByUserId, int? dataSharingAgreementId = null)
        {
            var dp = new Core.Data.DataProvider
            {
                Title = title,
                WebsiteUrl = websiteUrl,
                License = license,
                IsOpenDataLicensed = false,
                IsApprovedImport = false, // pending admin approval
                IsRestrictedEdit = true,
                ImportConfig = ocpiConfigJson,
                Comments = "OCPI with Data Sharing Agreement",
                DataSharingAgreementId = dataSharingAgreementId,

                // a feed submitted under a data sharing agreement is imported automatically once approved,
                // so it should not be labelled as manually maintained data
                DataProviderStatusTypeId = GetAutomatedImportStatusTypeId() ?? ManualDataEntryStatusTypeId
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

        /// <summary>
        /// Reference data id for manually maintained providers, used as a fallback when the automated
        /// import status cannot be found.
        /// </summary>
        private const int ManualDataEntryStatusTypeId = 1;

        /// <summary>
        /// Resolves the "Automated Import" status from reference data by title, so the id does not have to
        /// be hard coded here. Returns null when no matching status exists.
        /// </summary>
        private int? GetAutomatedImportStatusTypeId()
        {
            return dataModel.DataProviderStatusTypes
                .Where(s => s.Title.Contains("Automated"))
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefault();
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

        /// <summary>
        /// Minimum time between automatic import attempts for the same data provider. A provider imported
        /// more recently than this is skipped when approved imports are queued.
        /// </summary>
        public static readonly TimeSpan MinimumImportInterval = TimeSpan.FromHours(1);

        public System.Collections.Generic.List<int> GetApprovedImportAgreementIds()
        {
            var minimumNextImportUtc = DateTime.UtcNow.Subtract(MinimumImportInterval);

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

            // agreements license their data as CC0, which applies once approved. Withdrawing approval leaves
            // this alone: anything already imported was shared under that license and stays open data.
            if (isApproved)
            {
                dataProvider.IsOpenDataLicensed = true;
            }

            dataModel.SaveChanges();
        }

        /// <summary>
        /// Finds existing data providers which could be the same organisation as an agreement, so a new
        /// provider is not created for one we already have. Providers are compared on any of the given titles
        /// and on their website.
        /// </summary>
        public System.Collections.Generic.List<DataProviderMatch> FindPotentialMatches(System.Collections.Generic.IEnumerable<string> titles, string websiteUrl, int? excludedDataProviderId = null)
        {
            var titleList = (titles ?? Enumerable.Empty<string>())
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matches = new System.Collections.Generic.List<DataProviderMatch>();

            foreach (var candidate in dataModel.DataProviders.AsNoTracking().OrderBy(dp => dp.Title).ToList())
            {
                if (excludedDataProviderId.HasValue && candidate.Id == excludedDataProviderId.Value)
                {
                    continue;
                }

                var reason = GetMatchReason(candidate.Title, candidate.WebsiteUrl, titleList, websiteUrl);
                if (reason == null)
                {
                    continue;
                }

                matches.Add(new DataProviderMatch
                {
                    DataProviderId = candidate.Id,
                    Title = candidate.Title,
                    WebsiteUrl = candidate.WebsiteUrl,
                    Reason = reason,
                    LinkedAgreementId = candidate.DataSharingAgreementId,
                    CanLink = !candidate.DataSharingAgreementId.HasValue && string.IsNullOrWhiteSpace(candidate.ImportConfig)
                });
            }

            return matches;
        }

        /// <summary>
        /// Why a provider could be the same organisation as the given names and website, or null when it does not appear to be.
        /// </summary>
        public static string GetMatchReason(string providerTitle, string providerWebsiteUrl, System.Collections.Generic.IEnumerable<string> titles, string websiteUrl)
        {
            var reasons = new System.Collections.Generic.List<string>();

            if (titles != null && titles.Any(title => OperatorInfoManager.IsSimilarName(providerTitle, title)))
            {
                reasons.Add("a similar name");
            }

            var websiteHost = OperatorInfoManager.GetWebsiteHost(websiteUrl);
            if (!string.IsNullOrEmpty(websiteHost) && OperatorInfoManager.GetWebsiteHost(providerWebsiteUrl) == websiteHost)
            {
                reasons.Add("the same website (" + websiteHost + ")");
            }

            return reasons.Count == 0 ? null : "has " + string.Join(" and ", reasons);
        }

        /// <summary>
        /// True when nothing depends on a provider, so removing it loses no data: it has never been approved
        /// or imported, and no POI or metadata refers to it.
        /// </summary>
        internal static bool IsUnused(Core.Data.OCMEntities dataModel, Core.Data.DataProvider provider)
        {
            return provider.IsApprovedImport != true
                && !provider.DateLastImported.HasValue
                && !dataModel.ChargePoints.Any(cp => cp.DataProviderId == provider.Id)
                && !dataModel.MetadataGroups.Any(group => group.DataProviderId == provider.Id);
        }

        /// <summary>
        /// Points an agreement at an existing data provider, moving its import configuration there and removing
        /// the provider which was created for the submission. The import has to be approved again, because the
        /// provider it now belongs to has not been reviewed with this configuration.
        /// </summary>
        /// <param name="importConfigJson">Import configuration targeting <paramref name="dataProviderId"/>, or null when there is none yet</param>
        /// <exception cref="InvalidOperationException">The link would lose data or take over another import, with the reason as the message</exception>
        public Model.DataProvider LinkAgreementToProvider(int agreementId, int dataProviderId, string importConfigJson, int linkedByUserId)
        {
            var target = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderId);
            if (target == null)
            {
                throw new InvalidOperationException($"Data provider #{dataProviderId} was not found.");
            }

            if (target.DataSharingAgreementId == agreementId)
            {
                throw new InvalidOperationException($"{target.Title} is already linked to this agreement.");
            }

            if (target.DataSharingAgreementId.HasValue)
            {
                throw new InvalidOperationException($"{target.Title} is already linked to agreement #{target.DataSharingAgreementId}.");
            }

            if (!string.IsNullOrWhiteSpace(target.ImportConfig))
            {
                throw new InvalidOperationException($"{target.Title} already has its own import configuration, so linking would replace it.");
            }

            var replaced = dataModel.DataProviders.FirstOrDefault(dp => dp.DataSharingAgreementId == agreementId);
            if (replaced != null)
            {
                if (!IsUnused(dataModel, replaced))
                {
                    throw new InvalidOperationException($"{replaced.Title} is already in use by this agreement, so it cannot be replaced.");
                }

                dataModel.DataProviders.Remove(replaced);
            }

            target.DataSharingAgreementId = agreementId;
            target.ImportConfig = importConfigJson;
            target.IsApprovedImport = false;

            dataModel.SaveChanges();

            var user = new UserManager().GetUser(linkedByUserId);
            AuditLogManager.Log(user, AuditEventType.UpdatedItem,
                "{EntityType:\"DataProvider\",EntityID:" + target.Id + "}",
                $"Linked data provider {target.Title} to data sharing agreement #{agreementId}");

            return Model.Extensions.DataProvider.FromDataModel(target);
        }

        /// <summary>
        /// Updates only the stored import configuration for a data provider, leaving all other details untouched.
        /// </summary>
        public bool UpdateImportConfig(int dataProviderId, string ocpiConfigJson)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderId);
            if (dataProvider == null)
            {
                return false;
            }

            dataProvider.ImportConfig = ocpiConfigJson;
            dataModel.SaveChanges();

            return true;
        }

        /// <summary>
        /// Saves the reviewed details of an OCPI data provider. The license and comments only fill gaps, so the
        /// details of an existing provider which has been linked to an agreement are kept, and the status is
        /// left as it is unless one is chosen.
        /// </summary>
        public Model.DataProvider UpdateOCPIDataProvider(int dataProviderId, string title, string websiteUrl, string license, string ocpiConfigJson, int? dataProviderStatusTypeId, int updatedByUserId)
        {
            var dataProvider = dataModel.DataProviders.FirstOrDefault(dp => dp.Id == dataProviderId);
            if (dataProvider == null)
            {
                return null;
            }

            dataProvider.Title = title;
            dataProvider.WebsiteUrl = websiteUrl;
            dataProvider.ImportConfig = ocpiConfigJson;

            if (string.IsNullOrWhiteSpace(dataProvider.License))
            {
                dataProvider.License = license;
            }

            if (string.IsNullOrWhiteSpace(dataProvider.Comments))
            {
                dataProvider.Comments = "OCPI with Data Sharing Agreement";
            }

            if (dataProviderStatusTypeId.HasValue)
            {
                dataProvider.DataProviderStatusTypeId = dataProviderStatusTypeId;
            }

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
