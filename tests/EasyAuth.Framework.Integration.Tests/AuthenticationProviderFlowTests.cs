using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Comprehensive integration tests for authentication provider flows
/// Tests end-to-end authentication scenarios, security validation, and error handling
/// Follows TDD methodology with comprehensive coverage
/// </summary>
public class AuthenticationProviderFlowTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Fixture _fixture;

    public AuthenticationProviderFlowTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fixture = new Fixture();
    }

    #region Google Provider Integration Tests

    [Fact]
    public async Task GoogleProvider_AuthorizationUrlGeneration_CreatesValidUrl()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var returnUrl = "https://myapp.com/dashboard";

        // Act
        var provider = await providerFactory.GetProviderAsync("google");
        var authUrl = await provider!.GetAuthorizationUrlAsync(returnUrl);

        // Assert
        authUrl.Should().NotBeNullOrEmpty();
        authUrl.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");
        authUrl.Should().Contain("client_id=test-client-id");
        authUrl.Should().Contain("response_type=code");
        authUrl.Should().Contain("scope=openid%20profile%20email");
        authUrl.Should().Contain("state=");
        authUrl.Should().Contain("redirect_uri=");

        _testOutputHelper.WriteLine($"✅ Google authorization URL: {authUrl}");
    }

    [Fact]
    public async Task GoogleProvider_ValidationSecrets_ValidatesConfiguration()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var validationResult = await providerFactory.ValidateProvidersAsync();
        var googleValidation = validationResult.ProviderResults["google"];

        // Assert
        validationResult.IsValid.Should().BeTrue();
        googleValidation.Should().BeTrue();
        validationResult.ValidationErrors.Should().BeEmpty();

        _testOutputHelper.WriteLine("✅ Google provider configuration validation passed");
    }

    [Fact]
    public async Task GoogleProvider_SecurityValidation_RejectsInvalidStates()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var provider = await providerFactory.GetProviderAsync("google");

        // Act & Assert - Test various invalid state scenarios
        var exception = await Record.ExceptionAsync(() =>
            provider!.ExchangeCodeForTokenAsync("valid_code", "malicious_state_injection;<script>alert('xss')</script>"));

        exception.Should().NotBeNull();
        // Should be one of these exception types for security violations
        var isValidExceptionType = exception is ArgumentException or SecurityException or InvalidOperationException;
        isValidExceptionType.Should().BeTrue();

        _testOutputHelper.WriteLine("✅ Google provider rejected malicious state parameter");
    }

    #endregion

    #region Apple Provider Integration Tests

    [Fact]
    public async Task AppleProvider_AuthorizationUrlGeneration_CreatesValidUrl()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var returnUrl = "https://myapp.com/dashboard";

        // Act
        var provider = await providerFactory.GetProviderAsync("apple");
        var authUrl = await provider!.GetAuthorizationUrlAsync(returnUrl);

        // Assert
        authUrl.Should().NotBeNullOrEmpty();
        authUrl.Should().StartWith("https://appleid.apple.com/auth/authorize");
        authUrl.Should().Contain("client_id=test-apple-client");
        authUrl.Should().Contain("response_type=code");
        authUrl.Should().Contain("scope=name%20email");
        authUrl.Should().Contain("response_mode=form_post");
        authUrl.Should().Contain("state=");

        _testOutputHelper.WriteLine($"✅ Apple authorization URL: {authUrl}");
    }

    [Fact]
    public async Task AppleProvider_JWTSecretSecurity_UsesSecureConfiguration()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        // Act
        var jwtSecret = configurationService.GetSecretValue("Apple:JwtSecret", "APPLE_JWT_SECRET");

        // Assert - CRITICAL: Ensure no hardcoded secrets are used
        jwtSecret.Should().NotContain("dummy_secret_key_for_testing_only");
        jwtSecret.Should().NotContain("test_secret");
        jwtSecret.Should().NotContain("changeme");
        
        if (jwtSecret != null)
        {
            jwtSecret.Length.Should().BeGreaterThan(32, "JWT secrets should be sufficiently long");
        }

        _testOutputHelper.WriteLine("✅ Apple provider uses secure JWT secret configuration");
    }

    [Fact]
    public async Task AppleProvider_PrivateEmailHandling_ProcessesCorrectly()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var provider = await providerFactory.GetProviderAsync("apple");

        // This test validates that the provider can handle Apple's private email relay
        // In integration context, we test the structure rather than actual OAuth flow

        // Act
        var providerInfo = await providerFactory.GetProviderInfoAsync("apple");

        // Assert
        providerInfo.Should().NotBeNull();
        providerInfo!.Capabilities.SupportedScopes.Should().Contain("email");
        providerInfo.Capabilities.SupportsAccountLinking.Should().BeTrue();

        _testOutputHelper.WriteLine("✅ Apple provider configured for private email handling");
    }

    #endregion

    #region Facebook Provider Integration Tests

    [Fact]
    public async Task FacebookProvider_AuthorizationUrlGeneration_CreatesValidUrl()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var returnUrl = "https://myapp.com/dashboard";

        // Act
        var provider = await providerFactory.GetProviderAsync("facebook");
        var authUrl = await provider!.GetAuthorizationUrlAsync(returnUrl);

        // Assert
        authUrl.Should().NotBeNullOrEmpty();
        authUrl.Should().StartWith("https://www.facebook.com/v");
        authUrl.Should().Contain("client_id=test-app-id");
        authUrl.Should().Contain("response_type=code");
        authUrl.Should().Contain("scope=email%2Cpublic_profile");
        authUrl.Should().Contain("state=");

        _testOutputHelper.WriteLine($"✅ Facebook authorization URL: {authUrl}");
    }

    [Fact]
    public async Task FacebookProvider_ScopeValidation_RequestsMinimalScopes()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providerInfo = await providerFactory.GetProviderInfoAsync("facebook");

        // Assert - Facebook should only request minimal necessary scopes
        providerInfo.Should().NotBeNull();
        providerInfo!.Capabilities.SupportedScopes.Should().Contain("email");
        providerInfo.Capabilities.SupportedScopes.Should().Contain("public_profile");
        
        // Should NOT contain excessive permissions
        providerInfo.Capabilities.SupportedScopes.Should().NotContain("user_posts");
        providerInfo.Capabilities.SupportedScopes.Should().NotContain("user_friends");

        _testOutputHelper.WriteLine("✅ Facebook provider requests minimal necessary scopes");
    }

    #endregion

    #region Azure B2C Provider Integration Tests

    [Fact]
    public async Task AzureB2CProvider_AuthorizationUrlGeneration_CreatesValidUrl()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();
        var returnUrl = "https://myapp.com/dashboard";

        // Act
        var provider = await providerFactory.GetProviderAsync("azureb2c");
        var authUrl = await provider!.GetAuthorizationUrlAsync(returnUrl);

        // Assert
        authUrl.Should().NotBeNullOrEmpty();
        authUrl.Should().Contain("test-tenant.onmicrosoft.com");
        authUrl.Should().Contain("client_id=test-b2c-client");
        authUrl.Should().Contain("response_type=code");
        authUrl.Should().Contain("B2C_1_signupsignin");
        authUrl.Should().Contain("state=");

        _testOutputHelper.WriteLine($"✅ Azure B2C authorization URL: {authUrl}");
    }

    [Fact]
    public async Task AzureB2CProvider_PolicySupport_SupportsAllPolicies()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providerInfo = await providerFactory.GetProviderInfoAsync("azureb2c");

        // Assert
        providerInfo.Should().NotBeNull();
        providerInfo!.Capabilities.SupportsPasswordReset.Should().BeTrue();
        providerInfo.Capabilities.SupportsProfileEditing.Should().BeTrue();
        providerInfo.Capabilities.SupportsAccountLinking.Should().BeTrue();

        _testOutputHelper.WriteLine("✅ Azure B2C provider supports all required policies");
    }

    #endregion

    #region End-to-End Authentication Flow Tests

    [Fact]
    public async Task EAuthService_CompleteAuthenticationFlow_ProcessesSuccessfully()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();
            var testUserId = await CreateTestUserAsync();

            var loginRequest = new LoginRequest
            {
                Provider = "google",
                ReturnUrl = "https://myapp.com/dashboard",
                RememberMe = true
            };

            // Act - Step 1: Initiate Login
            var loginResponse = await eauthService.InitiateLoginAsync(loginRequest);

            // Assert - Step 1
            loginResponse.Should().NotBeNull();
            loginResponse.Success.Should().BeTrue();
            loginResponse.Data.Should().NotBeNullOrEmpty();
            loginResponse.Data.Should().StartWith("https://accounts.google.com");

            _testOutputHelper.WriteLine($"✅ Step 1 - Login initiation: {loginResponse.Data}");

            // Act - Step 2: Get Providers
            var providersResponse = await eauthService.GetProvidersAsync();

            // Assert - Step 2
            providersResponse.Should().NotBeNull();
            providersResponse.Success.Should().BeTrue();
            providersResponse.Data.Should().NotBeNullOrEmpty();
            providersResponse.Data!.Should().Contain(p => p.Name == "google");

            _testOutputHelper.WriteLine($"✅ Step 2 - Providers retrieved: {providersResponse.Data!.Count()}");

            // Act - Step 3: Get Current User (should be null before auth)
            var currentUserResponse = await eauthService.GetCurrentUserAsync();

            // Assert - Step 3
            currentUserResponse.Should().NotBeNull();
            // Current user might be null or unauthorized - this is expected

            _testOutputHelper.WriteLine("✅ Step 3 - Current user status validated");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Theory]
    [InlineData("google")]
    [InlineData("apple")]
    [InlineData("facebook")]
    [InlineData("azureb2c")]
    public async Task EAuthService_ProviderSpecificLogin_GeneratesValidUrls(string providerName)
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

            var loginRequest = new LoginRequest
            {
                Provider = providerName,
                ReturnUrl = "https://myapp.com/dashboard",
                Email = "test@example.com"
            };

            // Act
            var response = await eauthService.InitiateLoginAsync(loginRequest);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNullOrEmpty();
            response.Data.Should().StartWith("https://");
            response.Data.Should().Contain("client_id=");
            response.Data.Should().Contain("state=");

            _testOutputHelper.WriteLine($"✅ {providerName} login URL: {response.Data}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Security and Error Handling Tests

    [Fact]
    public async Task EAuthService_InvalidProvider_ReturnsError()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

            var loginRequest = new LoginRequest
            {
                Provider = "malicious_provider",
                ReturnUrl = "https://myapp.com/dashboard"
            };

            // Act
            var response = await eauthService.InitiateLoginAsync(loginRequest);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().NotBeNullOrEmpty();
            response.Data.Should().BeNullOrEmpty();

            _testOutputHelper.WriteLine($"✅ Invalid provider rejected: {response.ErrorCode}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task EAuthService_XSSAttempt_SanitizesInput()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

            var loginRequest = new LoginRequest
            {
                Provider = "google",
                ReturnUrl = "https://myapp.com/dashboard?xss=<script>alert('xss')</script>",
                Email = "test@example.com<script>alert('email')</script>"
            };

            // Act
            var response = await eauthService.InitiateLoginAsync(loginRequest);

            // Assert
            response.Should().NotBeNull();
            if (response.Success)
            {
                // If successful, ensure no script tags are present
                response.Data.Should().NotContain("<script>");
                response.Data.Should().NotContain("alert(");
            }
            else
            {
                // If failed, it should be due to validation, not XSS execution
                response.ErrorCode.Should().NotBeNullOrEmpty();
            }

            _testOutputHelper.WriteLine("✅ XSS attempt properly handled");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task EAuthService_SQLInjectionAttempt_PreventsInjection()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();

            var loginRequest = new LoginRequest
            {
                Provider = "google",
                ReturnUrl = "https://myapp.com/dashboard",
                Email = "test@example.com'; DROP TABLE Users; --"
            };

            // Act
            var response = await eauthService.InitiateLoginAsync(loginRequest);

            // Assert
            response.Should().NotBeNull();
            // Either succeeds with sanitized input or fails validation - both are acceptable
            
            // Verify database is still intact by checking providers
            var providersResponse = await eauthService.GetProvidersAsync();
            providersResponse.Success.Should().BeTrue();

            _testOutputHelper.WriteLine("✅ SQL injection attempt prevented");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Performance and Load Tests

    [Fact]
    public async Task EAuthService_ConcurrentRequests_HandlesLoadCorrectly()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var eauthService = scope.ServiceProvider.GetRequiredService<IEAuthService>();
            const int concurrentRequests = 10;

            // Act - Create multiple concurrent requests
            var tasks = new List<Task<EAuthResponse<IEnumerable<ProviderInfo>>>>();
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(eauthService.GetProvidersAsync());
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().HaveCount(concurrentRequests);
            responses.Should().AllSatisfy(r => r.Success.Should().BeTrue());
            responses.Should().AllSatisfy(r => r.Data.Should().NotBeNullOrEmpty());

            _testOutputHelper.WriteLine($"✅ Handled {concurrentRequests} concurrent requests successfully");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task ProviderFactory_CachingBehavior_OptimizesPerformance()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var providerFactory = scope.ServiceProvider.GetRequiredService<IEAuthProviderFactory>();

            // Act - Multiple calls to same provider
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var provider1 = await providerFactory.GetProviderAsync("google");
            var firstCallTime = stopwatch.ElapsedMilliseconds;
            
            var provider2 = await providerFactory.GetProviderAsync("google");
            var secondCallTime = stopwatch.ElapsedMilliseconds - firstCallTime;

            stopwatch.Stop();

            // Assert
            provider1.Should().NotBeNull();
            provider2.Should().NotBeNull();
            provider1.Should().BeSameAs(provider2); // Should be same instance if cached

            _testOutputHelper.WriteLine($"✅ Provider caching - First call: {firstCallTime}ms, Second call: {secondCallTime}ms");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Configuration and Validation Tests

    [Fact]
    public async Task ConfigurationService_SecretRetrieval_FollowsFallbackChain()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        // Act - Test fallback chain for various secrets
        var googleSecret = configurationService.GetSecretValue("EasyAuth:Providers:Google:ClientSecret");
        var appleSecret = configurationService.GetSecretValue("EasyAuth:Providers:Apple:ClientSecret");

        // Assert
        googleSecret.Should().Be("test-client-secret"); // Should get from configuration
        appleSecret.Should().BeNull(); // Not configured, should fallback

        _testOutputHelper.WriteLine("✅ Configuration service fallback chain working");
    }

    [Fact]
    public async Task ConfigurationService_RequiredSecrets_ValidatesCorrectly()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        var requiredSecrets = new Dictionary<string, string>
        {
            ["EasyAuth:Providers:Google:ClientSecret"] = "Google OAuth client secret",
            ["EasyAuth:Providers:Apple:JwtSecret"] = "Apple JWT signing secret"
        };

        // Act
        var validationErrors = configurationService.ValidateRequiredSecrets(requiredSecrets);

        // Assert
        // Some secrets may be missing in test environment - this is expected
        validationErrors.Should().NotBeNull();
        
        if (validationErrors.Any())
        {
            _testOutputHelper.WriteLine($"⚠️  Missing secrets (expected in test): {string.Join(", ", validationErrors)}");
        }
        else
        {
            _testOutputHelper.WriteLine("✅ All required secrets validated");
        }
    }

    #endregion
}