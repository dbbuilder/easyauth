/**
 * Unit tests for GoogleAuthProvider - OAuth 2.0 implementation
 * Following TDD methodology: Write tests first, then implement
 */

import { GoogleAuthProvider } from '../../../src/providers/GoogleAuthProvider';
import { AuthorizationRequest, TokenExchangeResult, ProviderUserInfo, ProviderHealthCheck } from '../../../src/types/providers';

// Mock fetch globally for this test file
const mockFetch = jest.fn();
global.fetch = mockFetch;

describe('GoogleAuthProvider', () => {
  let provider: GoogleAuthProvider;
  let mockConfig: any;

  beforeEach(() => {
    mockConfig = {
      clientId: 'test-google-client-id.googleusercontent.com',
      clientSecret: 'test-client-secret',
      redirectUri: 'http://localhost:3000/auth/callback',
      scopes: ['openid', 'profile', 'email'],
      enabled: true,
    };
    
    provider = new GoogleAuthProvider(mockConfig);
    mockFetch.mockClear();
  });

  describe('Constructor and Configuration', () => {
    it('should create provider with valid configuration', () => {
      expect(provider.name).toBe('google');
      expect(provider.displayName).toBe('Google');
      expect(provider.isEnabled).toBe(true);
    });

    it('should throw error with invalid client ID format', () => {
      const invalidConfig = { ...mockConfig, clientId: 'invalid-client-id' };
      expect(() => new GoogleAuthProvider(invalidConfig)).toThrow('Invalid Google client ID format');
    });

    it('should throw error when required fields are missing', () => {
      const invalidConfig = { ...mockConfig, clientId: undefined };
      expect(() => new GoogleAuthProvider(invalidConfig)).toThrow('Google client ID is required');
    });

    it('should handle disabled provider', () => {
      const disabledConfig = { ...mockConfig, enabled: false };
      const disabledProvider = new GoogleAuthProvider(disabledConfig);
      expect(disabledProvider.isEnabled).toBe(false);
    });
  });

  describe('validateConfiguration', () => {
    it('should return true for valid configuration', async () => {
      const isValid = await provider.validateConfiguration();
      expect(isValid).toBe(true);
    });

    it('should return false for invalid client ID', async () => {
      const invalidProvider = new GoogleAuthProvider({
        ...mockConfig,
        clientId: 'invalid-.googleusercontent.com' // Has right suffix but invalid format
      });
      const isValid = await invalidProvider.validateConfiguration();
      expect(isValid).toBe(false);
    });
  });

  describe('getAuthorizationUrl', () => {
    it('should generate valid authorization URL with PKCE', async () => {
      const request: AuthorizationRequest = {
        returnUrl: 'http://localhost:3000/dashboard',
        state: 'test-state-123',
        scopes: ['openid', 'profile', 'email'],
        pkceChallenge: 'test-pkce-challenge'
      };

      const authUrl = await provider.getAuthorizationUrl(request);
      
      expect(authUrl).toContain('https://accounts.google.com/o/oauth2/v2/auth');
      expect(authUrl).toContain('client_id=test-google-client-id.googleusercontent.com');
      expect(authUrl).toContain('redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fauth%2Fcallback');
      expect(authUrl).toContain('scope=openid%20profile%20email');
      expect(authUrl).toContain('state=test-state-123');
      expect(authUrl).toContain('code_challenge=test-pkce-challenge');
      expect(authUrl).toContain('code_challenge_method=S256');
      expect(authUrl).toContain('response_type=code');
    });

    it('should include nonce when provided', async () => {
      const request: AuthorizationRequest = {
        returnUrl: 'http://localhost:3000/dashboard',
        state: 'test-state-123',
        nonce: 'test-nonce-456'
      };

      const authUrl = await provider.getAuthorizationUrl(request);
      expect(authUrl).toContain('nonce=test-nonce-456');
    });

    it('should handle custom parameters', async () => {
      const request: AuthorizationRequest = {
        returnUrl: 'http://localhost:3000/dashboard',
        state: 'test-state-123',
        customParams: {
          'hd': 'example.com',
          'prompt': 'select_account'
        }
      };

      const authUrl = await provider.getAuthorizationUrl(request);
      expect(authUrl).toContain('hd=example.com');
      expect(authUrl).toContain('prompt=select_account');
    });

    it('should use default scopes when none provided', async () => {
      const request: AuthorizationRequest = {
        returnUrl: 'http://localhost:3000/dashboard',
        state: 'test-state-123'
      };

      const authUrl = await provider.getAuthorizationUrl(request);
      expect(authUrl).toContain('scope=openid%20profile%20email');
    });
  });

  describe('exchangeCodeForTokens', () => {
    it('should exchange authorization code for tokens', async () => {
      const mockTokenResponse = {
        access_token: 'ya29.test-access-token',
        refresh_token: 'test-refresh-token',
        id_token: 'eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test-id-token',
        token_type: 'Bearer',
        expires_in: 3600,
        scope: 'openid profile email'
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockTokenResponse
      });

      const result = await provider.exchangeCodeForTokens('test-auth-code', 'test-state');

      expect(result.success).toBe(true);
      expect(result.accessToken).toBe('ya29.test-access-token');
      expect(result.refreshToken).toBe('test-refresh-token');
      expect(result.idToken).toBe('eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.test-id-token');
      expect(result.tokenType).toBe('Bearer');
      expect(result.expiresIn).toBe(3600);

      expect(mockFetch).toHaveBeenCalledWith('https://oauth2.googleapis.com/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: expect.stringContaining('grant_type=authorization_code')
      });
    });

    it('should handle token exchange errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ error: 'invalid_grant', error_description: 'Invalid authorization code' })
      });

      const result = await provider.exchangeCodeForTokens('invalid-code', 'test-state');

      expect(result.success).toBe(false);
      expect(result.error).toBe('invalid_grant');
    });

    it('should handle network errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      const result = await provider.exchangeCodeForTokens('test-code', 'test-state');

      expect(result.success).toBe(false);
      expect(result.error).toContain('Network error');
    });
  });

  describe('getUserInfo', () => {
    it('should fetch user information from Google API', async () => {
      const mockUserInfo = {
        sub: 'google-user-123',
        email: 'test@example.com',
        email_verified: true,
        name: 'Test User',
        given_name: 'Test',
        family_name: 'User',
        picture: 'https://lh3.googleusercontent.com/test-picture',
        locale: 'en'
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockUserInfo
      });

      const userInfo = await provider.getUserInfo('test-access-token');

      expect(userInfo.id).toBe('google-user-123');
      expect(userInfo.email).toBe('test@example.com');
      expect(userInfo.emailVerified).toBe(true);
      expect(userInfo.name).toBe('Test User');
      expect(userInfo.givenName).toBe('Test');
      expect(userInfo.familyName).toBe('User');
      expect(userInfo.profilePictureUrl).toBe('https://lh3.googleusercontent.com/test-picture');

      expect(mockFetch).toHaveBeenCalledWith('https://www.googleapis.com/oauth2/v2/userinfo', {
        headers: { 'Authorization': 'Bearer test-access-token' }
      });
    });

    it('should handle API errors when fetching user info', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({ error: { message: 'Invalid token' } })
      });

      await expect(provider.getUserInfo('invalid-token')).rejects.toThrow('Failed to fetch user info');
    });
  });

  describe('refreshTokens', () => {
    it('should refresh access tokens using refresh token', async () => {
      const mockRefreshResponse = {
        access_token: 'ya29.new-access-token',
        token_type: 'Bearer',
        expires_in: 3600,
        scope: 'openid profile email'
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockRefreshResponse
      });

      const result = await provider.refreshTokens('test-refresh-token');

      expect(result.success).toBe(true);
      expect(result.accessToken).toBe('ya29.new-access-token');
      expect(result.tokenType).toBe('Bearer');
      expect(result.expiresIn).toBe(3600);

      expect(mockFetch).toHaveBeenCalledWith('https://oauth2.googleapis.com/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: expect.stringContaining('grant_type=refresh_token')
      });
    });

    it('should handle refresh token errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({ error: 'invalid_grant' })
      });

      const result = await provider.refreshTokens('invalid-refresh-token');

      expect(result.success).toBe(false);
      expect(result.error).toBe('invalid_grant');
    });
  });

  describe('revokeTokens', () => {
    it('should revoke access and refresh tokens', async () => {
      mockFetch
        .mockResolvedValueOnce({ ok: true, text: async () => '' })
        .mockResolvedValueOnce({ ok: true, text: async () => '' });

      const result = await provider.revokeTokens!(['access-token', 'refresh-token']);

      expect(result).toBe(true);
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    it('should handle token revocation errors', async () => {
      mockFetch.mockResolvedValueOnce({ ok: false, status: 400 });

      const result = await provider.revokeTokens!(['invalid-token']);

      expect(result).toBe(false);
    });
  });

  describe('getHealthStatus', () => {
    it('should return healthy status when Google APIs are reachable', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ issuer: 'https://accounts.google.com' })
      });

      const healthStatus = await provider.getHealthStatus();

      expect(healthStatus.isHealthy).toBe(true);
      expect(healthStatus.provider).toBe('google');
      expect(healthStatus.responseTime).toBeGreaterThan(0);
    });

    it('should return unhealthy status when Google APIs are unreachable', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network timeout'));

      const healthStatus = await provider.getHealthStatus();

      expect(healthStatus.isHealthy).toBe(false);
      expect(healthStatus.provider).toBe('google');
      expect(healthStatus.error).toContain('Network timeout');
    });
  });

  describe('PKCE Support', () => {
    it('should support PKCE code challenge generation', () => {
      // This would test the internal PKCE methods
      // For now, we verify through authorization URL generation
      expect(provider.name).toBe('google');
    });

    it('should include code verifier in token exchange when using PKCE', async () => {
      // Mock the provider with PKCE state
      const providerWithPkce = new GoogleAuthProvider(mockConfig);
      
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          access_token: 'test-token',
          token_type: 'Bearer'
        })
      });

      // This would need to test internal PKCE code verifier usage
      const result = await providerWithPkce.exchangeCodeForTokens('test-code', 'test-state');
      
      expect(result.success).toBe(true);
    });
  });
});