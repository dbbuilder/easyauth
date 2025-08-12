/**
 * Unit tests for EasyAuthClient - Core authentication client
 * Following TDD methodology: Write tests first, then implement
 */

import { EasyAuthClient } from '../../src/auth/EasyAuthClient';
import { AuthProvider, AuthConfig, LoginRequest } from '../../src/types';

describe('EasyAuthClient', () => {
  let client: EasyAuthClient;
  let mockConfig: AuthConfig;

  beforeEach(() => {
    mockConfig = {
      apiBaseUrl: 'https://api.example.com',
      providers: {
        google: {
          clientId: 'google-client-id',
          enabled: true
        },
        facebook: {
          clientId: 'facebook-client-id',
          enabled: true
        }
      },
      defaultProvider: 'google'
    };
    
    client = new EasyAuthClient(mockConfig);
  });

  describe('Constructor', () => {
    it('should create client with valid configuration', () => {
      expect(client).toBeDefined();
      expect(client.isInitialized).toBe(true);
    });

    it('should throw error for invalid configuration', () => {
      expect(() => new EasyAuthClient(null as any)).toThrow('Configuration is required');
      expect(() => new EasyAuthClient({} as any)).toThrow('API base URL is required');
    });

    it('should validate provider configurations', () => {
      const invalidConfig = {
        apiBaseUrl: 'https://api.example.com',
        providers: {
          google: {
            clientId: '', // Invalid empty client ID
            enabled: true
          }
        }
      };
      
      expect(() => new EasyAuthClient(invalidConfig)).toThrow('Google provider client ID is required');
    });
  });

  describe('getAvailableProviders', () => {
    it('should return all enabled providers', async () => {
      const providers = await client.getAvailableProviders();
      
      expect(providers).toHaveLength(2);
      expect(providers.map(p => p.name)).toContain('google');
      expect(providers.map(p => p.name)).toContain('facebook');
    });

    it('should exclude disabled providers', async () => {
      const configWithDisabled = {
        ...mockConfig,
        providers: {
          ...mockConfig.providers,
          facebook: { ...mockConfig.providers.facebook, enabled: false }
        }
      };
      
      const clientWithDisabled = new EasyAuthClient(configWithDisabled);
      const providers = await clientWithDisabled.getAvailableProviders();
      
      expect(providers).toHaveLength(1);
      expect(providers[0].name).toBe('google');
    });
  });

  describe('initiateLogin', () => {
    it('should initiate login for valid provider', async () => {
      const request: LoginRequest = {
        provider: 'google',
        returnUrl: 'https://app.example.com/callback'
      };
      
      const result = await client.initiateLogin(request);
      
      expect(result.success).toBe(true);
      expect(result.authUrl).toContain('accounts.google.com');
      expect(result.authUrl).toContain('client_id=google-client-id');
      expect(result.state).toBeDefined();
    });

    it('should use default provider when none specified', async () => {
      const request: LoginRequest = {
        returnUrl: 'https://app.example.com/callback'
      };
      
      const result = await client.initiateLogin(request);
      
      expect(result.success).toBe(true);
      expect(result.authUrl).toContain('accounts.google.com'); // Default provider is Google
    });

    it('should return error for invalid provider', async () => {
      const request: LoginRequest = {
        provider: 'invalid-provider',
        returnUrl: 'https://app.example.com/callback'
      };
      
      const result = await client.initiateLogin(request);
      
      expect(result.success).toBe(false);
      expect(result.error).toBe('Provider "invalid-provider" is not available');
    });

    it('should validate return URL format', async () => {
      const request: LoginRequest = {
        provider: 'google',
        returnUrl: 'invalid-url'
      };
      
      const result = await client.initiateLogin(request);
      
      expect(result.success).toBe(false);
      expect(result.error).toBe('Invalid return URL format');
    });

    it('should include CSRF state parameter', async () => {
      const request: LoginRequest = {
        provider: 'google',
        returnUrl: 'https://app.example.com/callback'
      };
      
      const result = await client.initiateLogin(request);
      
      expect(result.state).toBeDefined();
      expect(result.state).toMatch(/^[a-zA-Z0-9_-]+$/); // Base64 URL-safe characters
      expect(result.state.length).toBeGreaterThan(16); // Sufficient entropy
    });
  });

  describe('handleCallback', () => {
    it('should handle successful callback', async () => {
      // First initiate login to get a valid state
      const loginRequest = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com/callback'
      };
      
      const loginResult = await client.initiateLogin(loginRequest);
      expect(loginResult.success).toBe(true);
      expect(loginResult.state).toBeDefined();
      
      // Now handle callback with the valid state
      const callbackData = {
        code: 'auth_code_123',
        state: loginResult.state!,
        provider: 'google' as AuthProvider
      };
      
      const result = await client.handleCallback(callbackData);
      
      expect(result.success).toBe(true);
      expect(result.session).toBeDefined();
      expect(result.user).toBeDefined();
      expect(result.tokens).toBeDefined();
    });

    it('should validate state parameter', async () => {
      const callbackData = {
        code: 'auth_code_123',
        state: 'invalid_state',
        provider: 'google'
      };
      
      const result = await client.handleCallback(callbackData);
      
      expect(result.success).toBe(false);
      expect(result.error).toBe('Invalid state parameter - possible CSRF attack');
    });

    it('should handle provider errors', async () => {
      const callbackData = {
        error: 'access_denied',
        error_description: 'User denied access',
        state: 'valid_state',
        provider: 'google'
      };
      
      const result = await client.handleCallback(callbackData);
      
      expect(result.success).toBe(false);
      expect(result.error).toBe('User denied access');
    });
  });

  describe('getCurrentSession', () => {
    it('should return current session if valid', async () => {
      // First initiate login to get a valid state
      const loginRequest = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com/callback'
      };
      
      const loginResult = await client.initiateLogin(loginRequest);
      expect(loginResult.success).toBe(true);
      
      // Handle callback to create session
      await client.handleCallback({
        code: 'auth_code_123',
        state: loginResult.state!,
        provider: 'google' as AuthProvider
      });
      
      const session = await client.getCurrentSession();
      
      expect(session).toBeDefined();
      expect(session?.isValid).toBe(true);
      expect(session?.user).toBeDefined();
    });

    it('should return null when no session exists', async () => {
      const session = await client.getCurrentSession();
      expect(session).toBeNull();
    });

    it('should return null for expired sessions', async () => {
      // Mock expired session
      const expiredSession = {
        sessionId: 'expired-session',
        user: { id: '1', email: 'test@example.com' },
        isValid: false,
        expiresAt: new Date(Date.now() - 1000) // Expired 1 second ago
      };
      
      // Simulate stored expired session
      (client as any).currentSession = expiredSession;
      
      const session = await client.getCurrentSession();
      expect(session).toBeNull();
    });
  });

  describe('signOut', () => {
    it('should sign out current user', async () => {
      // First initiate login to get a valid state
      const loginRequest = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com/callback'
      };
      
      const loginResult = await client.initiateLogin(loginRequest);
      expect(loginResult.success).toBe(true);
      
      // Handle callback to create session
      await client.handleCallback({
        code: 'auth_code_123',
        state: loginResult.state!,
        provider: 'google' as AuthProvider
      });
      
      const result = await client.signOut();
      
      expect(result).toBe(true);
      
      const session = await client.getCurrentSession();
      expect(session).toBeNull();
    });

    it('should return true even when no session exists', async () => {
      const result = await client.signOut();
      expect(result).toBe(true);
    });
  });

  describe('refreshSession', () => {
    it('should refresh valid session', async () => {
      // First create a valid session through proper flow
      const loginRequest = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com/callback'
      };
      
      const loginResult = await client.initiateLogin(loginRequest);
      expect(loginResult.success).toBe(true);
      
      // Handle callback to create session with refresh token
      const callbackResult = await client.handleCallback({
        code: 'auth_code_123',
        state: loginResult.state!,
        provider: 'google' as AuthProvider
      });
      
      expect(callbackResult.success).toBe(true);
      expect(callbackResult.session?.refreshToken).toBeDefined();
      
      // Now refresh the session
      const result = await client.refreshSession();
      
      expect(result.success).toBe(true);
      expect(result.session).toBeDefined();
    });

    it('should return error when no refresh token available', async () => {
      const sessionWithoutRefresh = {
        sessionId: 'test-session',
        user: { id: '1', email: 'test@example.com' },
        isValid: true,
        expiresAt: new Date(Date.now() + 3600000)
        // No refresh token
      };
      
      (client as any).currentSession = sessionWithoutRefresh;
      
      const result = await client.refreshSession();
      
      expect(result.success).toBe(false);
      expect(result.error).toBe('No refresh token available');
    });
  });

  describe('getProviderInfo', () => {
    it('should return provider information', async () => {
      const providerInfo = await client.getProviderInfo('google');
      
      expect(providerInfo).toBeDefined();
      expect(providerInfo.name).toBe('google');
      expect(providerInfo.displayName).toBe('Google');
      expect(providerInfo.capabilities).toContain('oauth2');
    });

    it('should return null for unknown provider', async () => {
      const providerInfo = await client.getProviderInfo('unknown');
      expect(providerInfo).toBeNull();
    });
  });
});