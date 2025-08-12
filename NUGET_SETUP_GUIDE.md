# EasyAuth Framework - NuGet Setup Guide

Quick setup guide for integrating EasyAuth Framework via NuGet packages in your .NET applications.

## 📦 Package Installation

### Option 1: Complete Framework (Recommended)
```bash
# Install the main package with all features
dotnet add package EasyAuth.Framework
```

### Option 2: Core Only
```bash
# Install core package only (minimal dependencies)
dotnet add package EasyAuth.Framework.Core
```

## 🚀 Quick Setup

### 1. Add EasyAuth to your ASP.NET Core application

```csharp
// Program.cs or Startup.cs
using EasyAuth.Framework.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add EasyAuth services
builder.Services.AddEasyAuth(builder.Configuration, options =>
{
    // Configure providers
    options.Providers.Google.ClientId = "your-google-client-id";
    options.Providers.Google.ClientSecret = "your-google-client-secret";
    options.Providers.Google.Enabled = true;
    
    options.Providers.Facebook.ClientId = "your-facebook-app-id";
    options.Providers.Facebook.AppSecret = "your-facebook-app-secret";
    options.Providers.Facebook.Enabled = true;
    
    // Database configuration
    options.Database.ConnectionString = "your-connection-string";
    options.Database.AutoMigrate = true;
    
    // Security settings
    options.Security.RequireHttps = true;
    options.Security.JwtSecret = "your-jwt-secret";
});

var app = builder.Build();

// Use EasyAuth middleware
app.UseEasyAuth();

app.Run();
```

### 2. Add authentication endpoints

```csharp
// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using EasyAuth.Framework.Core.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IEAuthService _authService;

    public AuthController(IEAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("login/{provider}")]
    public async Task<IActionResult> Login(string provider, [FromQuery] string? returnUrl = null)
    {
        var result = await _authService.InitiateAuthenticationAsync(provider, returnUrl);
        if (result.Success)
        {
            return Redirect(result.AuthUrl);
        }
        return BadRequest(result.Error);
    }

    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string state)
    {
        var result = await _authService.HandleCallbackAsync(provider, code, state);
        if (result.Success)
        {
            // Set authentication cookie or return token
            return Ok(result);
        }
        return BadRequest(result.Error);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync(User);
        return Ok();
    }
}
```

### 3. Configure authentication in appsettings.json

```json
{
  "EasyAuth": {
    "Database": {
      "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=EasyAuthDb;Trusted_Connection=true;",
      "AutoMigrate": true
    },
    "Providers": {
      "Google": {
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret",
        "Enabled": true
      },
      "Facebook": {
        "ClientId": "your-facebook-app-id",
        "AppSecret": "your-facebook-app-secret",
        "Enabled": true
      },
      "Apple": {
        "ClientId": "your-apple-client-id",
        "TeamId": "your-apple-team-id",
        "KeyId": "your-apple-key-id",
        "Enabled": false
      }
    },
    "Security": {
      "JwtSecret": "your-256-bit-secret-key",
      "RequireHttps": true,
      "TokenExpirationMinutes": 60
    }
  }
}
```

## 🔒 Secure Configuration (Production)

### Using Azure Key Vault

```csharp
// Program.cs
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault for secure configuration
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential()
    );
}

// EasyAuth will automatically use Key Vault secrets
builder.Services.AddEasyAuth(builder.Configuration);
```

### Environment Variables

```bash
# Set environment variables for secure configuration
export EASYAUTH__PROVIDERS__GOOGLE__CLIENTSECRET="your-secure-secret"
export EASYAUTH__PROVIDERS__FACEBOOK__APPSECRET="your-secure-secret"
export EASYAUTH__SECURITY__JWTSECRET="your-256-bit-secret"
```

## 🎯 Framework-Specific Integration

### Minimal APIs

```csharp
// Program.cs
var app = builder.Build();

app.MapGet("/auth/login/{provider}", async (string provider, IEAuthService authService, string? returnUrl = null) =>
{
    var result = await authService.InitiateAuthenticationAsync(provider, returnUrl);
    return result.Success ? Results.Redirect(result.AuthUrl) : Results.BadRequest(result.Error);
});

app.MapGet("/auth/callback/{provider}", async (string provider, string code, string state, IEAuthService authService) =>
{
    var result = await authService.HandleCallbackAsync(provider, code, state);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result.Error);
});
```

### MVC with Views

```csharp
// Views/Account/Login.cshtml
<div class="login-providers">
    <a href="/auth/login/google" class="btn btn-google">
        Sign in with Google
    </a>
    <a href="/auth/login/facebook" class="btn btn-facebook">
        Sign in with Facebook
    </a>
</div>
```

### Blazor Server

```csharp
// Pages/Login.razor
@page "/login"
@inject IEAuthService AuthService
@inject NavigationManager Navigation

<div class="login-container">
    <button class="btn btn-primary" @onclick="() => LoginWithProvider(\"google\")">
        Sign in with Google
    </button>
    <button class="btn btn-primary" @onclick="() => LoginWithProvider(\"facebook\")">
        Sign in with Facebook
    </button>
</div>

@code {
    private async Task LoginWithProvider(string provider)
    {
        var result = await AuthService.InitiateAuthenticationAsync(provider);
        if (result.Success)
        {
            Navigation.NavigateTo(result.AuthUrl, forceLoad: true);
        }
    }
}
```

## 🗄️ Database Setup

EasyAuth automatically creates and manages database tables. Supported databases:

- **SQL Server** (default)
- **Azure SQL Database**
- **SQL Server LocalDB**

### Custom Database Configuration

```csharp
builder.Services.AddEasyAuth(builder.Configuration, options =>
{
    options.Database.ConnectionString = "your-connection-string";
    options.Database.AutoMigrate = true;
    options.Database.CommandTimeout = 30;
    options.Database.RetryPolicy = true;
});
```

## 🔧 Advanced Configuration

### Custom User Claims

```csharp
builder.Services.AddEasyAuth(builder.Configuration, options =>
{
    options.UserMapping.EmailClaimType = "email";
    options.UserMapping.NameClaimType = "name";
    options.UserMapping.AdditionalClaims = new[]
    {
        "picture",
        "locale",
        "timezone"
    };
});
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddEasyAuthHealthChecks();

app.MapHealthChecks("/health");
```

### Logging and Monitoring

```csharp
builder.Services.AddEasyAuth(builder.Configuration, options =>
{
    options.Logging.LogLevel = LogLevel.Information;
    options.Logging.IncludeUserDetails = false; // GDPR compliance
    options.Monitoring.EnableMetrics = true;
    options.Monitoring.ApplicationInsightsKey = "your-app-insights-key";
});
```

## 🚀 Deployment Checklist

### Production Requirements

- [ ] Configure secure secrets (Key Vault/Environment Variables)
- [ ] Set up SSL/TLS certificates
- [ ] Configure OAuth provider redirect URIs
- [ ] Set up database with proper permissions
- [ ] Configure logging and monitoring
- [ ] Test authentication flows end-to-end
- [ ] Configure CORS policies
- [ ] Set up health checks and monitoring

### OAuth Provider Setup

1. **Google**: [Google Cloud Console](https://console.cloud.google.com)
2. **Facebook**: [Facebook Developers](https://developers.facebook.com)
3. **Apple**: [Apple Developer Portal](https://developer.apple.com)
4. **Azure B2C**: [Azure Portal](https://portal.azure.com)

## 📚 Additional Resources

- [Full Documentation](https://github.com/dbbuilder/easyauth#readme)
- [API Reference](https://github.com/dbbuilder/easyauth/wiki/api-reference)
- [Configuration Guide](https://github.com/dbbuilder/easyauth/wiki/configuration)
- [Security Best Practices](https://github.com/dbbuilder/easyauth/wiki/security)
- [Troubleshooting Guide](https://github.com/dbbuilder/easyauth/wiki/troubleshooting)

## 🐛 Support

- **Issues**: [GitHub Issues](https://github.com/dbbuilder/easyauth/issues)
- **Discussions**: [GitHub Discussions](https://github.com/dbbuilder/easyauth/discussions)
- **Security**: security@easyauth.dev

---

**EasyAuth Framework** - Enterprise-grade authentication made simple.