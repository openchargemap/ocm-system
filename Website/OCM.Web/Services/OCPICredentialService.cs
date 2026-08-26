using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCM.Import;
using OCM.Import.Providers.OCPI;

namespace OCM.Web.Services
{
    /// <summary>
    /// Creates the OCPI- prefixed secrets which approved imports use to authenticate against their data feed.
    /// A secret is only written once the feed has been verified with that exact credential, so an approval
    /// never leaves a credential in the vault that does not work.
    /// </summary>
    public class OCPICredentialService : IOCPICredentialService
    {
        /// <summary>
        /// Tag recording which data sharing agreement a credential was created for, used to detect name collisions.
        /// </summary>
        private const string AgreementTag = "ocm-agreement-id";

        private readonly IConfiguration _configuration;
        private readonly ILogger<OCPICredentialService> _logger;
        private readonly Lazy<SecretClient> _secretClient;
        private readonly ImportSettings _importSettings;

        public OCPICredentialService(IConfiguration configuration, ILogger<OCPICredentialService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _importSettings = new ImportSettings();
            _configuration.GetSection("ImportSettings").Bind(_importSettings);

            _secretClient = new Lazy<SecretClient>(CreateSecretClient, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public bool IsVaultConfigured =>
            !string.IsNullOrWhiteSpace(_importSettings.KeyVaultUri)
            && !string.IsNullOrWhiteSpace(_importSettings.KeyVaultTenantId)
            && !string.IsNullOrWhiteSpace(_importSettings.KeyVaultClientId)
            && !string.IsNullOrWhiteSpace(_importSettings.KeyVaultSecret);

        public async Task<OCPICredentialProvisioningResult> ProvisionCredentialAsync(OCPICredentialProvisioningRequest request, CancellationToken cancellationToken = default)
        {
            var result = new OCPICredentialProvisioningResult();

            if (request == null)
            {
                result.Errors.Add("No credential provisioning request was supplied.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.LocationsEndpointUrl))
            {
                result.Errors.Add("A locations endpoint URL is required before an import can be approved.");
                return result;
            }

            var requiresCredential = !string.IsNullOrWhiteSpace(request.AuthHeaderKey)
                || !string.IsNullOrWhiteSpace(request.SubmittedCredential);

            if (!requiresCredential)
            {
                return await VerifyOpenFeedAsync(request, result);
            }

            result.CredentialKey = ResolveCredentialKey(request, result);
            if (result.CredentialKey == null)
            {
                return result;
            }

            var existingSecret = await TryGetSecretAsync(result.CredentialKey, result, cancellationToken);

            if (!CheckSecretOwnership(existingSecret, request, result))
            {
                return result;
            }

            var resolved = ResolveCredentialValue(request, result, existingSecret);
            if (resolved.Value == null)
            {
                result.Errors.Add($"No credential value is available for '{result.CredentialKey}'. Re-enter the submitted credential on this review, or create the secret in the key vault first.");
                return result;
            }

            result.CredentialSource = resolved.Source;

            var verification = await new OCPIFeedValidator(_logger).VerifyFeedWithStoredCredentialAsync(
                request.LocationsEndpointUrl,
                request.AuthHeaderKey,
                request.AuthHeaderValuePrefix,
                resolved.Value);

            result.Verification = verification;
            result.Log.AddRange(verification.DiscoveryLog);
            result.Warnings.AddRange(verification.Warnings);

            if (!verification.IsValid)
            {
                result.Errors.Add($"The OCPI feed could not be read using the {DescribeSource(resolved.Source)} credential, so no key vault secret was created.");
                result.Errors.AddRange(verification.Errors);
                return result;
            }

            result.IsVerified = true;
            result.Log.Add($"Verified {verification.LocationCount} location(s) and {verification.EvseCount} EVSE(s) using the {DescribeSource(resolved.Source)} credential.");

            if (resolved.Source == OCPICredentialSource.Vault)
            {
                result.SecretAlreadyCurrent = true;
                result.IsSuccess = true;
                result.Log.Add($"Key vault already holds a working credential named '{result.CredentialKey}'.");
                return result;
            }

            if (!IsVaultConfigured)
            {
                // No vault available, typically local development. The import can still run when the value is
                // already present in configuration, otherwise approving would leave it unable to authenticate.
                if (resolved.Source == OCPICredentialSource.Configuration)
                {
                    result.IsSuccess = true;
                    result.Warnings.Add($"Key vault is not configured. '{result.CredentialKey}' was verified from local configuration and was not stored in a vault.");
                    return result;
                }

                result.Errors.Add($"Key vault is not configured, so the credential '{result.CredentialKey}' cannot be stored. Add the key vault settings or create the secret manually before approving.");
                return result;
            }

            return await WriteSecretAsync(result, request.AgreementId, resolved.Value, cancellationToken);
        }

        public async Task<OCPICredentialStatus> GetCredentialStatusAsync(string credentialKey, CancellationToken cancellationToken = default)
        {
            var status = new OCPICredentialStatus
            {
                CredentialKey = credentialKey,
                VaultAvailable = IsVaultConfigured
            };

            if (string.IsNullOrWhiteSpace(credentialKey) || !IsVaultConfigured)
            {
                return status;
            }

            if (!OCPICredentialNaming.IsValidSecretName(credentialKey))
            {
                status.Error = "Not a valid key vault secret name. Use letters, digits and dashes only.";
                return status;
            }

            try
            {
                var secret = await _secretClient.Value.GetSecretAsync(credentialKey, cancellationToken: cancellationToken);
                status.Exists = true;
                status.IsEnabled = secret.Value.Properties.Enabled ?? true;
                status.UpdatedOn = secret.Value.Properties.UpdatedOn;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                status.Exists = false;
            }
            catch (Exception ex)
            {
                status.Error = ex.Message;
                _logger?.LogWarning(ex, "Unable to read key vault secret {CredentialKey}", credentialKey);
            }

            return status;
        }

        private async Task<OCPICredentialProvisioningResult> VerifyOpenFeedAsync(OCPICredentialProvisioningRequest request, OCPICredentialProvisioningResult result)
        {
            result.CredentialNotRequired = true;

            var verification = await new OCPIFeedValidator(_logger).VerifyFeedWithStoredCredentialAsync(
                request.LocationsEndpointUrl,
                null,
                null,
                null);

            result.Verification = verification;
            result.Log.AddRange(verification.DiscoveryLog);
            result.Warnings.AddRange(verification.Warnings);

            if (!verification.IsValid)
            {
                result.Errors.Add("The OCPI feed could not be read without an authorization header.");
                result.Errors.AddRange(verification.Errors);
                return result;
            }

            result.IsVerified = true;
            result.IsSuccess = true;
            return result;
        }

        private static string ResolveCredentialKey(OCPICredentialProvisioningRequest request, OCPICredentialProvisioningResult result)
        {
            var credentialKey = OCPICredentialNaming.NormaliseCredentialKey(request.CredentialKey)
                ?? OCPICredentialNaming.BuildCredentialKey(request.ProviderName);

            if (credentialKey == null)
            {
                result.Errors.Add("Unable to derive a key vault secret name. Set a provider name or an explicit secret name.");
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.CredentialKey)
                && !string.Equals(credentialKey, request.CredentialKey.Trim(), StringComparison.Ordinal))
            {
                result.Warnings.Add($"Secret name '{request.CredentialKey.Trim()}' was normalised to '{credentialKey}'.");
            }

            return credentialKey;
        }

        private async Task<KeyVaultSecret> TryGetSecretAsync(string credentialKey, OCPICredentialProvisioningResult result, CancellationToken cancellationToken)
        {
            if (!IsVaultConfigured)
            {
                return null;
            }

            try
            {
                var secret = await _secretClient.Value.GetSecretAsync(credentialKey, cancellationToken: cancellationToken);
                return secret.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                result.Log.Add($"No existing key vault secret named '{credentialKey}'.");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Unable to read the existing key vault secret '{credentialKey}': {ex.Message}");
                _logger?.LogWarning(ex, "Unable to read key vault secret {CredentialKey}", credentialKey);
            }

            return null;
        }

        private (string Value, OCPICredentialSource Source) ResolveCredentialValue(
            OCPICredentialProvisioningRequest request,
            OCPICredentialProvisioningResult result,
            KeyVaultSecret existingSecret)
        {
            if (!string.IsNullOrWhiteSpace(request.SubmittedCredential))
            {
                return (request.SubmittedCredential.Trim(), OCPICredentialSource.Submission);
            }

            if (!string.IsNullOrWhiteSpace(existingSecret?.Value))
            {
                result.Log.Add($"Using the credential already stored in the key vault as '{result.CredentialKey}'.");
                return (existingSecret.Value, OCPICredentialSource.Vault);
            }

            var configured = _configuration[result.CredentialKey];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                result.Log.Add($"Using the credential already present in configuration for '{result.CredentialKey}'.");
                return (configured, OCPICredentialSource.Configuration);
            }

            return (null, OCPICredentialSource.None);
        }

        /// <summary>
        /// Guards against a derived secret name colliding with a credential which belongs to a different
        /// agreement, which would otherwise overwrite, or silently reuse, another provider's credential.
        /// </summary>
        private static bool CheckSecretOwnership(KeyVaultSecret existingSecret, OCPICredentialProvisioningRequest request, OCPICredentialProvisioningResult result)
        {
            if (existingSecret == null)
            {
                return true;
            }

            if (!existingSecret.Properties.Tags.TryGetValue(AgreementTag, out var ownerAgreementId)
                || string.IsNullOrWhiteSpace(ownerAgreementId))
            {
                result.Warnings.Add($"The key vault secret '{result.CredentialKey}' already exists without an agreement tag, so it was probably created by hand. Confirm it belongs to this import.");
                return true;
            }

            if (string.Equals(ownerAgreementId, request.AgreementId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                return true;
            }

            result.Errors.Add($"The key vault secret '{result.CredentialKey}' already belongs to data sharing agreement #{ownerAgreementId}. Set a distinct secret name for this import before approving.");
            return false;
        }

        private async Task<OCPICredentialProvisioningResult> WriteSecretAsync(
            OCPICredentialProvisioningResult result,
            int agreementId,
            string credentialValue,
            CancellationToken cancellationToken)
        {
            try
            {
                var secret = new KeyVaultSecret(result.CredentialKey, credentialValue);
                secret.Properties.ContentType = "text/plain";
                secret.Properties.Tags["source"] = "ocm-import-approval";
                secret.Properties.Tags[AgreementTag] = agreementId.ToString(CultureInfo.InvariantCulture);

                await _secretClient.Value.SetSecretAsync(secret, cancellationToken);

                result.SecretWritten = true;
                result.IsSuccess = true;
                result.Log.Add($"Stored the verified credential in the key vault as '{result.CredentialKey}'.");

                ReloadConfiguration(result);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                result.Errors.Add($"The key vault rejected the write for '{result.CredentialKey}'. The application identity needs the secrets 'set' permission.");
                _logger?.LogError(ex, "Key vault denied writing secret {CredentialKey}", result.CredentialKey);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unable to store the credential '{result.CredentialKey}' in the key vault: {ex.Message}");
                _logger?.LogError(ex, "Error writing key vault secret {CredentialKey}", result.CredentialKey);
            }

            return result;
        }

        /// <summary>
        /// Key vault secrets are loaded into configuration at startup, so reload to make the new credential
        /// usable by import jobs running in this process without a restart.
        /// </summary>
        private void ReloadConfiguration(OCPICredentialProvisioningResult result)
        {
            if (_configuration is not IConfigurationRoot configurationRoot)
            {
                result.Warnings.Add("Configuration could not be reloaded, so a restart may be needed before the new credential is used.");
                return;
            }

            try
            {
                configurationRoot.Reload();

                if (string.IsNullOrWhiteSpace(_configuration[result.CredentialKey]))
                {
                    result.Warnings.Add($"'{result.CredentialKey}' was stored but is not visible in configuration yet. Key vault propagation can lag by a few seconds.");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Configuration reload failed after storing the credential: {ex.Message}");
                _logger?.LogWarning(ex, "Configuration reload failed after storing key vault secret {CredentialKey}", result.CredentialKey);
            }
        }

        private static string DescribeSource(OCPICredentialSource source)
        {
            return source switch
            {
                OCPICredentialSource.Submission => "submitted",
                OCPICredentialSource.Vault => "existing key vault",
                OCPICredentialSource.Configuration => "configured",
                _ => "supplied"
            };
        }

        private SecretClient CreateSecretClient()
        {
            if (!IsVaultConfigured)
            {
                return null;
            }

            return new SecretClient(
                new Uri(_importSettings.KeyVaultUri),
                new ClientSecretCredential(
                    _importSettings.KeyVaultTenantId,
                    _importSettings.KeyVaultClientId,
                    _importSettings.KeyVaultSecret));
        }
    }
}
