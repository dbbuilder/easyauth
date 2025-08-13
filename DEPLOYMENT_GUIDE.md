# EasyAuth Deployment Guide

## Overview

This guide covers deploying EasyAuth Framework for production use, including both backend (.NET API) and frontend (React/Vue) applications.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Backend Deployment](#backend-deployment)
3. [Frontend Deployment](#frontend-deployment)
4. [OAuth Provider Configuration](#oauth-provider-configuration)
5. [Production Checklist](#production-checklist)
6. [Monitoring and Maintenance](#monitoring-and-maintenance)

## Prerequisites

### System Requirements

- .NET 6+ runtime for backend
- Node.js 16+ for frontend packages
- SQL Server or compatible database
- HTTPS-enabled domain
- SSL certificates

### Security Requirements

- Valid SSL certificates for all domains
- Secure secret management (Azure Key Vault, AWS Secrets Manager, etc.)
- HTTPS redirect configuration
- CORS properly configured for production domains

## Backend Deployment

### 1. Database Setup

```sql
-- Create EasyAuth database
CREATE DATABASE EasyAuthProd;

-- Run EasyAuth migration scripts
-- (Located in src/EasyAuth.Framework.Core/Database/)
```

### 2. Configuration

#### appsettings.Production.json

```json
{
  "EasyAuth": {
    "ConnectionString": "Server=prod-sql-server;Database=EasyAuthProd;Trusted_Connection=true;TrustServerCertificate=true;",
    "JwtSecret": "#{JwtSecret}#",
    "Providers": {
      "Google": {
        "Enabled": true,
        "ClientId": "#{GoogleClientId}#",
        "ClientSecret": "#{GoogleClientSecret}#",
        "CallbackPath": "/auth/google/callback"
      },
      "Facebook": {
        "Enabled": true,
        "AppId": "#{FacebookAppId}#",
        "AppSecret": "#{FacebookAppSecret}#",
        "CallbackPath": "/auth/facebook/callback"
      }
    },
    "Security": {
      "RequireHttps": true,
      "CookieSecure": true,
      "CookieSameSite": "Strict"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "EasyAuth": "Information"
    }
  }
}
```

### 3. Azure App Service Deployment

#### azure-pipelines.yml

```yaml
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - src/

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '6.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build application'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration)'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration $(buildConfiguration) --collect "Code coverage"'

- task: DotNetCoreCLI@2
  displayName: 'Publish application'
  inputs:
    command: 'publish'
    projects: '**/YourApi.csproj'
    arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'

- task: AzureWebApp@1
  displayName: 'Deploy to Azure App Service'
  inputs:
    azureSubscription: 'Azure Service Connection'
    appType: 'webApp'
    appName: 'your-easyauth-api'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

### 4. Docker Deployment

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["YourApi/YourApi.csproj", "YourApi/"]
COPY ["src/EasyAuth.Framework.Core/", "src/EasyAuth.Framework.Core/"]
RUN dotnet restore "YourApi/YourApi.csproj"

COPY . .
WORKDIR "/src/YourApi"
RUN dotnet build "YourApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YourApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourApi.dll"]
```

#### docker-compose.yml

```yaml
version: '3.8'
services:
  easyauth-api:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - EasyAuth__ConnectionString=Server=sql-server;Database=EasyAuthProd;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=true;
      - EasyAuth__JwtSecret=${JWT_SECRET}
    depends_on:
      - sql-server
    
  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

## Frontend Deployment

### 1. React Application

#### Environment Variables

```bash
# .env.production
REACT_APP_EASYAUTH_API_URL=https://your-api.com
REACT_APP_ENVIRONMENT=production
```

#### Build and Deploy

```bash
# Build for production
npm run build

# Deploy to static hosting (Netlify, Vercel, etc.)
# Or serve with nginx/Apache
```

#### nginx.conf

```nginx
server {
    listen 80;
    server_name your-app.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-app.com;
    
    ssl_certificate /path/to/certificate.crt;
    ssl_certificate_key /path/to/private.key;
    
    root /var/www/html;
    index index.html;
    
    # Handle React Router
    location / {
        try_files $uri $uri/ /index.html;
    }
    
    # API proxy
    location /api/ {
        proxy_pass https://your-api.com/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 2. Vue Application

#### Build Configuration

```javascript
// vite.config.js
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['vue'],
          easyauth: ['@easyauth/vue']
        }
      }
    }
  },
  define: {
    __API_URL__: JSON.stringify(process.env.VITE_API_URL || 'https://your-api.com')
  }
})
```

## OAuth Provider Configuration

### 1. Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 Client ID
5. Configure authorized redirect URIs:
   - `https://your-api.com/auth/google/callback`
   - `https://your-app.com/auth/callback` (if using frontend callback)

### 2. Facebook OAuth Setup

1. Go to [Facebook for Developers](https://developers.facebook.com/)
2. Create new app
3. Add Facebook Login product
4. Configure Valid OAuth Redirect URIs:
   - `https://your-api.com/auth/facebook/callback`

### 3. Apple Sign-In Setup

1. Go to [Apple Developer Portal](https://developer.apple.com/)
2. Create new Service ID
3. Configure Return URLs:
   - `https://your-api.com/auth/apple/callback`
4. Generate private key for JWT signing

## Production Checklist

### Security

- [ ] HTTPS enabled on all domains
- [ ] SSL certificates valid and auto-renewing
- [ ] JWT secrets properly randomized and secure
- [ ] OAuth secrets stored in secure key management
- [ ] CORS configured for production domains only
- [ ] Security headers configured (HSTS, CSP, etc.)
- [ ] Rate limiting enabled
- [ ] SQL injection protection verified
- [ ] XSS protection enabled

### Performance

- [ ] Database indexed properly
- [ ] Connection pooling configured
- [ ] Static assets served via CDN
- [ ] Gzip/Brotli compression enabled
- [ ] Caching headers configured
- [ ] Bundle size optimized
- [ ] Lazy loading implemented where appropriate

### Monitoring

- [ ] Application Insights/monitoring configured
- [ ] Error tracking enabled (Sentry, etc.)
- [ ] Health checks implemented
- [ ] Log aggregation configured
- [ ] Performance monitoring enabled
- [ ] Uptime monitoring configured

### Backup and Recovery

- [ ] Database backup strategy implemented
- [ ] Backup restoration tested
- [ ] Disaster recovery plan documented
- [ ] Configuration backup automated

### Compliance

- [ ] GDPR compliance documented
- [ ] Privacy policy updated
- [ ] Terms of service updated
- [ ] Data retention policies implemented
- [ ] Audit logging enabled

## Monitoring and Maintenance

### Health Checks

```csharp
// Add to Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddEasyAuthHealthCheck();

app.MapHealthChecks("/health");
```

### Logging

```csharp
// Configure structured logging
builder.Logging.AddApplicationInsights();
builder.Logging.AddJsonConsole();
```

### Performance Monitoring

```javascript
// Frontend performance monitoring
import { getCLS, getFID, getFCP, getLCP, getTTFB } from 'web-vitals';

getCLS(console.log);
getFID(console.log);
getFCP(console.log);
getLCP(console.log);
getTTFB(console.log);
```

### Maintenance Tasks

#### Daily
- Monitor error rates
- Check performance metrics
- Verify uptime status

#### Weekly
- Review security logs
- Update dependencies (if needed)
- Performance optimization review

#### Monthly
- Security audit
- Backup restoration test
- Capacity planning review
- Documentation updates

## Troubleshooting

### Common Issues

1. **OAuth callback failures**
   - Verify redirect URIs match exactly
   - Check HTTPS configuration
   - Validate OAuth app configuration

2. **Token refresh issues**
   - Check JWT secret configuration
   - Verify database connectivity
   - Review token expiration settings

3. **CORS errors**
   - Validate CORS origins configuration
   - Check preflight requests
   - Verify headers configuration

### Support

For deployment issues:
1. Check the [GitHub Issues](https://github.com/dbbuilder/easyauth/issues)
2. Review the [troubleshooting guide](TROUBLESHOOTING.md)
3. Contact support with deployment logs and configuration (sanitized)