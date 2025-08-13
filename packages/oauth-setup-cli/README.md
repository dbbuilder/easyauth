# EasyAuth OAuth Setup CLI

Comprehensive OAuth application setup tool for the EasyAuth Framework, supporting automated creation and configuration of OAuth applications across Google, Facebook, Apple, and Azure B2C providers.

## Features

🚀 **Comprehensive Provider Support**
- Google OAuth 2.0 (with Google Cloud SDK integration)
- Facebook Login (with Graph API automation)  
- Apple Sign-In (with App Store Connect API)
- Azure B2C (with Microsoft Graph API for complete tenant setup)

🤖 **Automated Setup**
- CLI tool installation and authentication
- OAuth app creation via APIs where possible
- Guided manual setup with exact instructions
- Credential validation and testing

🔧 **Production Ready**
- Multiple output formats (ENV, JSON, YAML)
- TypeScript integration code generation
- Development and production configuration
- Comprehensive error handling and fallbacks

⚡ **Developer Experience**
- Interactive and non-interactive modes
- Dry-run capability for testing
- Verbose logging for debugging
- Force overwrite for updates

## Installation

```bash
# Install globally
npm install -g @easyauth/oauth-setup

# Or use directly with npx
npx @easyauth/oauth-setup --help
```

## Quick Start

```bash
# Setup all providers interactively
npx @easyauth/oauth-setup \
  --project "MyApp" \
  --domain "myapp.com"

# Setup specific provider only
npx @easyauth/oauth-setup \
  --project "MyApp" \
  --domain "myapp.com" \
  --google-only

# Non-interactive mode for CI/CD
npx @easyauth/oauth-setup \
  --project "MyApp" \
  --domain "myapp.com" \
  --non-interactive \
  --output-json
```

## Command Line Options

### Required Options
- `--project, -p <name>` - Project name (used for app naming)
- `--domain, -d <domain>` - Your domain name (e.g., myapp.com)

### Provider Selection
- `--providers <list>` - Comma-separated list (default: all)
- `--google-only` - Setup Google OAuth only
- `--facebook-only` - Setup Facebook Login only  
- `--apple-only` - Setup Apple Sign-In only
- `--azure-only` - Setup Azure B2C only

### Output Options
- `--output-format <format>` - Format: env, json, yaml (default: env)
- `--output-file <file>` - Custom output file path

### Execution Options
- `--non-interactive` - Run without prompts (CI mode)
- `--dry-run` - Show what would be done without executing
- `--force` - Overwrite existing configurations
- `--verbose` - Enable detailed logging

## Provider-Specific Setup

### Google OAuth 2.0

**Prerequisites:**
- Google Cloud SDK installed and authenticated
- Google Cloud project with billing enabled

**Automated Features:**
- Google Cloud project creation/selection
- API enablement (OAuth 2.0, Google+ API)
- OAuth consent screen configuration
- OAuth 2.0 client creation

**Manual Steps Required:**
- OAuth consent screen review and configuration
- Client credential configuration in Google Cloud Console

### Facebook Login

**Prerequisites:**
- Facebook Developer account
- Facebook Graph API access token with `apps_management` permission

**Automated Features:**
- Facebook app creation via Graph API
- Facebook Login product configuration
- OAuth redirect URI setup
- Test app creation for development

**Manual Steps Required:**
- Business verification for production apps
- App review process for advanced permissions

### Apple Sign-In

**Prerequisites:**
- Apple Developer account ($99/year)
- App Store Connect API key with Developer role

**Automated Features:**
- Service ID creation via App Store Connect API
- App ID creation and configuration
- Sign-In with Apple key generation
- Domain verification file creation

**Manual Steps Required:**
- Sign-In with Apple configuration in Apple Developer Portal
- Domain verification file upload to web server
- Private key management and secure storage

### Azure B2C (Comprehensive Setup)

**Prerequisites:**
- Azure CLI installed and authenticated
- Azure subscription with appropriate permissions

**Automated Features:**
- B2C tenant creation and configuration
- Identity Experience Framework setup
- Multiple application registrations (web, native, API, management)
- User flows creation (sign-up/sign-in, profile edit, password reset)
- Identity providers configuration
- Custom attributes and API connectors
- Branding and localization setup

**Manual Steps Required:**
- B2C tenant approval (may require manual review)
- Custom policies setup for advanced scenarios
- Identity provider credential configuration

## Output Files

### Environment Variables (.env.oauth)
```bash
# Google OAuth
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret

# Facebook Login  
FACEBOOK_APP_ID=1234567890123456
FACEBOOK_APP_SECRET=your-app-secret

# Apple Sign-In
APPLE_SERVICE_ID=com.yourapp.service
APPLE_TEAM_ID=ABCD123456
APPLE_KEY_ID=ABCD123456
APPLE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n..."

# Azure B2C
AZURE_B2C_CLIENT_ID=your-client-id
AZURE_B2C_CLIENT_SECRET=your-client-secret
AZURE_B2C_TENANT_ID=yourtenant.onmicrosoft.com
AZURE_B2C_AUTHORITY=https://yourtenant.b2clogin.com

# EasyAuth Configuration
EASYAUTH_BASE_URL=https://yourdomain.com
EASYAUTH_ENVIRONMENT=production
```

### TypeScript Integration (easyauth-config.ts)
```typescript
import { EnhancedEasyAuthClient } from '@easyauth/sdk';

export const easyAuthConfig = {
  baseUrl: process.env.EASYAUTH_BASE_URL || 'https://yourdomain.com',
  environment: process.env.EASYAUTH_ENVIRONMENT as 'development' | 'staging' | 'production' || 'production',
  
  providers: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!
    },
    facebook: {
      appId: process.env.FACEBOOK_APP_ID!,
      appSecret: process.env.FACEBOOK_APP_SECRET!
    },
    // ... other providers
  }
};

export const easyAuthClient = new EnhancedEasyAuthClient(easyAuthConfig);
export default easyAuthClient;
```

## Integration with EasyAuth Framework

The generated credentials work seamlessly with the EasyAuth Framework. After running the setup:

1. **Add Environment Variables**: Copy generated credentials to your project's environment configuration

2. **Configure EasyAuth**: Use the generated TypeScript configuration file

3. **⚠️ CRITICAL - API Endpoints**: EasyAuth uses **EXCLUSIVE** paths under `/api/EAuth/` (NOT `/api/auth/` or other variants):
   - ✅ OAuth callbacks: `/api/EAuth/{provider}/callback`
   - ✅ Login endpoints: `/api/EAuth/{provider}/login`  
   - ✅ Logout: `/api/EAuth/logout`
   - ✅ User info: `/api/EAuth/user`
   - ❌ Do NOT use: `/api/auth/`, `/auth/`, or other path variants

4. **Register Middleware**: In your ASP.NET Core application:
   ```csharp
   // Program.cs
   builder.Services.AddEasyAuth(builder.Configuration);
   
   var app = builder.Build();
   
   // CRITICAL: This line activates all EasyAuth endpoints
   app.MapEasyAuth();
   ```

## CI/CD Integration

For automated deployments:

```bash
# In your CI/CD pipeline
npx @easyauth/oauth-setup \
  --project "$PROJECT_NAME" \
  --domain "$DOMAIN_NAME" \
  --non-interactive \
  --output-json \
  --output-file oauth-config.json
  
# Upload credentials to secure environment variables
```

## Troubleshooting

### Common Issues

**CLI Tool Authentication Failures**
- Ensure Azure CLI is logged in: `az login`
- Verify Google Cloud SDK auth: `gcloud auth list`
- Check permissions for creating resources

**Provider API Errors**
- Facebook: Verify access token has `apps_management` permission
- Apple: Ensure API key has Developer role
- Azure: Check subscription limits and B2C availability

**Manual Setup Required**
- Some providers require manual configuration steps
- The CLI provides detailed instructions for manual setup
- Use `--verbose` flag for additional debugging information

### Getting Help

- Run with `--help` for command line options
- Use `--dry-run` to test configuration without changes
- Enable `--verbose` for detailed logging
- Check provider-specific documentation for manual setup steps

## Security Considerations

- **Credential Storage**: Store generated credentials securely
- **Environment Separation**: Use different credentials for development/staging/production
- **Access Control**: Limit access to OAuth app management
- **Regular Rotation**: Rotate secrets regularly, especially for production
- **Audit Logs**: Monitor OAuth app usage and access patterns

## Development

### Building from Source

```bash
git clone https://github.com/dbbuilder/easyauth.git
cd easyauth/packages/oauth-setup-cli
npm install
npm run build
npm test
```

### Contributing

- Follow TypeScript best practices
- Add tests for new providers
- Update documentation for new features
- Test across different environments

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Support

- 📖 [EasyAuth Documentation](https://docs.easyauth.dev)
- 🐛 [Issue Tracker](https://github.com/dbbuilder/easyauth/issues)
- 💬 [Discussions](https://github.com/dbbuilder/easyauth/discussions)