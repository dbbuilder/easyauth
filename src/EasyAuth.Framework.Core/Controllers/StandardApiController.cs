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
    /// </summary>
    /// <remarks>
    /// Frontend apps call this to determine if user is logged in and get complete user information.
    /// 
    /// **Response includes comprehensive claims data:**
    /// - All standard user properties (id, email, name, etc.)
    /// - Complete `claims` dictionary with provider-specific data
    /// - Provider identification for handling different auth sources
    /// - Token expiry information for session management
    /// 
    /// **Claims Examples by Provider:**
    /// 
    /// **Apple Sign-In:**
    /// ```json
    /// {
    ///   "claims": {
    ///     "sub": "001234.567890abcdef.1234",
    ///     "email": "user@example.com",
    ///     "email_verified": "true",
    ///     "aud": "com.yourapp.service",
    ///     "iss": "https://appleid.apple.com"
    ///   }
    /// }
    /// ```
    /// 
    /// **Facebook:**
    /// ```json
    /// {
    ///   "claims": {
    ///     "id": "123456789012345",
    ///     "email": "user@example.com",
    ///     "name": "John Doe",
    ///     "picture": "https://graph.facebook.com/v18.0/123456789/picture",
    ///     "locale": "en_US"
    ///   }
    /// }
    /// ```
    /// 
    /// **Azure B2C:**
    /// ```json
    /// {
    ///   "claims": {
    ///     "oid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    ///     "email": "user@company.com",
    ///     "extension_department": "Engineering",
    ///     "tfp": "B2C_1_SignUpOrSignIn"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Authentication status retrieved successfully with complete user data and claims</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="500">Internal server error occurred</response>
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
            
            // Create consistent user info with comprehensive claims
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
                TimeZone = userProfile?.TimeZone,
                Claims = ExtractAllClaims(User),
                LinkedAccounts = Array.Empty<Models.LinkedAccount>() // TODO: Implement account linking
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
    /// Get complete user profile with all claims data
    /// </summary>
    /// <remarks>
    /// Returns comprehensive user information including all provider-specific claims.
    /// This endpoint provides everything available from the authentication provider.
    /// 
    /// **What's Included:**
    /// - Standard user properties (id, email, name, roles, etc.)
    /// - Complete `claims` dictionary with all raw provider data
    /// - Profile metadata (locale, timezone, last login)
    /// - Provider identification for conditional logic
    /// 
    /// **Claims Dictionary Usage:**
    /// The `claims` field contains all raw data from the authentication provider:
    /// 
    /// ```csharp
    /// // Access claims safely
    /// var department = user.Claims.TryGetValue("extension_department", out var dept) ? dept : "Unknown";
    /// 
    /// // Provider-specific handling
    /// if (user.Provider == "Apple")
    /// {
    ///     var isPrivateEmail = user.Claims["email"].Contains("privaterelay.appleid.com");
    /// }
    /// 
    /// // Azure B2C custom attributes
    /// if (user.Provider == "AzureB2C")
    /// {
    ///     var jobTitle = user.Claims.GetValueOrDefault("jobTitle", "Not specified");
    ///     var userFlow = user.Claims.GetValueOrDefault("tfp", "unknown");
    /// }
    /// ```
    /// 
    /// **Security Note:** Claims may contain sensitive information. Only expose necessary data to frontend applications.
    /// </remarks>
    /// <response code="200">User profile retrieved successfully with all available claims</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User profile not found in database</response>
    /// <response code="500">Internal server error occurred</response>
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

            // Convert to consistent API format with comprehensive claims
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
                TimeZone = profile.TimeZone,
                Claims = ExtractAllClaims(User),
                LinkedAccounts = Array.Empty<Models.LinkedAccount>() // TODO: Implement account linking
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
    /// Get available claims documentation for all providers
    /// </summary>
    /// <remarks>
    /// Returns comprehensive documentation of all available claims for each supported authentication provider.
    /// This endpoint helps developers understand what data is available from each provider.
    /// 
    /// **Use this endpoint to:**
    /// - Discover all available claims for each provider
    /// - Understand claim availability and requirements
    /// - Get examples of claim values and structures
    /// - Plan your application's user data handling
    /// 
    /// **Response includes:**
    /// - Complete claim reference for Apple, Facebook, and Azure B2C
    /// - Availability requirements (always available vs requires permissions)
    /// - Example values and data structures
    /// - Usage patterns and best practices
    /// </remarks>
    /// <response code="200">Claims documentation retrieved successfully</response>
    [HttpGet("claims-reference")]
    public IActionResult GetClaimsReference()
    {
        var claimsReference = new
        {
            version = "2.4.0",
            lastUpdated = "2024-01-15",
            providers = new
            {
                apple = new
                {
                    name = "Apple Sign-In",
                    claims = new[]
                    {
                        new { name = "sub", type = "string", description = "Apple's unique user ID", example = "001234.567890abcdef.1234", alwaysAvailable = true, scope = "default" },
                        new { name = "email", type = "string", description = "User's email address", example = "user@example.com", alwaysAvailable = false, scope = "email" },
                        new { name = "email_verified", type = "string", description = "Email verification status", example = "true", alwaysAvailable = false, scope = "email" },
                        new { name = "aud", type = "string", description = "Your app's Client ID", example = "com.yourapp.service", alwaysAvailable = true, scope = "default" },
                        new { name = "iss", type = "string", description = "Token issuer", example = "https://appleid.apple.com", alwaysAvailable = true, scope = "default" },
                        new { name = "iat", type = "number", description = "Token issued timestamp", example = "1642234567", alwaysAvailable = true, scope = "default" },
                        new { name = "exp", type = "number", description = "Token expiration timestamp", example = "1642238167", alwaysAvailable = true, scope = "default" }
                    },
                    notes = new[]
                    {
                        "Apple may use private email relay (contains 'privaterelay.appleid.com')",
                        "Names are not provided in ID token - DisplayName derived from email",
                        "User can deny email permission - handle gracefully"
                    }
                },
                facebook = new
                {
                    name = "Facebook Login",
                    claims = new[]
                    {
                        new { name = "id", type = "string", description = "Facebook user ID", example = "123456789012345", alwaysAvailable = true, scope = "default" },
                        new { name = "email", type = "string", description = "User's email address", example = "user@example.com", alwaysAvailable = false, scope = "email" },
                        new { name = "name", type = "string", description = "User's full name", example = "John Doe", alwaysAvailable = false, scope = "public_profile" },
                        new { name = "first_name", type = "string", description = "User's first name", example = "John", alwaysAvailable = false, scope = "public_profile" },
                        new { name = "last_name", type = "string", description = "User's last name", example = "Doe", alwaysAvailable = false, scope = "public_profile" },
                        new { name = "picture", type = "string", description = "Profile picture URL", example = "https://graph.facebook.com/v18.0/123456789/picture", alwaysAvailable = false, scope = "public_profile" },
                        new { name = "locale", type = "string", description = "User's locale", example = "en_US", alwaysAvailable = false, scope = "public_profile" },
                        new { name = "timezone", type = "string", description = "Timezone offset from UTC", example = "-8", alwaysAvailable = true, scope = "default" },
                        new { name = "verified", type = "string", description = "Account verification status", example = "true", alwaysAvailable = false, scope = "public_profile" }
                    },
                    notes = new[]
                    {
                        "Picture claim contains JSON structure - extract data.url",
                        "Business features available with business_management scope",
                        "Instagram integration available with instagram_basic scope"
                    }
                },
                azureB2C = new
                {
                    name = "Azure B2C",
                    claims = new[]
                    {
                        new { name = "sub", type = "string", description = "Azure B2C user object ID", example = "a1b2c3d4-e5f6-7890-abcd-ef1234567890", alwaysAvailable = true, scope = "openid" },
                        new { name = "oid", type = "string", description = "Same as sub (Azure AD standard)", example = "a1b2c3d4-e5f6-7890-abcd-ef1234567890", alwaysAvailable = true, scope = "openid" },
                        new { name = "email", type = "string", description = "User's email address", example = "user@company.com", alwaysAvailable = true, scope = "email" },
                        new { name = "name", type = "string", description = "User's display name", example = "John Doe", alwaysAvailable = false, scope = "profile" },
                        new { name = "given_name", type = "string", description = "User's first name", example = "John", alwaysAvailable = false, scope = "profile" },
                        new { name = "family_name", type = "string", description = "User's last name", example = "Doe", alwaysAvailable = false, scope = "profile" },
                        new { name = "tfp", type = "string", description = "Trust Framework Policy name", example = "B2C_1_SignUpOrSignIn", alwaysAvailable = true, scope = "openid" },
                        new { name = "extension_department", type = "string", description = "Custom department attribute", example = "Engineering", alwaysAvailable = false, scope = "custom" },
                        new { name = "jobTitle", type = "string", description = "User's job title", example = "Software Engineer", alwaysAvailable = false, scope = "profile" }
                    },
                    notes = new[]
                    {
                        "Custom attributes use 'extension_' prefix",
                        "Multi-tenant scenarios include 'tid' claim with tenant ID",
                        "Custom policies provide additional claims based on configuration"
                    }
                }
            },
            usage = new
            {
                safeAccess = "user.Claims.TryGetValue(\"claim_name\", out var value) ? value : \"default\"",
                providerCheck = "if (user.Provider == \"Apple\") { /* provider-specific logic */ }",
                customAttributes = "var department = user.Claims.GetValueOrDefault(\"extension_department\", \"Unknown\");",
                security = "Always validate critical claims and handle missing data gracefully"
            }
        };

        return this.ApiOk(claimsReference, "Claims reference documentation");
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

    /// <summary>
    /// Extracts all claims from ClaimsPrincipal for API response
    /// Provides comprehensive access to provider-specific claims for debugging and advanced scenarios
    /// </summary>
    private Dictionary<string, string> ExtractAllClaims(ClaimsPrincipal user)
    {
        var claims = new Dictionary<string, string>();
        
        foreach (var claim in user.Claims)
        {
            // Use the original claim type as the key for raw access
            var key = claim.Type;
            
            // For standard claim types, use friendly names while preserving originals
            var friendlyKey = claim.Type switch
            {
                ClaimTypes.NameIdentifier => "sub",
                ClaimTypes.Email => "email", 
                ClaimTypes.Name => "name",
                ClaimTypes.GivenName => "given_name",
                ClaimTypes.Surname => "family_name",
                ClaimTypes.Role => "role",
                _ => claim.Type
            };
            
            // Avoid duplicate keys - prefer original claim type over friendly names
            if (!claims.ContainsKey(key))
            {
                claims[key] = claim.Value;
            }
            
            // Also add friendly key if different and not already present
            if (friendlyKey != key && !claims.ContainsKey(friendlyKey))
            {
                claims[friendlyKey] = claim.Value;
            }
        }
        
        return claims;
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