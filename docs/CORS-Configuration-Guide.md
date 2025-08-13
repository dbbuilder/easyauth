# EasyAuth CORS Configuration Guide

## Overview

EasyAuth provides flexible CORS (Cross-Origin Resource Sharing) configuration that automatically handles both development and production scenarios.

## Configuration Methods

### 1. appsettings.json Configuration (Recommended)

Configure your production origins in `appsettings.json`:

```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://www.yourapp.com",
        "https://app.yourapp.com",
        "https://staging.yourapp.com",
        "https://www.remote2me.net"
      ],
      "EnableAutoDetection": true,
      "AutoLearnOrigins": false,
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
      "AllowedHeaders": [
        "Authorization",
        "Content-Type",
        "Accept",
        "X-Requested-With"
      ]
    }
  }
}
```

### 2. Code Configuration

Configure CORS programmatically in `Program.cs` or `Startup.cs`:

```csharp
builder.Services.ConfigureEasyAuth(options =>
{
    // Configure your production origins
    options.Cors.AllowedOrigins.AddRange(new[]
    {
        "https://www.yourapp.com",
        "https://app.yourapp.com", 
        "https://staging.yourapp.com"
    });
    
    // Enable auto-detection for flexibility
    options.Cors.EnableAutoDetection = true;
    
    // Disable auto-learning in production for security
    options.Cors.AutoLearnOrigins = false;
});
```

## CORS Policies

EasyAuth uses different CORS policies based on environment:

### Development Environment
- **Policy**: `EasyAuthDevelopment`
- **Behavior**: Allows all origins (`*`) for maximum flexibility
- **Auto-Detection**: Enabled by default

### Production Environment
- **Policy**: `EasyAuthProduction` or `EasyAuthAuto`
- **Behavior**: Only allows configured origins
- **Security**: Strict origin validation

## How Origins Are Resolved

EasyAuth combines multiple origin sources:

1. **Configured Origins**: From `appsettings.json` or code configuration
2. **Default Development Origins**: Automatic localhost ports (3000, 5173, 8080, etc.)
3. **Auto-Learned Origins**: Dynamically discovered origins (if enabled)

### Default Development Origins

EasyAuth automatically includes these localhost origins:

```
React:          https://localhost:3000, http://localhost:3000
Vite:           https://localhost:5173, http://localhost:5173
Vue CLI:        https://localhost:8080, http://localhost:8080
Angular:        https://localhost:4200, http://localhost:4200
Next.js:        https://localhost:3001, http://localhost:3001
Svelte:         https://localhost:5000, http://localhost:5000
Nuxt.js:        https://localhost:3002, http://localhost:3002
Common ports:   8000, 8001, 8888, 9000, 9001
```

## Path Detection

EasyAuth CORS middleware automatically detects authentication requests for these paths:

- `/api/EAuth/*` (Primary path - v2.4.3+)
- `/api/auth/*` (Legacy compatibility)
- `/easyauth/*` (Alternative path)
- `/eauth/*` (Short path)

## Troubleshooting CORS Issues

### 1. Origin Not Allowed Error

```
Access to XMLHttpRequest at 'https://api.yourdomain.com/api/EAuth/me' 
from origin 'https://www.yourdomain.com' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

**Solution**: Add your frontend origin to the configuration:

```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://www.yourdomain.com"
      ]
    }
  }
}
```

### 2. Wrong API Path

```
GET https://api.yourdomain.com/eauth/me net::ERR_FAILED 404 (Not Found)
```

**Solution**: Use the correct API path `/api/EAuth/me` instead of `/eauth/me`:

```typescript
// ✅ Correct
const response = await axios.get('/api/EAuth/me');

// ❌ Incorrect
const response = await axios.get('/eauth/me');
```

### 3. Development vs Production

**Development**: CORS is permissive (allows all origins)
**Production**: CORS is restrictive (only configured origins)

Make sure to test your production CORS configuration before deploying.

## Security Best Practices

### 1. Production Configuration
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://www.yourapp.com"  // Only your production domain
      ],
      "EnableAutoDetection": false,  // Disable for strict security
      "AutoLearnOrigins": false      // Never enable in production
    }
  }
}
```

### 2. Staging Configuration
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://staging.yourapp.com",
        "https://preview.yourapp.com"
      ],
      "EnableAutoDetection": true,   // Allow some flexibility
      "AutoLearnOrigins": false      // Still secure
    }
  }
}
```

### 3. Development Configuration
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [],          // Use defaults
      "EnableAutoDetection": true,   // Full flexibility
      "AutoLearnOrigins": true       // Learn new origins
    }
  }
}
```

## Environment Variables

You can also configure origins via environment variables:

```bash
EasyAuth__Cors__AllowedOrigins__0=https://www.yourapp.com
EasyAuth__Cors__AllowedOrigins__1=https://app.yourapp.com
EasyAuth__Cors__EnableAutoDetection=true
```

## Logging

EasyAuth provides detailed CORS logging. Enable debug logging to troubleshoot:

```json
{
  "Logging": {
    "LogLevel": {
      "EasyAuth.Framework.Core.Configuration": "Debug"
    }
  }
}
```

You'll see logs like:
```
[Debug] Auto-allowing configured origin: https://www.yourapp.com
[Warning] Rejecting unknown origin: https://malicious-site.com
[Information] EasyAuth CORS Production Policy: 5 origins configured
```

## Testing Your Configuration

### 1. Test Development
```bash
curl -H "Origin: http://localhost:3000" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: authorization" \
     -X OPTIONS \
     https://localhost:5001/api/EAuth/providers
```

### 2. Test Production
```bash
curl -H "Origin: https://www.yourapp.com" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: authorization" \
     -X OPTIONS \
     https://api.yourapp.com/api/EAuth/providers
```

You should see headers like:
```
Access-Control-Allow-Origin: https://www.yourapp.com
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: authorization, content-type
Access-Control-Allow-Credentials: true
```

## Common Configuration Examples

### Single Page Application (SPA)
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": ["https://myapp.com"]
    }
  }
}
```

### Multiple Subdomains
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://app.mycompany.com",
        "https://admin.mycompany.com",
        "https://dashboard.mycompany.com"
      ]
    }
  }
}
```

### Multi-Environment Setup
```json
{
  "EasyAuth": {
    "Cors": {
      "AllowedOrigins": [
        "https://myapp.com",
        "https://staging.myapp.com",
        "https://dev.myapp.com"
      ]
    }
  }
}
```

## Migration from v2.4.2

If you're upgrading from v2.4.2, note these changes:

1. **New Primary Path**: `/api/EAuth/*` instead of `/api/auth/*`
2. **Configuration-Driven**: Origins now come from configuration, not hardcoded
3. **Enhanced Logging**: Better debugging with detailed CORS logs
4. **Path Detection**: Improved detection for all EasyAuth paths

Update your frontend to use `/api/EAuth/` paths and configure your production origins in `appsettings.json`.