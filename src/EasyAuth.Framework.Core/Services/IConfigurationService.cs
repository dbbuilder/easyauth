using Microsoft.Extensions.Configuration;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Provides unified configuration lookup with Key Vault/environment variable fallback pattern
    /// This service abstracts the complexity of looking up secrets from multiple sources
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets a configuration value with fallback chain: Key Vault → Environment Variable → App Settings → Default
        /// </summary>
        /// <param name="key">The configuration key (e.g., "Apple:JwtSecret")</param>
        /// <param name="fallbackEnvVar">Optional environment variable name to check</param>
        /// <param name="defaultValue">Optional default value if not found anywhere</param>
        /// <returns>The configuration value or null if not found</returns>
        string? GetSecretValue(string key, string? fallbackEnvVar = null, string? defaultValue = null);

        /// <summary>
        /// Gets a required configuration value with fallback chain, throws if not found
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="fallbackEnvVar">Optional environment variable name to check</param>
        /// <exception cref="InvalidOperationException">Thrown when the required value is not found</exception>
        string GetRequiredSecretValue(string key, string? fallbackEnvVar = null);

        /// <summary>
        /// Gets a regular configuration value (non-secret) with environment variable fallback
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="fallbackEnvVar">Optional environment variable name to check</param>
        /// <param name="defaultValue">Optional default value</param>
        string? GetConfigValue(string key, string? fallbackEnvVar = null, string? defaultValue = null);

        /// <summary>
        /// Validates that all required secrets are configured
        /// </summary>
        /// <param name="requiredSecrets">Dictionary of secret keys and their descriptions</param>
        /// <returns>List of validation errors, empty if all secrets are valid</returns>
        List<string> ValidateRequiredSecrets(Dictionary<string, string> requiredSecrets);
    }
}