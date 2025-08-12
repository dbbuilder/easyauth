using System.Net;
using System.Text.Json;
using AutoFixture;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Integration tests for provider authentication flows
/// Tests provider factory and basic authentication service integration
/// </summary>
public class ProviderAuthenticationFlowTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Fixture _fixture;

    public ProviderAuthenticationFlowTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ProviderFactory_GetProviders_ReturnsConfiguredProviders()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providers = await providerFactory.GetProvidersAsync();

        // Assert
        providers.Should().NotBeNullOrEmpty();
        providers.Should().HaveCount(4); // Google, Apple, Facebook, AzureB2C

        _testOutputHelper.WriteLine($"Retrieved {providers.Count()} providers");
    }

    [Theory]
    [InlineData("google")]
    [InlineData("apple")]
    [InlineData("facebook")]
    [InlineData("azureb2c")]
    public async Task ProviderFactory_GetSpecificProvider_ReturnsProvider(string providerName)
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var provider = await providerFactory.GetProviderAsync(providerName);

        // Assert
        provider.Should().NotBeNull();

        _testOutputHelper.WriteLine($"Provider {providerName} retrieved successfully");
    }

    [Fact]
    public async Task ProviderFactory_GetInvalidProvider_ReturnsNull()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var provider = await providerFactory.GetProviderAsync("invalid_provider");

        // Assert
        provider.Should().BeNull();

        _testOutputHelper.WriteLine("Invalid provider returned null as expected");
    }

    [Fact]
    public async Task ProviderFactory_GetAllProviderInfo_ReturnsProviderInfo()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providerInfos = await providerFactory.GetAllProviderInfoAsync();

        // Assert
        providerInfos.Should().NotBeNullOrEmpty();
        providerInfos.Should().HaveCount(4);

        var providerNames = providerInfos.Select(p => p.Name).ToList();
        providerNames.Should().Contain("google");
        providerNames.Should().Contain("apple");
        providerNames.Should().Contain("facebook");
        providerNames.Should().Contain("azureb2c");

        foreach (var providerInfo in providerInfos)
        {
            providerInfo.Name.Should().NotBeNullOrEmpty();
            providerInfo.DisplayName.Should().NotBeNullOrEmpty();
            providerInfo.IsEnabled.Should().BeTrue();

            _testOutputHelper.WriteLine($"Provider: {providerInfo.Name} - {providerInfo.DisplayName}");
        }
    }

    [Theory]
    [InlineData("google")]
    [InlineData("apple")]
    [InlineData("facebook")]
    [InlineData("azureb2c")]
    public async Task ProviderFactory_GetProviderInfo_ReturnsValidInfo(string providerName)
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providerInfo = await providerFactory.GetProviderInfoAsync(providerName);

        // Assert
        providerInfo.Should().NotBeNull();
        providerInfo?.Name.Should().Be(providerName);
        providerInfo?.DisplayName.Should().NotBeNullOrEmpty();
        providerInfo?.IsEnabled.Should().BeTrue();

        _testOutputHelper.WriteLine($"Provider {providerName}: {providerInfo?.DisplayName}");
    }

    [Fact]
    public async Task ProviderFactory_ValidateProviders_ReturnsValidationResult()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var validationResult = await providerFactory.ValidateProvidersAsync();

        // Assert
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue();
        validationResult.ValidationErrors.Should().BeEmpty();
        validationResult.ProviderResults.Should().NotBeEmpty();

        _testOutputHelper.WriteLine($"Provider validation: {validationResult.IsValid}");
        _testOutputHelper.WriteLine($"Provider results: {validationResult.ProviderResults.Count}");
    }

    [Fact]
    public async Task ProviderFactory_GetProviderCapabilities_ReturnsCapabilities()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var capabilities = await providerFactory.GetProviderCapabilitiesAsync("google");

        // Assert
        capabilities.Should().NotBeNull();
        capabilities?.SupportedMethods.Should().NotBeNull();
        capabilities?.SupportedScopes.Should().NotBeNull();
        capabilities?.MaxSessionDurationMinutes.Should().BeGreaterThan(0);

        _testOutputHelper.WriteLine($"Google capabilities: Methods={capabilities?.SupportedMethods.Length}, Scopes={capabilities?.SupportedScopes.Length}");
    }

    [Fact]
    public async Task ProviderFactory_GetProviderHealth_ReturnsHealthStatus()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var health = await providerFactory.GetProviderHealthAsync("google");

        // Assert
        health.Should().NotBeNull();
        health?.ProviderName.Should().Be("google");
        health?.LastChecked.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));

        _testOutputHelper.WriteLine($"Google health: IsHealthy={health?.IsHealthy}, ResponseTime={health?.ResponseTimeMs}ms");
    }

    [Fact]
    public async Task EAuthService_IntegrateWithProviders_CompletesWorkflow()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();
            var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

            // Step 1: Get providers through EAuthService
            var providersResponse = await eauthService.GetProvidersAsync();
            providersResponse.Success.Should().BeTrue();
            providersResponse.Data.Should().NotBeNullOrEmpty();

            // Step 2: Get providers through ProviderFactory
            var providers = await providerFactory.GetProvidersAsync();
            providers.Should().HaveCount(providersResponse.Data?.Count() ?? 0);

            // Step 3: Validate providers
            var validationResult = await providerFactory.ValidateProvidersAsync();
            validationResult.IsValid.Should().BeTrue();

            _testOutputHelper.WriteLine($"✅ EAuthService and ProviderFactory integration completed");
            _testOutputHelper.WriteLine($"   - EAuthService providers: ✅ {providersResponse.Data?.Count()}");
            _testOutputHelper.WriteLine($"   - ProviderFactory providers: ✅ {providers.Count()}");
            _testOutputHelper.WriteLine($"   - Validation: ✅ {validationResult.IsValid}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }
}