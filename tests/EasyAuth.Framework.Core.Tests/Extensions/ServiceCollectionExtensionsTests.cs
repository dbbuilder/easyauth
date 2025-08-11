using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Extensions;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Extensions
{
    /// <summary>
    /// TDD tests for service collection extensions
    /// These tests define the expected behavior before implementation (RED phase)
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        private readonly IConfiguration _configuration;
        private readonly ServiceCollection _services;

        public ServiceCollectionExtensionsTests()
        {
            // Setup test configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:ConnectionString"] = "Server=localhost;Database=EAuthTest;Trusted_Connection=true;",
                ["EasyAuth:Providers:Google:Enabled"] = "true",
                ["EasyAuth:Providers:Google:ClientId"] = "test-google-client-id",
                ["EasyAuth:Providers:Google:ClientSecret"] = "test-google-client-secret",
                ["EasyAuth:Providers:AzureB2C:Enabled"] = "true",
                ["EasyAuth:Providers:AzureB2C:ClientId"] = "test-b2c-client-id",
                ["EasyAuth:Providers:AzureB2C:TenantId"] = "test.onmicrosoft.com",
                ["EasyAuth:Providers:AzureB2C:SignUpSignInPolicyId"] = "B2C_1_SignUpSignIn",
                ["EasyAuth:Providers:Facebook:Enabled"] = "true",
                ["EasyAuth:Providers:Facebook:AppId"] = "test-facebook-app-id",
                ["EasyAuth:Providers:Apple:Enabled"] = "true",
                ["EasyAuth:Providers:Apple:ClientId"] = "test-apple-client-id",
                ["EasyAuth:Framework:EnableSwagger"] = "true",
                ["EasyAuth:Framework:EnableHealthChecks"] = "true"
            });
            _configuration = configBuilder.Build();
            _services = new ServiceCollection();
            
            // Add required base services
            _services.AddLogging();
            _services.AddHttpClient();
            _services.AddOptions();
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public void AddEasyAuth_ShouldRegisterCoreServices()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            serviceProvider.GetService<IEAuthService>().Should().NotBeNull();
            serviceProvider.GetService<IEAuthProviderFactory>().Should().NotBeNull();
            serviceProvider.GetService<IEAuthDatabaseService>().Should().NotBeNull();
        }

        [Fact]
        public void AddEasyAuth_ShouldRegisterAllEnabledProviders()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            serviceProvider.GetService<GoogleAuthProvider>().Should().NotBeNull();
            serviceProvider.GetService<AzureB2CAuthProvider>().Should().NotBeNull();
            serviceProvider.GetService<FacebookAuthProvider>().Should().NotBeNull();
            serviceProvider.GetService<AppleAuthProvider>().Should().NotBeNull();
        }

        [Fact]
        public void AddEasyAuth_ShouldConfigureOptions()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var options = serviceProvider.GetService<IOptions<EAuthOptions>>();
            options.Should().NotBeNull();
            options!.Value.Should().NotBeNull();
            options.Value.ConnectionString.Should().Be("Server=localhost;Database=EAuthTest;Trusted_Connection=true;");
        }

        [Fact]
        public void AddEasyAuth_ShouldRegisterHttpClients()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            httpClientFactory.Should().NotBeNull();
            
            // Should be able to create named clients for providers
            var googleClient = httpClientFactory!.CreateClient("GoogleAuth");
            var facebookClient = httpClientFactory.CreateClient("FacebookAuth");
            
            googleClient.Should().NotBeNull();
            facebookClient.Should().NotBeNull();
        }

        [Fact]
        public void AddEasyAuth_WithCustomConfiguration_ShouldUseProvidedConfig()
        {
            // Arrange
            var customConfig = new EAuthOptions
            {
                ConnectionString = "custom-connection-string",
                Providers = new AuthProvidersOptions
                {
                    Google = new GoogleOptions { Enabled = false },
                    DefaultProvider = "AzureB2C"
                }
            };

            // Act
            _services.AddEasyAuth(options =>
            {
                options.ConnectionString = customConfig.ConnectionString;
                options.Providers.Google = customConfig.Providers.Google;
                options.Providers.DefaultProvider = customConfig.Providers.DefaultProvider;
            });
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var options = serviceProvider.GetService<IOptions<EAuthOptions>>();
            options.Should().NotBeNull();
            options!.Value.ConnectionString.Should().Be("custom-connection-string");
            options.Value.Providers.DefaultProvider.Should().Be("AzureB2C");
        }

        [Fact]
        public void AddEasyAuthProviders_ShouldOnlyRegisterProviders()
        {
            // Arrange & Act
            _services.AddEasyAuthProviders(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert - Providers should be registered
            serviceProvider.GetService<GoogleAuthProvider>().Should().NotBeNull();
            serviceProvider.GetService<IEAuthProviderFactory>().Should().NotBeNull();
            
            // Core services should not be registered
            serviceProvider.GetService<IEAuthService>().Should().BeNull();
            serviceProvider.GetService<IEAuthDatabaseService>().Should().BeNull();
        }

        [Fact]
        public void AddEasyAuthDatabase_ShouldOnlyRegisterDatabaseServices()
        {
            // Arrange & Act
            _services.AddEasyAuthDatabase(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert - Database services should be registered
            serviceProvider.GetService<IEAuthDatabaseService>().Should().NotBeNull();
            
            // Other services should not be registered
            serviceProvider.GetService<IEAuthService>().Should().BeNull();
            serviceProvider.GetService<IEAuthProviderFactory>().Should().BeNull();
        }

        [Fact]
        public void AddEasyAuth_ShouldRegisterHealthChecks_WhenEnabled()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            _services.AddHealthChecks(); // Required for health check registration
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var healthCheckService = serviceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
            healthCheckService.Should().NotBeNull();
        }

        [Fact]
        public void AddEasyAuth_ShouldValidateConfiguration()
        {
            // Arrange - Invalid configuration
            var invalidConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EasyAuth:ConnectionString"] = "", // Missing required field
                    ["EasyAuth:Providers:Google:Enabled"] = "true",
                    ["EasyAuth:Providers:Google:ClientId"] = "" // Missing required field
                })
                .Build();

            // Act & Assert
            var action = () => _services.AddEasyAuth(invalidConfig);
            action.Should().Throw<ArgumentException>()
                .WithMessage("*EasyAuth configuration is invalid*");
        }

        [Fact]
        public async Task AddEasyAuth_ShouldHandleDisabledProviders()
        {
            // Arrange - Configuration with some providers disabled
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:ConnectionString"] = "Server=localhost;Database=EAuthTest;Trusted_Connection=true;",
                ["EasyAuth:Providers:Google:Enabled"] = "false", // Disabled
                ["EasyAuth:Providers:AzureB2C:Enabled"] = "true",
                ["EasyAuth:Providers:AzureB2C:ClientId"] = "test-b2c-client-id",
                ["EasyAuth:Providers:AzureB2C:TenantId"] = "test.onmicrosoft.com",
                ["EasyAuth:Providers:AzureB2C:SignUpSignInPolicyId"] = "B2C_1_SignUpSignIn"
            });
            var config = configBuilder.Build();

            // Act
            _services.AddEasyAuth(config);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert - Only enabled providers should be functional
            var providerFactory = serviceProvider.GetService<IEAuthProviderFactory>();
            providerFactory.Should().NotBeNull();
            
            var providers = await providerFactory!.GetProvidersAsync();
            providers.Should().NotContain(p => p.ProviderName == "Google");
            providers.Should().Contain(p => p.ProviderName == "AzureB2C");
        }

        [Fact]
        public void AddEasyAuth_ShouldRegisterCustomServices_WhenProvided()
        {
            // Arrange
            var customService = new Mock<IEAuthService>();

            // Act
            _services.AddEasyAuth(_configuration, options =>
            {
                options.Replace<IEAuthService>(customService.Object);
            });
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var service = serviceProvider.GetService<IEAuthService>();
            service.Should().BeSameAs(customService.Object);
        }

        [Fact]
        public void AddEasyAuth_ShouldSupportMultipleConfigurations()
        {
            // Arrange - Multiple configuration sources
            var config1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EasyAuth:ConnectionString"] = "connection1",
                    ["EasyAuth:Providers:Google:Enabled"] = "true"
                })
                .Build();

            var config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EasyAuth:ConnectionString"] = "connection2",
                    ["EasyAuth:Providers:AzureB2C:Enabled"] = "true"
                })
                .Build();

            // Act - Configure base settings
            _services.Configure<EAuthOptions>(config1.GetSection("EasyAuth"));
            _services.Configure<EAuthOptions>(config2.GetSection("EasyAuth"));
            var serviceProvider = _services.BuildServiceProvider();

            // Assert - Should use the last configured values
            var options = serviceProvider.GetService<IOptions<EAuthOptions>>();
            options.Should().NotBeNull();
            options!.Value.ConnectionString.Should().Be("connection2");
        }

        [Fact]
        public void AddEasyAuth_ShouldRegisterBackgroundServices()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert - Background services should be registered for cleanup, health monitoring, etc.
            var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            hostedServices.Should().NotBeNull();
            hostedServices.Should().NotBeEmpty();
        }

        [Fact]
        public void AddEasyAuth_ShouldConfigureLogging()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.Should().NotBeNull();
            
            var eauthLogger = loggerFactory!.CreateLogger<IEAuthService>();
            eauthLogger.Should().NotBeNull();
        }

        [Fact]
        public void AddEasyAuthSwagger_ShouldRegisterSwaggerServices_WhenEnabled()
        {
            // Arrange & Act
            _services.AddEasyAuth(_configuration);
            _services.AddEasyAuthSwagger();
            
            // Assert - Should not throw and should register Swagger-related services
            var serviceProvider = _services.BuildServiceProvider();
            serviceProvider.Should().NotBeNull();
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Testing")]
        [InlineData("Production")]
        public void AddEasyAuth_ShouldHandleDifferentEnvironments(string environment)
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

            // Act & Assert - Should not throw for any environment
            var action = () => _services.AddEasyAuth(_configuration);
            action.Should().NotThrow();

            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }

        [Fact]
        public void AddEasyAuth_ShouldSupportChaining()
        {
            // Arrange & Act - Should support method chaining
            var result = _services
                .AddEasyAuth(_configuration)
                .AddEasyAuthSwagger();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(_services);
        }

        #endregion

        #region Helper Methods

        private void AssertServiceRegistered<T>(ServiceProvider serviceProvider) where T : class
        {
            var service = serviceProvider.GetService<T>();
            service.Should().NotBeNull($"Service {typeof(T).Name} should be registered");
        }

        #endregion
    }
}