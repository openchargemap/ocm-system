using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace OCM.Web.Services
{
    /// <summary>
    /// Server side validation of Cloudflare Turnstile tokens, used to reduce automated signups on the registration form.
    /// The browser widget produces a single use token which we exchange with Cloudflare's siteverify endpoint.
    /// </summary>
    public class TurnstileValidator
    {
        private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        /// <summary>
        /// Name of the form field the Turnstile widget populates in the browser.
        /// </summary>
        public const string TokenFieldName = "cf-turnstile-response";

        private readonly ILogger _logger;

        public TurnstileValidator(ILogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Public site key, safe to render into the page. Empty if Turnstile has not been configured.
        /// </summary>
        public static string SiteKey => ConfigurationManager.AppSettings["TurnstileSiteKey"];

        /// <summary>
        /// Turnstile is only enforced once a secret key is present, so unconfigured environments
        /// (local dev, tests) behave as they did before Turnstile was introduced.
        /// </summary>
        public static bool IsEnabled =>
            !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["TurnstileSecretKey"])
            && !string.IsNullOrWhiteSpace(SiteKey);

        /// <summary>
        /// Validates a Turnstile token against Cloudflare's siteverify endpoint.
        /// Returns true when Turnstile is not configured, otherwise a token must be present and valid.
        /// Network or service failures are treated as a failed validation.
        /// </summary>
        public async Task<bool> IsTokenValidAsync(string token, string remoteIp = null)
        {
            if (!IsEnabled) return true;

            if (string.IsNullOrWhiteSpace(token)) return false;

            var secret = ConfigurationManager.AppSettings["TurnstileSecretKey"];

            try
            {
                using (var httpClient = CreateHttpClient())
                {
                    var fields = new Dictionary<string, string>
                    {
                        { "secret", secret },
                        { "response", token }
                    };

                    // remoteip is optional. Only supply it when we have the real client address,
                    // as requests reaching this app are proxied and would otherwise report the edge address.
                    if (!string.IsNullOrWhiteSpace(remoteIp))
                    {
                        fields.Add("remoteip", remoteIp);
                    }

                    var response = await httpClient.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(fields));
                    var body = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<SiteVerifyResponse>(body);

                    if (result == null)
                    {
                        _logger?.LogWarning("Turnstile siteverify returned an unreadable response: {Body}", body);
                        return false;
                    }

                    if (!result.Success)
                    {
                        _logger?.LogWarning("Turnstile validation rejected a submission: {Errors}",
                            result.ErrorCodes != null ? string.Join(",", result.ErrorCodes) : "(none)");
                    }

                    return result.Success;
                }
            }
            catch (Exception ex)
            {
                // Fail closed: if we cannot confirm the token we do not create the account.
                _logger?.LogError(ex, "Turnstile validation could not be completed");
                return false;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "openchargemap-turnstile/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");

            return httpClient;
        }

        private class SiteVerifyResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("error-codes")]
            public string[] ErrorCodes { get; set; }

            [JsonProperty("hostname")]
            public string Hostname { get; set; }

            [JsonProperty("action")]
            public string Action { get; set; }
        }
    }
}
