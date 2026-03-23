using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCM.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace OCM.Web.Services
{
    /// <summary>
    /// Validates an OCPI feed by fetching sample data and checking structure
    /// </summary>
    public class OCPIFeedValidator
    {
        private readonly ILogger _logger;
        private static readonly string[] PreferredVersions = ["2.2.1", "2.2", "2.1.1"];

        public OCPIFeedValidator(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Fetches and validates an OCPI endpoint, discovering the correct auth/header/url settings as needed.
        /// </summary>
        public async Task<OCPIValidationResult> ValidateFeedAsync(string locationsEndpointUrl, string authHeaderKey = null, string authHeaderValue = null)
        {
            var result = new OCPIValidationResult();

            try
            {
                if (!Uri.TryCreate(locationsEndpointUrl, UriKind.Absolute, out var uri))
                {
                    result.Errors.Add("Invalid URL format");
                    return result;
                }

                if (uri.Scheme != "https")
                {
                    result.Warnings.Add("URL does not use HTTPS. HTTPS is strongly recommended for OCPI endpoints.");
                }

                using var httpClient = CreateHttpClient();

                DiscoveryResult discoveryResult = null;
                foreach (var authOption in BuildAuthOptions(authHeaderKey, authHeaderValue))
                {
                    discoveryResult = await DiscoverFeedAsync(httpClient, locationsEndpointUrl, authOption);
                    result.DiscoveryLog.AddRange(discoveryResult.Log);

                    if (discoveryResult.IsSuccess)
                    {
                        break;
                    }
                }

                if (discoveryResult == null || !discoveryResult.IsSuccess)
                {
                    result.Errors.Add("Unable to discover a valid OCPI locations endpoint using the supplied URL and credentials.");
                    return result;
                }

                result.ResolvedLocationsEndpointUrl = discoveryResult.LocationsEndpointUrl;
                result.ResolvedAuthHeaderKey = discoveryResult.AuthHeaderKey;
                result.ResolvedAuthHeaderValuePrefix = discoveryResult.AuthHeaderValuePrefix;
                result.ResolvedAuthHeaderDisplayValue = discoveryResult.AuthHeaderDisplayValue;

                if (!string.Equals(locationsEndpointUrl, discoveryResult.LocationsEndpointUrl, StringComparison.OrdinalIgnoreCase))
                {
                    result.Warnings.Add($"Resolved locations endpoint to {discoveryResult.LocationsEndpointUrl}.");
                }

                PopulateValidationResult(result, discoveryResult.Locations);
                result.IsValid = !result.Errors.Any();
            }
            catch (TaskCanceledException)
            {
                result.Errors.Add("Request timed out. The endpoint did not respond within 30 seconds.");
            }
            catch (HttpRequestException ex)
            {
                result.Errors.Add($"Connection error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unexpected error during validation: {ex.Message}");
                _logger?.LogError(ex, "Error validating OCPI feed at {Url}", locationsEndpointUrl);
            }

            return result;
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "openchargemap-OCPI-validator/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");

            return httpClient;
        }

        private void PopulateValidationResult(OCPIValidationResult result, List<dynamic> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                result.Warnings.Add("Endpoint returned an empty locations list. The feed may not have any data yet.");
                result.LocationCount = 0;
                return;
            }

            result.LocationCount = locations.Count;

            var operatorCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var countries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalEvses = 0;
            int locationsWithoutCoords = 0;
            int locationsWithoutAddress = 0;

            foreach (dynamic loc in locations)
            {
                try
                {
                    var evses = loc.evses;
                    if (evses != null)
                    {
                        totalEvses += ((Newtonsoft.Json.Linq.JArray)evses).Count;
                    }
                }
                catch { }

                try
                {
                    string operatorName = null;
                    var op = loc.@operator;
                    if (op != null)
                    {
                        operatorName = (string)op.name;
                    }

                    if (string.IsNullOrEmpty(operatorName))
                    {
                        operatorName = (string)loc.party_id;
                    }

                    if (!string.IsNullOrEmpty(operatorName))
                    {
                        if (operatorCounts.ContainsKey(operatorName))
                        {
                            operatorCounts[operatorName]++;
                        }
                        else
                        {
                            operatorCounts[operatorName] = 1;
                        }
                    }
                }
                catch { }

                try
                {
                    string country = (string)loc.country;
                    if (!string.IsNullOrEmpty(country))
                    {
                        countries.Add(country);
                    }
                }
                catch { }

                try
                {
                    var coords = loc.coordinates;
                    if (coords == null || coords.latitude == null || coords.longitude == null)
                    {
                        locationsWithoutCoords++;
                    }
                }
                catch { locationsWithoutCoords++; }

                try
                {
                    string address = (string)loc.address;
                    if (string.IsNullOrEmpty(address))
                    {
                        locationsWithoutAddress++;
                    }
                }
                catch { }

                if (result.SampleLocations.Count < 5)
                {
                    try
                    {
                        string name = (string)loc.name;
                        if (!string.IsNullOrEmpty(name))
                        {
                            result.SampleLocations.Add(name);
                        }
                    }
                    catch { }
                }
            }

            result.EvseCount = totalEvses;
            result.DiscoveredCountries = countries.OrderBy(c => c).ToList();
            result.DiscoveredOperators = operatorCounts.OrderByDescending(o => o.Value)
                .Select(o => new DiscoveredOperator { Name = o.Key, LocationCount = o.Value })
                .ToList();

            if (locationsWithoutCoords > 0)
            {
                result.Warnings.Add($"{locationsWithoutCoords} location(s) are missing coordinates.");
            }

            if (locationsWithoutAddress > 0)
            {
                result.Warnings.Add($"{locationsWithoutAddress} location(s) are missing address information.");
            }

            if (!result.DiscoveredOperators.Any())
            {
                result.Warnings.Add("No operator information found in any locations. A default operator mapping will be required.");
            }

            if (totalEvses == 0)
            {
                result.Warnings.Add("No EVSEs found in the location data.");
            }
        }

        private async Task<DiscoveryResult> DiscoverFeedAsync(HttpClient httpClient, string submittedUrl, AuthOption authOption)
        {
            var result = new DiscoveryResult
            {
                AuthHeaderKey = authOption.HeaderKey,
                AuthHeaderValuePrefix = authOption.ValuePrefix,
                AuthHeaderDisplayValue = authOption.DisplayValue
            };

            result.Log.Add($"Trying {authOption.Description} against {submittedUrl}");

            var initialResponse = await GetResponseContentAsync(httpClient, submittedUrl, authOption, result.Log, "submitted endpoint");
            if (!initialResponse.Success)
            {
                return result;
            }

            if (!TryParseOcpiDataToken(initialResponse.Content, out var dataToken, out var parseError))
            {
                result.Log.Add(parseError);
                return result;
            }

            if (LooksLikeVersionsResponse(dataToken))
            {
                result.Log.Add("Detected OCPI versions endpoint. Trying supported versions until a working locations endpoint is found.");
                return await ResolveFromVersionsAsync(httpClient, dataToken, authOption, result);
            }

            if (TryExtractLocations(initialResponse.Content, out var directLocations))
            {
                result.IsSuccess = true;
                result.LocationsEndpointUrl = submittedUrl;
                result.Locations = directLocations;
                result.Log.Add("Submitted URL is a working OCPI locations endpoint.");
                return result;
            }

            result.Log.Add("Submitted URL returned JSON but not a recognizable OCPI locations response.");
            return result;
        }

        private async Task<DiscoveryResult> ResolveFromVersionsAsync(HttpClient httpClient, Newtonsoft.Json.Linq.JToken versionsData, AuthOption authOption, DiscoveryResult result)
        {
            var versions = versionsData
                .Select(v => new { Version = (string)v["version"], Url = (string)v["url"] })
                .Where(v => !string.IsNullOrWhiteSpace(v.Version) && !string.IsNullOrWhiteSpace(v.Url))
                .OrderBy(v => Array.IndexOf(PreferredVersions, v.Version) >= 0 ? Array.IndexOf(PreferredVersions, v.Version) : int.MaxValue)
                .ThenByDescending(v => v.Version)
                .ToList();

            foreach (var version in versions)
            {
                result.Log.Add($"Trying version {version.Version} via {version.Url}");

                var versionDetailResponse = await GetResponseContentAsync(httpClient, version.Url, authOption, result.Log, $"version {version.Version}");
                if (!versionDetailResponse.Success)
                {
                    continue;
                }

                var versionDetailObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(versionDetailResponse.Content);
                var endpoints = versionDetailObj?["data"]?["endpoints"];
                if (endpoints == null || endpoints.Type != Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    result.Log.Add($"Version {version.Version} did not return an endpoints array.");
                    continue;
                }

                var locationsEndpoint = endpoints.FirstOrDefault(e =>
                    string.Equals((string)e["identifier"], "locations", StringComparison.OrdinalIgnoreCase));

                if (locationsEndpoint == null)
                {
                    result.Log.Add($"Version {version.Version} has no locations endpoint.");
                    continue;
                }

                var locationsUrl = (string)locationsEndpoint["url"];
                if (string.IsNullOrWhiteSpace(locationsUrl))
                {
                    result.Log.Add($"Version {version.Version} has an empty locations URL.");
                    continue;
                }

                var locationsResponse = await GetResponseContentAsync(httpClient, locationsUrl, authOption, result.Log, $"locations endpoint for {version.Version}");
                if (!locationsResponse.Success)
                {
                    continue;
                }

                if (TryExtractLocations(locationsResponse.Content, out var locations))
                {
                    result.IsSuccess = true;
                    result.LocationsEndpointUrl = locationsUrl;
                    result.Locations = locations;
                    result.Log.Add($"Resolved working locations endpoint {locationsUrl} using version {version.Version} and {authOption.Description}.");
                    return result;
                }

                result.Log.Add($"Locations endpoint for version {version.Version} did not return a recognizable OCPI locations response.");
            }

            return result;
        }

        private static bool TryParseOcpiDataToken(string content, out Newtonsoft.Json.Linq.JToken dataToken, out string error)
        {
            dataToken = null;
            error = null;

            try
            {
                if (!content.TrimStart().StartsWith("{"))
                {
                    error = "Response is not an OCPI object response.";
                    return false;
                }

                var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(content);
                dataToken = obj?["data"];
                if (dataToken == null)
                {
                    error = "Response does not contain a data property.";
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse JSON response: {ex.Message}";
                return false;
            }
        }

        private static bool LooksLikeVersionsResponse(Newtonsoft.Json.Linq.JToken dataToken)
        {
            if (dataToken?.Type != Newtonsoft.Json.Linq.JTokenType.Array)
            {
                return false;
            }

            var firstItem = dataToken.First;
            return firstItem != null
                && firstItem["version"] != null
                && firstItem["url"] != null
                && firstItem["evses"] == null
                && firstItem["coordinates"] == null;
        }

        private static bool TryExtractLocations(string content, out List<dynamic> locations)
        {
            locations = null;

            try
            {
                if (content.TrimStart().StartsWith("{"))
                {
                    var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(content);
                    var dataToken = obj?["data"];
                    if (dataToken != null && dataToken.Type == Newtonsoft.Json.Linq.JTokenType.Array && !LooksLikeVersionsResponse(dataToken))
                    {
                        locations = dataToken.ToObject<List<dynamic>>();
                        return true;
                    }
                }
                else if (content.TrimStart().StartsWith("["))
                {
                    locations = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private async Task<(bool Success, string Content)> GetResponseContentAsync(HttpClient httpClient, string url, AuthOption authOption, List<string> log, string description)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(authOption.HeaderKey) && authOption.HeaderValue != null)
            {
                request.Headers.TryAddWithoutValidation(authOption.HeaderKey, authOption.HeaderValue);
            }

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                log.Add($"{description} returned HTTP {(int)response.StatusCode} {response.ReasonPhrase} using {authOption.Description}.");
                return (false, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                log.Add($"{description} returned an empty response using {authOption.Description}.");
                return (false, null);
            }

            return (true, content);
        }

        private IEnumerable<AuthOption> BuildAuthOptions(string authHeaderKey, string authHeaderValue)
        {
            if (string.IsNullOrWhiteSpace(authHeaderValue))
            {
                yield return new AuthOption(null, null, null, "no authorization header", "(none)");
                yield break;
            }

            var headerKey = string.IsNullOrWhiteSpace(authHeaderKey) ? "Authorization" : authHeaderKey.Trim();
            var rawValue = authHeaderValue.Trim();

            if (!string.Equals(headerKey, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                yield return new AuthOption(headerKey, rawValue, null, $"{headerKey} header", $"{headerKey}: <credential>");
                yield break;
            }

            if (rawValue.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
            {
                yield return new AuthOption("Authorization", rawValue, "Token ", "Authorization header with Token prefix", "Authorization: Token <credential>");
                var stripped = rawValue.Substring("Token ".Length).Trim();
                if (!string.IsNullOrEmpty(stripped))
                {
                    yield return new AuthOption("Authorization", stripped, string.Empty, "Authorization header with raw credential", "Authorization: <credential>");
                }
                yield break;
            }

            if (rawValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || rawValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var prefix = rawValue.Substring(0, rawValue.IndexOf(' ') + 1);
                yield return new AuthOption("Authorization", rawValue, prefix, $"Authorization header with {prefix.Trim()} prefix", $"Authorization: {prefix}<credential>");
                yield break;
            }

            yield return new AuthOption("Authorization", "Token " + rawValue, "Token ", "Authorization header with Token prefix", "Authorization: Token <credential>");
            yield return new AuthOption("Authorization", rawValue, string.Empty, "Authorization header with raw credential", "Authorization: <credential>");
        }

        private sealed class AuthOption
        {
            public AuthOption(string headerKey, string headerValue, string valuePrefix, string description, string displayValue)
            {
                HeaderKey = headerKey;
                HeaderValue = headerValue;
                ValuePrefix = valuePrefix;
                Description = description;
                DisplayValue = displayValue;
            }

            public string HeaderKey { get; }
            public string HeaderValue { get; }
            public string ValuePrefix { get; }
            public string Description { get; }
            public string DisplayValue { get; }
        }

        private sealed class DiscoveryResult
        {
            public bool IsSuccess { get; set; }
            public string LocationsEndpointUrl { get; set; }
            public string AuthHeaderKey { get; set; }
            public string AuthHeaderValuePrefix { get; set; }
            public string AuthHeaderDisplayValue { get; set; }
            public List<dynamic> Locations { get; set; }
            public List<string> Log { get; } = new List<string>();
        }
    }
}
