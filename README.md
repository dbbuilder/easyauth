# EasyAuth Framework

**Enterprise-grade authentication framework with multi-provider support**

## Overview

EasyAuth is a self-contained .NET authentication framework that supports multiple identity providers (Azure AD B2C, Google, Apple, Meta/Facebook) with automatic database setup and simple integration for Vue.js and React applications.

## Key Features

- **Multi-Provider Support**: Azure AD B2C, Google OAuth, Apple Sign-In, Meta/Facebook Login
- **Self-Contained**: Single NuGet package with automatic database setup
- **Secure by Default**: BFF pattern with HTTP-only cookies
- **Framework Agnostic**: Works with Vue.js, React, or vanilla JavaScript
- **Enterprise Ready**: SQL Server backend, comprehensive logging, health checks
- **Zero Configuration**: Automatic setup with minimal configuration required

## Quick Start

### Backend Integration

```bash
dotnet add package EasyAuth.Framework
```

```csharp
// Program.cs
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);
app.UseEasyAuth(builder.Configuration);
```

### Frontend Integration

```bash
npm install @easyauth/client
```

```typescript
import { EasyAuthClient, useEasyAuth } from '@easyauth/client';

const authClient = new EasyAuthClient({
  baseUrl: 'https://your-api.com'
});

// Vue.js
const auth = useEasyAuth(authClient);

// React
const auth = useEasyAuthReact(authClient);
```

## Supported Identity Providers

- ✅ Azure AD B2C (with federated providers)
- ✅ Google OAuth 2.0
- ✅ Apple Sign-In
- ✅ Meta/Facebook Login
- 🔄 Custom OAuth providers (coming soon)

## Documentation

- [Getting Started](docs/getting-started.md)
- [Configuration Guide](docs/configuration.md)
- [Provider Setup](docs/providers.md)
- [API Reference](docs/api-reference.md)

## License

MIT
