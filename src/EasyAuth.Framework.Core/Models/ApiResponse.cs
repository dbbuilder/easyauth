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
/// User information in consistent API format
/// </summary>
public class ApiUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();

    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("lastLogin")]
    public DateTimeOffset? LastLogin { get; set; }

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }
}