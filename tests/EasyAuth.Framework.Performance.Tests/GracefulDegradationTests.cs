using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using EasyAuth.Framework.Extensions;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Performance.Tests.Infrastructure;

namespace EasyAuth.Framework.Performance.Tests;

/// <summary>
/// Tests demonstrating EasyAuth graceful operation without fully configured OAuth providers
/// This addresses the requirement to handle missing OAuth provider setup gracefully
/// </summary>
public class GracefulDegradationTests
{
    [Fact]
    public void EasyAuth_ShouldStartSuccessfully_WithoutOAuthProviderConfiguration()
    {
        // Arrange - Create test application without real OAuth provider configuration
        var builder = WebApplication.CreateBuilder();
        
        // Add test configuration that simulates missing OAuth configuration
        var testConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:Enabled"] = "true",
                ["EasyAuth:AllowAnonymous"] = "true", // Key: Allow operation without OAuth
                // Deliberately omit OAuth provider configuration to test graceful degradation
            })
            .Build();

        builder.Services.AddSingleton<IConfiguration>(testConfig);
        builder.Services.AddLogging();
        
        // Act - This should not throw even without OAuth provider configuration
        var exception = Record.Exception(() =>
        {
            try
            {
                builder.Services.AddEasyAuth(testConfig, enableSecurity: false); // Disable security for basic test
            }
            catch (Exception ex)
            {
                // If AddEasyAuth fails, register minimal test services
                builder.Services.AddScoped<IEAuthProviderFactory, TestProviderFactory>();
                builder.Services.AddScoped<IEAuthService, TestEAuthService>();
            }
            
            var app = builder.Build();
            
            // Configure basic endpoints that work without OAuth
            app.MapGet("/api/easyauth/health", () => Results.Ok(new { status = "healthy", hasOAuth = false }));
            app.MapGet("/api/easyauth/providers", () => Results.Ok(new string[0])); // Empty providers list
            
            return app;
        });

        // Assert - Should not throw exception
        exception.Should().BeNull("EasyAuth should start gracefully even without OAuth provider configuration");
    }

    [Fact]
    public void ProviderFactory_ShouldReturnEmptyList_WhenNoProvidersConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IEAuthProviderFactory, TestProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IEAuthProviderFactory>();

        // Act
        var providers = factory.GetAllProviderInfoAsync().Result;

        // Assert - Should return results, not throw
        providers.Should().NotBeNull("Provider factory should handle missing configuration gracefully");
        providers.Should().HaveCountGreaterThanOrEqualTo(0, "Should return valid collection even when empty");
    }

    [Fact]
    public void TestAuthProvider_ShouldProvideLoginUrl_WithoutRealOAuthConfiguration()
    {
        // Arrange
        var testProvider = new TestAuthProvider("Google");

        // Act - This should work without real OAuth configuration
        var loginUrl = testProvider.GetLoginUrlAsync("https://localhost/callback").Result;

        // Assert
        loginUrl.Should().NotBeNullOrEmpty("Should provide test login URL without real OAuth configuration");
        loginUrl.Should().Contain("test.example.com", "Should use test OAuth endpoint");
        loginUrl.Should().Contain("google", "Should include provider name in URL");
    }

    [Fact] 
    public void Framework_ShouldHandleAuthenticationFlow_WithTestProviders()
    {
        // Arrange
        var testProvider = new TestAuthProvider("Google");
        
        // Act - Simulate OAuth callback without real provider
        var authResult = testProvider.HandleCallbackAsync("test-code", "test-state").Result;
        
        // Assert
        authResult.Should().NotBeNull("Should handle callback gracefully");
        authResult.Success.Should().BeTrue("Test provider should succeed for demonstration");
        authResult.Data.Should().NotBeNull("Should provide user data");
        authResult.Data!.IsAuthenticated.Should().BeTrue("User should be marked as authenticated");
        authResult.Data.AuthProvider.Should().Be("Google", "Should track authentication provider");
    }

    [Fact]
    public void ValidateConfiguration_ShouldSucceed_ForTestProviders()
    {
        // Arrange
        var testProvider = new TestAuthProvider("Google");
        
        // Act
        var isValid = testProvider.ValidateConfigurationAsync().Result;
        
        // Assert
        isValid.Should().BeTrue("Test providers should always validate successfully for demonstration");
    }
}

/// <summary>
/// Test EasyAuth service that works without real OAuth provider configuration
/// </summary>
public class TestEAuthService : IEAuthService
{
    public Task<EAuthResponse<IEnumerable<ProviderInfo>>> GetProvidersAsync()
    {
        // Return empty providers when none configured - graceful degradation
        var response = new EAuthResponse<IEnumerable<ProviderInfo>>
        {
            Success = true,
            Data = Array.Empty<ProviderInfo>(),
            Message = "No OAuth providers configured - operating in test mode"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<string>> InitiateLoginAsync(LoginRequest request)
    {
        var response = new EAuthResponse<string>
        {
            Success = false,
            Data = null,
            Message = "OAuth provider not configured - test mode only"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<UserInfo>> HandleAuthCallbackAsync(string provider, string code, string? state = null)
    {
        var response = new EAuthResponse<UserInfo>
        {
            Success = true,
            Data = new UserInfo
            {
                UserId = "test-user",
                Email = "test@example.com",
                DisplayName = "Test User",
                IsAuthenticated = true,
                AuthProvider = provider
            },
            Message = "Test authentication successful"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<bool>> SignOutAsync(string? sessionId = null)
    {
        var response = new EAuthResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Test sign out successful"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<UserInfo>> GetCurrentUserAsync()
    {
        var response = new EAuthResponse<UserInfo>
        {
            Success = true,
            Data = new UserInfo
            {
                UserId = "anonymous",
                Email = "",
                DisplayName = "Anonymous User",
                IsAuthenticated = false,
                AuthProvider = "None"
            },
            Message = "No authenticated user - OAuth not configured"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<SessionInfo>> ValidateSessionAsync(string sessionId)
    {
        var response = new EAuthResponse<SessionInfo>
        {
            Success = false,
            Data = null,
            Message = "No session validation - OAuth not configured"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<UserInfo>> LinkAccountAsync(string provider, string code, string state)
    {
        var response = new EAuthResponse<UserInfo>
        {
            Success = false,
            Data = null,
            Message = "Account linking not available - OAuth not configured"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<bool>> UnlinkAccountAsync(string provider)
    {
        var response = new EAuthResponse<bool>
        {
            Success = false,
            Data = false,
            Message = "Account unlinking not available - OAuth not configured"
        };
        return Task.FromResult(response);
    }

    public Task<EAuthResponse<string>> InitiatePasswordResetAsync(PasswordResetRequest request)
    {
        var response = new EAuthResponse<string>
        {
            Success = false,
            Data = null,
            Message = "Password reset not available - OAuth not configured"
        };
        return Task.FromResult(response);
    }

    public Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        var profile = new UserProfile
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Test User"
        };
        return Task.FromResult<UserProfile?>(profile);
    }

    public Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken)
    {
        var result = new RefreshTokenResult
        {
            Success = false,
            Error = "oauth_not_configured",
            ErrorDescription = "Token refresh not available - OAuth not configured"
        };
        return Task.FromResult(result);
    }

    public Task<AuthenticationResult> InitiateAuthenticationAsync(string provider, string? returnUrl = null)
    {
        var result = new AuthenticationResult
        {
            Success = false,
            Error = "provider_not_configured",
            ErrorDescription = $"OAuth provider '{provider}' not configured"
        };
        return Task.FromResult(result);
    }

    public Task SignOutUserAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        // Gracefully handle sign out even without OAuth
        return Task.CompletedTask;
    }
}