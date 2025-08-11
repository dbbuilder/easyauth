using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Factory interface for creating and managing authentication providers
    /// </summary>
    public interface IEAuthProviderFactory
    {
        /// <summary>
        /// Gets all available authentication providers
        /// </summary>
        /// <returns>Collection of enabled authentication providers</returns>
        Task<IEnumerable<IEAuthProvider>> GetProvidersAsync();

        /// <summary>
        /// Gets a specific authentication provider by name
        /// </summary>
        /// <param name="providerName">Provider name (case-insensitive)</param>
        /// <returns>Authentication provider or null if not found/disabled</returns>
        Task<IEAuthProvider?> GetProviderAsync(string providerName);

        /// <summary>
        /// Gets the default authentication provider
        /// </summary>
        /// <returns>Default authentication provider</returns>
        Task<IEAuthProvider?> GetDefaultProviderAsync();

        /// <summary>
        /// Validates all provider configurations
        /// </summary>
        /// <returns>Validation result with errors if any</returns>
        Task<ProviderValidationResult> ValidateProvidersAsync();

        /// <summary>
        /// Gets provider information for client applications
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <returns>Provider information or null if not found</returns>
        Task<ProviderInfo?> GetProviderInfoAsync(string providerName);

        /// <summary>
        /// Gets information for all providers
        /// </summary>
        /// <returns>Collection of provider information</returns>
        Task<IEnumerable<ProviderInfo>> GetAllProviderInfoAsync();

        /// <summary>
        /// Registers a custom authentication provider
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <param name="provider">Provider instance</param>
        Task RegisterCustomProviderAsync(string providerName, IEAuthProvider provider);

        /// <summary>
        /// Gets provider capabilities
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <returns>Provider capabilities or null if not found</returns>
        Task<ProviderCapabilities?> GetProviderCapabilitiesAsync(string providerName);

        /// <summary>
        /// Gets providers that support a specific capability
        /// </summary>
        /// <param name="capability">Capability name (e.g., "PasswordReset", "ProfileEditing")</param>
        /// <returns>Providers that support the capability</returns>
        Task<IEnumerable<IEAuthProvider>> GetProvidersByCapabilityAsync(string capability);

        /// <summary>
        /// Refreshes the provider cache
        /// </summary>
        Task RefreshProviderCacheAsync();

        /// <summary>
        /// Gets provider health status
        /// </summary>
        /// <param name="providerName">Provider name</param>
        /// <returns>Provider health status or null if not found</returns>
        Task<ProviderHealth?> GetProviderHealthAsync(string providerName);

        /// <summary>
        /// Gets health status for all providers
        /// </summary>
        /// <returns>Collection of provider health statuses</returns>
        Task<IEnumerable<ProviderHealth>> GetAllProviderHealthAsync();
    }
}