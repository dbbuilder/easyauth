# EasyAuth Framework Integration Guide

## 🚀 Quick Start - Correct Usage

### ⚠️ **CRITICAL: Both Parameters Required**

The most common integration error is missing the `IHostEnvironment` parameter:

```csharp
// ❌ WRONG - Will cause CorrelationIdMiddleware crash
builder.Services.AddEasyAuth(builder.Configuration);

// ✅ CORRECT - Both parameters required
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
```

### 📋 **Complete Working Examples**

#### Minimal API Setup
```csharp
using EasyAuth.Framework.Extensions;
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Required: Add both configuration AND environment
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure EasyAuth middleware
app.UseEasyAuth(builder.Configuration);

// Your API endpoints
app.MapGet("/api/health", () => "EasyAuth is working!");

app.Run();
```

#### MVC Application Setup
```csharp
using EasyAuth.Framework.Extensions;
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllers();

// Required: Add both configuration AND environment  
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure EasyAuth middleware
app.UseEasyAuth(builder.Configuration);

// Add MVC routing
app.MapControllers();

app.Run();
```

#### Blazor Server Setup
```csharp
using EasyAuth.Framework.Extensions;
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Required: Add both configuration AND environment
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure EasyAuth middleware
app.UseEasyAuth(builder.Configuration);

// Configure Blazor
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

## 🔧 **Configuration Options**

### Basic Configuration (appsettings.json)
```json
{
  "EasyAuth": {
    "ConnectionString": "Server=localhost;Database=EasyAuth;Trusted_Connection=true;",
    "Providers": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret"
      }
    },
    "Session": {
      "IdleTimeoutHours": 24,
      "CookieName": "EasyAuth.Session"
    },
    "CORS": {
      "AllowedOrigins": ["https://yourapp.com"]
    }
  }
}
```

### Advanced Configuration with Security
```csharp
var builder = WebApplication.CreateBuilder(args);

// Enhanced security configuration
builder.Services.AddEasyAuth(
    builder.Configuration, 
    builder.Environment,
    enableSecurity: true  // Enables comprehensive security middleware
);

var app = builder.Build();

// Full security middleware pipeline
app.UseEasyAuth(enableSecurity: true);

app.Run();
```

## 🛡️ **Security Features (v2.3.1+)**

### Automatic Security Hardening
```csharp
// Includes all security features by default
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseEasyAuth(builder.Configuration); // Automatically includes:
// - Input validation middleware
// - Rate limiting and DDoS protection  
// - CSRF token validation
// - Security headers
// - Audit logging
```

### Custom Security Configuration
```csharp
builder.Services.AddEasyAuthSecurity(
    inputValidation: options => {
        options.MaxRequestSizeBytes = 5 * 1024 * 1024; // 5MB
        options.EnablePatternDetection = true;
    },
    rateLimit: options => {
        options.GlobalRequestsPerMinute = 100;
        options.EnableProgressivePenalties = true;
    },
    csrf: options => {
        options.Enabled = true;
        options.RequireHttps = true;
    }
);
```

## 🌐 **Zero-Configuration CORS**

EasyAuth automatically detects common development ports:

```csharp
// Development: Auto-detects React (3000), Vue (8080), Angular (4200), etc.
// Production: Uses configured origins only

// Override auto-detection if needed:
"EasyAuth": {
  "CORS": {
    "AllowedOrigins": ["https://myapp.com", "https://admin.myapp.com"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowCredentials": true
  }
}
```

## 📊 **Health Checks & Monitoring**

```csharp
// Enable health checks
"EasyAuth": {
  "Framework": {
    "EnableHealthChecks": true,
    "EnableSwagger": true
  }
}

// Access health endpoint
// GET /health
```

## 🔌 **Provider Configuration**

### Google OAuth
```json
{
  "EasyAuth": {
    "Providers": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id.googleusercontent.com",
        "ClientSecret": "your-google-client-secret",
        "CallbackPath": "/signin-google",
        "Scopes": ["openid", "profile", "email"]
      }
    }
  }
}
```

### Facebook Login
```json
{
  "EasyAuth": {
    "Providers": {
      "Facebook": {
        "Enabled": true,
        "AppId": "your-facebook-app-id",
        "AppSecret": "your-facebook-app-secret",
        "CallbackPath": "/signin-facebook",
        "Scopes": ["email", "public_profile"]
      }
    }
  }
}
```

### Apple Sign-In
```json
{
  "EasyAuth": {
    "Providers": {
      "Apple": {
        "Enabled": true,
        "ClientId": "your.apple.service.id",
        "KeyId": "your-apple-key-id",
        "TeamId": "your-apple-team-id",
        "PrivateKey": "-----BEGIN PRIVATE KEY-----..."
      }
    }
  }
}
```

## ❌ **Common Errors & Solutions**

### 1. CorrelationIdMiddleware Registration Error
```
Error: Unable to resolve service for type 'Microsoft.AspNetCore.Http.RequestDelegate'
```

**Solution**: Include both required parameters
```csharp
// ❌ Wrong
builder.Services.AddEasyAuth(builder.Configuration);

// ✅ Correct  
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
```

### 2. CORS Issues in Development
```
Error: Access to fetch has been blocked by CORS policy
```

**Solution**: EasyAuth auto-detects development ports, but verify your client port:
```json
{
  "EasyAuth": {
    "CORS": {
      "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
    }
  }
}
```

### 3. Database Connection Issues
```
Error: Cannot open database
```

**Solution**: Verify connection string format:
```json
{
  "EasyAuth": {
    "ConnectionString": "Server=localhost;Database=EasyAuth;Trusted_Connection=true;"
  }
}
```

## 🧪 **Testing Your Integration**

### Quick Verification
```bash
# 1. Build your project
dotnet build

# 2. Run your project  
dotnet run

# 3. Test health endpoint
curl http://localhost:5000/health

# 4. Test Swagger UI (in development)
# Navigate to: http://localhost:5000/swagger
```

### Integration Test Example
```csharp
[Test]
public async Task EasyAuth_Integration_ShouldWork()
{
    // Arrange
    var builder = WebApplication.CreateBuilder();
    builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
    
    var app = builder.Build();
    app.UseEasyAuth(builder.Configuration);
    
    // Act & Assert - Should not throw
    await app.StartAsync();
    await app.StopAsync();
}
```

## 📞 **Support & Resources**

- **Documentation**: [EasyAuth Docs](https://github.com/dbbuilder/easyauth)
- **Issues**: [GitHub Issues](https://github.com/dbbuilder/easyauth/issues)  
- **NuGet**: [EasyAuth.Framework](https://www.nuget.org/packages/EasyAuth.Framework)
- **Examples**: Check the `/examples` directory in the repository

---

## ✅ **Integration Checklist**

- [ ] Include both `builder.Configuration` AND `builder.Environment` in `AddEasyAuth()`
- [ ] Add `app.UseEasyAuth(builder.Configuration)` to middleware pipeline
- [ ] Configure connection string in appsettings.json
- [ ] Enable desired authentication providers
- [ ] Test health endpoint: `/health`
- [ ] Verify CORS settings for your client applications
- [ ] Review security settings for production deployment

**Remember**: Both parameters are required for `AddEasyAuth()` - this prevents 99% of integration issues!