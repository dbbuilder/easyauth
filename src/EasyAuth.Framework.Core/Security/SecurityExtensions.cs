using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using EasyAuth.Framework.Core.Security;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Extension methods for configuring EasyAuth security features
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds comprehensive security services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureInputValidation">Optional input validation configuration</param>
    /// <param name="configureRateLimit">Optional rate limiting configuration</param>
    /// <param name="configureCsrf">Optional CSRF protection configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEasyAuthSecurity(
        this IServiceCollection services,
        Action<InputValidationOptions>? configureInputValidation = null,
        Action<RateLimitingOptions>? configureRateLimit = null,
        Action<CsrfProtectionOptions>? configureCsrf = null)
    {
        // Add memory cache for rate limiting
        services.AddMemoryCache();

        // Configure input validation options
        var inputValidationOptions = new InputValidationOptions();
        configureInputValidation?.Invoke(inputValidationOptions);
        services.AddSingleton(inputValidationOptions);

        // Configure rate limiting options
        var rateLimitOptions = new RateLimitingOptions();
        configureRateLimit?.Invoke(rateLimitOptions);
        services.AddSingleton(rateLimitOptions);

        // Configure CSRF protection options
        var csrfOptions = new CsrfProtectionOptions();
        configureCsrf?.Invoke(csrfOptions);
        services.AddSingleton(csrfOptions);

        // Add antiforgery services for CSRF protection
        services.AddAntiforgery(options =>
        {
            options.HeaderName = csrfOptions.HeaderName;
            options.Cookie.Name = csrfOptions.CookieName;
            options.Cookie.HttpOnly = false; // SPA needs to read this
            options.Cookie.SecurePolicy = csrfOptions.RequireHttps 
                ? Microsoft.AspNetCore.Http.CookieSecurePolicy.Always 
                : Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });

        return services;
    }

    /// <summary>
    /// Adds the complete EasyAuth security middleware pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuthSecurity(this IApplicationBuilder app)
    {
        // Add security middleware in proper order (most restrictive first)
        
        // 1. Input validation (reject malicious requests early)
        app.UseMiddleware<InputValidationMiddleware>();

        // 2. Rate limiting (prevent abuse)
        app.UseMiddleware<RateLimitingMiddleware>();

        // 3. CSRF protection (for state-changing operations)
        app.UseMiddleware<CsrfProtectionMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds only input validation security
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseInputValidation(this IApplicationBuilder app)
    {
        app.UseMiddleware<InputValidationMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds only rate limiting security
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds only CSRF protection security
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        app.UseMiddleware<CsrfProtectionMiddleware>();
        return app;
    }

    /// <summary>
    /// Configures security headers for enhanced protection
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="isDevelopment">Whether running in development mode</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment = false)
    {
        app.Use(async (context, next) =>
        {
            // Security headers for all responses
            var headers = context.Response.Headers;

            // Prevent clickjacking
            headers.Add("X-Frame-Options", "DENY");

            // Prevent MIME type sniffing
            headers.Add("X-Content-Type-Options", "nosniff");

            // XSS protection
            headers.Add("X-XSS-Protection", "1; mode=block");

            // Referrer policy
            headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content Security Policy (basic, can be customized)
            if (!isDevelopment)
            {
                headers.Add("Content-Security-Policy", 
                    "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'");
            }

            // HSTS (only in production with HTTPS)
            if (!isDevelopment && context.Request.IsHttps)
            {
                headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            // Feature policy / Permissions policy
            headers.Add("Permissions-Policy", 
                "camera=(), microphone=(), location=(), payment=(), usb=()");

            await next();
        });

        return app;
    }

    /// <summary>
    /// Enables comprehensive security audit logging
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityAuditLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var startTime = DateTime.UtcNow;
            
            await next();

            var duration = DateTime.UtcNow - startTime;
            var loggerFactory = context.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("EasyAuth.Security");

            // Log security-relevant events
            if (context.Response.StatusCode >= 400)
            {
                logger?.LogWarning("Security Event: {Method} {Path} returned {StatusCode} from {IP} in {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    context.Connection.RemoteIpAddress,
                    duration.TotalMilliseconds);
            }

            // Log authentication failures
            if (context.Response.StatusCode == 401)
            {
                logger?.LogWarning("Authentication failure: {Path} from {IP} - User: {User}",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress,
                    context.User?.Identity?.Name ?? "Anonymous");
            }

            // Log authorization failures
            if (context.Response.StatusCode == 403)
            {
                logger?.LogWarning("Authorization failure: {Path} from {IP} - User: {User}",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress,
                    context.User?.Identity?.Name ?? "Anonymous");
            }
        });

        return app;
    }
}