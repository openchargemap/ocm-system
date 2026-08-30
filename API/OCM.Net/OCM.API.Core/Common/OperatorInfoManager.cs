using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public class OperatorInfoManager : ManagerBase
    {
        /// <summary>
        /// Words which say nothing about which operator a name refers to, so are ignored when comparing names.
        /// </summary>
        private static readonly HashSet<string> _ignoredNameWords = new HashSet<string>(StringComparer.Ordinal)
        {
            // legal entity suffixes
            "ab", "ag", "aps", "as", "asa", "bv", "co", "corp", "doo", "gmbh", "inc", "kft", "kg", "limited",
            "llc", "ltd", "nv", "oy", "oyj", "plc", "sa", "sarl", "sas", "sp", "spa", "srl", "zoo",
            // generic charging network terms
            "charge", "charger", "chargers", "charging", "e", "electric", "emobility", "energie", "energy",
            "ev", "evs", "group", "holding", "mobility", "network", "networks", "power", "recharge",
            "station", "stations"
        };

        /// <summary>
        /// Shared email providers, where two operators using the same domain tells us nothing about them being the same.
        /// </summary>
        private static readonly HashSet<string> _sharedEmailDomains = new HashSet<string>(StringComparer.Ordinal)
        {
            "aol.com", "gmail.com", "gmx.de", "gmx.net", "googlemail.com", "hotmail.co.uk", "hotmail.com",
            "icloud.com", "live.com", "mail.com", "me.com", "outlook.com", "protonmail.com", "web.de",
            "yahoo.co.uk", "yahoo.com", "yandex.ru"
        };

        public static string NormalizeTitle(string title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? string.Empty
                : new string(title.Trim().Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        /// <summary>
        /// The ISO country code from a country operator title such as "Ionity (DE)", or null when the title has no country suffix.
        /// </summary>
        public static string GetCountryCodeFromTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var trimmed = title.TrimEnd();
            if (!trimmed.EndsWith(")", StringComparison.Ordinal)) return null;

            var open = trimmed.LastIndexOf('(');
            if (open < 1 || trimmed[open - 1] != ' ') return null;

            var code = trimmed.Substring(open + 1, trimmed.Length - open - 2);
            return code.Length == 2 && code.All(char.IsLetter) ? code.ToUpperInvariant() : null;
        }

        /// <summary>
        /// An operator title with any country code suffix removed, so "Ionity (DE)" becomes "Ionity".
        /// </summary>
        public static string RemoveCountryCode(string title)
        {
            if (GetCountryCodeFromTitle(title) == null) return (title ?? string.Empty).Trim();

            var trimmed = title.TrimEnd();
            return trimmed.Substring(0, trimmed.LastIndexOf('(')).Trim();
        }

        /// <summary>
        /// The words which actually distinguish an operator name, in a stable order: the country suffix, punctuation,
        /// legal entity suffixes, single letters (from abbreviations such as B.V.) and generic charging terms are all
        /// dropped. Names made up entirely of generic terms (such as "EV Charge")
        /// keep all of their words, as otherwise they would all look alike.
        /// </summary>
        public static List<string> GetNameTokens(string title)
        {
            var words = new string(RemoveCountryCode(title).Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ').ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var distinctive = words.Where(w => w.Length > 1 && !_ignoredNameWords.Contains(w)).ToList();
            var tokens = string.Concat(distinctive).Length >= 4 ? distinctive : words;
            tokens.Sort(StringComparer.Ordinal);
            return tokens;
        }

        /// <summary>
        /// The comparison key for an operator name: its distinctive words only, in a stable order.
        /// </summary>
        public static string GetComparisonName(string title)
        {
            return string.Concat(GetNameTokens(title));
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

        /// <summary>
        /// True when two operator names are close enough that an editor should check whether they are the same operator:
        /// the same distinctive words, one name being a longer form of the other, or a small spelling difference.
        /// Deliberately conservative, as operators in this sector share a lot of generic wording.
        /// </summary>
        public static bool IsSimilarName(string left, string right)
        {
            var leftTokens = GetNameTokens(left);
            var rightTokens = GetNameTokens(right);
            if (leftTokens.Count == 0 || rightTokens.Count == 0) return false;

            var a = string.Concat(leftTokens);
            var b = string.Concat(rightTokens);
            if (a == b) return true;

            // every distinctive word of the shorter name appears in the longer one, e.g. "Fastned" and "Fastned Belgium"
            var fewer = leftTokens.Count <= rightTokens.Count ? leftTokens : rightTokens;
            var more = leftTokens.Count <= rightTokens.Count ? rightTokens : leftTokens;
            if (string.Concat(fewer).Length >= 5 && fewer.All(t => more.Contains(t))) return true;

            var shorter = a.Length <= b.Length ? a : b;
            var longer = a.Length <= b.Length ? b : a;

            // the same name written as one word or as two, e.g. "ChargePoint" and "Charge Point"
            if (shorter.Length >= 5 && longer.Contains(shorter)) return true;

            // a spelling variant or typo of the same name, allowing more difference in longer names
            var allowedEdits = shorter.Length >= 10 ? 2 : shorter.Length >= 6 ? 1 : 0;
            return allowedEdits > 0 && longer.Length - shorter.Length <= allowedEdits && EditDistance(a, b) <= allowedEdits;
        }

        private static int EditDistance(string a, string b)
        {
            var previous = Enumerable.Range(0, b.Length + 1).ToArray();
            var current = new int[b.Length + 1];

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
                }
                Array.Copy(current, previous, current.Length);
            }

            return previous[b.Length];
        }

        private static string GetEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var at = email.LastIndexOf('@');
            return at > 0 ? email.Substring(at + 1).Trim().ToLowerInvariant() : null;
        }

        /// <summary>
        /// Finds existing operators which could be the same as the details being submitted, graded by how likely a
        /// duplicate each one is. Matches in other countries are advisory only: an international network is expected
        /// to have one operator per country, all sharing a name and a website.
        /// </summary>
        public List<OperatorMatch> FindPotentialDuplicates(string operatorName, string countryIsoCode, string websiteUrl, string contactEmail, int? excludedId = null)
        {
            var isoCode = (countryIsoCode ?? string.Empty).Trim().ToUpperInvariant();
            var targetTitle = string.IsNullOrEmpty(isoCode)
                ? NormalizeTitle(RemoveCountryCode(operatorName))
                : NormalizeTitle(RemoveCountryCode(operatorName) + " (" + isoCode + ")");
            var websiteHost = GetWebsiteHost(websiteUrl);

            var emailDomain = GetEmailDomain(contactEmail);
            if (emailDomain != null && _sharedEmailDomains.Contains(emailDomain)) emailDomain = null;

            var matches = new List<OperatorMatch>();

            foreach (var candidate in GetOperators())
            {
                if (excludedId.HasValue && candidate.ID == excludedId.Value) continue;

                var isSameTitle = NormalizeTitle(candidate.Title) == targetTitle;

                var reasons = new List<string>();
                if (IsSimilarName(candidate.Title, operatorName)) reasons.Add("a similar name");
                if (!string.IsNullOrEmpty(websiteHost) && GetWebsiteHost(candidate.WebsiteURL) == websiteHost) reasons.Add("the same website (" + websiteHost + ")");
                if (!string.IsNullOrEmpty(emailDomain) && GetEmailDomain(candidate.ContactEmail) == emailDomain) reasons.Add("the same contact email domain (" + emailDomain + ")");
                if (!isSameTitle && reasons.Count == 0) continue;

                var candidateCountry = GetCountryCodeFromTitle(candidate.Title);
                var matchType = isSameTitle
                    ? OperatorMatchType.DuplicateTitle
                    : candidateCountry != null && candidateCountry != isoCode
                        ? OperatorMatchType.OtherCountry
                        : OperatorMatchType.PossibleDuplicate;

                var reason = matchType == OperatorMatchType.DuplicateTitle
                    ? "already uses this name for this country"
                    : matchType == OperatorMatchType.OtherCountry
                        ? "is listed for " + candidateCountry + " with " + string.Join(" and ", reasons)
                        : "has " + string.Join(" and ", reasons);

                matches.Add(new OperatorMatch { Operator = candidate, MatchType = matchType, Reason = reason });
            }

            return matches.OrderBy(m => m.MatchType).ThenBy(m => m.Operator.Title).ToList();
        }

        public OperatorInfo SaveCountryOperator(int userId, int countryId, OperatorInfo update, bool confirmNotDuplicate)
        {
            var country = dataModel.Countries.FirstOrDefault(c => c.Id == countryId);
            if (country == null) throw new ArgumentException("Unknown country", nameof(countryId));
            if (string.IsNullOrWhiteSpace(update.WebsiteURL)) throw new InvalidOperationException("A website URL is required.");

            var isoCode = country.Isocode.Trim().ToUpperInvariant();
            var name = (update.Title ?? string.Empty).Trim();
            var countrySuffix = " (" + isoCode + ")";
            if (name.EndsWith(countrySuffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - countrySuffix.Length).Trim();
            }
            var title = name + countrySuffix;
            var existing = update.ID > 1 ? dataModel.Operators.FirstOrDefault(o => o.Id == update.ID) : null;

            // check against the same matches the editor was shown, so what is warned about is what is enforced
            var matches = FindPotentialDuplicates(name, isoCode, update.WebsiteURL, update.ContactEmail, existing?.Id);

            var duplicateTitle = matches.FirstOrDefault(m => m.MatchType == OperatorMatchType.DuplicateTitle);
            if (duplicateTitle != null) throw new InvalidOperationException($"\"{duplicateTitle.Operator.Title}\" already exists. Edit that operator instead of adding a new one.");

            var needsConfirmation = matches.Where(m => m.RequiresConfirmation).ToList();
            if (needsConfirmation.Any() && !confirmNotDuplicate)
            {
                throw new InvalidOperationException($"This may be a duplicate of {string.Join(", ", needsConfirmation.Select(m => m.Operator.Title))}. Confirm this is a separate operator to save it.");
            }

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
