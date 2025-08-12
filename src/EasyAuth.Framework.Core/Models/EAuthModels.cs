using System.ComponentModel.DataAnnotations;

namespace EasyAuth.Framework.Core.Models
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class EAuthResponse<T>
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The response data (null if operation failed)
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Human-readable message describing the result
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error code for failed operations (null if successful)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Additional metadata about the operation
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// User information model
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Unique user identifier
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the user
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User roles and permissions
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Additional claims from the authentication provider
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = new();

        /// <summary>
        /// Whether the user is currently authenticated
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Last successful login timestamp
        /// </summary>
        public DateTimeOffset? LastLoginDate { get; set; }

        /// <summary>
        /// URL to the user's profile picture
        /// </summary>
        public string ProfilePictureUrl { get; set; } = string.Empty;

        /// <summary>
        /// Authentication provider used for this session
        /// </summary>
        public string AuthProvider { get; set; } = string.Empty;

        /// <summary>
        /// Other linked authentication accounts
        /// </summary>
        public UserAccount[] LinkedAccounts { get; set; } = Array.Empty<UserAccount>();
    }

    /// <summary>
    /// User account model for provider linking
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// Authentication provider name
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// User ID from the authentication provider
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Email address associated with this account
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name from this account
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// When this account was linked
        /// </summary>
        public DateTimeOffset LinkedDate { get; set; }

        /// <summary>
        /// Whether this is the primary authentication account
        /// </summary>
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Session information model
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Unique session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// When the session expires
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Whether the session is currently valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// User ID associated with this session
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// IP address where the session was created
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User agent string from the client
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Authentication provider used for this session
        /// </summary>
        public string AuthProvider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Authentication provider to use for login
        /// </summary>
        [Required]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Email address for login hint (optional)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// URL to redirect to after successful login
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Whether to persist the login session
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// Additional parameters for the authentication provider
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Password reset request model
    /// </summary>
    public class PasswordResetRequest
    {
        /// <summary>
        /// Email address for password reset
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Authentication provider to handle the password reset
        /// </summary>
        public string Provider { get; set; } = "AzureB2C";
    }

    /// <summary>
    /// Provider information model
    /// </summary>
    public class ProviderInfo
    {
        /// <summary>
        /// Internal provider name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable provider name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this provider is enabled and available
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// URL to initiate login with this provider
        /// </summary>
        public string LoginUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to the provider's icon/logo
        /// </summary>
        public string IconUrl { get; set; } = string.Empty;

        /// <summary>
        /// Description of the authentication provider
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// OAuth scopes supported by this provider
        /// </summary>
        public string[] SupportedScopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Provider capabilities and features
        /// </summary>
        public ProviderCapabilities Capabilities { get; set; } = new();

        /// <summary>
        /// Additional provider metadata
        /// </summary>
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

    /// <summary>
    /// User profile model for API responses
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// User unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User display name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// User profile picture URL
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// User locale preference
        /// </summary>
        public string? Locale { get; set; }

        /// <summary>
        /// User timezone preference
        /// </summary>
        public string? TimeZone { get; set; }

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime LastLoginAt { get; set; }

        /// <summary>
        /// User roles
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Custom user claims
        /// </summary>
        public Dictionary<string, object> CustomClaims { get; set; } = new();
    }

    /// <summary>
    /// Token refresh result
    /// </summary>
    public class RefreshTokenResult
    {
        /// <summary>
        /// Whether token refresh was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// New access token
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// New refresh token (if rotated)
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token expiration in seconds
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Error code if refresh failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Human-readable error description
        /// </summary>
        public string? ErrorDescription { get; set; }
    }

    /// <summary>
    /// Authentication initiation result
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Whether authentication initiation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// OAuth provider authorization URL
        /// </summary>
        public string? AuthUrl { get; set; }

        /// <summary>
        /// CSRF protection state parameter
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Error code if initiation failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Human-readable error description
        /// </summary>
        public string? ErrorDescription { get; set; }
    }
}
