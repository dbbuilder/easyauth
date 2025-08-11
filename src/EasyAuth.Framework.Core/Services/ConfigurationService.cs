using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EasyAuth.Framework.Core.Configuration;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Unified configuration service that implements graceful fallback pattern for secrets and config values
    /// Fallback chain: Key Vault → Environment Variable → App Settings → Default Value
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly KeyVaultOptions? _keyVaultOptions;

        public ConfigurationService(
            IConfiguration configuration,
            ILogger<ConfigurationService> logger,
            KeyVaultOptions? keyVaultOptions = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyVaultOptions = keyVaultOptions;
        }

        /// <summary>
        /// Retrieves secret values using graceful fallback chain: Key Vault → Environment → App Settings → Default
        /// Logs security-aware messages without exposing actual secret values in logs
        /// </summary>
        public string? GetSecretValue(string key, string? fallbackEnvVar = null, string? defaultValue = null)
        {
            _logger.LogDebug("Looking up secret value for key: {Key}", key);

            // 1. Try Key Vault first (if configured)
            if (_keyVaultOptions?.SecretNames.TryGetValue(key, out var secretName) == true)
            {
                var keyVaultValue = _configuration[secretName];
                if (!string.IsNullOrEmpty(keyVaultValue))
                {
                    _logger.LogDebug("Found secret {Key} in Key Vault", key);
                    return keyVaultValue;
                }
                _logger.LogDebug("Secret {Key} not found in Key Vault (secret name: {SecretName})", key, secretName);
            }

            // 2. Try environment variable
            if (!string.IsNullOrEmpty(fallbackEnvVar))
            {
                var envValue = Environment.GetEnvironmentVariable(fallbackEnvVar);
                if (!string.IsNullOrEmpty(envValue))
                {
                    _logger.LogDebug("Found secret {Key} in environment variable {EnvVar}", key, fallbackEnvVar);
                    return envValue;
                }
                _logger.LogDebug("Secret {Key} not found in environment variable {EnvVar}", key, fallbackEnvVar);
            }

            // 3. Try direct configuration lookup (app settings)
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                _logger.LogDebug("Found secret {Key} in app settings", key);
                return configValue;
            }

            // 4. Return default value
            if (!string.IsNullOrEmpty(defaultValue))
            {
                _logger.LogDebug("Using default value for secret {Key}", key);
                return defaultValue;
            }

            _logger.LogWarning("Secret {Key} not found in any configuration source", key);
            return null;
        }

        /// <summary>
        /// Retrieves required secret values with validation and detailed error reporting
        /// Throws InvalidOperationException with comprehensive source list if secret is not found
        /// </summary>
        public string GetRequiredSecretValue(string key, string? fallbackEnvVar = null)
        {
            var value = GetSecretValue(key, fallbackEnvVar);

            if (string.IsNullOrEmpty(value))
            {
                var sources = new List<string>();
                if (_keyVaultOptions?.SecretNames.ContainsKey(key) == true)
                    sources.Add($"Key Vault ({_keyVaultOptions.SecretNames[key]})");
                if (!string.IsNullOrEmpty(fallbackEnvVar))
                    sources.Add($"Environment Variable ({fallbackEnvVar})");
                sources.Add($"App Settings ({key})");

                var errorMessage = $"Required secret '{key}' not found in any configuration source. " +
                                 $"Searched: {string.Join(", ", sources)}";

                _logger.LogError("SECURITY VIOLATION: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }

        /// <summary>
        /// Retrieves non-sensitive configuration values with Environment → App Settings → Default fallback
        /// Prioritizes environment variables for containerized deployment scenarios
        /// </summary>
        public string? GetConfigValue(string key, string? fallbackEnvVar = null, string? defaultValue = null)
        {
            _logger.LogDebug("Looking up config value for key: {Key}", key);

            // 1. Try environment variable first (for containerized environments)
            if (!string.IsNullOrEmpty(fallbackEnvVar))
            {
                var envValue = Environment.GetEnvironmentVariable(fallbackEnvVar);
                if (!string.IsNullOrEmpty(envValue))
                {
                    _logger.LogDebug("Found config {Key} in environment variable {EnvVar}", key, fallbackEnvVar);
                    return envValue;
                }
            }

            // 2. Try app settings
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                _logger.LogDebug("Found config {Key} in app settings", key);
                return configValue;
            }

            // 3. Return default
            if (!string.IsNullOrEmpty(defaultValue))
            {
                _logger.LogDebug("Using default value for config {Key}", key);
                return defaultValue;
            }

            _logger.LogDebug("Config {Key} not found, returning null", key);
            return null;
        }

        /// <summary>
        /// Validates multiple required secrets and detects placeholder/test values for security
        /// Returns comprehensive error list for configuration validation during startup
        /// </summary>
        public List<string> ValidateRequiredSecrets(Dictionary<string, string> requiredSecrets)
        {
            var errors = new List<string>();

            foreach (var (key, description) in requiredSecrets)
            {
                try
                {
                    var value = GetSecretValue(key);
                    if (string.IsNullOrEmpty(value))
                    {
                        errors.Add($"Missing required secret: {key} ({description})");
                    }
                    else if (value.Contains("dummy") || value.Contains("test") || value.Contains("placeholder"))
                    {
                        errors.Add($"Invalid secret value for {key}: appears to be a placeholder or test value");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating secret {key}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                _logger.LogError("Configuration validation failed with {ErrorCount} errors", errors.Count);
                foreach (var error in errors)
                {
                    _logger.LogError("Config Error: {Error}", error);
                }
            }
            else
            {
                _logger.LogInformation("All required secrets validated successfully");
            }

            return errors;
        }
    }
}