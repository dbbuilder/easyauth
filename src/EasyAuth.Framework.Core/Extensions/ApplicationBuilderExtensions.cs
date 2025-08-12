using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EasyAuth.Framework.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Application builder extensions for EasyAuth Framework
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures EasyAuth middleware and endpoints with automatic Swagger setup
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuth(this IApplicationBuilder app)
    {
        // Get options to check if Swagger is enabled
        var options = app.ApplicationServices.GetService<IOptions<EAuthOptions>>()?.Value;
        var isSwaggerEnabled = options?.Framework?.EnableSwagger ?? true;

        // Add EasyAuth Swagger UI in development
        if (isSwaggerEnabled && app.ApplicationServices.GetService<IHostEnvironment>()?.IsDevelopment() == true)
        {
            app.UseEasyAuthSwagger();
        }

        // Configure CORS with EasyAuth policies  
        app.UseCors();

        // Add authentication middleware
        app.UseAuthentication();
        app.UseAuthorization();

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
}