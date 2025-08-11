using System.Collections.Concurrent;
using System.Diagnostics;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Factory for creating and managing authentication providers
    /// Follows TDD methodology - minimal implementation to make tests pass
    /// </summary>
    public class EAuthProviderFactory : IEAuthProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EAuthOptions _options;
        private readonly ILogger<EAuthProviderFactory> _logger;
        private readonly ConcurrentDictionary<string, IEAuthProvider> _providerCache;
        private readonly ConcurrentDictionary<string, IEAuthProvider> _customProviders;
        private readonly ConcurrentDictionary<string, ProviderHealth> _healthCache;

        // Provider type mappings
        private readonly Dictionary<string, Type> _providerTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Google"] = typeof(GoogleAuthProvider),
            ["AzureB2C"] = typeof(AzureB2CAuthProvider),
            ["Facebook"] = typeof(FacebookAuthProvider),
            ["Apple"] = typeof(AppleAuthProvider)
        };

        public EAuthProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<EAuthOptions> options,
            ILogger<EAuthProviderFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _providerCache = new ConcurrentDictionary<string, IEAuthProvider>(StringComparer.OrdinalIgnoreCase);
            _customProviders = new ConcurrentDictionary<string, IEAuthProvider>(StringComparer.OrdinalIgnoreCase);
            _healthCache = new ConcurrentDictionary<string, ProviderHealth>(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IEnumerable<IEAuthProvider>> GetProvidersAsync()
        {
            // TDD GREEN Phase: Return all enabled providers
            await Task.CompletedTask;

            var providers = new List<IEAuthProvider>();

            // Check each provider configuration
            if (_options.Providers.Google?.Enabled == true)
            {
                var provider = await GetProviderAsync("Google");
                if (provider != null) providers.Add(provider);
            }

            if (_options.Providers.AzureB2C?.Enabled == true)
            {
                var provider = await GetProviderAsync("AzureB2C");
                if (provider != null) providers.Add(provider);
            }

            if (_options.Providers.Facebook?.Enabled == true)
            {
                var provider = await GetProviderAsync("Facebook");
                if (provider != null) providers.Add(provider);
            }

            if (_options.Providers.Apple?.Enabled == true)
            {
                var provider = await GetProviderAsync("Apple");
                if (provider != null) providers.Add(provider);
            }

            // Add custom providers
            providers.AddRange(_customProviders.Values.Where(p => p.IsEnabled));

            return providers;
        }

        public async Task<IEAuthProvider?> GetProviderAsync(string providerName)
        {
            // TDD GREEN Phase: Return provider by name with caching
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(providerName))
                return null;

            // Check custom providers first
            if (_customProviders.TryGetValue(providerName, out var customProvider))
            {
                return customProvider.IsEnabled ? customProvider : null;
            }

            // Check cache first
            if (_providerCache.TryGetValue(providerName, out var cachedProvider))
            {
                return cachedProvider.IsEnabled ? cachedProvider : null;
            }

            // Create provider if not cached
            var provider = CreateProvider(providerName);
            if (provider != null)
            {
                _providerCache.TryAdd(providerName, provider);
                return provider.IsEnabled ? provider : null;
            }

            return null;
        }

        public async Task<IEAuthProvider?> GetDefaultProviderAsync()
        {
            // TDD GREEN Phase: Return configured default or first enabled
            await Task.CompletedTask;

            var defaultProviderName = _options.Providers.DefaultProvider;
            
            if (!string.IsNullOrWhiteSpace(defaultProviderName))
            {
                var defaultProvider = await GetProviderAsync(defaultProviderName);
                if (defaultProvider != null)
                    return defaultProvider;
            }

            // Return first enabled provider
            var providers = await GetProvidersAsync();
            return providers.FirstOrDefault();
        }

        public async Task<ProviderValidationResult> ValidateProvidersAsync()
        {
            // TDD GREEN Phase: Validate all provider configurations
            var result = new ProviderValidationResult { IsValid = true };

            try
            {
                var providers = await GetProvidersAsync();

                foreach (var provider in providers)
                {
                    try
                    {
                        var isValid = await provider.ValidateConfigurationAsync();
                        result.ProviderResults[provider.ProviderName] = isValid;

                        if (!isValid)
                        {
                            result.IsValid = false;
                            result.ValidationErrors.Add($"{provider.ProviderName} configuration is invalid");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.IsValid = false;
                        result.ProviderResults[provider.ProviderName] = false;
                        result.ValidationErrors.Add($"{provider.ProviderName} validation failed: {ex.Message}");
                        _logger.LogError(ex, "Error validating provider {ProviderName}", provider.ProviderName);
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationErrors.Add($"General validation error: {ex.Message}");
                _logger.LogError(ex, "Error during provider validation");
            }

            return result;
        }

        public async Task<ProviderInfo?> GetProviderInfoAsync(string providerName)
        {
            // TDD GREEN Phase: Return provider metadata
            await Task.CompletedTask;

            var provider = await GetProviderAsync(providerName);
            if (provider == null)
                return null;

            try
            {
                var loginUrl = await provider.GetLoginUrlAsync();
                var capabilities = await GetProviderCapabilitiesAsync(providerName);

                return new ProviderInfo
                {
                    Name = provider.ProviderName,
                    DisplayName = provider.DisplayName,
                    IsEnabled = provider.IsEnabled,
                    LoginUrl = loginUrl,
                    IconUrl = GetProviderIconUrl(providerName),
                    Description = GetProviderDescription(providerName),
                    SupportedScopes = GetProviderScopes(providerName),
                    Capabilities = capabilities ?? new ProviderCapabilities()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider info for {ProviderName}", providerName);
                return null;
            }
        }

        public async Task<IEnumerable<ProviderInfo>> GetAllProviderInfoAsync()
        {
            // TDD GREEN Phase: Return info for all providers
            var providerInfos = new List<ProviderInfo>();

            // Check all possible providers (including disabled ones for metadata)
            var allProviderNames = new[] { "Google", "AzureB2C", "Facebook", "Apple" }
                .Concat(_customProviders.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var providerName in allProviderNames)
            {
                var providerInfo = await GetProviderInfoAsync(providerName);
                if (providerInfo != null)
                {
                    providerInfos.Add(providerInfo);
                }
            }

            return providerInfos;
        }

        public async Task RegisterCustomProviderAsync(string providerName, IEAuthProvider provider)
        {
            // TDD GREEN Phase: Register custom provider
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _customProviders.AddOrUpdate(providerName, provider, (key, existing) => provider);
            
            _logger.LogInformation("Registered custom provider: {ProviderName}", providerName);
        }

        public async Task<ProviderCapabilities?> GetProviderCapabilitiesAsync(string providerName)
        {
            // TDD GREEN Phase: Return provider capabilities
            await Task.CompletedTask;

            var provider = await GetProviderAsync(providerName);
            if (provider == null)
                return null;

            // Define capabilities based on provider type
            return providerName.ToLowerInvariant() switch
            {
                "google" => new ProviderCapabilities
                {
                    SupportsPasswordReset = true,
                    SupportsProfileEditing = false,
                    SupportsAccountLinking = true,
                    SupportsRefreshTokens = true,
                    SupportsLogout = true,
                    SupportedMethods = new[] { "OAuth2", "OpenIDConnect" },
                    SupportedScopes = new[] { "openid", "profile", "email" },
                    MaxSessionDurationMinutes = 60
                },
                "azureb2c" => new ProviderCapabilities
                {
                    SupportsPasswordReset = true,
                    SupportsProfileEditing = true,
                    SupportsAccountLinking = true,
                    SupportsRefreshTokens = true,
                    SupportsLogout = true,
                    SupportedMethods = new[] { "OAuth2", "OpenIDConnect", "SAML" },
                    SupportedScopes = new[] { "openid", "profile", "email", "offline_access" },
                    MaxSessionDurationMinutes = 1440
                },
                "facebook" => new ProviderCapabilities
                {
                    SupportsPasswordReset = false,
                    SupportsProfileEditing = false,
                    SupportsAccountLinking = true,
                    SupportsRefreshTokens = true,
                    SupportsLogout = true,
                    SupportedMethods = new[] { "OAuth2" },
                    SupportedScopes = new[] { "email", "public_profile" },
                    MaxSessionDurationMinutes = 5183940 // ~10 years (Facebook's long-lived tokens)
                },
                "apple" => new ProviderCapabilities
                {
                    SupportsPasswordReset = false,
                    SupportsProfileEditing = false,
                    SupportsAccountLinking = false,
                    SupportsRefreshTokens = true,
                    SupportsLogout = false,
                    SupportedMethods = new[] { "OAuth2", "JWT" },
                    SupportedScopes = new[] { "name", "email" },
                    MaxSessionDurationMinutes = 525600 // 1 year (Apple's refresh token duration)
                },
                _ => new ProviderCapabilities()
            };
        }

        public async Task<IEnumerable<IEAuthProvider>> GetProvidersByCapabilityAsync(string capability)
        {
            // TDD GREEN Phase: Filter providers by capability
            var providers = await GetProvidersAsync();
            var filteredProviders = new List<IEAuthProvider>();

            foreach (var provider in providers)
            {
                var capabilities = await GetProviderCapabilitiesAsync(provider.ProviderName);
                if (capabilities == null)
                    continue;

                var supportsCapability = capability.ToLowerInvariant() switch
                {
                    "passwordreset" => capabilities.SupportsPasswordReset,
                    "profileediting" => capabilities.SupportsProfileEditing,
                    "accountlinking" => capabilities.SupportsAccountLinking,
                    "refreshtokens" => capabilities.SupportsRefreshTokens,
                    "logout" => capabilities.SupportsLogout,
                    _ => false
                };

                if (supportsCapability)
                {
                    filteredProviders.Add(provider);
                }
            }

            return filteredProviders;
        }

        public async Task RefreshProviderCacheAsync()
        {
            // TDD GREEN Phase: Clear and refresh provider cache
            await Task.CompletedTask;

            _providerCache.Clear();
            _healthCache.Clear();

            _logger.LogInformation("Provider cache refreshed");
        }

        public async Task<ProviderHealth?> GetProviderHealthAsync(string providerName)
        {
            // TDD GREEN Phase: Check provider health
            await Task.CompletedTask;

            var provider = await GetProviderAsync(providerName);
            if (provider == null)
                return null;

            // Check cache first
            if (_healthCache.TryGetValue(providerName, out var cachedHealth) && 
                cachedHealth.LastChecked > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                return cachedHealth;
            }

            // Perform health check
            var stopwatch = Stopwatch.StartNew();
            var health = new ProviderHealth
            {
                ProviderName = providerName,
                LastChecked = DateTimeOffset.UtcNow
            };

            try
            {
                var isHealthy = await provider.ValidateConfigurationAsync();
                health.IsHealthy = isHealthy;
                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

                if (!isHealthy)
                {
                    health.ErrorMessage = $"Provider {providerName} configuration validation failed";
                }
            }
            catch (Exception ex)
            {
                health.IsHealthy = false;
                health.ErrorMessage = ex.Message;
                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Health check failed for provider {ProviderName}", providerName);
            }

            // Cache the result
            _healthCache.AddOrUpdate(providerName, health, (key, existing) => health);

            return health;
        }

        public async Task<IEnumerable<ProviderHealth>> GetAllProviderHealthAsync()
        {
            // TDD GREEN Phase: Check health of all providers
            var healthResults = new List<ProviderHealth>();

            var allProviderNames = new[] { "Google", "AzureB2C", "Facebook", "Apple" }
                .Concat(_customProviders.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var providerName in allProviderNames)
            {
                var health = await GetProviderHealthAsync(providerName);
                if (health != null)
                {
                    healthResults.Add(health);
                }
            }

            return healthResults;
        }

        #region Private Helper Methods

        private IEAuthProvider? CreateProvider(string providerName)
        {
            try
            {
                if (!_providerTypes.TryGetValue(providerName, out var providerType))
                    return null;

                // Check if provider is enabled
                var isEnabled = providerName.ToLowerInvariant() switch
                {
                    "google" => _options.Providers.Google?.Enabled == true,
                    "azureb2c" => _options.Providers.AzureB2C?.Enabled == true,
                    "facebook" => _options.Providers.Facebook?.Enabled == true,
                    "apple" => _options.Providers.Apple?.Enabled == true,
                    _ => false
                };

                if (!isEnabled)
                    return null;

                // Try to get provider from DI container
                var provider = _serviceProvider.GetService(providerType) as IEAuthProvider;
                
                if (provider == null)
                {
                    _logger.LogWarning("Provider {ProviderName} not registered in DI container", providerName);
                }

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider {ProviderName}", providerName);
                return null;
            }
        }

        private string GetProviderIconUrl(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "google" => "https://developers.google.com/identity/images/g-logo.png",
                "azureb2c" => "https://docs.microsoft.com/en-us/azure/active-directory-b2c/media/overview/azureadb2c_icon.png",
                "facebook" => "https://www.facebook.com/images/fb_icon_325x325.png",
                "apple" => "https://appleid.cdn-apple.com/appleid/button",
                _ => string.Empty
            };
        }

        private string GetProviderDescription(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "google" => "Sign in with your Google account",
                "azureb2c" => "Sign in with your organization account",
                "facebook" => "Sign in with your Facebook account",
                "apple" => "Sign in with your Apple ID",
                _ => $"Sign in with {providerName}"
            };
        }

        private string[] GetProviderScopes(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "google" => _options.Providers.Google?.Scopes ?? new[] { "openid", "profile", "email" },
                "azureb2c" => _options.Providers.AzureB2C?.Scopes ?? new[] { "openid", "profile", "offline_access" },
                "facebook" => _options.Providers.Facebook?.Scopes ?? new[] { "email", "public_profile" },
                "apple" => _options.Providers.Apple?.Scopes ?? new[] { "name", "email" },
                _ => Array.Empty<string>()
            };
        }

        #endregion
    }
}