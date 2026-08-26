using System;
using System.Text;

namespace OCM.Import.Providers.OCPI
{
    /// <summary>
    /// Naming rules for the secrets vault entries which hold OCPI import credentials.
    /// The import worker and website both surface vault secrets as configuration keys, so
    /// the secret name is also the <see cref="OCPIProviderConfiguration.CredentialKey"/> value.
    /// </summary>
    public static class OCPICredentialNaming
    {
        /// <summary>
        /// Prefix used to identify OCPI import credentials in the vault (and to filter them out of configuration).
        /// </summary>
        public const string CredentialKeyPrefix = "OCPI-";

        /// <summary>
        /// Azure Key Vault limits secret names to 127 characters of [0-9a-zA-Z-].
        /// </summary>
        public const int MaxSecretNameLength = 127;

        /// <summary>
        /// Builds the conventional credential key for a provider, e.g. "my provider!" becomes "OCPI-MY-PROVIDER".
        /// Returns null if no usable name can be derived.
        /// </summary>
        public static string BuildCredentialKey(string providerName)
        {
            var sanitised = Sanitise(providerName);

            if (string.IsNullOrEmpty(sanitised))
            {
                return null;
            }

            return Truncate(CredentialKeyPrefix + sanitised);
        }

        /// <summary>
        /// Normalises an admin supplied credential key so it is a valid vault secret name and carries the OCPI- prefix.
        /// Returns null if no usable name can be derived.
        /// </summary>
        public static string NormaliseCredentialKey(string credentialKey)
        {
            var sanitised = Sanitise(credentialKey);

            if (string.IsNullOrEmpty(sanitised))
            {
                return null;
            }

            if (sanitised.StartsWith(CredentialKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // re-apply the canonical prefix casing without disturbing the remainder
                sanitised = CredentialKeyPrefix + sanitised.Substring(CredentialKeyPrefix.Length);
                return Truncate(sanitised);
            }

            return Truncate(CredentialKeyPrefix + sanitised);
        }

        /// <summary>
        /// True if the given name is usable as a vault secret name.
        /// </summary>
        public static bool IsValidSecretName(string secretName)
        {
            if (string.IsNullOrEmpty(secretName) || secretName.Length > MaxSecretNameLength)
            {
                return false;
            }

            foreach (var c in secretName)
            {
                if (!IsAllowedCharacter(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Sanitise(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new StringBuilder(value.Length);
            var lastWasSeparator = false;

            foreach (var c in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    builder.Append(c);
                    lastWasSeparator = false;
                }
                else if (!lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        private static string Truncate(string value)
        {
            if (value.Length <= MaxSecretNameLength)
            {
                return value;
            }

            return value.Substring(0, MaxSecretNameLength).TrimEnd('-');
        }

        private static bool IsAllowedCharacter(char c)
        {
            return c == '-' || (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
    }
}
