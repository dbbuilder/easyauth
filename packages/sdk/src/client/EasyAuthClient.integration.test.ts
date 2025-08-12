/**
 * Integration tests for EasyAuthClient authentication flows
 * TDD RED phase - defining expected behavior for real authentication
 */

import { EasyAuthClient } from './EasyAuthClient';
import { EasyAuthConfig, LoginOptions, AuthProvider, UserProfile } from '../types';

describe('EasyAuthClient Integration Tests', () => {
  let client: EasyAuthClient;
  let mockConfig: EasyAuthConfig;

  beforeEach(() => {
    mockConfig = {
      baseUrl: 'https://api.example.com',
      enableLogging: false
    };
    
    client = new EasyAuthClient(mockConfig);
  });

  describe('Real login flow implementation', () => {
    it('should successfully login with Google and return user data', async () => {
      const mockUser: UserProfile = {
        id: 'user123',
        email: 'test@example.com',
        name: 'Test User',
        provider: 'google' as AuthProvider
      };

      // Mock successful API response
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: true,
          user: mockUser,
          token: 'jwt-token-123',
          expiresAt: new Date(Date.now() + 3600000).toISOString() // 1 hour
        })
      });

      const options: LoginOptions = {
        provider: 'google',
        returnUrl: 'https://app.example.com'
      };

      const result = await client.login(options);

      expect(result.success).toBe(true);
      expect(result.user).toEqual(mockUser);
      expect(result.token).toBe('jwt-token-123');
      expect(client.isAuthenticated()).toBe(true);
      expect(client.getUser()).toEqual(mockUser);
      expect(client.getToken()).toBe('jwt-token-123');
    });

    it('should handle login failure with proper error response', async () => {
      // Mock failed API response
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: false,
          error: {
            code: 'INVALID_CREDENTIALS',
            message: 'Invalid authentication credentials'
          }
        })
      });

      const options: LoginOptions = {
        provider: 'google',
        returnUrl: 'https://app.example.com'
      };

      const result = await client.login(options);

      expect(result.success).toBe(false);
      expect(result.error?.code).toBe('INVALID_CREDENTIALS');
      expect(result.error?.message).toBe('Invalid authentication credentials');
      expect(client.isAuthenticated()).toBe(false);
    });

    it('should emit login event on successful authentication', async () => {
      const mockUser: UserProfile = {
        id: 'user123',
        email: 'test@example.com', 
        name: 'Test User',
        provider: 'google' as AuthProvider
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: true,
          user: mockUser,
          token: 'jwt-token-123'
        })
      });

      const loginCallback = jest.fn();
      client.on('login', loginCallback);

      await client.login({ provider: 'google' });

      expect(loginCallback).toHaveBeenCalledWith(
        expect.objectContaining({
          user: mockUser,
          token: 'jwt-token-123'
        })
      );
    });

    it('should emit error event on login failure', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const errorCallback = jest.fn();
      client.on('error', errorCallback);

      await client.login({ provider: 'google' });

      expect(errorCallback).toHaveBeenCalledWith(
        expect.objectContaining({
          error: expect.objectContaining({
            message: 'Network error'
          })
        })
      );
    });
  });

  describe('Token refresh implementation', () => {
    it('should successfully refresh expired token', async () => {
      // Set up expired session by directly modifying the client's session
      client.setToken('old-token');
      
      // Access the private session to set expired state (for testing purposes)
      const clientSession = (client as any).session;
      clientSession.expiresAt = new Date(Date.now() - 1000); // Expired 1 second ago
      clientSession.refreshToken = 'refresh-token-123';
      clientSession.isAuthenticated = true; // Ensure it's marked as authenticated

      const newUser: UserProfile = {
        id: 'user123',
        email: 'test@example.com',
        name: 'Test User',  
        provider: 'google' as AuthProvider
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: true,
          user: newUser,
          token: 'new-jwt-token-456',
          expiresAt: new Date(Date.now() + 3600000).toISOString()
        })
      });

      const result = await client.refresh();

      expect(result.success).toBe(true);
      expect(result.token).toBe('new-jwt-token-456');
      expect(client.getToken()).toBe('new-jwt-token-456');
      expect(client.isAuthenticated()).toBe(true);
    });

    it('should emit token_refresh event on successful refresh', async () => {
      client.setToken('old-token');
      
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: true,
          token: 'new-token-456'
        })
      });

      const refreshCallback = jest.fn();
      client.on('token_refresh', refreshCallback);

      await client.refresh();

      expect(refreshCallback).toHaveBeenCalledWith(
        expect.objectContaining({
          token: 'new-token-456'
        })
      );
    });
  });

  describe('Logout implementation', () => {
    it('should successfully logout and clear session', async () => {
      // Setup authenticated session
      client.setToken('jwt-token-123');

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ success: true })
      });

      await client.logout();

      expect(client.isAuthenticated()).toBe(false);
      expect(client.getUser()).toBeNull();
      expect(client.getToken()).toBeNull();
    });

    it('should emit logout event', async () => {
      client.setToken('jwt-token-123');
      
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ success: true })
      });

      const logoutCallback = jest.fn();
      client.on('logout', logoutCallback);

      await client.logout();

      expect(logoutCallback).toHaveBeenCalled();
    });
  });

  describe('Session persistence', () => {
    it('should persist session to storage on successful login', async () => {
      const mockUser: UserProfile = {
        id: 'user123',
        email: 'test@example.com',
        name: 'Test User',
        provider: 'google' as AuthProvider
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          success: true,
          user: mockUser,
          token: 'jwt-token-123'
        })
      });

      await client.login({ provider: 'google' });

      // Verify localStorage was called
      expect(window.localStorage.setItem).toHaveBeenCalledWith(
        'easyauth_session',
        expect.stringContaining('jwt-token-123')
      );
    });

    it('should restore session from storage on initialization', () => {
      const storedSession = {
        isAuthenticated: true,
        user: { id: 'user123', name: 'Test User', provider: 'google' },
        token: 'stored-token-123',
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      (window.localStorage.getItem as jest.Mock).mockReturnValueOnce(
        JSON.stringify(storedSession)
      );

      const newClient = new EasyAuthClient(mockConfig);

      expect(newClient.isAuthenticated()).toBe(true);
      expect(newClient.getToken()).toBe('stored-token-123');
    });
  });
});