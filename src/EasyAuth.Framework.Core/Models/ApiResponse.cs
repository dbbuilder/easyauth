using System.Text.Json.Serialization;

namespace EasyAuth.Framework.Core.Models;

/// <summary>
/// Unified API response format for all EasyAuth endpoints
/// Provides consistent structure that frontend applications can rely on
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The response data (null if operation failed or no data)
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error code for failed operations (null if successful)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Detailed error information for debugging
    /// </summary>
    [JsonPropertyName("errorDetails")]
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp when the response was generated (ISO 8601)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("O");

    /// <summary>
    /// Request correlation ID for tracing
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// API version information
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Additional metadata about the response
    /// </summary>
    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse<object> Ok(string? message = null, string? correlationId = null)
    {
        return new ApiResponse<object>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> CreateError(string error, string? message = null, object? errorDetails = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            Message = message ?? "An error occurred",
            ErrorDetails = errorDetails,
            CorrelationId = correlationId
        };
    }


    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ApiResponse<T> ValidationError(string message, Dictionary<string, string[]>? validationErrors = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = "VALIDATION_ERROR",
            Message = message,
            ErrorDetails = validationErrors,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates an unauthorized response
    /// </summary>
    public static ApiResponse<T> Unauthorized(string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = "UNAUTHORIZED",
            Message = message ?? "Authentication required",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates a forbidden response
    /// </summary>
    public static ApiResponse<T> Forbidden(string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = "FORBIDDEN",
            Message = message ?? "Access denied",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    public static ApiResponse<T> NotFound(string? message = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = "NOT_FOUND",
            Message = message ?? "Resource not found",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates an internal server error response
    /// </summary>
    public static ApiResponse<T> InternalError(string? message = null, object? errorDetails = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = "INTERNAL_ERROR",
            Message = message ?? "An internal error occurred",
            ErrorDetails = errorDetails,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Adds metadata to the response
    /// </summary>
    public ApiResponse<T> WithMeta(string key, object value)
    {
        Meta ??= new Dictionary<string, object>();
        Meta[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID for request tracing
    /// </summary>
    public ApiResponse<T> WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }
}

/// <summary>
/// Authentication-specific API response models
/// </summary>
public static class AuthApiResponse
{
    /// <summary>
    /// Authentication status response
    /// </summary>
    public class AuthStatus
    {
        [JsonPropertyName("isAuthenticated")]
        public bool IsAuthenticated { get; set; }

        [JsonPropertyName("user")]
        public ApiUserInfo? User { get; set; }

        [JsonPropertyName("tokenExpiry")]
        public DateTimeOffset? TokenExpiry { get; set; }

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// Login response
    /// </summary>
    public class LoginResult
    {
        [JsonPropertyName("authUrl")]
        public string? AuthUrl { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("redirectRequired")]
        public bool RedirectRequired { get; set; }
    }

    /// <summary>
    /// Token refresh response
    /// </summary>
    public class TokenRefresh
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }

    /// <summary>
    /// Logout response
    /// </summary>
    public class LogoutResult
    {
        [JsonPropertyName("loggedOut")]
        public bool LoggedOut { get; set; }

        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }
    }
}

/// <summary>
/// Common error codes used throughout EasyAuth API
/// </summary>
public static class ErrorCodes
{
    // Authentication errors
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string SessionExpired = "SESSION_EXPIRED";

    // Authorization errors
    public const string Forbidden = "FORBIDDEN";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";

    // Validation errors
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string MissingParameter = "MISSING_PARAMETER";
    public const string InvalidParameter = "INVALID_PARAMETER";

    // Provider errors
    public const string ProviderError = "PROVIDER_ERROR";
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string InvalidProvider = "INVALID_PROVIDER";
    public const string ProviderConfigurationError = "PROVIDER_CONFIGURATION_ERROR";

    // System errors
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string NetworkError = "NETWORK_ERROR";

    // Rate limiting
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";

    // Resource errors
    public const string NotFound = "NOT_FOUND";
    public const string ResourceExists = "RESOURCE_EXISTS";
    public const string ResourceLocked = "RESOURCE_LOCKED";
}

/// <summary>
/// User information in consistent API format with comprehensive claims documentation
/// </summary>
/// <remarks>
/// This model provides access to user data from all supported authentication providers.
/// The `claims` dictionary contains all raw provider-specific data for advanced scenarios.
/// 
/// **Available Claims by Provider:**
/// 
/// **Apple Sign-In:**
/// - `sub`: Apple's unique user ID (always available)
/// - `email`: User's email address (requires email scope)
/// - `email_verified`: Email verification status ("true"/"false")
/// - `aud`: Your app's Client ID
/// - `iss`: Token issuer ("https://appleid.apple.com")
/// - `iat`, `exp`: Token timestamps
/// 
/// **Facebook:**
/// - `id`: Facebook user ID (always available)
/// - `email`: User's email (requires email permission)
/// - `name`, `first_name`, `last_name`: User's name components
/// - `picture`: Profile picture URL
/// - `locale`: User's locale (e.g., "en_US")
/// - `timezone`: Timezone offset from UTC
/// - `verified`: Account verification status
/// 
/// **Azure B2C:**
/// - `sub`/`oid`: Azure B2C user object ID
/// - `email`, `emails`: User's email address(es)
/// - `name`, `given_name`, `family_name`: User's name components
/// - `extension_*`: Custom attributes (e.g., "extension_department")
/// - `tfp`: Trust Framework Policy name
/// - `aud`: Application ID
/// 
/// **Usage Examples:**
/// ```csharp
/// // Safe claim access
/// string department = user.Claims.TryGetValue("extension_department", out var dept) ? dept : "Unknown";
/// 
/// // Provider-specific handling
/// if (user.Provider == "Apple" &amp;&amp; user.Claims.ContainsKey("email"))
/// {
///     bool isPrivateRelay = user.Claims["email"].Contains("privaterelay.appleid.com");
/// }
/// ```
/// </remarks>
public class ApiUserInfo
{
    /// <summary>
    /// Unique user identifier from the authentication provider
    /// </summary>
    /// <example>"a1b2c3d4-e5f6-7890-abcd-ef1234567890" (Azure B2C), "123456789012345" (Facebook), "001234.567890abcdef.1234" (Apple)</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (availability depends on provider permissions)
    /// </summary>
    /// <example>"user@example.com" or "abc123@privaterelay.appleid.com" (Apple private relay)</example>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// User's display name or full name
    /// </summary>
    /// <example>"John Doe" (Facebook/Azure B2C) or "user" (Apple - derived from email)</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// User's first name (not available from Apple)
    /// </summary>
    /// <example>"John"</example>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (not available from Apple)
    /// </summary>
    /// <example>"Doe"</example>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// URL to user's profile picture (availability depends on provider)
    /// </summary>
    /// <example>"https://graph.facebook.com/v18.0/123456789/picture" (Facebook)</example>
    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }

    /// <summary>
    /// Authentication provider used for this session
    /// </summary>
    /// <example>"Apple", "Facebook", "AzureB2C"</example>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Application-specific user roles
    /// </summary>
    /// <example>["Admin", "User"]</example>
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Application-specific user permissions
    /// </summary>
    /// <example>["read:users", "write:posts"]</example>
    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    /// <example>"2024-01-15T10:30:00Z"</example>
    [JsonPropertyName("lastLogin")]
    public DateTimeOffset? LastLogin { get; set; }

    /// <summary>
    /// Whether the user's email/account is verified
    /// </summary>
    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; }

    /// <summary>
    /// User's locale preference (e.g., "en-US", "fr-FR")
    /// </summary>
    /// <example>"en-US"</example>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// User's timezone preference
    /// </summary>
    /// <example>"America/New_York"</example>
    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// All raw claims from the authentication provider
    /// </summary>
    /// <remarks>
    /// Contains every claim provided by the authentication provider. Use this for:
    /// - Provider-specific data not mapped to standard properties
    /// - Custom attributes from Azure B2C
    /// - Advanced scenarios requiring raw token data
    /// 
    /// **Common Claim Examples:**
    /// - Apple: `sub`, `email`, `email_verified`, `aud`, `iss`
    /// - Facebook: `id`, `email`, `name`, `picture`, `locale`, `timezone`
    /// - Azure B2C: `oid`, `email`, `tfp`, `extension_department`
    /// 
    /// **Safe Access Pattern:**
    /// ```csharp
    /// string value = claims.TryGetValue("claim_name", out var claim) ? claim : "default";
    /// ```
    /// </remarks>
    /// <example>
    /// {
    ///   "sub": "001234.567890abcdef.1234",
    ///   "email": "user@example.com",
    ///   "email_verified": "true",
    ///   "aud": "com.yourapp.service",
    ///   "iss": "https://appleid.apple.com"
    /// }
    /// </example>
    [JsonPropertyName("claims")]
    public Dictionary<string, string> Claims { get; set; } = new();

    /// <summary>
    /// Linked authentication accounts from other providers
    /// </summary>
    /// <remarks>
    /// When account linking is enabled, this contains information about
    /// other authentication providers the user has connected to their account.
    /// </remarks>
    [JsonPropertyName("linkedAccounts")]
    public LinkedAccount[] LinkedAccounts { get; set; } = Array.Empty<LinkedAccount>();
}

/// <summary>
/// Information about a linked authentication account
/// </summary>
public class LinkedAccount
{
    /// <summary>
    /// Authentication provider name
    /// </summary>
    /// <example>"Facebook"</example>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// User ID from the linked provider
    /// </summary>
    /// <example>"987654321"</example>
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Email associated with the linked account
    /// </summary>
    /// <example>"same.user@example.com"</example>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Display name from the linked account
    /// </summary>
    /// <example>"John Doe"</example>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// When this account was linked
    /// </summary>
    /// <example>"2024-01-10T08:00:00Z"</example>
    [JsonPropertyName("linkedAt")]
    public DateTimeOffset LinkedAt { get; set; }

    /// <summary>
    /// Whether this is the primary authentication method
    /// </summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}