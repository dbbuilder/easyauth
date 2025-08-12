# EasyAuth Framework - Troubleshooting Guide

This guide helps resolve common issues when using the EasyAuth Framework v2.2.0 and later.

## 🚨 Common Issues

### Extension Methods Not Found (Most Common Issue)

**Problem:** `AddEasyAuth`, `UseEasyAuth`, `AddEasyAuthSwagger` or other extension methods are not recognized.

**Check Your Version First:**
```bash
dotnet list package | grep EasyAuth
```

**If you see version 2.1.0 or earlier:**
```bash
# ✅ Update to v2.2.0 to get new extension methods
dotnet add package EasyAuth.Framework --version 2.2.0
dotnet add package EasyAuth.Framework.Core --version 2.2.0
```

**If you're already on v2.2.0 but methods still not found:**
```csharp
// ✅ Add this using statement at the top of Program.cs
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Now these methods should be available
builder.Services.AddEasyAuth(builder.Configuration);
builder.Services.AddEasyAuthSwagger();

var app = builder.Build();
app.UseEasyAuth();
```

**Root Causes:**
- Missing `using EasyAuth.Framework.Core.Extensions;` statement
- Package not properly installed or restored
- Using an older version that doesn't have the extensions

---

### Package Not Found During Installation

**Problem:** `dotnet add package EasyAuth.Framework` fails or package doesn't exist.

**Solution:**
```bash
# ✅ Use the correct package names
dotnet add package EasyAuth.Framework.Core --version 2.2.0
# OR
dotnet add package EasyAuth.Framework --version 2.2.0

# Clear NuGet cache if needed
dotnet nuget locals all --clear
dotnet restore
```

**Verification:**
```bash
# Check installed packages
dotnet list package | grep EasyAuth

# Should show:
# > EasyAuth.Framework.Core    2.2.0
```

---

### Compilation Errors After Installation

**Problem:** Build fails with missing references or compilation errors.

**Solution:**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build

# If using Visual Studio, also try:
# - Restart Visual Studio
# - Clear NuGet cache: Tools > NuGet Package Manager > Package Manager Settings > Clear All NuGet Caches
```

---

### CORS Issues with Frontend

**Problem:** Frontend applications get CORS errors when calling EasyAuth endpoints.

**Solution 1 - Automatic CORS (Recommended):**
```csharp
// Program.cs - EasyAuth includes enhanced CORS automatically
builder.Services.AddEasyAuth(builder.Configuration);

var app = builder.Build();
app.UseEasyAuth(); // This includes CORS middleware automatically
```

**Solution 2 - Manual CORS Configuration:**
```csharp
// If you need custom CORS settings
builder.Services.AddEasyAuthCors(options => {
    options.AllowedOrigins.Add("https://localhost:3000"); // React
    options.AllowedOrigins.Add("https://localhost:8080"); // Vue
    options.AllowedOrigins.Add("https://localhost:4200"); // Angular
});

var app = builder.Build();
app.UseEasyAuthCors(); // Use before routing
app.UseRouting();
```

**Solution 3 - Framework-Specific CORS:**
```csharp
// Quick setup for specific frameworks
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.React);
// OR
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.Vue);
// OR
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.Angular);
```

---

### Configuration Not Loading

**Problem:** EasyAuth settings from `appsettings.json` are not being loaded.

**Solution:**

1. **Check Configuration Structure:**
```json
{
  "EasyAuth": {
    "Providers": {
      "Google": {
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret",
        "Enabled": true
      },
      "Facebook": {
        "ClientId": "your-facebook-app-id",
        "ClientSecret": "your-facebook-app-secret",
        "Enabled": true
      }
    },
    "Database": {
      "ConnectionString": "Server=localhost;Database=EasyAuth;Trusted_Connection=true;",
      "Provider": "SqlServer"
    }
  }
}
```

2. **Verify Configuration Binding:**
```csharp
// ✅ Correct: Pass configuration to AddEasyAuth
builder.Services.AddEasyAuth(builder.Configuration);

// ❌ Incorrect: Don't forget configuration parameter
builder.Services.AddEasyAuth(); // This won't load appsettings.json
```

---

### Swagger Documentation Not Appearing

**Problem:** `/swagger` endpoint returns 404 or documentation doesn't show.

**Solution:**
```csharp
// Program.cs
builder.Services.AddEasyAuth(builder.Configuration);
builder.Services.AddEasyAuthSwagger(); // ✅ Add this line

var app = builder.Build();

// ✅ Make sure you're in Development environment
if (app.Environment.IsDevelopment())
{
    app.UseEasyAuth(); // Automatically includes Swagger in development
}
else
{
    app.UseEasyAuth();
}
```

**Verification:**
- Navigate to `https://localhost:XXXX/swagger`
- Should see EasyAuth API documentation with custom styling

---

### Database Connection Issues

**Problem:** Database-related errors or connection failures.

**Solution:**

1. **Check Connection String:**
```json
{
  "EasyAuth": {
    "Database": {
      "ConnectionString": "Server=localhost;Database=EasyAuthDb;Trusted_Connection=true;",
      "Provider": "SqlServer"
    }
  }
}
```

2. **Verify Database Provider:**
```csharp
// For SQL Server (default)
builder.Services.AddEasyAuth(builder.Configuration);

// For other providers, ensure correct packages are installed
```

3. **Test Connection:**
```csharp
// Add logging to see detailed errors
builder.Services.AddLogging(config => {
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});
```

---

### Authentication Providers Not Working

**Problem:** OAuth login fails or redirects incorrectly.

**Solution:**

1. **Verify Provider Configuration:**
```json
{
  "EasyAuth": {
    "Providers": {
      "Google": {
        "ClientId": "123456789-abcdef.apps.googleusercontent.com",
        "ClientSecret": "your-secret-here",
        "Enabled": true,
        "Scope": "openid profile email"
      }
    }
  }
}
```

2. **Check Redirect URIs:**
   - Google Console: `https://localhost:XXXX/api/auth/callback/google`
   - Facebook Developer: `https://localhost:XXXX/api/auth/callback/facebook`
   - Apple Developer: `https://localhost:XXXX/api/auth/callback/apple`

3. **Test Configuration:**
```csharp
// Enable detailed logging
builder.Services.AddLogging(config => {
    config.AddConsole();
    config.AddFilter("EasyAuth", LogLevel.Debug);
});
```

---

## 📋 Version Compatibility

### EasyAuth Framework v2.2.0+ (Latest)
- ✅ `AddEasyAuth()`, `UseEasyAuth()` extension methods
- ✅ `AddEasyAuthSwagger()` for enhanced documentation  
- ✅ Enhanced CORS with 40+ framework defaults
- ✅ Dual targeting (.NET 8.0 and .NET 9.0)

### EasyAuth Framework v2.1.0 and Earlier
- ❌ No `UseEasyAuth()` extension method
- ❌ No `AddEasyAuthSwagger()` method
- ⚠️ Limited CORS support
- ⚠️ Manual configuration required

**Migration from v2.1.0 to v2.2.0:**
```bash
# Update packages
dotnet add package EasyAuth.Framework --version 2.2.0
dotnet add package EasyAuth.Framework.Core --version 2.2.0

# Clean and restore
dotnet clean
dotnet restore
```

```csharp
// Before (v2.1.0)
builder.Services.AddEasyAuth(); // Basic setup
app.UseAuthentication();
app.UseAuthorization();

// After (v2.2.0)
using EasyAuth.Framework.Core.Extensions;

builder.Services.AddEasyAuth(builder.Configuration);
builder.Services.AddEasyAuthSwagger(); // Optional
app.UseEasyAuth(); // Replaces UseAuthentication/UseAuthorization
```

---

## 🔧 Quick Diagnostic Commands

### Check Package Installation
```bash
dotnet list package | grep EasyAuth
```

### Verify Configuration
```bash
# Check if appsettings.json is being copied
ls bin/Debug/net8.0/appsettings*.json

# Check configuration loading (add temporary logging)
```

### Test CORS
```bash
# Test from browser console (replace with your port)
fetch('https://localhost:7001/api/auth/providers')
  .then(r => r.json())
  .then(console.log)
  .catch(console.error);
```

### Clear All Caches
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Clean and restore
dotnet clean
dotnet restore

# Restart IDE after this
```

---

## 🔍 Advanced Troubleshooting

### Enable Debug Logging
```csharp
// Program.cs - Add detailed logging
builder.Services.AddLogging(config => {
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Debug);
    
    // EasyAuth specific logging
    config.AddFilter("EasyAuth", LogLevel.Trace);
    config.AddFilter("EasyAuth.Framework.Core", LogLevel.Debug);
});
```

### Verify Extension Methods at Runtime
```csharp
// Add this temporary code to check if extensions are loaded
var serviceCollection = new ServiceCollection();
var hasAddEasyAuth = typeof(ServiceCollectionExtensions)
    .GetMethods()
    .Any(m => m.Name == "AddEasyAuth");
    
Console.WriteLine($"AddEasyAuth method available: {hasAddEasyAuth}");
```

### Check Package Dependencies
```bash
# List all dependencies
dotnet list package --include-transitive | grep -E "(EasyAuth|Microsoft|Swashbuckle)"
```

---

## 📞 Getting Help

### Before Opening an Issue

1. **Check this troubleshooting guide** ✅
2. **Verify you're using v2.2.0 or later** ✅  
3. **Try the quick diagnostic commands** ✅
4. **Enable debug logging** ✅

### Create a Minimal Reproduction

```csharp
// Minimal Program.cs for testing
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEasyAuth(builder.Configuration);
builder.Services.AddEasyAuthSwagger();

var app = builder.Build();

app.UseEasyAuth();
app.MapControllers();

app.Run();
```

### Include in Bug Reports

- EasyAuth Framework version
- .NET version (.NET 8.0 or .NET 9.0)
- Complete error message with stack trace
- Relevant configuration (remove secrets)
- Steps to reproduce

### Community Support

- **GitHub Issues**: https://github.com/dbbuilder/easyauth/issues
- **Discussions**: https://github.com/dbbuilder/easyauth/discussions
- **Documentation**: Check `/swagger` endpoint for API docs

---

## 🎯 Quick Reference

### Essential Using Statements
```csharp
using EasyAuth.Framework.Core.Extensions;  // Required for all extension methods
```

### Minimal Setup
```csharp
// Services
builder.Services.AddEasyAuth(builder.Configuration);

// Middleware
app.UseEasyAuth();
```

### Full Setup with All Features
```csharp
// Services
builder.Services.AddEasyAuth(builder.Configuration);
builder.Services.AddEasyAuthSwagger();
builder.Services.AddEasyAuthCors();

// Middleware  
app.UseEasyAuth(); // Includes CORS and Swagger automatically
```

---

*This guide covers EasyAuth Framework v2.2.0 and later. For older versions, please refer to the specific version documentation.*