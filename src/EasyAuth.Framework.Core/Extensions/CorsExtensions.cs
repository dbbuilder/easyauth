using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using EasyAuth.Framework.Core.Configuration;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Extension methods to simplify EasyAuth CORS configuration
/// Provides zero-config CORS setup for any frontend framework
/// </summary>
public static class EAuthCorsExtensions
{
    /// <summary>
    /// Adds EasyAuth CORS services with automatic configuration
    /// Call this in Program.cs ConfigureServices
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional CORS configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEasyAuthCors(
        this IServiceCollection services,
        Action<EAuthCorsOptions>? configureOptions = null)
    {
        // Configure CORS options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            // Use smart defaults
            services.Configure<EAuthCorsOptions>(options =>
            {
                // Auto-detect common frontend development ports
                options.AllowedOrigins.AddRange(new[]
                {
                    // React default ports
                    "http://localhost:3000",
                    "https://localhost:3000",
                    
                    // Vue/Vite default ports  
                    "http://localhost:5173",
                    "https://localhost:5173",
                    "http://localhost:8080",
                    "https://localhost:8080",
                    
                    // Angular default ports
                    "http://localhost:4200",
                    "https://localhost:4200",
                    
                    // Next.js default ports
                    "http://localhost:3001",
                    "https://localhost:3001",
                    
                    // Svelte default ports
                    "http://localhost:5000",
                    "https://localhost:5000",
                    
                    // Generic development ports
                    "http://localhost:8081",
                    "http://localhost:8082",
                    "http://localhost:9000",
                    "http://localhost:9090"
                });

                options.EnableAutoDetection = true;
                options.AutoLearnOrigins = true;
            });
        }

        // Register CORS configuration service
        services.AddSingleton<EAuthCorsConfiguration>();
        
        // Let the configuration service set up CORS
        var serviceProvider = services.BuildServiceProvider();
        var corsConfig = serviceProvider.GetRequiredService<EAuthCorsConfiguration>();
        corsConfig.ConfigureServices(services);

        return services;
    }

    /// <summary>
    /// Uses EasyAuth CORS middleware with automatic origin detection
    /// Call this in Program.cs Configure pipeline, before UseRouting
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseEasyAuthCors(this IApplicationBuilder app)
    {
        // NOTE: CorrelationIdMiddleware is registered in UseEasyAuth() to avoid duplicates
        
        // Add detection middleware
        app.UseMiddleware<EAuthCorsDetectionMiddleware>();
        
        // Apply CORS configuration
        var corsConfig = app.ApplicationServices.GetRequiredService<EAuthCorsConfiguration>();
        corsConfig.ConfigureMiddleware(app);

        return app;
    }

    /// <summary>
    /// Adds a specific origin to allowed CORS origins at runtime
    /// Useful for dynamic origin registration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="origin">Origin to allow (e.g., "https://myapp.com")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEasyAuthOrigin(this IServiceCollection services, string origin)
    {
        services.PostConfigure<EAuthCorsOptions>(options =>
        {
            if (!options.AllowedOrigins.Contains(origin))
            {
                options.AllowedOrigins.Add(origin);
            }
        });

        return services;
    }

    /// <summary>
    /// Configures EasyAuth CORS for a specific frontend framework
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="framework">Frontend framework type</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEasyAuthCorsForFramework(
        this IServiceCollection services, 
        FrontendFramework framework)
    {
        return framework switch
        {
            FrontendFramework.React => services.AddEasyAuthCors(options =>
            {
                options.AllowedOrigins.AddRange(new[]
                {
                    "http://localhost:3000",
                    "https://localhost:3000"
                });
            }),
            
            FrontendFramework.Vue => services.AddEasyAuthCors(options =>
            {
                options.AllowedOrigins.AddRange(new[]
                {
                    "http://localhost:8080",
                    "https://localhost:8080",
                    "http://localhost:5173",
                    "https://localhost:5173"
                });
            }),
            
            FrontendFramework.Angular => services.AddEasyAuthCors(options =>
            {
                options.AllowedOrigins.AddRange(new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200"
                });
            }),
            
            FrontendFramework.NextJs => services.AddEasyAuthCors(options =>
            {
                options.AllowedOrigins.AddRange(new[]
                {
                    "http://localhost:3000",
                    "https://localhost:3000",
                    "http://localhost:3001",
                    "https://localhost:3001"
                });
            }),
            
            FrontendFramework.Svelte => services.AddEasyAuthCors(options =>
            {
                options.AllowedOrigins.AddRange(new[]
                {
                    "http://localhost:5000",
                    "https://localhost:5000",
                    "http://localhost:5173",
                    "https://localhost:5173"
                });
            }),
            
            _ => services.AddEasyAuthCors() // Default auto-detection
        };
    }

    /// <summary>
    /// Quickly configure CORS for common deployment scenarios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="scenario">Deployment scenario</param>
    /// <param name="customOrigins">Additional custom origins</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEasyAuthCorsForScenario(
        this IServiceCollection services,
        DeploymentScenario scenario,
        params string[] customOrigins)
    {
        return services.AddEasyAuthCors(options =>
        {
            switch (scenario)
            {
                case DeploymentScenario.LocalDevelopment:
                    options.EnableAutoDetection = true;
                    options.AutoLearnOrigins = true;
                    options.AllowedOrigins.Add("*"); // Very permissive for dev
                    break;

                case DeploymentScenario.Staging:
                    options.EnableAutoDetection = false;
                    options.AllowedOrigins.Clear();
                    options.AllowedOrigins.AddRange(customOrigins);
                    break;

                case DeploymentScenario.Production:
                    options.EnableAutoDetection = false;
                    options.AutoLearnOrigins = false;
                    options.AllowedOrigins.Clear();
                    options.AllowedOrigins.AddRange(customOrigins);
                    break;
            }
        });
    }
}

/// <summary>
/// Supported frontend frameworks for automatic CORS configuration
/// </summary>
public enum FrontendFramework
{
    React,
    Vue,
    Angular,
    NextJs,
    Svelte,
    Vanilla
}

/// <summary>
/// Common deployment scenarios for CORS configuration
/// </summary>
public enum DeploymentScenario
{
    LocalDevelopment,
    Staging,
    Production
}