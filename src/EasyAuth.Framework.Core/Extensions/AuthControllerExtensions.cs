using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Authentication-specific controller extension methods
/// Provides consistent auth-related API responses
/// </summary>
public static class AuthControllerExtensions
{
    /// <summary>
    /// Returns authentication status response
    /// </summary>
    public static IActionResult AuthStatus(this ControllerBase controller, bool isAuthenticated, ApiUserInfo? user = null, DateTimeOffset? tokenExpiry = null, string? sessionId = null)
    {
        var data = new AuthApiResponse.AuthStatus
        {
            IsAuthenticated = isAuthenticated,
            User = user,
            TokenExpiry = tokenExpiry,
            SessionId = sessionId
        };

        var message = isAuthenticated ? "User is authenticated" : "User is not authenticated";
        return controller.ApiOk(data, message);
    }

    /// <summary>
    /// Returns login initiation response
    /// </summary>
    public static IActionResult LoginResponse(this ControllerBase controller, string? authUrl = null, string? provider = null, string? state = null, bool redirectRequired = true)
    {
        var data = new AuthApiResponse.LoginResult
        {
            AuthUrl = authUrl,
            Provider = provider,
            State = state,
            RedirectRequired = redirectRequired
        };

        var message = redirectRequired ? "Redirect to OAuth provider" : "Login initiated successfully";
        return controller.ApiOk(data, message);
    }

    /// <summary>
    /// Returns token refresh response
    /// </summary>
    public static IActionResult TokenRefreshResponse(this ControllerBase controller, string accessToken, string? refreshToken = null, int expiresIn = 3600)
    {
        var data = new AuthApiResponse.TokenRefresh
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };

        return controller.ApiOk(data, "Token refreshed successfully");
    }

    /// <summary>
    /// Returns logout response
    /// </summary>
    public static IActionResult LogoutResponse(this ControllerBase controller, bool loggedOut = true, string? redirectUrl = null)
    {
        var data = new AuthApiResponse.LogoutResult
        {
            LoggedOut = loggedOut,
            RedirectUrl = redirectUrl
        };

        var message = loggedOut ? "Logged out successfully" : "Logout failed";
        return controller.ApiOk(data, message);
    }

    /// <summary>
    /// Returns invalid provider error
    /// </summary>
    public static IActionResult InvalidProvider(this ControllerBase controller, string provider)
    {
        return controller.ApiBadRequest(
            ErrorCodes.InvalidProvider,
            $"Provider '{provider}' is not supported or configured",
            new { provider, supportedProviders = new[] { "Google", "Facebook", "AzureB2C", "Apple" } }
        );
    }

    /// <summary>
    /// Returns token expired error
    /// </summary>
    public static IActionResult TokenExpired(this ControllerBase controller, string? message = null)
    {
        return controller.ApiUnauthorized(message ?? "Authentication token has expired. Please log in again.");
    }

    /// <summary>
    /// Returns session expired error
    /// </summary>
    public static IActionResult SessionExpired(this ControllerBase controller, string? message = null)
    {
        return controller.ApiUnauthorized(message ?? "Session has expired. Please log in again.");
    }

    /// <summary>
    /// Returns provider unavailable error
    /// </summary>
    public static IActionResult ProviderUnavailable(this ControllerBase controller, string provider, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.ApiResponseWithStatus(StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.CreateError(
                ErrorCodes.ProviderUnavailable,
                message ?? $"Authentication provider '{provider}' is currently unavailable",
                new { provider },
                correlationId
            ));
    }

    /// <summary>
    /// Gets or generates a correlation ID for request tracing
    /// </summary>
    private static string GetCorrelationId(ControllerBase controller)
    {
        // Try to get correlation ID from headers
        if (controller.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Try to get from HttpContext items
        if (controller.HttpContext.Items.TryGetValue("CorrelationId", out var contextCorrelationId) && 
            contextCorrelationId is string correlationIdString)
        {
            return correlationIdString;
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString("N")[..12]; // Short correlation ID
        controller.HttpContext.Items["CorrelationId"] = newCorrelationId;
        return newCorrelationId;
    }
}