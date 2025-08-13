using NBomber.Contracts;
using NBomber.CSharp;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EasyAuth.Framework.Performance.Tests.Infrastructure;

namespace EasyAuth.Framework.Performance.Tests;

/// <summary>
/// Performance tests for authentication provider operations
/// </summary>
public class ProviderPerformanceTests
{
    private const int DefaultDurationMinutes = 1;

    [Fact]
    public void GoogleProvider_LoginUrlGeneration_ShouldHandleHighThroughput()
    {
        var scenario = Scenario.Create("google_login_url_generation", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            try
            {
                var provider = await providerFactory.GetProviderAsync("Google");
                if (provider == null) return Response.Fail();
                
                var loginUrl = await provider.GetLoginUrlAsync($"https://localhost/callback?state={context.ScenarioInfo.ScenarioName}");
                
                return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Error in Google login URL generation");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 500, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void FacebookProvider_LoginUrlGeneration_ShouldHandleHighThroughput()
    {
        var scenario = Scenario.Create("facebook_login_url_generation", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            try
            {
                var provider = await providerFactory.GetProviderAsync("Facebook");
                if (provider == null) return Response.Fail();
                
                var loginUrl = await provider.GetLoginUrlAsync($"https://localhost/callback?state={context.ScenarioInfo.ScenarioName}");
                
                return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Error in Facebook login URL generation");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 400, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void AppleProvider_LoginUrlGeneration_ShouldHandleHighThroughput()
    {
        var scenario = Scenario.Create("apple_login_url_generation", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            try
            {
                var provider = await providerFactory.GetProviderAsync("Apple");
                if (provider == null) return Response.Fail();
                
                var loginUrl = await provider.GetLoginUrlAsync($"https://localhost/callback?state={context.ScenarioInfo.ScenarioName}");
                
                return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Error in Apple login URL generation");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 300, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void AllProviders_ConcurrentOperation_ShouldHandleMixedLoad()
    {
        var googleScenario = Scenario.Create("concurrent_google", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            var provider = await providerFactory.GetProviderAsync("Google");
            if (provider == null) return Response.Fail();
            var loginUrl = await provider.GetLoginUrlAsync("https://localhost/callback");
            return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
        })
        .WithWeight(40)
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 200, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        var facebookScenario = Scenario.Create("concurrent_facebook", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            var provider = await providerFactory.GetProviderAsync("Facebook");
            if (provider == null) return Response.Fail();
            var loginUrl = await provider.GetLoginUrlAsync("https://localhost/callback");
            return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
        })
        .WithWeight(35)
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 175, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        var appleScenario = Scenario.Create("concurrent_apple", async context =>
        {
            var services = CreateTestServices();
            var serviceProvider = services.BuildServiceProvider();
            var providerFactory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

            var provider = await providerFactory.GetProviderAsync("Apple");
            if (provider == null) return Response.Fail();
            var loginUrl = await provider.GetLoginUrlAsync("https://localhost/callback");
            return !string.IsNullOrEmpty(loginUrl) ? Response.Ok() : Response.Fail();
        })
        .WithWeight(25)
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 125, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(googleScenario, facebookScenario, appleScenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
            .Run();
    }

    private static ServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Base EasyAuth configuration with graceful degradation
                ["EasyAuth:Enabled"] = "true",
                ["EasyAuth:AllowAnonymous"] = "true", // Allow operation without OAuth providers
                
                // Google Provider Configuration - Optional, gracefully degraded if missing
                ["EasyAuth:Providers:Google:Enabled"] = "false", // Start disabled for graceful testing
                ["EasyAuth:Providers:Google:ClientId"] = "test-google-client-id",
                ["EasyAuth:Providers:Google:ClientSecret"] = "test-google-client-secret",
                
                // Facebook Provider Configuration - Optional, gracefully degraded if missing
                ["EasyAuth:Providers:Facebook:Enabled"] = "false", // Start disabled for graceful testing
                ["EasyAuth:Providers:Facebook:AppId"] = "test-facebook-app-id",
                ["EasyAuth:Providers:Facebook:AppSecret"] = "test-facebook-app-secret",
                
                // Apple Provider Configuration - Optional, gracefully degraded if missing
                ["EasyAuth:Providers:Apple:Enabled"] = "false", // Start disabled for graceful testing
                ["EasyAuth:Providers:Apple:ClientId"] = "test-apple-client-id",
                ["EasyAuth:Providers:Apple:TeamId"] = "test-team-id",
                ["EasyAuth:Providers:Apple:KeyId"] = "test-key-id",
                ["EasyAuth:Providers:Apple:JwtSecret"] = "test-jwt-secret-for-performance-testing-only"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient();
        
        // Register test provider factory that works without real OAuth configuration
        services.AddScoped<IEAuthProviderFactory, TestProviderFactory>();
        
        return services;
    }
}