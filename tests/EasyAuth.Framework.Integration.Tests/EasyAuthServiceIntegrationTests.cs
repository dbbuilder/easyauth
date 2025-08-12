using AutoFixture;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Integration tests for core EasyAuth service functionality
/// Tests the main authentication flows using the actual service interfaces
/// </summary>
public class EasyAuthServiceIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Fixture _fixture;

    public EasyAuthServiceIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetProvidersAsync_ReturnsAvailableProviders()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        // Act
        var response = await eauthService.GetProvidersAsync();

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNullOrEmpty();
        response.Data.Should().HaveCount(4); // Google, Apple, Facebook, AzureB2C

        var providerNames = response.Data?.Select(p => p.Name).ToList();
        providerNames.Should().Contain("google");
        providerNames.Should().Contain("apple");
        providerNames.Should().Contain("facebook");
        providerNames.Should().Contain("azureb2c");

        _testOutputHelper.WriteLine($"Available providers: {string.Join(", ", providerNames ?? new List<string>())}");
    }

    [Theory]
    [InlineData("google")]
    [InlineData("apple")]
    [InlineData("facebook")]
    [InlineData("azureb2c")]
    public async Task InitiateLoginAsync_WithValidProvider_ReturnsAuthorizationUrl(string provider)
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        var loginRequest = _fixture.Build<LoginRequest>()
            .With(r => r.Provider, provider)
            .With(r => r.ReturnUrl, "https://localhost/callback")
            .Create();

        // Act
        var response = await eauthService.InitiateLoginAsync(loginRequest);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNullOrEmpty();

        // Verify URL structure based on provider
        switch (provider.ToLower())
        {
            case "google":
                response.Data.Should().StartWith("https://accounts.google.com");
                break;
            case "apple":
                response.Data.Should().StartWith("https://appleid.apple.com");
                break;
            case "facebook":
                response.Data.Should().StartWith("https://www.facebook.com");
                break;
            case "azureb2c":
                response.Data.Should().StartWith("https://test-tenant.b2clogin.com");
                break;
        }

        response.Data.Should().Contain("client_id=");
        response.Data.Should().Contain("redirect_uri=");
        response.Data.Should().Contain("response_type=code");
        response.Data.Should().Contain("state="); // CSRF protection

        _testOutputHelper.WriteLine($"Provider: {provider}");
        _testOutputHelper.WriteLine($"Auth URL: {response.Data}");
    }

    [Fact]
    public async Task InitiateLoginAsync_WithInvalidProvider_ReturnsError()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        var loginRequest = _fixture.Build<LoginRequest>()
            .With(r => r.Provider, "invalid_provider")
            .With(r => r.ReturnUrl, "https://localhost/callback")
            .Create();

        // Act
        var response = await eauthService.InitiateLoginAsync(loginRequest);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();
        response.Message.Should().Contain("invalid_provider");

        _testOutputHelper.WriteLine($"Expected error: {response.Message}");
    }

    [Fact]
    public async Task HandleAuthCallbackAsync_WithInvalidCode_ReturnsError()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        var invalidCode = "invalid_authorization_code";
        var state = "test_state_parameter";

        // Act
        var response = await eauthService.HandleAuthCallbackAsync("google", invalidCode, state);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Expected auth callback error: {response.Message}");
    }

    [Fact]
    public async Task HandleAuthCallbackAsync_WithMissingState_ReturnsError()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        var testCode = "test_authorization_code";

        // Act - Missing state parameter
        var response = await eauthService.HandleAuthCallbackAsync("google", testCode, null);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Expected missing state error: {response.Message}");
    }

    [Fact]
    public async Task ValidateSessionAsync_WithInvalidSession_ReturnsError()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        var invalidSessionId = Guid.NewGuid().ToString();

        // Act
        var response = await eauthService.ValidateSessionAsync(invalidSessionId);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Expected invalid session error: {response.Message}");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithoutSession_ReturnsError()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        // Act
        var response = await eauthService.GetCurrentUserAsync();

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Expected no session error: {response.Message}");
    }

    [Fact]
    public async Task SignOutAsync_WithoutSession_ReturnsSuccess()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

        // Act - Sign out without session should be idempotent
        var response = await eauthService.SignOutAsync(null);

        // Assert
        response.Should().NotBeNull();
        // Sign out should be idempotent - succeeds even without session
        response.Success.Should().BeTrue();
        response.Data.Should().BeTrue();

        _testOutputHelper.WriteLine("Sign out without session completed successfully (idempotent)");
    }

    [Fact]
    public async Task ProviderFactory_GetAllEnabledProviders_ReturnsProviders()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providers = await providerFactory.GetProvidersAsync();

        // Assert
        providers.Should().NotBeNullOrEmpty();
        providers.Should().HaveCount(4); // Google, Apple, Facebook, AzureB2C

        // For now just verify we got providers since IEAuthProvider interface structure is not defined yet
        foreach (var provider in providers)
        {
            provider.Should().NotBeNull();
            _testOutputHelper.WriteLine($"Provider retrieved successfully");
        }
    }

    [Theory]
    [InlineData("google")]
    [InlineData("apple")]
    [InlineData("facebook")]
    [InlineData("azureb2c")]
    public async Task ProviderFactory_GetProvider_ReturnsValidProvider(string providerName)
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var provider = await providerFactory.GetProviderAsync(providerName);

        // Assert
        provider.Should().NotBeNull();

        // For now just verify provider exists since IEAuthProvider interface is not fully defined yet
        _testOutputHelper.WriteLine($"Provider {providerName} retrieved successfully");
    }

    [Fact]
    public async Task ProviderFactory_GetInvalidProvider_ThrowsException()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await providerFactory.GetProviderAsync("invalid_provider"));

        exception.Message.Should().Contain("invalid_provider");

        _testOutputHelper.WriteLine($"Expected exception for invalid provider: {exception.Message}");
    }

    [Fact]
    public async Task DatabaseService_InitializeDatabase_CompletesSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        // Act
        var initResult = await databaseService.InitializeDatabaseAsync();
        var isInitialized = await databaseService.IsDatabaseInitializedAsync();
        var version = await databaseService.GetDatabaseVersionAsync();

        // Assert
        initResult.Should().BeTrue();
        isInitialized.Should().BeTrue();
        version.Should().NotBeNullOrEmpty();

        _testOutputHelper.WriteLine($"Database initialized: {initResult}");
        _testOutputHelper.WriteLine($"Database version: {version}");
    }

    [Fact]
    public async Task CompleteAuthenticationFlow_Simulation_ValidatesIntegration()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

            // Step 1: Get available providers
            var providersResponse = await eauthService.GetProvidersAsync();
            providersResponse.Success.Should().BeTrue();
            providersResponse.Data.Should().NotBeNullOrEmpty();

            // Step 2: Initiate login with Google
            var loginRequest = new LoginRequest
            {
                Provider = "google",
                ReturnUrl = "https://localhost/dashboard"
            };

            var loginResponse = await eauthService.InitiateLoginAsync(loginRequest);
            loginResponse.Success.Should().BeTrue();
            loginResponse.Data.Should().NotBeNullOrEmpty();

            // Step 3: Simulate callback (will fail with invalid code, but validates endpoint)
            var callbackResponse = await eauthService.HandleAuthCallbackAsync(
                "google", "simulated_auth_code", "test_state");

            // This will fail because we don't have a valid auth code, but it validates the flow
            callbackResponse.Success.Should().BeFalse();
            callbackResponse.Message.Should().NotBeNullOrEmpty();

            _testOutputHelper.WriteLine("✅ Complete authentication flow simulation completed");
            _testOutputHelper.WriteLine($"   - Providers: ✅ {providersResponse.Data?.Count()} providers");
            _testOutputHelper.WriteLine($"   - Login initiation: ✅ {loginResponse.Success}");
            _testOutputHelper.WriteLine($"   - Callback handling: ✅ {callbackResponse.Success} (expected failure with invalid code)");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }
}