# EasyAuth Framework Standalone Requirements

## Goal: Eliminate NEasyAuthMiddleware Dependency

To make EasyAuth Framework completely standalone and eliminate the need for NEasyAuthMiddleware, the following changes must be made to the EasyAuth Framework source code:

## Core Framework Changes Required

### 1. **Fix CorrelationIdMiddleware Registration (CRITICAL)**

**File**: `EasyAuth.Framework.Core/Extensions/ServiceCollectionExtensions.cs`

**Problem**:
```csharp
// BROKEN - Current implementation
services.AddSingleton<CorrelationIdMiddleware>();
```

**Solution**:
```csharp
// FIXED - Remove from service collection entirely
// Don't register middleware in DI container

// OR if needed, register properly:
services.AddTransient<CorrelationIdMiddleware>();
```

**Root Cause**: Middleware should NOT be registered in the DI container as services. They should be added to the pipeline using `app.UseMiddleware<T>()`.

### 2. **Implement Proper Middleware Registration Pattern**

**File**: `EasyAuth.Framework/Extensions/ApplicationBuilderExtensions.cs`

**Current Broken Pattern**:
```csharp
public static IApplicationBuilder UseEasyAuth(this IApplicationBuilder app, IConfiguration configuration)
{
    // This tries to resolve middleware from DI - WRONG
    app.UseMiddleware<CorrelationIdMiddleware>();
    return app;
}
```

**Correct Pattern**:
```csharp
public static IApplicationBuilder UseEasyAuth(this IApplicationBuilder app, IConfiguration configuration)
{
    // Method 1: Direct instantiation (simplest)
    app.Use(async (context, next) =>
    {
        // Correlation ID logic here directly
        context.Items["CorrelationId"] = Guid.NewGuid().ToString();
        await next();
    });
    
    // Method 2: Factory pattern
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // Method 3: Proper middleware with dependencies
    app.UseMiddleware<EasyAuthMiddleware>();
    
    return app;
}
```

### 3. **Create Standalone Authentication Scheme**

**File**: `EasyAuth.Framework.Core/Authentication/EasyAuthAuthenticationHandler.cs`

**Required Implementation**:
```csharp
public class EasyAuthAuthenticationHandler : AuthenticationHandler<EasyAuthSchemeOptions>
{
    public EasyAuthAuthenticationHandler(IOptionsMonitor<EasyAuthSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Handle X-MS-CLIENT-PRINCIPAL header (Azure Easy Auth format)
        var principal = Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(principal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Decode and validate the principal
            var claimsPrincipal = DecodeEasyAuthPrincipal(principal);
            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to authenticate EasyAuth principal");
            return Task.FromResult(AuthenticateResult.Fail("Invalid EasyAuth principal"));
        }
    }

    private ClaimsPrincipal DecodeEasyAuthPrincipal(string encodedPrincipal)
    {
        // Implementation to decode the Azure Easy Auth principal
        // This is the core logic that NEasyAuthMiddleware provides
        var bytes = Convert.FromBase64String(encodedPrincipal);
        var json = Encoding.UTF8.GetString(bytes);
        var principalData = JsonSerializer.Deserialize<EasyAuthPrincipal>(json);
        
        var claims = new List<Claim>();
        // Convert principalData to claims
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return new ClaimsPrincipal(identity);
    }
}
```

### 4. **Add Complete Service Registration**

**File**: `EasyAuth.Framework.Core/Extensions/ServiceCollectionExtensions.cs`

**Complete Implementation**:
```csharp
public static IServiceCollection AddEasyAuth(this IServiceCollection services, IConfiguration configuration)
{
    // Configure options
    services.Configure<EasyAuthOptions>(configuration.GetSection("EasyAuth"));
    
    // Add authentication scheme
    services.AddAuthentication("EasyAuth")
        .AddScheme<EasyAuthSchemeOptions, EasyAuthAuthenticationHandler>("EasyAuth", options => { });
    
    // Add CORS with auto-detection
    AddEasyAuthCors(services, configuration);
    
    // Add core services
    services.AddScoped<IEasyAuthService, EasyAuthService>();
    services.AddScoped<IOAuthProviderService, OAuthProviderService>();
    services.AddScoped<ITokenService, TokenService>();
    
    // DO NOT register middleware as services
    // services.AddSingleton<CorrelationIdMiddleware>(); // REMOVE THIS
    
    return services;
}

private static void AddEasyAuthCors(IServiceCollection services, IConfiguration configuration)
{
    var corsOrigins = new List<string>();
    
    // Add configured origins
    var configuredOrigins = configuration.GetSection("EasyAuth:CORS:AllowedOrigins").Get<string[]>();
    if (configuredOrigins != null) corsOrigins.AddRange(configuredOrigins);
    
    // Auto-detect development origins
    var environment = configuration["ASPNETCORE_ENVIRONMENT"];
    if (environment == "Development")
    {
        var devPorts = new[] { "3000", "5173", "8080", "4200", "3001", "8000", "5000", "5001" };
        foreach (var port in devPorts)
        {
            corsOrigins.Add($"http://localhost:{port}");
            corsOrigins.Add($"https://localhost:{port}");
        }
    }
    
    services.AddCors(options =>
    {
        options.AddPolicy("EasyAuthPolicy", policy =>
        {
            policy.WithOrigins(corsOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}
```

### 5. **Implement OAuth Provider Endpoints**

**File**: `EasyAuth.Framework/Controllers/AuthController.cs`

**Required Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IOAuthProviderService _providerService;
    private readonly IEasyAuthService _authService;

    public AuthController(IOAuthProviderService providerService, IEasyAuthService authService)
    {
        _providerService = providerService;
        _authService = authService;
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _providerService.GetAvailableProvidersAsync();
        return Ok(providers);
    }

    [HttpPost("login")]
    public async Task<IActionResult> InitiateLogin([FromBody] LoginRequest request)
    {
        var authUrl = await _authService.GetAuthUrlAsync(request.Provider, request.ReturnUrl);
        return Ok(new { authUrl, success = true });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> HandleCallback([FromBody] CallbackRequest request)
    {
        var result = await _authService.HandleCallbackAsync(request);
        return Ok(result);
    }
}
```

### 6. **Create Minimal Middleware Implementation**

**File**: `EasyAuth.Framework.Core/Middleware/EasyAuthMiddleware.cs`

**Lightweight Implementation**:
```csharp
public class EasyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EasyAuthMiddleware> _logger;

    public EasyAuthMiddleware(RequestDelegate next, ILogger<EasyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add correlation ID
        if (!context.Items.ContainsKey("CorrelationId"))
        {
            context.Items["CorrelationId"] = Guid.NewGuid().ToString();
        }

        // Add any other EasyAuth-specific context
        context.Items["EasyAuth.Timestamp"] = DateTimeOffset.UtcNow;

        await _next(context);
    }
}
```

## Files That Need to be Created/Modified

### **New Files Needed**:
1. `EasyAuth.Framework.Core/Authentication/EasyAuthAuthenticationHandler.cs`
2. `EasyAuth.Framework.Core/Authentication/EasyAuthSchemeOptions.cs`
3. `EasyAuth.Framework.Core/Models/EasyAuthPrincipal.cs`
4. `EasyAuth.Framework.Core/Services/IEasyAuthService.cs`
5. `EasyAuth.Framework.Core/Services/EasyAuthService.cs`
6. `EasyAuth.Framework/Controllers/AuthController.cs`
7. `EasyAuth.Framework.Core/Middleware/EasyAuthMiddleware.cs`

### **Files to Modify**:
1. `EasyAuth.Framework.Core/Extensions/ServiceCollectionExtensions.cs` - Remove problematic middleware registration
2. `EasyAuth.Framework/Extensions/ApplicationBuilderExtensions.cs` - Fix middleware pipeline
3. `EasyAuth.Framework.Core/Configuration/EasyAuthOptions.cs` - Add CORS configuration

## Dependencies to Remove

### **From EasyAuth Framework**:
- Remove dependency on NEasyAuthMiddleware package
- Remove problematic CorrelationIdMiddleware singleton registration
- Remove any Azure Functions-specific dependencies

## Key Principles for Standalone Implementation

### 1. **Proper Middleware Pattern**
- Never register middleware in DI container as services
- Use `app.UseMiddleware<T>()` or `app.Use()` for pipeline registration
- Middleware should have `RequestDelegate next` parameter

### 2. **Authentication Scheme Implementation**
- Implement `AuthenticationHandler<T>` properly
- Handle Azure Easy Auth `X-MS-CLIENT-PRINCIPAL` header format
- Provide fallback for non-Azure scenarios

### 3. **Auto-Configuration**
- Detect development environment automatically
- Auto-configure CORS for common dev server ports
- Provide sensible defaults while allowing overrides

### 4. **Service Architecture**
- Keep services and middleware separate
- Use proper dependency injection patterns
- Implement interfaces for testability

## Testing Requirements

### **Unit Tests Needed**:
```csharp
[Test]
public void AddEasyAuth_Should_Not_Register_Middleware_As_Service()
{
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder().Build();
    
    services.AddEasyAuth(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    
    // This should NOT work (middleware shouldn't be in DI)
    Assert.Throws<InvalidOperationException>(() => 
        serviceProvider.GetService<CorrelationIdMiddleware>());
}

[Test]
public void EasyAuthAuthenticationHandler_Should_Authenticate_Valid_Principal()
{
    // Test authentication handler with valid Azure Easy Auth principal
}

[Test]
public void UseEasyAuth_Should_Add_Middleware_To_Pipeline()
{
    // Test that middleware is properly added to pipeline
}
```

## Migration Path

### **For Existing Projects Using NEasyAuthMiddleware**:
1. Remove NEasyAuthMiddleware package reference
2. Update to new EasyAuth Framework version
3. Remove manual CORS configuration (now auto-detected)
4. Update authentication scheme name if needed

### **Breaking Changes**:
- Authentication scheme name may change
- Some configuration options may be renamed
- Middleware ordering requirements may change

## Success Criteria

1. ✅ **No NEasyAuthMiddleware dependency**
2. ✅ **Framework starts without errors**
3. ✅ **OAuth providers endpoint works**
4. ✅ **CORS auto-detection works**
5. ✅ **Authentication flows work end-to-end**
6. ✅ **Zero manual configuration in development**
7. ✅ **Backwards compatibility with existing configs**

## Implementation Priority

### **Phase 1 (Critical)**:
1. Fix CorrelationIdMiddleware registration issue
2. Implement basic authentication handler
3. Add OAuth provider endpoints

### **Phase 2 (Important)**:
1. Add auto-CORS configuration
2. Implement proper middleware pipeline
3. Add comprehensive error handling

### **Phase 3 (Enhancement)**:
1. Add advanced auto-detection features
2. Implement production security features
3. Add comprehensive documentation

This standalone implementation will eliminate the need for NEasyAuthMiddleware entirely while providing a superior developer experience with auto-configuration and proper .NET authentication patterns.