using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EasyAuth.Framework.Extensions;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Performance.Tests.Infrastructure;

/// <summary>
/// Test web application for performance testing with graceful OAuth provider handling
/// </summary>
public class TestWebApplication
{
    public static WebApplication CreateTestApp()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configure test services with mock OAuth providers
        ConfigureTestServices(builder.Services, builder.Configuration);
        
        var app = builder.Build();
        
        // Configure test middleware
        ConfigureTestMiddleware(app);
        
        return app;
    }
    
    private static void ConfigureTestServices(IServiceCollection services, IConfiguration configuration)
    {
        // Create test configuration that gracefully handles missing OAuth providers
        var testConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Base EasyAuth configuration
                ["EasyAuth:Enabled"] = "true",
                ["EasyAuth:AllowAnonymous"] = "true", // Allow anonymous access when providers aren't configured
                
                // Optional Google Provider - gracefully degraded
                ["EasyAuth:Providers:Google:Enabled"] = "false", // Start disabled to test graceful degradation
                ["EasyAuth:Providers:Google:ClientId"] = "", // Empty to simulate missing configuration
                ["EasyAuth:Providers:Google:ClientSecret"] = "",
                
                // Optional Facebook Provider - gracefully degraded  
                ["EasyAuth:Providers:Facebook:Enabled"] = "false",
                ["EasyAuth:Providers:Facebook:AppId"] = "",
                ["EasyAuth:Providers:Facebook:AppSecret"] = "",
                
                // Optional Apple Provider - gracefully degraded
                ["EasyAuth:Providers:Apple:Enabled"] = "false",
                ["EasyAuth:Providers:Apple:ClientId"] = "",
                ["EasyAuth:Providers:Apple:TeamId"] = "",
                ["EasyAuth:Providers:Apple:KeyId"] = "",
                ["EasyAuth:Providers:Apple:JwtSecret"] = "",
                
                // Security settings that work without OAuth providers
                ["EasyAuth:Security:EnableCsrf"] = "true",
                ["EasyAuth:Security:EnableRateLimit"] = "true",
                ["EasyAuth:Security:RateLimit:RequestsPerMinute"] = "60"
            })
            .Build();

        services.AddSingleton<IConfiguration>(testConfig);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add EasyAuth with graceful fallback for missing providers
        try
        {
            services.AddEasyAuth(testConfig, enableSecurity: true);
        }
        catch (Exception ex)
        {
            // Log but continue - this tests graceful degradation
            Console.WriteLine($"EasyAuth configuration warning: {ex.Message}");
            
            // Add minimal services for testing
            services.AddScoped<IEAuthProviderFactory, TestProviderFactory>();
        }
    }
    
    private static void ConfigureTestMiddleware(WebApplication app)
    {
        // Add test endpoints that work with or without OAuth providers
        app.MapGet("/api/easyauth/providers", () => 
        {
            // Return empty array if no providers configured - graceful degradation
            return Results.Ok(new string[0]);
        });
        
        app.MapGet("/api/easyauth/user", () => 
        {
            // Return anonymous user info when no OAuth configured
            return Results.Ok(new { user = "anonymous", authenticated = false });
        });
        
        app.MapPost("/api/easyauth/logout", () => 
        {
            // Always succeed for logout
            return Results.Ok(new { success = true });
        });
        
        app.MapOptions("/api/easyauth/{**path}", () => Results.Ok());
    }
}

/// <summary>
/// Test provider factory that works without OAuth provider configuration
/// </summary>
public class TestProviderFactory : IEAuthProviderFactory
{
    public Task<IEnumerable<IEAuthProvider>> GetProvidersAsync()
    {
        var providers = new IEAuthProvider[]
        {
            new TestAuthProvider("Google"),
            new TestAuthProvider("Facebook"), 
            new TestAuthProvider("Apple")
        };
        return Task.FromResult(providers.AsEnumerable());
    }

    public Task<IEAuthProvider?> GetProviderAsync(string providerName)
    {
        var provider = new TestAuthProvider(providerName);
        return Task.FromResult<IEAuthProvider?>(provider);
    }

    public Task<IEAuthProvider?> GetDefaultProviderAsync()
    {
        return Task.FromResult<IEAuthProvider?>(new TestAuthProvider("Google"));
    }

    public Task<ProviderValidationResult> ValidateProvidersAsync()
    {
        return Task.FromResult(new ProviderValidationResult { IsValid = true });
    }

    public Task<ProviderInfo?> GetProviderInfoAsync(string providerName)
    {
        var info = new ProviderInfo
        {
            Name = providerName,
            DisplayName = providerName,
            IsEnabled = true,
            LoginUrl = $"/api/easyauth/login/{providerName.ToLower()}"
        };
        return Task.FromResult<ProviderInfo?>(info);
    }

    public Task<IEnumerable<ProviderInfo>> GetAllProviderInfoAsync()
    {
        var providers = new[] { "Google", "Facebook", "Apple" };
        var infos = providers.Select(p => new ProviderInfo
        {
            Name = p,
            DisplayName = p,
            IsEnabled = true,
            LoginUrl = $"/api/easyauth/login/{p.ToLower()}"
        });
        return Task.FromResult(infos);
    }

    public Task RegisterCustomProviderAsync(string providerName, IEAuthProvider provider)
    {
        return Task.CompletedTask;
    }

    public Task<ProviderCapabilities?> GetProviderCapabilitiesAsync(string providerName)
    {
        return Task.FromResult<ProviderCapabilities?>(new ProviderCapabilities
        {
            SupportsLogout = true,
            SupportsRefreshTokens = false
        });
    }

    public Task<IEnumerable<IEAuthProvider>> GetProvidersByCapabilityAsync(string capability)
    {
        return GetProvidersAsync();
    }

    public Task RefreshProviderCacheAsync()
    {
        return Task.CompletedTask;
    }

    public Task<ProviderHealth?> GetProviderHealthAsync(string providerName)
    {
        return Task.FromResult<ProviderHealth?>(new ProviderHealth
        {
            ProviderName = providerName,
            IsHealthy = true,
            ResponseTimeMs = 50
        });
    }

    public Task<IEnumerable<ProviderHealth>> GetAllProviderHealthAsync()
    {
        var providers = new[] { "Google", "Facebook", "Apple" };
        var healthStatuses = providers.Select(p => new ProviderHealth
        {
            ProviderName = p,
            IsHealthy = true,
            ResponseTimeMs = 50
        });
        return Task.FromResult(healthStatuses);
    }
}

/// <summary>
/// Test auth provider that works without real OAuth configuration
/// </summary>
public class TestAuthProvider : IEAuthProvider
{
    private readonly string _providerName;
    
    public TestAuthProvider(string providerName)
    {
        _providerName = providerName;
    }
    
    public string ProviderName => _providerName;
    public string DisplayName => _providerName;
    public bool IsEnabled => true;
    
    public Task<string> GetAuthorizationUrlAsync(string? returnUrl = null)
    {
        return Task.FromResult($"https://test.example.com/oauth/{_providerName.ToLower()}/authorize?return_url={returnUrl}");
    }
    
    public Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string? state = null)
    {
        return Task.FromResult(new TokenResponse
        {
            AccessToken = "test-access-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        });
    }
    
    public Task<UserInfo> GetUserInfoAsync(TokenResponse tokens)
    {
        return Task.FromResult(new UserInfo
        {
            UserId = $"test-user-{_providerName}",
            Email = $"test@{_providerName.ToLower()}.com",
            DisplayName = $"Test User ({_providerName})",
            IsAuthenticated = true,
            AuthProvider = _providerName
        });
    }
    
    public Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null)
    {
        // Return a test URL that doesn't require real OAuth configuration
        // This ensures the framework works even when OAuth providers aren't set up
        return Task.FromResult($"https://test.example.com/oauth/{_providerName.ToLower()}?return_url={returnUrl}");
    }
    
    public Task<EAuthResponse<UserInfo>> HandleCallbackAsync(string code, string? state = null)
    {
        // Return test result for graceful operation without real OAuth
        var response = new EAuthResponse<UserInfo>
        {
            Success = true,
            Data = new UserInfo
            {
                UserId = $"test-user-{_providerName}",
                Email = $"test@{_providerName.ToLower()}.com",
                DisplayName = $"Test User ({_providerName})",
                IsAuthenticated = true,
                AuthProvider = _providerName
            },
            Message = "Authentication successful"
        };
        return Task.FromResult(response);
    }
    
    public Task<string> GetLogoutUrlAsync(string? returnUrl = null)
    {
        return Task.FromResult($"https://test.example.com/oauth/{_providerName.ToLower()}/logout?return_url={returnUrl}");
    }
    
    public Task<string?> GetPasswordResetUrlAsync(string email)
    {
        return Task.FromResult<string?>($"https://test.example.com/oauth/{_providerName.ToLower()}/reset?email={email}");
    }
    
    public Task<bool> ValidateConfigurationAsync()
    {
        // Always valid for test scenarios
        return Task.FromResult(true);
    }
}