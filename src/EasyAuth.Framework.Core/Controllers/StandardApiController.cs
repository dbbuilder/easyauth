using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Extensions;
using System.Security.Claims;

namespace EasyAuth.Framework.Core.Controllers;

/// <summary>
/// Standard API endpoints that frontend applications expect
/// Based on real-world integration experience and common patterns
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class StandardApiController : ControllerBase
{
    private readonly IEAuthService _authService;
    private readonly ILogger<StandardApiController> _logger;

    public StandardApiController(
        IEAuthService authService,
        ILogger<StandardApiController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Check authentication status - the most commonly needed endpoint
    /// Frontend apps call this to determine if user is logged in
    /// </summary>
    [HttpGet("auth-check")]
    public async Task<IActionResult> CheckAuthStatus()
    {
        try
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return this.AuthStatus(false);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.AuthStatus(false);
            }

            // Get user profile from database
            var userProfile = await _authService.GetUserProfileAsync(userId);
            
            // Create consistent user info
            var userInfo = new Models.ApiUserInfo
            {
                Id = userId,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? userProfile?.Name,
                FirstName = userProfile?.FirstName,
                LastName = userProfile?.LastName,
                ProfilePicture = userProfile?.ProfilePictureUrl,
                Provider = User.FindFirst("provider")?.Value ?? "unknown",
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
                LastLogin = userProfile?.LastLoginAt,
                IsVerified = true,
                Locale = userProfile?.Locale,
                TimeZone = userProfile?.TimeZone
            };

            return this.AuthStatus(
                isAuthenticated: true, 
                user: userInfo,
                tokenExpiry: GetTokenExpiry(),
                sessionId: User.FindFirst("session_id")?.Value
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return this.ApiInternalError("Failed to check authentication status", new { exception = ex.Message });
        }
    }

    /// <summary>
    /// Login endpoint that frontends expect
    /// Redirects to OAuth provider or handles direct login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Models.LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Provider))
            {
                return this.ApiBadRequest(
                    ErrorCodes.MissingParameter, 
                    "Provider is required", 
                    new { parameter = "provider", supportedProviders = new[] { "Google", "Facebook", "AzureB2C", "Apple" } }
                );
            }

            // Handle different login types - Note: Models.LoginRequest doesn't have Password field, all OAuth for now
            if (!string.IsNullOrEmpty(request.Email))
            {
                // Direct email/password login
                return await HandleDirectLogin(request);
            }
            else
            {
                // OAuth provider login
                return await HandleOAuthLogin(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt");
            return this.ApiInternalError("Login failed", new { exception = ex.Message });
        }
    }

    /// <summary>
    /// Logout endpoint - clears session and tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                await _authService.SignOutUserAsync(User);
            }

            return this.LogoutResponse(loggedOut: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Even if there's an error, we want to indicate logout success to the frontend
            return this.LogoutResponse(loggedOut: true);
        }
    }

    /// <summary>
    /// Refresh JWT tokens
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return this.ApiBadRequest(
                    ErrorCodes.MissingParameter,
                    "Refresh token is required",
                    new { parameter = "refreshToken" }
                );
            }

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            
            if (!result.Success)
            {
                return this.TokenExpired("Invalid or expired refresh token. Please log in again.");
            }

            return this.TokenRefreshResponse(
                result.AccessToken!,
                result.RefreshToken,
                result.ExpiresIn
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return this.ApiInternalError("Token refresh failed", new { exception = ex.Message });
        }
    }

    /// <summary>
    /// Get user profile - commonly needed endpoint
    /// </summary>
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.ApiUnauthorized("User ID not found in authentication token");
            }

            var profile = await _authService.GetUserProfileAsync(userId);
            
            if (profile == null)
            {
                return this.ApiNotFound("User profile not found");
            }

            // Convert to consistent API format
            var userInfo = new Models.ApiUserInfo
            {
                Id = profile.Id,
                Email = profile.Email,
                Name = profile.Name,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                ProfilePicture = profile.ProfilePictureUrl,
                Provider = User.FindFirst("provider")?.Value,
                Roles = profile.Roles.ToArray(),
                LastLogin = profile.LastLoginAt,
                IsVerified = true,
                Locale = profile.Locale,
                TimeZone = profile.TimeZone
            };

            return this.ApiOk(userInfo, "Profile retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return this.ApiInternalError("Failed to retrieve profile", new { exception = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        var healthData = new
        {
            status = "healthy",
            service = "EasyAuth API",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0-alpha.1"
        };

        return this.ApiOk(healthData, "Service is healthy");
    }

    #region Private Helper Methods

    private async Task<IActionResult> HandleDirectLogin(Models.LoginRequest request)
    {
        // For future implementation of direct email/password login
        await Task.CompletedTask;
        return this.ApiBadRequest(
            "DIRECT_LOGIN_NOT_IMPLEMENTED",
            "Direct email/password login is not yet implemented",
            new { supportedMethods = new[] { "OAuth" } }
        );
    }

    private async Task<IActionResult> HandleOAuthLogin(Models.LoginRequest request)
    {
        var result = await _authService.InitiateAuthenticationAsync(
            request.Provider!,
            request.ReturnUrl);

        if (!result.Success)
        {
            if (result.Error == "unsupported_provider")
            {
                return this.InvalidProvider(request.Provider!);
            }

            return this.ApiBadRequest(
                result.Error ?? "OAUTH_INITIATION_FAILED",
                "Failed to initiate OAuth login",
                new { provider = request.Provider }
            );
        }

        return this.LoginResponse(
            authUrl: result.AuthUrl,
            provider: request.Provider,
            state: result.State,
            redirectRequired: true
        );
    }

    private DateTimeOffset? GetTokenExpiry()
    {
        var expClaim = User.FindFirst("exp")?.Value;
        if (long.TryParse(expClaim, out var exp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(exp);
        }
        return null;
    }

    #endregion
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}