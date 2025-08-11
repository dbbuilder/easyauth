using System.ComponentModel.DataAnnotations;

namespace EasyAuth.Framework.Core.Models
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class EAuthResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// User information model
    /// </summary>
    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> Claims { get; set; } = new();
        public bool IsAuthenticated { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public UserAccount[] LinkedAccounts { get; set; } = Array.Empty<UserAccount>();
    }

    /// <summary>
    /// User account model for provider linking
    /// </summary>
    public class UserAccount
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTimeOffset LinkedDate { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Session information model
    /// </summary>
    public class SessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsValid { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;
        
        public string? Email { get; set; }
        public string? ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Password reset request model
    /// </summary>
    public class PasswordResetRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Provider { get; set; } = "AzureB2C";
    }

    /// <summary>
    /// Provider information model
    /// </summary>
    public class ProviderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string LoginUrl { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] SupportedScopes { get; set; } = Array.Empty<string>();
        public ProviderCapabilities Capabilities { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Provider capabilities and features
    /// </summary>
    public class ProviderCapabilities
    {
        /// <summary>
        /// Supports password reset functionality
        /// </summary>
        public bool SupportsPasswordReset { get; set; }

        /// <summary>
        /// Supports profile editing
        /// </summary>
        public bool SupportsProfileEditing { get; set; }

        /// <summary>
        /// Supports account linking
        /// </summary>
        public bool SupportsAccountLinking { get; set; }

        /// <summary>
        /// Supports refresh tokens
        /// </summary>
        public bool SupportsRefreshTokens { get; set; }

        /// <summary>
        /// Supports logout URL
        /// </summary>
        public bool SupportsLogout { get; set; }

        /// <summary>
        /// Supported authentication methods
        /// </summary>
        public string[] SupportedMethods { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Supported OAuth scopes
        /// </summary>
        public string[] SupportedScopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Maximum session duration in minutes
        /// </summary>
        public int MaxSessionDurationMinutes { get; set; } = 1440; // 24 hours default
    }

    /// <summary>
    /// Provider health status
    /// </summary>
    public class ProviderHealth
    {
        /// <summary>
        /// Provider name
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the provider is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        public DateTimeOffset LastChecked { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Health check error message (if any)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional health metrics
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Provider validation result
    /// </summary>
    public class ProviderValidationResult
    {
        /// <summary>
        /// Whether all providers are valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error messages
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Provider-specific validation results
        /// </summary>
        public Dictionary<string, bool> ProviderResults { get; set; } = new();

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
