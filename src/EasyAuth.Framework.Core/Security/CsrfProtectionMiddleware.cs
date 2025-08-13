using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EasyAuth.Framework.Core.Security;

/// <summary>
/// CSRF protection middleware with token validation
/// Provides comprehensive protection against Cross-Site Request Forgery attacks
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;
    private readonly IAntiforgery _antiforgery;
    private readonly CsrfProtectionOptions _options;

    // Methods that require CSRF protection
    private static readonly string[] ProtectedMethods = { "POST", "PUT", "PATCH", "DELETE" };

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        ILogger<CsrfProtectionMiddleware> logger,
        IAntiforgery antiforgery,
        CsrfProtectionOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _antiforgery = antiforgery;
        _options = options ?? new CsrfProtectionOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip CSRF protection for safe methods or exempt paths
            if (!RequiresCsrfProtection(context))
            {
                await _next(context);
                return;
            }

            // Generate and set CSRF token for GET requests
            if (context.Request.Method == "GET")
            {
                await SetCsrfTokenAsync(context);
                await _next(context);
                return;
            }

            // Validate CSRF token for protected methods
            if (ProtectedMethods.Contains(context.Request.Method))
            {
                var isValid = await ValidateCsrfTokenAsync(context);
                if (!isValid)
                {
                    _logger.LogWarning("CSRF token validation failed for {Method} {Path} from {IP}",
                        context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
                    
                    await HandleCsrfFailure(context);
                    return;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CSRF protection middleware");
            await HandleCsrfError(context, "CSRF validation error");
        }
    }

    private bool RequiresCsrfProtection(HttpContext context)
    {
        // Skip for exempt paths
        if (_options.ExemptPaths.Any(path => 
            context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Skip for API endpoints with API key authentication
        if (context.Request.Headers.ContainsKey("X-API-Key"))
        {
            return false;
        }

        // Skip for CORS preflight requests
        if (context.Request.Method == "OPTIONS")
        {
            return false;
        }

        // Skip for health checks and metrics
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            return false;
        }

        return true;
    }

    private Task SetCsrfTokenAsync(HttpContext context)
    {
        try
        {
            var tokens = _antiforgery.GetAndStoreTokens(context);
            
            // Add token to response headers for SPA consumption
            context.Response.Headers["X-CSRF-Token"] = tokens.RequestToken;
            
            // Set token in cookie for traditional forms
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // JavaScript needs to read this
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(_options.TokenLifetimeHours)
            };
            
            context.Response.Cookies.Append(_options.CookieName, tokens.RequestToken!, cookieOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set CSRF token");
        }
        
        return Task.CompletedTask;
    }

    private async Task<bool> ValidateCsrfTokenAsync(HttpContext context)
    {
        try
        {
            // Check for token in various locations
            var token = GetCsrfTokenFromRequest(context);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No CSRF token found in request from {IP}", 
                    context.Connection.RemoteIpAddress);
                return false;
            }

            // Validate the token
            await _antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning("CSRF token validation failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CSRF token");
            return false;
        }
    }

    private string? GetCsrfTokenFromRequest(HttpContext context)
    {
        // Priority order: Header, Form, Query, Cookie
        
        // 1. Check X-CSRF-Token header (for AJAX requests)
        if (context.Request.Headers.TryGetValue("X-CSRF-Token", out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // 2. Check X-XSRF-TOKEN header (Angular convention)
        if (context.Request.Headers.TryGetValue("X-XSRF-TOKEN", out var xsrfValue))
        {
            return xsrfValue.FirstOrDefault();
        }

        // 3. Check form data
        if (context.Request.HasFormContentType && 
            context.Request.Form.TryGetValue("__RequestVerificationToken", out var formValue))
        {
            return formValue.FirstOrDefault();
        }

        // 4. Check query parameter (less secure, for compatibility)
        if (context.Request.Query.TryGetValue("csrf_token", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        // 5. Check cookie (traditional approach)
        if (context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieValue))
        {
            return cookieValue;
        }

        return null;
    }

    private async Task HandleCsrfFailure(HttpContext context)
    {
        context.Response.StatusCode = 403; // Forbidden
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "CSRF Token Validation Failed",
            message = "The request could not be verified. Please refresh the page and try again.",
            code = "CSRF_TOKEN_INVALID",
            timestamp = DateTimeOffset.UtcNow,
            correlationId = context.Items["CorrelationId"]?.ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleCsrfError(HttpContext context, string message)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "CSRF Protection Error",
            message = message,
            timestamp = DateTimeOffset.UtcNow,
            correlationId = context.Items["CorrelationId"]?.ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// Configuration options for CSRF protection middleware
/// </summary>
public class CsrfProtectionOptions
{
    /// <summary>
    /// Paths that are exempt from CSRF protection (default: ["/api/public", "/health", "/metrics"])
    /// </summary>
    public List<string> ExemptPaths { get; set; } = new()
    {
        "/api/public",
        "/health", 
        "/metrics",
        "/swagger"
    };

    /// <summary>
    /// CSRF token cookie name (default: "XSRF-TOKEN")
    /// </summary>
    public string CookieName { get; set; } = "XSRF-TOKEN";

    /// <summary>
    /// Token lifetime in hours (default: 24)
    /// </summary>
    public int TokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// Whether to require HTTPS for CSRF tokens (default: true)
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Whether to enable CSRF protection (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Custom header name for CSRF token (default: "X-CSRF-Token")
    /// </summary>
    public string HeaderName { get; set; } = "X-CSRF-Token";
}