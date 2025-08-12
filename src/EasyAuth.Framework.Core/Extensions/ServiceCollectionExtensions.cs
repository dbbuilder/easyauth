using System.ComponentModel.DataAnnotations;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring EasyAuth services in DI container
    /// Follows TDD methodology - minimal implementation to make tests pass
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds EasyAuth framework services to the DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration containing EasyAuth settings</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuth(this IServiceCollection services, IConfiguration configuration)
        {
            // TDD GREEN Phase: Register all required services

            // Configure options
            services.Configure<EAuthOptions>(configuration.GetSection(EAuthOptions.ConfigurationSection));

            // Validate configuration
            var eauthOptions = new EAuthOptions();
            configuration.GetSection(EAuthOptions.ConfigurationSection).Bind(eauthOptions);
            ValidateConfiguration(eauthOptions);

            // Register core services
            services.AddEasyAuthCore();
            services.AddEasyAuthProviders(configuration);
            services.AddEasyAuthDatabase(configuration);
            services.AddEasyAuthBackgroundServices();

            // Configure HTTP clients
            ConfigureHttpClients(services, eauthOptions);

            // Add health checks if enabled
            if (eauthOptions.Framework.EnableHealthChecks)
            {
                AddEasyAuthHealthChecks(services, eauthOptions);
            }

            // Add built-in CORS configuration for seamless frontend integration
            services.AddEasyAuthCors();

            // Note: CorrelationIdMiddleware is registered via app.UseMiddleware<T>() in UseEasyAuth()
            // No DI registration needed for middleware with RequestDelegate dependency

            return services;
        }

        /// <summary>
        /// Adds EasyAuth framework services with action-based configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuth(this IServiceCollection services, Action<EAuthOptions> configureOptions)
        {
            // TDD GREEN Phase: Support action-based configuration
            services.Configure(configureOptions);

            // Build options to validate
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<EAuthOptions>>().Value;
            ValidateConfiguration(options);

            // Register core services
            services.AddEasyAuthCore();
            services.AddEasyAuthProviders(options);
            services.AddEasyAuthDatabase(options);
            services.AddEasyAuthBackgroundServices();

            // Configure HTTP clients
            ConfigureHttpClients(services, options);

            // Add health checks if enabled
            if (options.Framework.EnableHealthChecks)
            {
                AddEasyAuthHealthChecks(services, options);
            }

            // Add Swagger documentation if enabled
            if (options.Framework.EnableSwagger)
            {
                services.AddEasyAuthSwagger();
            }

            return services;
        }

        /// <summary>
        /// Adds EasyAuth with custom service configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="configureServices">Custom service configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuth(this IServiceCollection services,
            IConfiguration configuration,
            Action<EAuthServiceOptions> configureServices)
        {
            // TDD GREEN Phase: Support custom service configuration
            services.AddEasyAuth(configuration);

            var serviceOptions = new EAuthServiceOptions(services);
            configureServices(serviceOptions);

            return services;
        }

        /// <summary>
        /// Adds only the authentication providers (without core services)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuthProviders(this IServiceCollection services, IConfiguration configuration)
        {
            // TDD GREEN Phase: Register only providers
            var eauthOptions = new EAuthOptions();
            configuration.GetSection(EAuthOptions.ConfigurationSection).Bind(eauthOptions);

            return AddEasyAuthProviders(services, eauthOptions);
        }

        /// <summary>
        /// Adds only the authentication providers with options
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="options">EasyAuth options</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuthProviders(this IServiceCollection services, EAuthOptions options)
        {
            // Register minimal dependencies needed by providers
            services.AddScoped<IConfigurationService, ConfigurationService>();
            
            // Register provider factory
            services.AddSingleton<IEAuthProviderFactory, EAuthProviderFactory>();

            // Register individual providers based on configuration
            if (options.Providers.Google?.Enabled == true)
            {
                services.AddScoped<GoogleAuthProvider>();
            }

            if (options.Providers.AzureB2C?.Enabled == true)
            {
                services.AddScoped<AzureB2CAuthProvider>();
            }

            if (options.Providers.Facebook?.Enabled == true)
            {
                services.AddScoped<FacebookAuthProvider>();
            }

            if (options.Providers.Apple?.Enabled == true)
            {
                services.AddScoped<AppleAuthProvider>();
            }

            return services;
        }

        /// <summary>
        /// Adds only the database services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuthDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // TDD GREEN Phase: Register only database services
            var eauthOptions = new EAuthOptions();
            configuration.GetSection(EAuthOptions.ConfigurationSection).Bind(eauthOptions);

            return AddEasyAuthDatabase(services, eauthOptions);
        }

        /// <summary>
        /// Adds only the database services with options
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="options">EasyAuth options</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuthDatabase(this IServiceCollection services, EAuthOptions options)
        {
            services.AddScoped<IEAuthDatabaseService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<EAuthDatabaseService>>();
                return new EAuthDatabaseService(options.ConnectionString, logger);
            });
            return services;
        }

        /// <summary>
        /// Adds comprehensive Swagger/OpenAPI documentation for EasyAuth endpoints
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEasyAuthSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "EasyAuth API",
                    Version = "v2.2.0",
                    Description = @"
🚀 **EasyAuth Framework API Documentation**

A powerful, zero-configuration authentication framework that supports:
- **Google, Facebook, Apple, Azure B2C** authentication
- **Universal Integration System** with StandardApiController
- **Smart CORS** with automatic origin detection
- **Database session management** with cleanup
- **Comprehensive error handling** with ApiResponse pattern

## 🎯 **Quick Start**
1. Configure your providers in `appsettings.json`
2. Call `/api/auth/login/{provider}` to start authentication
3. Use returned tokens for subsequent API calls
4. Enjoy zero-configuration authentication!

## 🛡️ **Security**
All endpoints use industry-standard security practices with JWT tokens and proper CORS handling.
",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "EasyAuth Framework",
                        Url = new Uri("https://github.com/yourusername/easyauth-framework")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                // Add comprehensive security definitions
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = @"
**JWT Authorization header using the Bearer scheme.**

Enter 'Bearer' [space] and then your token in the text input below.
Example: 'Bearer eyJhbGciOiJIUzI1NiIs...'

The token is obtained from the `/api/auth/login/{provider}` endpoint after successful authentication.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "API Key authentication using X-API-Key header",
                    Name = "X-API-Key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
                });

                // Add security requirements
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Enhanced documentation options
                options.EnableAnnotations();
                options.DescribeAllParametersInCamelCase();
                options.SchemaFilter<EasyAuthSchemaFilter>();
                options.OperationFilter<EasyAuthOperationFilter>();

                // Include XML documentation
                var xmlFiles = new[]
                {
                    "EasyAuth.Framework.Core.xml",
                    "EasyAuth.Framework.Extensions.xml"
                };

                foreach (var xmlFile in xmlFiles)
                {
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                    }
                }

                // Custom schema mappings for better documentation
                options.MapType<TokenResponse>(() => new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "object",
                    Description = "JWT token response containing access token and user information",
                    Example = new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["accessToken"] = new Microsoft.OpenApi.Any.OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."),
                        ["refreshToken"] = new Microsoft.OpenApi.Any.OpenApiString("refresh_token_here"),
                        ["expiresIn"] = new Microsoft.OpenApi.Any.OpenApiInteger(3600),
                        ["tokenType"] = new Microsoft.OpenApi.Any.OpenApiString("Bearer"),
                        ["user"] = new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["id"] = new Microsoft.OpenApi.Any.OpenApiString("user123"),
                            ["email"] = new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
                            ["name"] = new Microsoft.OpenApi.Any.OpenApiString("John Doe")
                        }
                    }
                });
            });

            return services;
        }

        #region Private Helper Methods

        private static IServiceCollection AddEasyAuthCore(this IServiceCollection services)
        {
            // Register core EasyAuth services
            services.AddScoped<IEAuthService, EAuthService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
            return services;
        }

        private static IServiceCollection AddEasyAuthBackgroundServices(this IServiceCollection services)
        {
            // TDD GREEN Phase: Register background services for cleanup, monitoring, etc.
            services.AddHostedService<EAuthBackgroundService>();
            return services;
        }

        private static void ConfigureHttpClients(IServiceCollection services, EAuthOptions options)
        {
            // Configure named HTTP clients for each provider
            services.AddHttpClient("GoogleAuth", client =>
            {
                client.BaseAddress = new Uri("https://accounts.google.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("FacebookAuth", client =>
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("AppleAuth", client =>
            {
                client.BaseAddress = new Uri("https://appleid.apple.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("AzureB2CAuth", client =>
            {
                if (!string.IsNullOrEmpty(options.Providers.AzureB2C?.TenantId))
                {
                    var tenantName = options.Providers.AzureB2C.TenantId.Replace(".onmicrosoft.com", "");
                    client.BaseAddress = new Uri($"https://{tenantName}.b2clogin.com/");
                }
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        private static void AddEasyAuthHealthChecks(IServiceCollection services, EAuthOptions options)
        {
            // TDD GREEN Phase: Add health checks for providers
            // Note: Actual health check implementations would be added here
        }

        private static void ValidateConfiguration(EAuthOptions options)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(options);

            if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
            {
                var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                throw new ArgumentException($"EasyAuth configuration is invalid: {errors}");
            }

            // Additional custom validation
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                throw new ArgumentException("EasyAuth configuration is invalid: ConnectionString is required");
            }

            // Validate enabled providers have required configuration
            if (options.Providers.Google?.Enabled == true && string.IsNullOrEmpty(options.Providers.Google.ClientId))
            {
                throw new ArgumentException("EasyAuth configuration is invalid: Google ClientId is required when Google provider is enabled");
            }

            if (options.Providers.AzureB2C?.Enabled == true)
            {
                if (string.IsNullOrEmpty(options.Providers.AzureB2C.ClientId) ||
                    string.IsNullOrEmpty(options.Providers.AzureB2C.TenantId) ||
                    string.IsNullOrEmpty(options.Providers.AzureB2C.SignUpSignInPolicyId))
                {
                    throw new ArgumentException("EasyAuth configuration is invalid: AzureB2C requires ClientId, TenantId, and SignUpSignInPolicyId");
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Custom service configuration options for EasyAuth
    /// </summary>
    public class EAuthServiceOptions
    {
        private readonly IServiceCollection _services;

        public EAuthServiceOptions(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Service collection for advanced customization
        /// </summary>
        public IServiceCollection Services => _services;

        /// <summary>
        /// Replace a service registration
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="implementation">New implementation</param>
        public void Replace<T>(T implementation) where T : class
        {
            var serviceType = typeof(T);
            var existingService = _services.FirstOrDefault(s => s.ServiceType == serviceType);

            if (existingService != null)
            {
                _services.Remove(existingService);
            }

            _services.AddSingleton<T>(implementation);
        }

        /// <summary>
        /// Add a custom provider
        /// </summary>
        /// <typeparam name="T">Provider type</typeparam>
        public void AddCustomProvider<T>() where T : class, IEAuthProvider
        {
            _services.AddScoped<T>();
        }
    }

    /// <summary>
    /// Background service for EasyAuth maintenance tasks
    /// </summary>
    internal class EAuthBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<EAuthBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EAuthBackgroundService(ILogger<EAuthBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TDD GREEN Phase: Basic background service implementation
            _logger.LogInformation("EasyAuth background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Perform periodic maintenance tasks
                    await PerformMaintenanceAsync().ConfigureAwait(false);

                    // Wait for next cycle
                    await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in EasyAuth background service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("EasyAuth background service stopped");
        }

        private async Task PerformMaintenanceAsync()
        {
            // TDD GREEN Phase: Basic maintenance tasks
            using var scope = _serviceProvider.CreateScope();

            try
            {
                // Clean up expired sessions
                var databaseService = scope.ServiceProvider.GetService<IEAuthDatabaseService>();
                if (databaseService != null)
                {
                    // Cleanup would be implemented here
                    await Task.CompletedTask.ConfigureAwait(false);
                }

                _logger.LogDebug("EasyAuth maintenance tasks completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing EasyAuth maintenance");
            }
        }
    }
}

