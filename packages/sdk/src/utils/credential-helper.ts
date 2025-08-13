/**
 * OAuth Credential Helper - Simplifies OAuth app setup for new projects
 */

export interface OAuthCredentials {
  google?: {
    clientId: string;
    clientSecret?: string;
  };
  facebook?: {
    appId: string;
    appSecret?: string;
  };
  apple?: {
    serviceId: string;
    teamId?: string;
    keyId?: string;
    privateKey?: string;
  };
  'azure-b2c'?: {
    clientId: string;
    tenantId: string;
    policy?: string;
  };
}

export interface EasyAuthProxyConfig {
  useProxy: boolean;
  proxyUrl?: string;
  apiKey?: string;
}

/**
 * Development OAuth App Credentials (shared for quick setup)
 * These are public development-only credentials for getting started
 */
const DEVELOPMENT_CREDENTIALS: OAuthCredentials = {
  google: {
    // EasyAuth Development App - Localhost only
    clientId: '1087823091234-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com'
  },
  facebook: {
    // EasyAuth Development App - Localhost only  
    appId: '1234567890123456'
  },
  apple: {
    // EasyAuth Development Service - Localhost only
    serviceId: 'com.easyauth.development'
  },
  'azure-b2c': {
    // EasyAuth Development Tenant - Localhost only
    clientId: '12345678-1234-1234-1234-123456789012',
    tenantId: 'easyauth-dev.onmicrosoft.com'
  }
};

/**
 * Credential Helper for simplified OAuth setup
 */
export class CredentialHelper {
  private isDevelopment: boolean;
  private useProxy: boolean;
  private proxyConfig?: EasyAuthProxyConfig;
  
  constructor(isDevelopment = false, proxyConfig?: EasyAuthProxyConfig) {
    this.isDevelopment = isDevelopment;
    this.useProxy = proxyConfig?.useProxy ?? false;
    this.proxyConfig = proxyConfig;
  }
  
  /**
   * Get OAuth credentials with automatic fallback to development credentials
   */
  getCredentials(userCredentials?: Partial<OAuthCredentials>): OAuthCredentials {
    if (this.useProxy) {
      // When using proxy, credentials are handled server-side
      return this.getProxyCredentials();
    }
    
    if (this.isDevelopment && !userCredentials) {
      console.warn('🔧 Using development OAuth credentials. These only work on localhost.');
      console.info('📖 See https://docs.easyauth.dev/oauth-setup for production setup.');
      return DEVELOPMENT_CREDENTIALS;
    }
    
    // Merge user credentials with development fallbacks
    return this.mergeCredentials(userCredentials || {}, DEVELOPMENT_CREDENTIALS);
  }
  
  /**
   * Generate setup instructions for production OAuth apps
   */
  generateSetupInstructions(providers: string[]): string {
    const instructions: string[] = [
      '🚀 OAuth Provider Setup Instructions',
      '=====================================',
      '',
      'To use EasyAuth in production, you need to create OAuth applications with each provider:',
      ''
    ];
    
    providers.forEach(provider => {
      const providerInstructions = this.getProviderInstructions(provider);
      instructions.push(...providerInstructions);
      instructions.push('');
    });
    
    instructions.push(
      '📝 After creating your apps, configure them in your EasyAuth setup:',
      '',
      '```typescript',
      'const easyAuth = new EasyAuthClient({',
      '  baseUrl: "https://your-api.com",',
      '  credentials: {',
      ...providers.map(p => `    ${p}: { clientId: "your-${p}-client-id" },`),
      '  }',
      '});',
      '```',
      '',
      '🔗 Complete guide: https://docs.easyauth.dev/oauth-setup'
    );
    
    return instructions.join('\n');
  }
  
  /**
   * Check if current setup is using development credentials
   */
  isUsingDevelopmentCredentials(credentials: OAuthCredentials): boolean {
    return Object.entries(credentials).some(([provider, config]) => {
      const devConfig = DEVELOPMENT_CREDENTIALS[provider as keyof OAuthCredentials];
      if (!devConfig || !config) return false;
      
      // Check if any key matches development credentials
      return Object.entries(config).some(([key, value]) => 
        devConfig[key as keyof typeof devConfig] === value
      );
    });
  }
  
  /**
   * Validate production readiness of credentials
   */
  validateProductionReadiness(credentials: OAuthCredentials): {
    isReady: boolean;
    warnings: string[];
    missing: string[];
  } {
    const warnings: string[] = [];
    const missing: string[] = [];
    
    if (this.isUsingDevelopmentCredentials(credentials)) {
      warnings.push('Using development OAuth credentials - these only work on localhost');
    }
    
    // Check for common production requirements
    Object.entries(credentials).forEach(([provider, config]) => {
      if (!config) {
        missing.push(`${provider} credentials`);
        return;
      }
      
      switch (provider) {
        case 'google':
          if (!config.clientId?.includes('.apps.googleusercontent.com')) {
            warnings.push('Google client ID should end with .apps.googleusercontent.com');
          }
          break;
        case 'facebook':
          if (config.appId?.length !== 16) {
            warnings.push('Facebook App ID should be 16 digits');
          }
          break;
        case 'apple':
          if (!config.serviceId?.startsWith('com.')) {
            warnings.push('Apple Service ID should be a reverse domain (com.yourcompany.app)');
          }
          break;
        case 'azure-b2c':
          if (!config.tenantId?.includes('.onmicrosoft.com')) {
            warnings.push('Azure B2C tenant ID should include .onmicrosoft.com');
          }
          break;
      }
    });
    
    return {
      isReady: missing.length === 0 && warnings.length === 0,
      warnings,
      missing
    };
  }
  
  private getProxyCredentials(): OAuthCredentials {
    // When using proxy, return empty credentials since they're handled server-side
    return {};
  }
  
  private mergeCredentials(user: Partial<OAuthCredentials>, fallback: OAuthCredentials): OAuthCredentials {
    const merged: OAuthCredentials = {};
    
    // Merge each provider's credentials
    const allProviders = new Set([...Object.keys(user), ...Object.keys(fallback)]);
    
    allProviders.forEach(provider => {
      const providerKey = provider as keyof OAuthCredentials;
      const userConfig = user[providerKey];
      const fallbackConfig = fallback[providerKey];
      
      if (userConfig && Object.keys(userConfig).length > 0) {
        merged[providerKey] = userConfig;
      } else if (fallbackConfig) {
        merged[providerKey] = fallbackConfig;
      }
    });
    
    return merged;
  }
  
  private getProviderInstructions(provider: string): string[] {
    switch (provider) {
      case 'google':
        return [
          '🔵 Google OAuth Setup:',
          '1. Go to https://console.developers.google.com/',
          '2. Create a new project or select existing one',
          '3. Enable Google+ API',
          '4. Create OAuth 2.0 Client ID (Web application)',
          '5. Add your domain to authorized origins',
          '6. Add redirect URI: https://yourdomain.com/auth/google/callback'
        ];
      
      case 'facebook':
        return [
          '🔵 Facebook Login Setup:',
          '1. Go to https://developers.facebook.com/',
          '2. Create a new app or select existing one',
          '3. Add Facebook Login product',
          '4. Configure Valid OAuth Redirect URIs',
          '5. Add: https://yourdomain.com/auth/facebook/callback',
          '6. Set app to Live mode for production'
        ];
      
      case 'apple':
        return [
          '🍎 Apple Sign-In Setup:',
          '1. Go to https://developer.apple.com/account/',
          '2. Register a new Service ID',
          '3. Configure domains and subdomains',
          '4. Add return URL: https://yourdomain.com/auth/apple/callback',
          '5. Create and download private key for server-to-server auth'
        ];
      
      case 'azure-b2c':
        return [
          '🔷 Azure B2C Setup:',
          '1. Create Azure B2C tenant at https://portal.azure.com/',
          '2. Register a new application',
          '3. Configure authentication redirect URIs',
          '4. Add: https://yourdomain.com/auth/azure-b2c/callback',
          '5. Create user flows for sign-up and sign-in'
        ];
      
      default:
        return [`📋 ${provider} setup instructions not available`];
    }
  }
}

/**
 * Quick setup helper for new projects
 */
export function createQuickSetup(options: {
  providers: string[];
  isDevelopment?: boolean;
  useProxy?: boolean;
  proxyConfig?: EasyAuthProxyConfig;
}): {
  credentials: OAuthCredentials;
  setupInstructions: string;
  helper: CredentialHelper;
} {
  const helper = new CredentialHelper(options.isDevelopment, options.useProxy ? options.proxyConfig : undefined);
  const credentials = helper.getCredentials();
  const setupInstructions = helper.generateSetupInstructions(options.providers);
  
  return {
    credentials,
    setupInstructions,
    helper
  };
}