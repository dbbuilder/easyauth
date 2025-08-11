using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Authentication provider interface for pluggable authentication strategies
    /// </summary>
    public interface IEAuthProvider
    {
        /// <summary>
        /// Provider name (e.g., "AzureB2C", "Google", "Apple", "Facebook")
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Display name for UI
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Whether this provider is enabled
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Generate authorization URL for OAuth flow
        /// </summary>
        Task<string> GetAuthorizationUrlAsync(string? returnUrl = null);
        
        /// <summary>
        /// Exchange authorization code for access tokens
        /// </summary>
        Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string? state = null);
        
        /// <summary>
        /// Get user information from provider using tokens
        /// </summary>
        Task<UserInfo> GetUserInfoAsync(TokenResponse tokens);
        
        /// <summary>
        /// Generate login URL for this provider
        /// </summary>
        Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null);
        
        /// <summary>
        /// Handle authentication callback from provider
        /// </summary>
        Task<EAuthResponse<UserInfo>> HandleCallbackAsync(string code, string? state = null);
        
        /// <summary>
        /// Generate logout URL for this provider
        /// </summary>
        Task<string> GetLogoutUrlAsync(string? returnUrl = null);
        
        /// <summary>
        /// Get password reset URL (if supported by provider)
        /// </summary>
        Task<string?> GetPasswordResetUrlAsync(string email);
        
        /// <summary>
        /// Validate provider-specific configuration
        /// </summary>
        Task<bool> ValidateConfigurationAsync();
    }
}
