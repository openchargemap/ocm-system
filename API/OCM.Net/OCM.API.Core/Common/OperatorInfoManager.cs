using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public class OperatorInfoManager : ManagerBase
    {
        public static string NormalizeTitle(string title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? string.Empty
                : new string(title.Trim().Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        public static string GetWebsiteHost(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                Uri.TryCreate("https://" + url.Trim(), UriKind.Absolute, out uri);
            }

            var host = uri?.Host?.Trim().ToLowerInvariant();
            return host != null && host.StartsWith("www.", StringComparison.Ordinal) ? host.Substring(4) : host;
        }

        public List<OperatorInfo> FindPotentialDuplicates(string title, string websiteUrl, string contactEmail, int? excludedId = null)
        {
            var normalizedTitle = NormalizeTitle(title);
            var websiteHost = GetWebsiteHost(websiteUrl);
            var contactDomain = GetEmailDomain(contactEmail);

            return GetOperators().Where(o => (!excludedId.HasValue || o.ID != excludedId.Value) &&
                (NormalizeTitle(o.Title) == normalizedTitle ||
                 (!string.IsNullOrEmpty(websiteHost) && GetWebsiteHost(o.WebsiteURL) == websiteHost) ||
                 (!string.IsNullOrEmpty(contactDomain) && GetEmailDomain(o.ContactEmail) == contactDomain) ||
                 SimilarTitle(o.Title, title))).ToList();
        }

        private static string GetEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var at = email.LastIndexOf('@');
            return at > 0 ? email.Substring(at + 1).Trim().ToLowerInvariant() : null;
        }

        private static bool SimilarTitle(string left, string right)
        {
            var a = NormalizeTitle(left);
            var b = NormalizeTitle(right);
            return a.Length >= 4 && b.Length >= 4 && (a.Contains(b) || b.Contains(a));
        }

        public OperatorInfo SaveCountryOperator(int userId, int countryId, OperatorInfo update, bool confirmWebsiteMatch)
        {
            var country = dataModel.Countries.FirstOrDefault(c => c.Id == countryId);
            if (country == null) throw new ArgumentException("Unknown country", nameof(countryId));

            var name = (update.Title ?? string.Empty).Trim();
            var countrySuffix = " (" + country.Isocode.Trim().ToUpperInvariant() + ")";
            if (name.EndsWith(countrySuffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - countrySuffix.Length).Trim();
            }
            var title = $"{name} ({country.Isocode.ToUpperInvariant()})";
            var existing = update.ID > 1 ? dataModel.Operators.FirstOrDefault(o => o.Id == update.ID) : null;
            var duplicateTitle = dataModel.Operators.Any(o => (existing == null || o.Id != existing.Id) && NormalizeTitle(o.Title) == NormalizeTitle(title));
            if (duplicateTitle) throw new InvalidOperationException("An operator with this title already exists.");

            var websiteHost = GetWebsiteHost(update.WebsiteURL);
            var websiteMatch = !string.IsNullOrEmpty(websiteHost) && dataModel.Operators.Any(o => (existing == null || o.Id != existing.Id) && GetWebsiteHost(o.WebsiteUrl) == websiteHost);
            if (websiteMatch && !confirmWebsiteMatch) throw new InvalidOperationException("An operator with the same website already exists. Confirm this is not a duplicate.");

            var isUpdate = existing != null;
            if (existing == null)
            {
                existing = new OCM.Core.Data.Operator();
                dataModel.Operators.Add(existing);
            }

            existing.Title = title;
            existing.WebsiteUrl = update.WebsiteURL;
            existing.Comments = update.Comments;
            existing.PhonePrimaryContact = update.PhonePrimaryContact;
            existing.PhoneSecondaryContact = update.PhoneSecondaryContact;
            existing.BookingUrl = update.BookingURL;
            existing.ContactEmail = update.ContactEmail;
            existing.FaultReportEmail = update.FaultReportEmail;

            dataModel.SaveChanges();
            var result = Model.Extensions.OperatorInfo.FromDataModel(existing);
            var user = new UserManager().GetUser(userId);
            AuditLogManager.Log(user, isUpdate ? AuditEventType.UpdatedItem : AuditEventType.CreatedItem, "{EntityType:\"Operator\",EntityID:" + result.ID + "}", $"User {(isUpdate ? "updated" : "added")} country operator {result.ID} {result.Title}");
            CacheManager.RefreshCachedData();
            return result;
        }

        public OperatorInfo GetOperatorInfo(int id)
        {
            var operatorInfo = DataModel.Operators.FirstOrDefault(o => o.Id == id);

            return Model.Extensions.OperatorInfo.FromDataModel(operatorInfo);
        }

        public OperatorInfo UpdateOperatorInfo(int userId, OperatorInfo update)
        {
            var operatorInfo = new OCM.Core.Data.Operator();
            bool isUpdate = false;
            if (update.ID > 1)
            {
                //existing operator
                operatorInfo = DataModel.Operators.FirstOrDefault(o => o.Id == update.ID);
                isUpdate = true;
            }

            operatorInfo.Title = update.Title;
            operatorInfo.WebsiteUrl = update.WebsiteURL;
            operatorInfo.Comments = update.Comments;
            operatorInfo.PhonePrimaryContact = update.PhonePrimaryContact;
            operatorInfo.PhoneSecondaryContact = update.PhoneSecondaryContact;
            operatorInfo.IsPrivateIndividual = update.IsPrivateIndividual;
            operatorInfo.IsRestrictedEdit = update.IsRestrictedEdit;
            operatorInfo.BookingUrl = update.BookingURL;
            operatorInfo.ContactEmail = update.ContactEmail;
            operatorInfo.FaultReportEmail = update.FaultReportEmail;

            if (operatorInfo.Id == 0)
            {
                //add new
                DataModel.Operators.Add(operatorInfo);
            }

            DataModel.SaveChanges();

            update = Model.Extensions.OperatorInfo.FromDataModel(operatorInfo);

            var user = new UserManager().GetUser(userId);
            AuditLogManager.Log(user, isUpdate? AuditEventType.UpdatedItem: AuditEventType.CreatedItem, "{EntityType:\"Operator\",EntityID:" + update.ID + "}", $"User {(isUpdate?"updated":"added")} operator {update.ID} {operatorInfo.Title}");

            CacheManager.RefreshCachedData();

            return update;
        }

        public List<OperatorInfo> GetOperators()
        {
            var operators = new List<Model.OperatorInfo>();
            foreach (var source in DataModel.Operators)
            {
                operators.Add(Model.Extensions.OperatorInfo.FromDataModel(source));
            }

            return operators.OrderBy(o => o.Title).ToList();
        }
    }
}
