using Azure.Identity;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Providers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasyAuth.Framework.Extensions
{
    /// <summary>
    /// Extension methods for configuring EasyAuth Framework services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add EasyAuth Framework to the service collection
        /// </summary>
        public static IServiceCollection AddEasyAuth(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // Configure options from configuration
            var eauthOptions = new EAuthOptions();
            configuration.GetSection(EAuthOptions.ConfigurationSection).Bind(eauthOptions);
            services.Configure<EAuthOptions>(configuration.GetSection(EAuthOptions.ConfigurationSection));

            // Add Azure Key Vault if configured
            if (eauthOptions.KeyVault != null && !string.IsNullOrEmpty(eauthOptions.KeyVault.BaseUrl))
            {
                AddKeyVaultConfiguration(services, configuration, eauthOptions.KeyVault, environment);
            }

            // Resolve connection string (from Key Vault or direct configuration)
            var connectionString = ResolveConnectionString(eauthOptions, configuration);

            // Add database service
            services.AddSingleton<IEAuthDatabaseService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<EAuthDatabaseService>>();
                return new EAuthDatabaseService(connectionString, logger);
            });

            // Add HTTP client factory for providers
            services.AddHttpClient();

            // Add authentication providers
            AddAuthenticationProviders(services, eauthOptions);

            // Add core authentication services
            services.AddScoped<IEAuthService, EAuthService>();

            // Add session configuration
            AddSessionServices(services, eauthOptions);

            // Add CORS configuration
            AddCorsServices(services, eauthOptions);

            // Add framework services
            AddFrameworkServices(services);

            // Add health checks if enabled
            if (eauthOptions.Framework.EnableHealthChecks)
            {
                services.AddHealthChecks()
                    .AddSqlServer(connectionString);
            }

            return services;
        }

        /// <summary>
        /// Configure the EasyAuth Framework middleware
        /// </summary>
        public static IApplicationBuilder UseEasyAuth(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            var eauthOptions = new EAuthOptions();
            configuration.GetSection(EAuthOptions.ConfigurationSection).Bind(eauthOptions);

            // Initialize database if auto-setup is enabled
            if (eauthOptions.Framework.AutoDatabaseSetup)
            {
                InitializeDatabase(app).GetAwaiter().GetResult();
            }

            // Configure middleware pipeline
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // Add CORS
            app.UseCors("EasyAuthCors");

            // Add health checks if enabled
            if (eauthOptions.Framework.EnableHealthChecks)
            {
                app.UseHealthChecks("/health");
            }

            return app;
        }

        private static void AddKeyVaultConfiguration(
            IServiceCollection services,
            IConfiguration configuration,
            KeyVaultOptions keyVaultOptions,
            IHostEnvironment environment)
        {
            if (environment.IsProduction() || environment.IsStaging())
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .AddAzureKeyVault(
                        new Uri(keyVaultOptions.BaseUrl),
                        new DefaultAzureCredential());

                var config = configurationBuilder.Build();
                services.AddSingleton<IConfiguration>(config);
            }
        }

        private static string ResolveConnectionString(EAuthOptions options, IConfiguration configuration)
        {
            // Try to get from Key Vault first if configured
            if (options.KeyVault?.UseConnectionStringFromKeyVault == true)
            {
                var connectionString = configuration[options.KeyVault.ConnectionStringSecretName];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }
            }

            // Fall back to direct configuration
            return options.ConnectionString;
        }

        private static void AddAuthenticationProviders(IServiceCollection services, EAuthOptions options)
        {
            var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookieOptions =>
                {
                    cookieOptions.LoginPath = "/auth/login";
                    cookieOptions.LogoutPath = "/auth/logout";
                    cookieOptions.AccessDeniedPath = "/auth/access-denied";
                    cookieOptions.Cookie.Name = options.Session.CookieName;
                    cookieOptions.Cookie.HttpOnly = options.Session.HttpOnly;
                    cookieOptions.Cookie.SecurePolicy = options.Session.Secure 
                        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.Always 
                        : Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                    cookieOptions.ExpireTimeSpan = TimeSpan.FromHours(options.Session.IdleTimeoutHours);
                    cookieOptions.SlidingExpiration = options.Session.SlidingExpiration;
                });

            // Add Google OAuth if configured
            if (options.Providers.Google?.Enabled == true)
            {
                authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, googleOptions =>
                {
                    googleOptions.ClientId = options.Providers.Google.ClientId;
                    googleOptions.ClientSecret = options.Providers.Google.ClientSecret;
                    googleOptions.CallbackPath = options.Providers.Google.CallbackPath;
                    
                    foreach (var scope in options.Providers.Google.Scopes)
                    {
                        googleOptions.Scope.Add(scope);
                    }
                });
                
                services.AddScoped<IEAuthProvider, GoogleAuthProvider>();
            }

            // Add Facebook OAuth if configured
            if (options.Providers.Facebook?.Enabled == true)
            {
                authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, facebookOptions =>
                {
                    facebookOptions.AppId = options.Providers.Facebook.AppId;
                    facebookOptions.AppSecret = options.Providers.Facebook.AppSecret;
                    facebookOptions.CallbackPath = options.Providers.Facebook.CallbackPath;
                    
                    foreach (var scope in options.Providers.Facebook.Scopes)
                    {
                        facebookOptions.Scope.Add(scope);
                    }
                });
            }

            // Additional providers (Apple, Azure B2C) would be added here
        }

        private static void AddSessionServices(IServiceCollection services, EAuthOptions options)
        {
            services.AddSession(sessionOptions =>
            {
                sessionOptions.IdleTimeout = TimeSpan.FromHours(options.Session.IdleTimeoutHours);
                sessionOptions.Cookie.HttpOnly = options.Session.HttpOnly;
                sessionOptions.Cookie.IsEssential = true;
                sessionOptions.Cookie.Name = options.Session.CookieName;
                sessionOptions.Cookie.SameSite = Enum.Parse<Microsoft.AspNetCore.Http.SameSiteMode>(options.Session.SameSite);
                sessionOptions.Cookie.SecurePolicy = options.Session.Secure 
                    ? Microsoft.AspNetCore.Http.CookieSecurePolicy.Always 
                    : Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            });
        }

        private static void AddCorsServices(IServiceCollection services, EAuthOptions options)
        {
            services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy("EasyAuthCors", policy =>
                {
                    policy.WithOrigins(options.Cors.AllowedOrigins)
                          .WithMethods(options.Cors.AllowedMethods)
                          .WithHeaders(options.Cors.AllowedHeaders);

                    if (options.Cors.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                });
            });
        }

        private static void AddFrameworkServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        private static async Task InitializeDatabase(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("EasyAuth.DatabaseInitializer");

            try
            {
                logger.LogInformation("Checking EasyAuth database initialization status");

                if (!await databaseService.IsDatabaseInitializedAsync())
                {
                    logger.LogInformation("EasyAuth database not initialized, starting setup");
                    await databaseService.InitializeDatabaseAsync();
                }

                await databaseService.ApplyMigrationsAsync();
                logger.LogInformation("EasyAuth database setup completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during EasyAuth database initialization");
                throw;
            }
        }
    }
}
