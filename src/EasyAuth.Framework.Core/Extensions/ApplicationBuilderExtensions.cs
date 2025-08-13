using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Security;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Application builder extensions for EasyAuth Framework
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures EasyAuth middleware and endpoints with automatic Swagger setup
    /// Provides zero-configuration development experience with auto-CORS detection
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="enableSecurity">Whether to enable comprehensive security middleware (default: true)</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuth(this IApplicationBuilder app, bool enableSecurity = true)
    {
        var environment = app.ApplicationServices.GetService<IHostEnvironment>();
        var isDevelopment = environment?.IsDevelopment() == true;
        
        // Get options to check configuration
        var options = app.ApplicationServices.GetService<IOptions<EAuthOptions>>()?.Value;
        var isSwaggerEnabled = options?.Framework?.EnableSwagger ?? true;

        // Add security headers first
        if (enableSecurity)
        {
            app.UseSecurityHeaders(isDevelopment);
        }

        // Add correlation ID middleware (for request tracing)
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add comprehensive security middleware if enabled
        if (enableSecurity)
        {
            app.UseEasyAuthSecurity();
        }

        // Add EasyAuth Swagger UI in development
        if (isSwaggerEnabled && isDevelopment)
        {
            app.UseEasyAuthSwagger();
        }

        // Configure environment-aware CORS with auto-detection
        if (isDevelopment)
        {
            // Development: Use permissive auto-detecting CORS
            app.UseCors("EasyAuthDevelopment");
            
            // Log auto-detected origins for developer awareness
            var detectedOrigins = EasyAuthDefaults.GetAllDevelopmentOrigins();
            var logger = app.ApplicationServices.GetService<ILogger>();
            logger?.LogInformation("🌐 EasyAuth auto-detected {Count} development origins. Zero CORS configuration required!", 
                detectedOrigins.Count);
            
            if (detectedOrigins.Count > 10)
            {
                logger?.LogInformation("💡 Tip: For faster startup, consider configuring specific origins in production");
            }
        }
        else
        {
            // Production: Use strict configured CORS
            app.UseCors("EasyAuthProduction");
            
            // Production security warnings
            app.ValidateProductionSecurity();
        }

        // Add authentication middleware (order is important)
        app.UseAuthentication();
        app.UseAuthorization();

        // Add security audit logging if enabled
        if (enableSecurity)
        {
            app.UseSecurityAuditLogging();
        }

        return app;
    }

    /// <summary>
    /// Configures EasyAuth Swagger UI with enhanced developer experience
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuthSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "EasyAuth API v2.2.0");
            options.RoutePrefix = "swagger";
            
            // Enhanced Swagger UI configuration for better developer experience
            options.DocumentTitle = "EasyAuth Framework - API Documentation";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();

            // Custom CSS for better branding
            options.InjectStylesheet("/swagger-ui/custom.css");
            
            // Custom JavaScript for enhanced functionality
            options.InjectJavascript("/swagger-ui/custom.js");

            // Add custom HTML in the head for better SEO and branding
            options.HeadContent = @"
                <meta name='description' content='EasyAuth Framework API Documentation - Zero-configuration authentication for modern applications'>
                <meta name='keywords' content='authentication, oauth, jwt, swagger, api, dotnet, framework'>
                <link rel='icon' type='image/x-icon' href='/favicon.ico'>
            ";

            // Custom configuration for better authentication testing
            options.OAuthClientId("swagger-ui");
            options.OAuthAppName("EasyAuth Framework Swagger UI");
            options.OAuthUsePkce();
        });

        // Serve custom CSS and JS files
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/swagger-ui",
            FileProvider = new Microsoft.Extensions.FileProviders.EmbeddedFileProvider(
                typeof(ApplicationBuilderExtensions).Assembly,
                "EasyAuth.Framework.Core.wwwroot")
        });

        return app;
    }

    /// <summary>
    /// Configures EasyAuth CORS with smart development detection
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuthCors(this IApplicationBuilder app)
    {
        var corsConfig = app.ApplicationServices.GetService<EAuthCorsConfiguration>();
        if (corsConfig != null)
        {
            corsConfig.ConfigureMiddleware(app);
        }
        else
        {
            // Fallback to default CORS if EAuthCorsConfiguration is not registered
            app.UseCors(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
        }

        return app;
    }

    /// <summary>
    /// Maps EasyAuth endpoints with enhanced Swagger documentation
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder MapEasyAuthEndpoints(this IApplicationBuilder app)
    {
        if (app is not WebApplication webApp)
        {
            return app;
        }

        // Add health check endpoint if enabled
        var options = app.ApplicationServices.GetService<IOptions<EAuthOptions>>()?.Value;
        if (options?.Framework?.EnableHealthChecks == true)
        {
            webApp.MapHealthChecks("/health");
        }

        return app;
    }

    /// <summary>
    /// Adds comprehensive EasyAuth middleware pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configureEndpoints">Optional endpoint configuration</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuthComplete(this IApplicationBuilder app, 
        Action<IApplicationBuilder>? configureEndpoints = null)
    {
        // Full EasyAuth pipeline setup
        app.UseEasyAuth();
        
        // Add routing
        app.UseRouting();
        
        // Configure custom endpoints if provided
        configureEndpoints?.Invoke(app);
        
        // Map default EasyAuth endpoints
        app.MapEasyAuthEndpoints();

        // Map controllers for EasyAuth endpoints
        if (app is WebApplication webApp)
        {
            webApp.MapControllers();
        }

        return app;
    }

    /// <summary>
    /// Validates production security configuration and provides warnings
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder ValidateProductionSecurity(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetService<ILogger>();
        var options = app.ApplicationServices.GetService<IOptions<EAuthOptions>>()?.Value;

        if (options?.Cors?.AllowedOrigins?.Any() != true)
        {
            logger?.LogWarning("⚠️  SECURITY WARNING: No CORS origins configured for production. Consider adding specific allowed origins in EasyAuth:CORS:AllowedOrigins");
        }

        var dangerousOrigins = options?.Cors?.AllowedOrigins?.Where(origin => 
            origin == "*" || 
            origin.Contains("localhost") || 
            origin.Contains("127.0.0.1")).ToList();

        if (dangerousOrigins?.Any() == true)
        {
            logger?.LogWarning("🔒 SECURITY WARNING: Production CORS includes development origins: {Origins}. Remove these for security.", 
                string.Join(", ", dangerousOrigins));
        }

        // Check for secure connection requirements
        if (options?.Session?.Secure == false)
        {
            logger?.LogWarning("🔐 SECURITY RECOMMENDATION: Consider enabling secure cookies in production (EasyAuth:Session:Secure)");
        }

        // Validate provider configurations
        var enabledProviders = 0;
        if (options?.Providers?.Google?.Enabled == true) enabledProviders++;
        if (options?.Providers?.Facebook?.Enabled == true) enabledProviders++;
        if (options?.Providers?.Apple?.Enabled == true) enabledProviders++;
        if (options?.Providers?.AzureB2C?.Enabled == true) enabledProviders++;

        if (enabledProviders == 0)
        {
            logger?.LogWarning("⚠️  No authentication providers enabled. Users will not be able to authenticate.");
        }
        else
        {
            logger?.LogInformation("✅ EasyAuth production security validation complete. {Count} provider(s) enabled.", enabledProviders);
        }

        return app;
    }
}