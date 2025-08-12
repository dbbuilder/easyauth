/**
 * TDD Tests for EasyAuthClient
 * These tests define the expected behavior before implementation
 */

import { EasyAuthClient } from './EasyAuthClient';
import { EasyAuthConfig, LoginOptions, AuthProvider } from '../types';

describe('EasyAuthClient', () => {
  let client: EasyAuthClient;
  let mockConfig: EasyAuthConfig;

  beforeEach(() => {
    mockConfig = {
      baseUrl: 'https://api.example.com',
      enableLogging: false
    };
    client = new EasyAuthClient(mockConfig);
  });

  describe('constructor', () => {
    it('should initialize with valid configuration', () => {
      expect(client.config.baseUrl).toBe('https://api.example.com');
      expect(client.config.apiVersion).toBe('1.0');
      expect(client.config.timeout).toBe(30000);
      expect(client.config.retryAttempts).toBe(3);
      expect(client.config.enableLogging).toBe(false);
    });

    it('should throw error for invalid baseUrl', () => {
      const invalidConfig = { baseUrl: '' };
      expect(() => new EasyAuthClient(invalidConfig)).toThrow('baseUrl is required');
    });

    it('should throw error for non-HTTP baseUrl', () => {
      const invalidConfig = { baseUrl: 'ftp://example.com' };
      expect(() => new EasyAuthClient(invalidConfig)).toThrow('baseUrl must be a valid HTTP/HTTPS URL');
    });

    it('should initialize with empty session by default', () => {
      const session = client.getSession();
      expect(session.isAuthenticated).toBe(false);
      expect(session.user).toBeNull();
    });
  });

  describe('session management', () => {
    it('should return current session state', () => {
      const session = client.getSession();
      expect(session).toHaveProperty('isAuthenticated');
      expect(session).toHaveProperty('user');
    });

    it('should indicate not authenticated by default', () => {
      expect(client.isAuthenticated()).toBe(false);
    });

    it('should return null user when not authenticated', () => {
      expect(client.getUser()).toBeNull();
    });

    it('should return null token when not authenticated', () => {
      expect(client.getToken()).toBeNull();
    });
  });

  describe('token management', () => {
    it('should allow setting token manually', () => {
      const testToken = 'test-jwt-token';
      client.setToken(testToken);
      
      const session = client.getSession();
      expect(session.token).toBe(testToken);
    });

    it('should clear token and session', () => {
      client.setToken('test-token');
      client.clearToken();
      
      expect(client.getToken()).toBeNull();
      expect(client.isAuthenticated()).toBe(false);
      expect(client.getUser()).toBeNull();
    });
  });

  describe('event handling', () => {
    it('should allow subscribing to events', () => {
      const callback = jest.fn();
      expect(() => client.on('login', callback)).not.toThrow();
    });

    it('should allow unsubscribing from events', () => {
      const callback = jest.fn();
      client.on('login', callback);
      expect(() => client.off('login', callback)).not.toThrow();
    });
  });

  describe('login flow implementation', () => {
    it('should handle network errors gracefully', async () => {
      const options: LoginOptions = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com'
      };

      const result = await client.login(options);
      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(result.error?.code).toBe('UNKNOWN_ERROR');
    });

    it('should accept LoginOptions and return AuthResult', async () => {
      const options: LoginOptions = {
        provider: 'google' as AuthProvider,
        returnUrl: 'https://app.example.com/dashboard'
      };

      const result = await client.login(options);
      expect(result).toHaveProperty('success');
      expect(result).toHaveProperty('error');
      expect(result.success).toBe(false); // Will fail due to no mock
    });
  });

  describe('logout flow implementation', () => {
    it('should complete logout even with network errors', async () => {
      // Should not throw, but complete gracefully
      await expect(client.logout()).resolves.not.toThrow();
      expect(client.isAuthenticated()).toBe(false);
    });
  });

  describe('refresh flow implementation', () => {
    it('should handle refresh errors gracefully', async () => {
      const result = await client.refresh();
      expect(result.success).toBe(false);
      expect(result.error).toBeDefined();
      expect(result.error?.code).toBe('UNKNOWN_ERROR');
    });
  });
});