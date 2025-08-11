using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Main authentication service interface
    /// </summary>
    public interface IEAuthService
    {
        /// <summary>
        /// Get all available authentication providers
        /// </summary>
        Task<EAuthResponse<IEnumerable<ProviderInfo>>> GetProvidersAsync();
        
        /// <summary>
        /// Initiate login with specific provider
        /// </summary>
        Task<EAuthResponse<string>> InitiateLoginAsync(LoginRequest request);
        
        /// <summary>
        /// Handle authentication callback
        /// </summary>
        Task<EAuthResponse<UserInfo>> HandleAuthCallbackAsync(string provider, string code, string? state = null);
        
        /// <summary>
        /// Sign out user and invalidate session
        /// </summary>
        Task<EAuthResponse<bool>> SignOutAsync(string? sessionId = null);
        
        /// <summary>
        /// Get current user information
        /// </summary>
        Task<EAuthResponse<UserInfo>> GetCurrentUserAsync();
        
        /// <summary>
        /// Validate current session
        /// </summary>
        Task<EAuthResponse<SessionInfo>> ValidateSessionAsync(string sessionId);
        
        /// <summary>
        /// Link account from another provider
        /// </summary>
        Task<EAuthResponse<UserInfo>> LinkAccountAsync(string provider, string code, string state);
        
        /// <summary>
        /// Unlink account from provider
        /// </summary>
        Task<EAuthResponse<bool>> UnlinkAccountAsync(string provider);
        
        /// <summary>
        /// Initiate password reset (for supported providers)
        /// </summary>
        Task<EAuthResponse<string>> InitiatePasswordResetAsync(PasswordResetRequest request);
    }
}
