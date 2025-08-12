# EasyAuth CORS Configuration Guide

EasyAuth provides **zero-configuration CORS setup** that eliminates the complexity experienced in the QuizGenerator project. No more manual CORS configuration headaches!

## 🚀 Zero-Config Setup (Recommended)

EasyAuth automatically configures CORS for you when you call `AddEasyAuth()`:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// This automatically includes smart CORS configuration
builder.Services.AddEasyAuth(builder.Configuration);

var app = builder.Build();

// This applies the CORS middleware automatically
app.UseEasyAuthCors(); // Add this line before UseRouting()
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

That's it! EasyAuth will automatically:
- ✅ Allow all common development ports (3000, 5173, 4200, 8080, etc.)
- ✅ Auto-detect new frontend origins
- ✅ Switch between permissive (dev) and secure (prod) policies
- ✅ Handle preflight requests correctly

## 🎯 Framework-Specific Setup

For even better defaults, specify your frontend framework:

```csharp
// For React applications
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.React);

// For Vue applications  
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.Vue);

// For Angular applications
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.Angular);

// For Next.js applications
builder.Services.AddEasyAuthCorsForFramework(FrontendFramework.NextJs);
```

## 🔧 Custom Configuration

Need specific origins? Easy:

```csharp
builder.Services.AddEasyAuthCors(options =>
{
    options.AllowedOrigins.Add("https://myapp.com");
    options.AllowedOrigins.Add("https://staging.myapp.com");
    options.EnableAutoDetection = false; // Disable auto-learning
});
```

## 🌍 Deployment Scenarios

Configure for different environments:

```csharp
// Development - Very permissive
builder.Services.AddEasyAuthCorsForScenario(
    DeploymentScenario.LocalDevelopment);

// Staging - Specific origins only
builder.Services.AddEasyAuthCorsForScenario(
    DeploymentScenario.Staging,
    "https://staging.myapp.com",
    "https://preview.myapp.com");

// Production - Locked down
builder.Services.AddEasyAuthCorsForScenario(
    DeploymentScenario.Production,
    "https://myapp.com",
    "https://www.myapp.com");
```

## 📱 Frontend Integration Examples

### React/Next.js
```javascript
// No CORS headers needed in your fetch calls!
const response = await fetch('/api/auth-check', {
    credentials: 'include' // EasyAuth handles the rest
});
```

### Vue.js
```javascript
// Axios configuration - EasyAuth handles CORS automatically
const api = axios.create({
    baseURL: '/api',
    withCredentials: true
});
```

### Angular
```typescript
// HttpClient - just works out of the box
this.http.get('/api/auth-check', { withCredentials: true })
    .subscribe(response => console.log(response));
```

## 🐛 Troubleshooting

### Common Issues Fixed

1. **"CORS policy blocks request"** → EasyAuth auto-detects your origin
2. **"Origin not allowed"** → Check console for auto-learning messages  
3. **"Credentials not allowed"** → EasyAuth enables credentials by default

### Debug Mode

Enable detailed CORS logging:

```json
{
  "Logging": {
    "LogLevel": {
      "EasyAuth.Framework.Core.Configuration": "Debug"
    }
  }
}
```

### Manual Override

If auto-detection isn't working:

```csharp
// Add your specific origin at runtime
builder.Services.AddEasyAuthOrigin("http://localhost:3001");
```

## 🔍 How It Works

EasyAuth CORS system:

1. **Auto-Detection**: Learns origins from incoming requests
2. **Smart Defaults**: Includes all common development ports
3. **Environment-Aware**: Permissive in dev, secure in prod
4. **Framework-Optimized**: Special handling for React, Vue, Angular
5. **Zero-Config**: Works immediately with `AddEasyAuth()`

## 📋 Configuration Options

```csharp
public class EAuthCorsOptions
{
    public List<string> AllowedOrigins { get; set; }      // Explicitly allowed origins
    public List<string> AllowedMethods { get; set; }      // Allowed HTTP methods  
    public List<string> AllowedHeaders { get; set; }      // Allowed headers
    public bool EnableAutoDetection { get; set; }         // Auto-learn origins
    public bool AutoLearnOrigins { get; set; }           // Remember learned origins
    public int PreflightMaxAge { get; set; }             // Preflight cache duration
}
```

## ✨ Migration from Manual CORS

### Before (Manual Setup)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("MyPolicy");
```

### After (EasyAuth)
```csharp
builder.Services.AddEasyAuth(builder.Configuration);
app.UseEasyAuthCors();
```

**Result**: 90% less CORS-related code and zero configuration headaches!