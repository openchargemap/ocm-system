using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OCM.Web.Models;

namespace OCM.Web.Services
{
    /// <summary>
    /// Details required to provision the secrets vault credential for an approved OCPI import.
    /// </summary>
    public class OCPICredentialProvisioningRequest
    {
        /// <summary>
        /// Agreement the credential belongs to, used for logging only.
        /// </summary>
        public int AgreementId { get; set; }

        /// <summary>
        /// Provider name from the stored import config, used to derive a secret name when none is supplied.
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Optional admin supplied secret name. When empty a name is derived from the provider name.
        /// </summary>
        public string CredentialKey { get; set; }

        /// <summary>
        /// The locations endpoint the import will call.
        /// </summary>
        public string LocationsEndpointUrl { get; set; }

        /// <summary>
        /// The auth header key the import will use, null/empty for an open feed.
        /// </summary>
        public string AuthHeaderKey { get; set; }

        /// <summary>
        /// The auth header value prefix the import will apply, e.g. "Token ".
        /// </summary>
        public string AuthHeaderValuePrefix { get; set; }

        /// <summary>
        /// The plaintext credential captured with the submission, if it is still held.
        /// </summary>
        public string SubmittedCredential { get; set; }
    }

    /// <summary>
    /// Where the credential used for verification came from.
    /// </summary>
    public enum OCPICredentialSource
    {
        None,
        Submission,
        Vault,
        Configuration
    }

    /// <summary>
    /// Outcome of verifying an OCPI feed and storing its credential in the secrets vault.
    /// </summary>
    public class OCPICredentialProvisioningResult
    {
        /// <summary>
        /// True when the import can run: the feed was verified and any required credential is stored.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// True when a live request to the feed succeeded using the exact configuration and credential.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// True when a new secret value was written to the vault during this operation.
        /// </summary>
        public bool SecretWritten { get; set; }

        /// <summary>
        /// True when the vault already held the same credential value, so no write was needed.
        /// </summary>
        public bool SecretAlreadyCurrent { get; set; }

        /// <summary>
        /// True when the feed needs no authorization header and therefore no stored credential.
        /// </summary>
        public bool CredentialNotRequired { get; set; }

        /// <summary>
        /// The vault secret name (and stored config CredentialKey) that was used.
        /// </summary>
        public string CredentialKey { get; set; }

        public OCPICredentialSource CredentialSource { get; set; } = OCPICredentialSource.None;

        public OCPIValidationResult Verification { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> Warnings { get; set; } = new List<string>();

        public List<string> Log { get; set; } = new List<string>();

        /// <summary>
        /// Short single line summary suitable for an admin status message.
        /// </summary>
        public string Summary
        {
            get
            {
                if (!IsSuccess)
                {
                    return Errors.Count > 0 ? Errors[0] : "Credential provisioning failed.";
                }

                if (CredentialNotRequired)
                {
                    return "Feed verified. No authorization header is required, so no vault credential was created.";
                }

                if (SecretWritten)
                {
                    return $"Feed verified and credential stored in the key vault as '{CredentialKey}'.";
                }

                if (SecretAlreadyCurrent)
                {
                    return $"Feed verified using the existing key vault credential '{CredentialKey}'.";
                }

                return $"Feed verified using credential '{CredentialKey}'.";
            }
        }
    }

    /// <summary>
    /// Current state of a credential in the secrets vault.
    /// </summary>
    public class OCPICredentialStatus
    {
        public string CredentialKey { get; set; }

        public bool VaultAvailable { get; set; }

        public bool Exists { get; set; }

        public bool IsEnabled { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }

        public string Error { get; set; }
    }

    /// <summary>
    /// Creates and verifies the secrets vault credentials used by approved OCPI imports.
    /// </summary>
    public interface IOCPICredentialService
    {
        /// <summary>
        /// True when the vault is configured for writes.
        /// </summary>
        bool IsVaultConfigured { get; }

        /// <summary>
        /// Verifies the feed with the resolved credential and, when it works, stores that credential in the vault.
        /// No secret is written unless verification succeeds.
        /// </summary>
        Task<OCPICredentialProvisioningResult> ProvisionCredentialAsync(OCPICredentialProvisioningRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the current vault state of a credential, for display during review.
        /// </summary>
        Task<OCPICredentialStatus> GetCredentialStatusAsync(string credentialKey, CancellationToken cancellationToken = default);
    }
}
