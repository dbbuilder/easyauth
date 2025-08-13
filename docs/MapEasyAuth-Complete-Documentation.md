# EasyAuth MapEasyAuth() Method - Complete Documentation

## 🔥 **Critical Implementation Requirements**

The `MapEasyAuth()` method is **essential** for EasyAuth Framework functionality. Without this call, **OAuth endpoints will NOT appear in Swagger and authentication will NOT work**.

## 📋 **Method Signature**

```csharp
public static WebApplication MapEasyAuth(
    this WebApplication app, 
    Action<IEndpointRouteBuilder>? configureEndpoints = null)
```

## ✅ **What MapEasyAuth() Does**

1. **Maps ALL EasyAuth Controllers** - Activates all OAuth endpoints under `/api/EAuth/`
2. **Registers Health Check Endpoints** - If enabled in configuration
3. **Provides Swagger Visibility** - Ensures endpoints appear in OpenAPI documentation
4. **Logs Available Endpoints** - Developer-friendly startup logging
5. **Validates Configuration** - Ensures proper routing setup

## 🎯 **Complete Integration Example**

### **Program.cs Setup (COMPLETE)**

```csharp
using EasyAuth.Framework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Add EasyAuth services FIRST
builder.Services.AddEasyAuth(builder.Configuration);

// 2. Add controllers for MVC/API support  
builder.Services.AddControllers();

// 3. Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations(); // For SwaggerOperation attributes
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Your App with EasyAuth", 
        Version = "v1" 
    });
});

var app = builder.Build();

// 4. Add EasyAuth middleware BEFORE routing
app.UseEasyAuth();

// 5. Add Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. Add routing middleware
app.UseRouting();

// 7. CRITICAL: Map EasyAuth endpoints - THIS IS REQUIRED
app.MapEasyAuth();

// 8. Map your own controllers/endpoints (optional)
app.MapControllers();

app.Run();
```

## 🔗 **Swagger Endpoints Available After MapEasyAuth()**

After calling `app.MapEasyAuth()`, Swagger will display:

### **Authentication Endpoints**
- `GET /api/EAuth/providers` - Get available authentication providers
- `POST /api/EAuth/login` - Initiate OAuth login with provider  
- `GET /api/EAuth/callback/{provider}` - Handle OAuth callback from provider
- `GET /api/EAuth/user` - Get current authenticated user info
- `POST /api/EAuth/logout` - Logout current user session
- `GET /api/EAuth/validate` - Validate current session
- `POST /api/EAuth/link/{provider}` - Link account from another provider
- `DELETE /api/EAuth/unlink/{provider}` - Unlink account from provider
- `POST /api/EAuth/reset-password` - Initiate password reset

### **Health Endpoints** (if enabled)
- `GET /health` - EasyAuth health check endpoint

## ⚡ **Custom Endpoint Configuration**

You can provide custom endpoint configuration:

```csharp
app.MapEasyAuth(endpoints =>
{
    // Add custom endpoints alongside EasyAuth
    endpoints.MapGet("/custom", () => "Custom endpoint");
    endpoints.MapPost("/webhook", async (HttpRequest request) => 
    {
        // Handle webhook
        return Results.Ok();
    });
});
```

## 🔧 **Troubleshooting**

### **Problem: Endpoints Don't Appear in Swagger**

**Solution Checklist:**
1. ✅ Called `app.MapEasyAuth()` after `app.UseRouting()`
2. ✅ Added `builder.Services.AddControllers()` 
3. ✅ Added `builder.Services.AddEndpointsApiExplorer()`
4. ✅ Added `options.EnableAnnotations()` to SwaggerGen
5. ✅ Called `app.UseEasyAuth()` before `app.MapEasyAuth()`

### **Problem: 404 Errors on EasyAuth Endpoints**

**Check:**
1. Ensure `app.MapEasyAuth()` is called
2. Verify routing middleware is added with `app.UseRouting()`
3. Check that EasyAuth services are registered with `builder.Services.AddEasyAuth()`

### **Problem: Build Errors**

**Common Issues:**
```csharp
// ❌ WRONG - Missing using directive
app.MapEasyAuth(); // CS0246 error

// ✅ CORRECT - Add using directive
using EasyAuth.Framework.Core.Extensions;
app.MapEasyAuth();
```

## 🔍 **Startup Logging Output**

When `MapEasyAuth()` is called, you'll see startup logs:

```
info: EasyAuth.Extensions[0]
      🔗 EasyAuth OAuth endpoints mapped:
info: EasyAuth.Extensions[0]
      ✅ /api/EAuth/providers - Get available authentication providers
info: EasyAuth.Extensions[0]
      ✅ /api/EAuth/login - Initiate OAuth login with provider
info: EasyAuth.Extensions[0]
      ✅ /api/EAuth/callback/{provider} - Handle OAuth callback from provider
info: EasyAuth.Extensions[0]
      ✅ /api/EAuth/user - Get current authenticated user info
info: EasyAuth.Extensions[0]
      ✅ /api/EAuth/logout - Logout current user session
info: EasyAuth.Extensions[0]
      ⚠️  CRITICAL: EasyAuth uses EXCLUSIVE /api/EAuth/ paths (NOT /api/auth/)
```

## 🚀 **Advanced Configuration**

### **Multi-Environment Setup**

```csharp
// Development
if (app.Environment.IsDevelopment())
{
    app.MapEasyAuth(endpoints =>
    {
        // Add development-only endpoints
        endpoints.MapGet("/dev/reset-demo-data", () => 
        {
            // Reset demo data
            return Results.Ok("Demo data reset");
        });
    });
}
else
{
    // Production
    app.MapEasyAuth();
}
```

### **Health Check Configuration**

Enable health checks in configuration:

```json
{
  "EasyAuth": {
    "Framework": {
      "EnableHealthChecks": true
    }
  }
}
```

## ❗ **Critical API Path Requirements**

**EasyAuth Framework uses EXCLUSIVE `/api/EAuth/` paths:**

✅ **Correct Paths:**
- `/api/EAuth/providers`
- `/api/EAuth/login`
- `/api/EAuth/callback/{provider}`
- `/api/EAuth/user`
- `/api/EAuth/logout`

❌ **Incorrect Paths (Do NOT use):**
- `/api/auth/` 
- `/auth/`
- Any other path variants

## 🔒 **Security Considerations**

1. **HTTPS Required in Production** - OAuth callbacks require HTTPS
2. **CORS Configuration** - Ensure proper CORS setup for frontend apps
3. **Rate Limiting** - Consider adding rate limiting for authentication endpoints
4. **Security Headers** - `UseEasyAuth()` adds security headers automatically

## 🎯 **Integration Testing**

Test that endpoints are properly mapped:

```csharp
[Test]
public async Task MapEasyAuth_ShouldExposeAllEndpoints()
{
    // Arrange
    using var app = CreateTestApp(builder =>
    {
        builder.Services.AddEasyAuth(Configuration);
        builder.Services.AddControllers();
    });

    app.UseEasyAuth();
    app.UseRouting();
    app.MapEasyAuth();

    // Act & Assert
    var response = await app.GetAsync("/api/EAuth/providers");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var userResponse = await app.GetAsync("/api/EAuth/user");
    userResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## 📝 **Method Implementation Details**

The `MapEasyAuth()` method performs these operations internally:

1. **Controller Mapping**: `app.MapControllers()` - Activates all EasyAuth controllers
2. **Endpoint Registration**: `app.MapEasyAuthEndpoints()` - Adds health checks if enabled  
3. **Custom Configuration**: Executes provided `configureEndpoints` action
4. **Logging**: Provides developer-friendly startup information
5. **Validation**: Ensures proper integration

## 🔄 **Version Compatibility**

- ✅ **EasyAuth v2.4.0+**: Full support with enhanced Swagger documentation
- ✅ **.NET 8.0+**: Primary target framework  
- ✅ **ASP.NET Core**: Compatible with all ASP.NET Core project types

---

## 🎯 **Quick Start Checklist**

For new projects integrating EasyAuth:

- [ ] Install EasyAuth NuGet packages
- [ ] Add `builder.Services.AddEasyAuth(builder.Configuration)` 
- [ ] Add `builder.Services.AddControllers()`
- [ ] Add `app.UseEasyAuth()`
- [ ] Add `app.UseRouting()`
- [ ] **Add `app.MapEasyAuth()` - THIS IS CRITICAL**
- [ ] Configure OAuth providers in appsettings.json
- [ ] Test endpoints appear in Swagger at `/swagger`
- [ ] Verify startup logs show mapped endpoints

**Without `app.MapEasyAuth()`, EasyAuth will NOT work. This method is not optional.**

---

*This documentation covers EasyAuth Framework v2.4.2. For the latest updates, refer to the official EasyAuth documentation.*