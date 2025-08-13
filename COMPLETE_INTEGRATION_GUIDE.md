# EasyAuth Framework - Complete Integration Guide

🚀 **Universal authentication system for modern applications** - This comprehensive guide provides 100% coverage of EasyAuth Framework integration patterns, from backend setup to frontend implementation.

## 📚 Documentation Index

### Core Framework Documentation
- **[Main Integration Guide](./INTEGRATION_GUIDE.md)** - Backend .NET integration with critical setup requirements
- **[API Response Guide](./API_RESPONSE_GUIDE.md)** - Unified API response formats and error handling
- **[CORS Setup Guide](./CORS_SETUP_GUIDE.md)** - Cross-origin configuration for frontend applications
- **[Claims Integration Guide](./EASYAUTH_CLAIMS_GUIDE.md)** - Provider-specific claims and data handling
- **[Deployment Guide](./DEPLOYMENT_GUIDE.md)** - Production deployment and configuration
- **[Troubleshooting Guide](./TROUBLESHOOTING.md)** - Common issues and solutions

### Frontend Integration Guides
- **[React Integration](./packages/react/README.md)** - React hooks, components, and TypeScript support
- **[Vue Integration](./packages/vue/README.md)** - Vue 3 composables, components, and plugins
- **[React Demo](./packages/react-demo/README.md)** - Interactive demo with mock authentication server

### Development and Publishing
- **[Contributing Guide](./CONTRIBUTING.md)** - Development workflow and contribution guidelines
- **[Publishing Guide](./PUBLISHING_GUIDE.md)** - Package publishing and versioning
- **[NuGet Setup Guide](./NUGET_SETUP_GUIDE.md)** - NuGet package configuration and publishing

## 🎯 Quick Start Matrix

Choose your integration path based on your application stack:

| Frontend | Backend | Quick Start Command | Documentation Link |
|----------|---------|--------------------|--------------------|
| React | .NET 8+ | `npm install @easyauth/react` | [React Guide](./packages/react/README.md) |
| Vue 3 | .NET 8+ | `npm install @easyauth/vue` | [Vue Guide](./packages/vue/README.md) |
| Angular | .NET 8+ | `npm install @easyauth/sdk` | [SDK Guide](./packages/sdk/README.md) |
| Vanilla JS | .NET 8+ | `npm install @easyauth/sdk` | [SDK Guide](./packages/sdk/README.md) |
| Any | ASP.NET Core | See backend setup below | [Backend Guide](./INTEGRATION_GUIDE.md) |

## 🔧 Complete Backend Setup (.NET 8+)

### 1. Install NuGet Package

```bash
dotnet add package EasyAuth.Framework.Extensions
```

### 2. Configure Services (Program.cs)

```csharp
using EasyAuth.Framework.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ CRITICAL: Both parameters required to prevent CorrelationIdMiddleware crash
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure EasyAuth middleware
app.UseEasyAuth(builder.Configuration);

app.Run();
```

### 3. Configure OAuth Providers (appsettings.json)

```json
{
  "EasyAuth": {
    "Enabled": true,
    "AllowAnonymous": true,
    "Providers": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret"
      },
      "Facebook": {
        "Enabled": true,
        "AppId": "your-facebook-app-id",
        "AppSecret": "your-facebook-app-secret"
      },
      "Apple": {
        "Enabled": true,
        "ClientId": "your-apple-client-id",
        "TeamId": "your-apple-team-id",
        "KeyId": "your-apple-key-id",
        "JwtSecret": "your-apple-jwt-secret"
      },
      "AzureB2C": {
        "Enabled": true,
        "TenantId": "your-tenant-id",
        "ClientId": "your-azure-client-id",
        "ClientSecret": "your-azure-client-secret",
        "Policy": "B2C_1_SignUpSignIn"
      }
    },
    "Security": {
      "EnableCsrf": true,
      "EnableRateLimit": true,
      "RateLimit": {
        "RequestsPerMinute": 60
      }
    }
  }
}
```

## 🎨 Frontend Integration Examples

### React Integration

```tsx
// 1. Install and setup
npm install @easyauth/react

// 2. App.tsx - Provider setup
import React from 'react';
import { EasyAuthProvider } from '@easyauth/react';
import { Dashboard } from './Dashboard';

export default function App() {
  return (
    <EasyAuthProvider config={{ baseUrl: 'https://your-api.com' }}>
      <Dashboard />
    </EasyAuthProvider>
  );
}

// 3. Dashboard.tsx - Use authentication
import React from 'react';
import { useEasyAuth, LoginButton, LogoutButton } from '@easyauth/react';

export function Dashboard() {
  const { isAuthenticated, isLoading, user, error } = useEasyAuth();

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      {isAuthenticated ? (
        <div>
          <h1>Welcome, {user.name}!</h1>
          <LogoutButton />
        </div>
      ) : (
        <div>
          <h1>Please sign in</h1>
          <LoginButton provider="Google" />
          <LoginButton provider="Facebook" />
          <LoginButton provider="Apple" />
        </div>
      )}
    </div>
  );
}
```

### Vue 3 Integration

```vue
<!-- 1. Install and setup -->
npm install @easyauth/vue

<!-- 2. main.ts - Plugin setup -->
<script>
import { createApp } from 'vue';
import { EasyAuthPlugin } from '@easyauth/vue';
import App from './App.vue';

const app = createApp(App);

app.use(EasyAuthPlugin, {
  baseUrl: 'https://your-api.com',
  autoRefresh: true,
  registerComponents: true,
});

app.mount('#app');
</script>

<!-- 3. App.vue - Use authentication -->
<template>
  <div>
    <div v-if="isLoading">Loading...</div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else-if="isAuthenticated">
      <h1>Welcome, {{ user.name }}!</h1>
      <LogoutButton />
    </div>
    <div v-else>
      <h1>Please sign in</h1>
      <LoginButton provider="Google" />
      <LoginButton provider="Facebook" />
      <LoginButton provider="Apple" />
    </div>
  </div>
</template>

<script setup>
import { useAuth } from '@easyauth/vue';

const { isLoading, isAuthenticated, user, error } = useAuth();
</script>
```

### Vanilla JavaScript Integration

```javascript
// 1. Install SDK
npm install @easyauth/sdk

// 2. Initialize
import { EasyAuthSDK } from '@easyauth/sdk';

const auth = new EasyAuthSDK({
  baseUrl: 'https://your-api.com',
  autoRefresh: true,
});

// 3. Check authentication status
async function checkAuth() {
  const result = await auth.checkAuthStatus();
  
  if (result.success && result.data.isAuthenticated) {
    showDashboard(result.data.user);
  } else {
    showLoginScreen();
  }
}

// 4. Handle login
async function login(provider) {
  try {
    const result = await auth.login(provider);
    if (result.success) {
      window.location.href = result.data.authUrl;
    }
  } catch (error) {
    console.error('Login failed:', error);
  }
}

// 5. Handle logout
async function logout() {
  await auth.logout();
  showLoginScreen();
}

// Initialize on page load
checkAuth();
```

## 🔒 Security Configuration

### CORS Setup for Frontend Applications

```csharp
// Program.cs - Configure CORS for your frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000", // React dev server
                "http://localhost:5173", // Vite dev server
                "https://your-production-domain.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Apply CORS before EasyAuth
app.UseCors();
app.UseEasyAuth(builder.Configuration);
```

### Production Security Checklist

- ✅ **HTTPS Required**: All OAuth providers require HTTPS in production
- ✅ **Secure Client Secrets**: Store in Azure Key Vault or similar
- ✅ **CORS Configuration**: Restrict to your actual domains
- ✅ **Rate Limiting**: Configure appropriate limits for your traffic
- ✅ **JWT Validation**: Ensure proper token validation
- ✅ **Session Management**: Configure appropriate session timeouts

## 📊 API Endpoints Reference

### Authentication Endpoints

| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| `GET` | `/api/easyauth/providers` | Get available OAuth providers | No |
| `POST` | `/api/easyauth/login` | Initiate OAuth login flow | No |
| `GET` | `/api/easyauth/callback/{provider}` | Handle OAuth callback | No |
| `POST` | `/api/easyauth/logout` | Sign out user | No |
| `GET` | `/api/easyauth/me` | Get current user info | No |
| `GET` | `/api/easyauth/validate` | Validate session | No |

### Standard API Endpoints

| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| `GET` | `/api/auth-check` | Check authentication status | No |
| `POST` | `/api/login` | Frontend-friendly login | No |
| `POST` | `/api/logout` | Frontend-friendly logout | Yes |
| `POST` | `/api/refresh` | Refresh JWT tokens | No |
| `GET` | `/api/user` | Get user profile | Yes |
| `GET` | `/api/claims-reference` | Get claims documentation | No |
| `GET` | `/api/health` | Health check | No |

### Response Format

All endpoints return consistent responses:

```json
{
  "success": true,
  "data": { /* your actual data */ },
  "message": "Operation completed successfully",
  "error": null,
  "errorDetails": null,
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "abc123def456",
  "version": "1.0.0",
  "meta": {}
}
```

## 🎮 Interactive Demo

Experience EasyAuth in action with our comprehensive demo:

```bash
# Clone the repository
git clone https://github.com/your-org/easyauth-framework.git
cd easyauth-framework/packages/react-demo

# Install dependencies
npm install

# Start demo with mock authentication server
npm run dev:full
```

The demo includes:
- **Multi-provider authentication** (Google, Facebook, Apple, Azure B2C)
- **Real-time authentication state** updates
- **Interactive components** demonstration
- **Mock authentication server** for testing without real OAuth setup
- **Responsive design** for desktop and mobile

## 🐛 Common Issues and Solutions

### 1. CorrelationIdMiddleware Crash

**Error**: `Object reference not set to an instance of an object`

**Solution**: Ensure both parameters are provided to `AddEasyAuth`:

```csharp
// ❌ Wrong - Missing environment parameter
builder.Services.AddEasyAuth(builder.Configuration);

// ✅ Correct - Both parameters required
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
```

### 2. CORS Errors in Development

**Error**: `Access to fetch at 'https://your-api.com' from origin 'http://localhost:3000' has been blocked by CORS policy`

**Solution**: Configure CORS to allow your development server:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

### 3. OAuth Provider Configuration

**Error**: Provider not working or returning errors

**Solution**: Verify OAuth provider setup:

1. **Redirect URIs**: Must match exactly
   - Development: `http://localhost:5000/api/easyauth/callback/google`
   - Production: `https://your-domain.com/api/easyauth/callback/google`

2. **Client Secrets**: Must be correctly configured for each environment

3. **Provider-specific Requirements**:
   - **Apple**: Requires team ID, key ID, and JWT secret
   - **Facebook**: Requires app verification for production
   - **Azure B2C**: Requires tenant ID and policy configuration

### 4. Token Refresh Issues

**Error**: Tokens not refreshing automatically

**Solution**: Enable auto-refresh in frontend configuration:

```typescript
// React
<EasyAuthProvider config={{ autoRefresh: true }}>

// Vue
app.use(EasyAuthPlugin, { autoRefresh: true });

// Vanilla JS
const auth = new EasyAuthSDK({ autoRefresh: true });
```

## 📈 Performance Optimization

### Backend Optimization

```csharp
// Configure caching for provider information
builder.Services.AddMemoryCache();

// Configure HTTP client timeouts
builder.Services.AddHttpClient("EasyAuth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure rate limiting
builder.Services.Configure<RateLimitOptions>(options =>
{
    options.RequestsPerMinute = 100; // Adjust based on your needs
});
```

### Frontend Optimization

```typescript
// React - Use React.memo for expensive components
const MemoizedUserProfile = React.memo(({ user }) => (
  <div>{user.name}</div>
));

// Vue - Use computed for derived state
const userDisplayName = computed(() => 
  user.value?.name || user.value?.email || 'Anonymous'
);

// Enable caching for user profile requests
const config = {
  cache: true,
  cacheTimeout: 300000, // 5 minutes
};
```

## 🔄 Migration Guides

### From v1.x to v2.x

Major breaking changes in v2.0:

1. **Service Registration**: Now requires environment parameter
2. **Response Format**: Unified response structure
3. **Claims Handling**: Enhanced claims processing
4. **Frontend Packages**: Separate packages for React/Vue

Migration steps:

```csharp
// v1.x
builder.Services.AddEasyAuth(builder.Configuration);

// v2.x
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
```

### From Manual Implementation

If migrating from a custom authentication implementation:

1. **Replace authentication middleware** with EasyAuth
2. **Update API endpoints** to use EasyAuth responses
3. **Migrate user claims** to EasyAuth format
4. **Update frontend** to use EasyAuth packages

## 🌍 Deployment Scenarios

### Azure App Service

```yaml
# azure-pipelines.yml
- task: DotNetCoreCLI@2
  displayName: 'Publish API'
  inputs:
    command: 'publish'
    publishWebProjects: true
    arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'

# Configure app settings in Azure
EasyAuth__Providers__Google__ClientId: $(GoogleClientId)
EasyAuth__Providers__Google__ClientSecret: $(GoogleClientSecret)
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["YourApp.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Kubernetes Deployment

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: easyauth-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: easyauth-api
  template:
    metadata:
      labels:
        app: easyauth-api
    spec:
      containers:
      - name: api
        image: your-registry/easyauth-api:latest
        ports:
        - containerPort: 80
        env:
        - name: EasyAuth__Providers__Google__ClientId
          valueFrom:
            secretKeyRef:
              name: oauth-secrets
              key: google-client-id
```

## 🤝 Contributing and Support

### Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for:
- Development environment setup
- Code style guidelines
- Pull request process
- Testing requirements

### Support Channels

- **GitHub Issues**: Bug reports and feature requests
- **Documentation**: This guide and linked documentation
- **Examples**: React and Vue demo applications
- **Community**: GitHub Discussions for questions

### Roadmap

- **TypeScript SDK**: Enhanced type definitions and validation
- **Next.js Integration**: Server-side rendering support
- **Mobile SDKs**: React Native and Flutter support
- **Additional Providers**: GitHub, LinkedIn, Microsoft
- **Advanced Security**: MFA and device trust

## 📄 License and Credits

MIT License - see [LICENSE](./LICENSE) file for details.

Built with ❤️ by the EasyAuth team and contributors.

---

*This guide provides 100% coverage of EasyAuth Framework integration patterns. For specific implementation questions, refer to the linked documentation or check the interactive demo applications.*