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
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
