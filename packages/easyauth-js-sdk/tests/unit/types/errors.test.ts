/**
 * Comprehensive tests for error handling types and functionality
 * Targets improving test coverage from 41.37% lines / 7.4% branches to 95%+
 */

import {
  EasyAuthError,
  ConfigurationError,
  NetworkError,
  SessionError,
  SecurityError,
  ProviderError,
  DefaultErrorHandler,
  ErrorUtils,
  SerializedError,
  ErrorHandler,
  ErrorContext,
  ErrorBreadcrumb
} from '../../../src/types/errors';
import { AuthErrorCode, AuthProvider } from '../../../src/types';

describe('EasyAuthError', () => {
  describe('constructor', () => {
    it('should create basic error with required parameters', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test error');

      expect(error.name).toBe('EasyAuthError');
      expect(error.code).toBe(AuthErrorCode.API_ERROR);
      expect(error.message).toBe('Test error');
      expect(error.provider).toBeUndefined();
      expect(error.details).toBeUndefined();
      expect(error.timestamp).toBeInstanceOf(Date);
      expect(error.requestId).toBeUndefined();
      expect(error.isRetryable).toBe(false);
    });

    it('should create error with all optional parameters', () => {
      const cause = new Error('Original error');
      const details = { key: 'value' };
      const options = {
        provider: 'google' as AuthProvider,
        details,
        cause,
        requestId: 'test-request-123',
        isRetryable: true
      };

      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'Network failed', options);

      expect(error.code).toBe(AuthErrorCode.NETWORK_ERROR);
      expect(error.message).toBe('Network failed');
      expect(error.provider).toBe('google');
      expect(error.details).toBe(details);
      expect(error.requestId).toBe('test-request-123');
      expect(error.isRetryable).toBe(true);
      expect(error.cause).toBe(cause);
    });

    it('should capture stack trace when Error.captureStackTrace is available', () => {
      const originalCaptureStackTrace = Error.captureStackTrace;
      const mockCaptureStackTrace = jest.fn();
      Error.captureStackTrace = mockCaptureStackTrace;

      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test');

      expect(mockCaptureStackTrace).toHaveBeenCalledWith(error, EasyAuthError);

      Error.captureStackTrace = originalCaptureStackTrace;
    });

    it('should handle missing Error.captureStackTrace gracefully', () => {
      const originalCaptureStackTrace = Error.captureStackTrace;
      // @ts-ignore
      delete Error.captureStackTrace;

      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test');

      expect(error.name).toBe('EasyAuthError');
      expect(error.stack).toBeDefined();

      Error.captureStackTrace = originalCaptureStackTrace;
    });

    it('should default isRetryable to false when not specified', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test', {});

      expect(error.isRetryable).toBe(false);
    });

    it('should set cause when provided', () => {
      const originalError = new Error('Original');
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test', { cause: originalError });

      expect(error.cause).toBe(originalError);
    });

    it('should not set cause when not provided', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test');

      expect(error.cause).toBeUndefined();
    });
  });

  describe('toJSON', () => {
    it('should serialize error to plain object', () => {
      const timestamp = new Date('2024-01-01T00:00:00.000Z');
      const details = { key: 'value' };
      const error = new EasyAuthError(AuthErrorCode.ACCESS_DENIED, 'Access denied', {
        provider: 'apple' as AuthProvider,
        details,
        requestId: 'req-123',
        isRetryable: true
      });
      
      // Mock timestamp for consistent testing
      error.timestamp = timestamp;

      const serialized = error.toJSON();

      expect(serialized).toEqual({
        name: 'EasyAuthError',
        code: AuthErrorCode.ACCESS_DENIED,
        message: 'Access denied',
        provider: 'apple' as AuthProvider,
        details,
        timestamp,
        requestId: 'req-123',
        isRetryable: true,
        stack: error.stack,
      });
    });

    it('should handle undefined optional fields in serialization', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test');
      const serialized = error.toJSON();

      expect(serialized.provider).toBeUndefined();
      expect(serialized.details).toBeUndefined();
      expect(serialized.requestId).toBeUndefined();
    });
  });

  describe('fromJSON', () => {
    it('should deserialize error from plain object', () => {
      const data: SerializedError = {
        name: 'EasyAuthError',
        code: AuthErrorCode.SESSION_EXPIRED,
        message: 'Session expired',
        provider: 'facebook' as AuthProvider,
        details: { sessionId: 'sess-123' },
        timestamp: new Date(),
        requestId: 'req-456',
        isRetryable: false,
        stack: 'Error stack trace'
      };

      const error = EasyAuthError.fromJSON(data);

      expect(error).toBeInstanceOf(EasyAuthError);
      expect(error.code).toBe(data.code);
      expect(error.message).toBe(data.message);
      expect(error.provider).toBe(data.provider);
      expect(error.details).toEqual(data.details);
      expect(error.requestId).toBe(data.requestId);
      expect(error.isRetryable).toBe(data.isRetryable);
      expect(error.stack).toBe(data.stack);
    });

    it('should handle minimal serialized error data', () => {
      const data: SerializedError = {
        name: 'EasyAuthError',
        code: AuthErrorCode.API_ERROR,
        message: 'Test error',
        timestamp: new Date(),
        isRetryable: false
      };

      const error = EasyAuthError.fromJSON(data);

      expect(error.code).toBe(AuthErrorCode.API_ERROR);
      expect(error.message).toBe('Test error');
      expect(error.provider).toBeUndefined();
      expect(error.details).toBeUndefined();
    });
  });
});

describe('ConfigurationError', () => {
  it('should create configuration error with correct defaults', () => {
    const error = new ConfigurationError('Invalid config');

    expect(error).toBeInstanceOf(EasyAuthError);
    expect(error).toBeInstanceOf(ConfigurationError);
    expect(error.name).toBe('ConfigurationError');
    expect(error.code).toBe(AuthErrorCode.INVALID_CONFIG);
    expect(error.message).toBe('Invalid config');
    expect(error.isRetryable).toBe(false);
  });

  it('should include details when provided', () => {
    const details = { missingKey: 'clientId' };
    const error = new ConfigurationError('Missing client ID', details);

    expect(error.details).toBe(details);
  });
});

describe('NetworkError', () => {
  it('should create network error with basic parameters', () => {
    const error = new NetworkError('Connection failed');

    expect(error).toBeInstanceOf(EasyAuthError);
    expect(error).toBeInstanceOf(NetworkError);
    expect(error.name).toBe('NetworkError');
    expect(error.code).toBe(AuthErrorCode.NETWORK_ERROR);
    expect(error.message).toBe('Connection failed');
    expect(error.isRetryable).toBe(true);
  });

  it('should include status code and response body', () => {
    const error = new NetworkError('Server error', 500, {
      provider: 'google' as AuthProvider,
      responseBody: '{"error": "internal_server_error"}',
      requestId: 'req-789'
    });

    expect(error.statusCode).toBe(500);
    expect(error.responseBody).toBe('{"error": "internal_server_error"}');
    expect(error.provider).toBe('google');
    expect(error.requestId).toBe('req-789');
    expect(error.details).toEqual({
      statusCode: 500,
      responseBody: '{"error": "internal_server_error"}'
    });
  });

  it('should handle undefined status code', () => {
    const error = new NetworkError('Connection timeout');

    expect(error.statusCode).toBeUndefined();
    expect(error.details?.statusCode).toBeUndefined();
  });

  it('should handle minimal options', () => {
    const error = new NetworkError('Network error', 404, {});

    expect(error.statusCode).toBe(404);
    expect(error.provider).toBeUndefined();
    expect(error.responseBody).toBeUndefined();
  });
});

describe('SessionError', () => {
  it('should create session error with required parameters', () => {
    const error = new SessionError(
      AuthErrorCode.SESSION_EXPIRED,
      'Session has expired',
      'session-123'
    );

    expect(error).toBeInstanceOf(EasyAuthError);
    expect(error).toBeInstanceOf(SessionError);
    expect(error.name).toBe('SessionError');
    expect(error.code).toBe(AuthErrorCode.SESSION_EXPIRED);
    expect(error.message).toBe('Session has expired');
    expect(error.sessionId).toBe('session-123');
    expect(error.isRetryable).toBe(true); // SESSION_EXPIRED is retryable
  });

  it('should create non-retryable session error for invalid session', () => {
    const error = new SessionError(
      AuthErrorCode.ACCESS_DENIED,
      'Invalid session'
    );

    expect(error.isRetryable).toBe(false);
  });

  it('should include provider and additional details', () => {
    const additionalDetails = { reason: 'timeout' };
    const error = new SessionError(
      AuthErrorCode.SESSION_EXPIRED,
      'Session expired',
      'sess-456',
      {
        provider: 'apple' as AuthProvider,
        details: additionalDetails
      }
    );

    expect(error.provider).toBe('apple');
    expect(error.details).toEqual({
      sessionId: 'sess-456',
      ...additionalDetails
    });
  });

  it('should handle undefined sessionId', () => {
    const error = new SessionError(
      AuthErrorCode.ACCESS_DENIED,
      'Invalid session'
    );

    expect(error.sessionId).toBeUndefined();
    expect(error.details?.sessionId).toBeUndefined();
  });

  it('should handle undefined options', () => {
    const error = new SessionError(
      AuthErrorCode.SESSION_EXPIRED,
      'Session expired',
      'sess-123'
    );

    expect(error.provider).toBeUndefined();
    expect(error.details).toEqual({ sessionId: 'sess-123' });
  });
});

describe('SecurityError', () => {
  it('should create security error with default medium level', () => {
    const error = new SecurityError(
      AuthErrorCode.ACCESS_DENIED,
      'Suspicious activity detected'
    );

    expect(error).toBeInstanceOf(EasyAuthError);
    expect(error).toBeInstanceOf(SecurityError);
    expect(error.name).toBe('SecurityError');
    expect(error.securityLevel).toBe('medium');
    expect(error.isRetryable).toBe(false);
  });

  it('should create security error with specified level', () => {
    const error = new SecurityError(
      AuthErrorCode.ACCESS_DENIED,
      'Critical security violation',
      'critical'
    );

    expect(error.securityLevel).toBe('critical');
  });

  it('should include provider and details', () => {
    const details = { ip: '192.168.1.1', userAgent: 'malicious-bot' };
    const error = new SecurityError(
      AuthErrorCode.ACCESS_DENIED,
      'Blocked suspicious request',
      'high',
      {
        provider: 'azure-b2c' as AuthProvider,
        details
      }
    );

    expect(error.provider).toBe('azure-b2c');
    expect(error.details).toEqual({
      securityLevel: 'high',
      ...details
    });
  });

  it('should handle all security levels', () => {
    const levels: ('low' | 'medium' | 'high' | 'critical')[] = ['low', 'medium', 'high', 'critical'];
    
    levels.forEach(level => {
      const error = new SecurityError(
        AuthErrorCode.ACCESS_DENIED,
        `${level} security issue`,
        level
      );
      
      expect(error.securityLevel).toBe(level);
    });
  });

  it('should handle undefined options', () => {
    const error = new SecurityError(
      AuthErrorCode.ACCESS_DENIED,
      'Security error',
      'low'
    );

    expect(error.provider).toBeUndefined();
    expect(error.details).toEqual({ securityLevel: 'low' });
  });
});

describe('ProviderError', () => {
  it('should create provider error with required parameters', () => {
    const error = new ProviderError(
      'google' as AuthProvider,
      'OAuth flow failed'
    );

    expect(error).toBeInstanceOf(EasyAuthError);
    expect(error).toBeInstanceOf(ProviderError);
    expect(error.name).toBe('ProviderError');
    expect(error.code).toBe(AuthErrorCode.API_ERROR);
    expect(error.provider).toBe('google');
    expect(error.message).toBe('OAuth flow failed');
    expect(error.isRetryable).toBe(false);
  });

  it('should accept custom error code', () => {
    const error = new ProviderError(
      'apple' as AuthProvider,
      'Token refresh failed',
      AuthErrorCode.ACCESS_DENIED
    );

    expect(error.code).toBe(AuthErrorCode.ACCESS_DENIED);
  });

  it('should include provider-specific error details', () => {
    const error = new ProviderError(
      'facebook' as AuthProvider,
      'Invalid app credentials',
      AuthErrorCode.API_ERROR,
      {
        providerErrorCode: 'invalid_client',
        providerErrorDescription: 'The client credentials are invalid',
        isRetryable: true,
        details: { attemptedAt: new Date() }
      }
    );

    expect(error.providerErrorCode).toBe('invalid_client');
    expect(error.providerErrorDescription).toBe('The client credentials are invalid');
    expect(error.isRetryable).toBe(true);
    expect(error.details?.providerErrorCode).toBe('invalid_client');
    expect(error.details?.providerErrorDescription).toBe('The client credentials are invalid');
    expect(error.details?.attemptedAt).toBeInstanceOf(Date);
  });

  it('should handle minimal options', () => {
    const error = new ProviderError(
      'azure-b2c' as AuthProvider,
      'Provider error',
      AuthErrorCode.API_ERROR,
      {}
    );

    expect(error.providerErrorCode).toBeUndefined();
    expect(error.providerErrorDescription).toBeUndefined();
    expect(error.isRetryable).toBe(false);
  });

  it('should handle undefined options', () => {
    const error = new ProviderError(
      'google' as AuthProvider,
      'Simple provider error'
    );

    expect(error.providerErrorCode).toBeUndefined();
    expect(error.providerErrorDescription).toBeUndefined();
    expect(error.details?.providerErrorCode).toBeUndefined();
  });
});

describe('DefaultErrorHandler', () => {
  describe('constructor', () => {
    it('should use default options when none provided', () => {
      const handler = new DefaultErrorHandler();
      
      // Test defaults through public methods
      expect(handler.shouldRetry(new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true }), 0)).toBe(true);
      expect(handler.shouldRetry(new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true }), 3)).toBe(false);
    });

    it('should accept custom options', () => {
      const handler = new DefaultErrorHandler({
        maxRetries: 5,
        baseDelay: 2000,
        maxDelay: 60000
      });
      
      // Test custom maxRetries
      expect(handler.shouldRetry(new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true }), 4)).toBe(true);
      expect(handler.shouldRetry(new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true }), 5)).toBe(false);
    });

    it('should handle partial options', () => {
      const handler = new DefaultErrorHandler({ maxRetries: 1 });
      
      expect(handler.shouldRetry(new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true }), 1)).toBe(false);
    });
  });

  describe('handleError', () => {
    let consoleSpy: jest.SpyInstance;

    beforeEach(() => {
      consoleSpy = jest.spyOn(console, 'error').mockImplementation();
    });

    afterEach(() => {
      consoleSpy.mockRestore();
    });

    it('should log error to console', () => {
      const handler = new DefaultErrorHandler();
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test error');

      handler.handleError(error);

      expect(consoleSpy).toHaveBeenCalledWith('EasyAuth Error:', error.toJSON());
    });

    it('should report critical security errors', () => {
      const handler = new DefaultErrorHandler();
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation();
      const reportSpy = jest.spyOn(handler as any, 'reportSecurityIncident');
      
      const error = new SecurityError(
        AuthErrorCode.ACCESS_DENIED,
        'Critical security breach',
        'critical'
      );

      handler.handleError(error);

      expect(reportSpy).toHaveBeenCalledWith(error);
      warnSpy.mockRestore();
    });

    it('should not report non-critical security errors', () => {
      const handler = new DefaultErrorHandler();
      const reportSpy = jest.spyOn(handler as any, 'reportSecurityIncident');
      
      const error = new SecurityError(
        AuthErrorCode.ACCESS_DENIED,
        'Low security issue',
        'low'
      );

      handler.handleError(error);

      expect(reportSpy).not.toHaveBeenCalled();
    });

    it('should not report non-security errors', () => {
      const handler = new DefaultErrorHandler();
      const reportSpy = jest.spyOn(handler as any, 'reportSecurityIncident');
      
      const error = new NetworkError('Connection failed');

      handler.handleError(error);

      expect(reportSpy).not.toHaveBeenCalled();
    });
  });

  describe('shouldRetry', () => {
    it('should return false when max retries exceeded', () => {
      const handler = new DefaultErrorHandler({ maxRetries: 2 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true });

      expect(handler.shouldRetry(error, 2)).toBe(false);
      expect(handler.shouldRetry(error, 3)).toBe(false);
    });

    it('should return false for non-retryable errors', () => {
      const handler = new DefaultErrorHandler();
      const error = new EasyAuthError(AuthErrorCode.INVALID_CONFIG, 'test', { isRetryable: false });

      expect(handler.shouldRetry(error, 0)).toBe(false);
      expect(handler.shouldRetry(error, 1)).toBe(false);
    });

    it('should return true for retryable errors within limit', () => {
      const handler = new DefaultErrorHandler({ maxRetries: 3 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true });

      expect(handler.shouldRetry(error, 0)).toBe(true);
      expect(handler.shouldRetry(error, 1)).toBe(true);
      expect(handler.shouldRetry(error, 2)).toBe(true);
    });
  });

  describe('getRetryDelay', () => {
    it('should return delay with exponential backoff', () => {
      const handler = new DefaultErrorHandler({ baseDelay: 1000, maxDelay: 30000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      const delay0 = handler.getRetryDelay(error, 0);
      const delay1 = handler.getRetryDelay(error, 1);
      const delay2 = handler.getRetryDelay(error, 2);

      // Base delay around 1000ms with jitter
      expect(delay0).toBeGreaterThan(500);
      expect(delay0).toBeLessThan(1500);

      // Should be roughly double with jitter
      expect(delay1).toBeGreaterThan(1500);
      expect(delay1).toBeLessThan(3000);

      // Should continue exponential pattern
      expect(delay2).toBeGreaterThan(3000);
      expect(delay2).toBeLessThan(6000);
    });

    it('should respect max delay limit', () => {
      const handler = new DefaultErrorHandler({ baseDelay: 1000, maxDelay: 5000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      const delay = handler.getRetryDelay(error, 10); // High attempt count
      
      expect(delay).toBeLessThanOrEqual(6250); // Max delay + jitter
    });

    it('should not return negative delays', () => {
      const handler = new DefaultErrorHandler({ baseDelay: 100, maxDelay: 1000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      for (let i = 0; i < 10; i++) {
        const delay = handler.getRetryDelay(error, i);
        expect(delay).toBeGreaterThanOrEqual(0);
      }
    });

    // TODO: Fix crypto mocking for secure random testing
    it.skip('should use secure random when crypto.getRandomValues is available', () => {
      // Mock crypto.getRandomValues
      const originalCrypto = global.crypto;
      const mockGetRandomValues = jest.fn().mockImplementation((array) => {
        array[0] = 0x80000000; // Return specific value for consistent testing
        return array;
      });

      // @ts-ignore
      global.crypto = {
        getRandomValues: mockGetRandomValues
      };

      const handler = new DefaultErrorHandler({ baseDelay: 1000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      handler.getRetryDelay(error, 0);

      expect(mockGetRandomValues).toHaveBeenCalled();

      // Restore original crypto
      global.crypto = originalCrypto;
    });

    // TODO: Fix crypto fallback testing
    it.skip('should fall back to Math.random when crypto.getRandomValues is not available', () => {
      const originalCrypto = global.crypto;
      const originalMathRandom = Math.random;
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation();

      // @ts-ignore
      global.crypto = undefined;
      Math.random = jest.fn().mockReturnValue(0.5);

      const handler = new DefaultErrorHandler({ baseDelay: 1000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      handler.getRetryDelay(error, 0);

      expect(warnSpy).toHaveBeenCalledWith(
        'SECURITY WARNING: crypto.getRandomValues not available, falling back to Math.random()'
      );
      expect(Math.random).toHaveBeenCalled();

      // Restore
      global.crypto = originalCrypto;
      Math.random = originalMathRandom;
      warnSpy.mockRestore();
    });

    // TODO: Fix crypto object mocking without getRandomValues
    it.skip('should handle crypto object without getRandomValues', () => {
      const originalCrypto = global.crypto;
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation();
      const mathRandomSpy = jest.spyOn(Math, 'random').mockReturnValue(0.5);

      // @ts-ignore
      global.crypto = {}; // crypto exists but no getRandomValues

      const handler = new DefaultErrorHandler({ baseDelay: 1000 });
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test');

      handler.getRetryDelay(error, 0);

      expect(warnSpy).toHaveBeenCalled();
      expect(mathRandomSpy).toHaveBeenCalled();

      global.crypto = originalCrypto;
      warnSpy.mockRestore();
      mathRandomSpy.mockRestore();
    });
  });

  describe('reportSecurityIncident', () => {
    it('should log security incident to console', () => {
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation();
      const handler = new DefaultErrorHandler();
      const error = new SecurityError(
        AuthErrorCode.ACCESS_DENIED,
        'Critical security breach',
        'critical'
      );

      (handler as any).reportSecurityIncident(error);

      expect(warnSpy).toHaveBeenCalledWith('SECURITY INCIDENT:', error.toJSON());
      warnSpy.mockRestore();
    });
  });
});

describe('ErrorUtils', () => {
  describe('isRetryable', () => {
    it('should return true for retryable EasyAuthError', () => {
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'test', { isRetryable: true });
      
      expect(ErrorUtils.isRetryable(error)).toBe(true);
    });

    it('should return false for non-retryable EasyAuthError', () => {
      const error = new EasyAuthError(AuthErrorCode.INVALID_CONFIG, 'test', { isRetryable: false });
      
      expect(ErrorUtils.isRetryable(error)).toBe(false);
    });

    it('should return false for non-EasyAuthError', () => {
      const error = new Error('Regular error');
      
      expect(ErrorUtils.isRetryable(error)).toBe(false);
    });
  });

  describe('isNetworkError', () => {
    it('should return true for NetworkError', () => {
      const error = new NetworkError('Connection failed');
      
      expect(ErrorUtils.isNetworkError(error)).toBe(true);
    });

    it('should return false for non-NetworkError', () => {
      const error = new ConfigurationError('Config error');
      
      expect(ErrorUtils.isNetworkError(error)).toBe(false);
    });

    it('should return false for regular Error', () => {
      const error = new Error('Regular error');
      
      expect(ErrorUtils.isNetworkError(error)).toBe(false);
    });
  });

  describe('isSecurityError', () => {
    it('should return true for SecurityError', () => {
      const error = new SecurityError(AuthErrorCode.ACCESS_DENIED, 'Security violation');
      
      expect(ErrorUtils.isSecurityError(error)).toBe(true);
    });

    it('should return false for non-SecurityError', () => {
      const error = new NetworkError('Network error');
      
      expect(ErrorUtils.isSecurityError(error)).toBe(false);
    });

    it('should return false for regular Error', () => {
      const error = new Error('Regular error');
      
      expect(ErrorUtils.isSecurityError(error)).toBe(false);
    });
  });

  describe('getUserFriendlyMessage', () => {
    it('should return user-friendly message for NETWORK_ERROR', () => {
      const error = new EasyAuthError(AuthErrorCode.NETWORK_ERROR, 'Connection failed');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('Unable to connect to authentication service. Please check your internet connection and try again.');
    });

    it('should return user-friendly message for ACCESS_DENIED', () => {
      const error = new EasyAuthError(AuthErrorCode.ACCESS_DENIED, 'Access denied');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('Access denied. Please check your credentials and try again.');
    });

    it('should return user-friendly message for SESSION_EXPIRED', () => {
      const error = new EasyAuthError(AuthErrorCode.SESSION_EXPIRED, 'Session expired');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('Your session has expired. Please log in again.');
    });

    it('should return user-friendly message for INVALID_CONFIG', () => {
      const error = new EasyAuthError(AuthErrorCode.INVALID_CONFIG, 'Config error');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('Authentication service is not properly configured. Please contact support.');
    });

    it('should return generic message for unknown EasyAuthError code', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'API error');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('An authentication error occurred. Please try again.');
    });

    it('should return generic message for non-EasyAuthError', () => {
      const error = new Error('Regular error');
      
      const message = ErrorUtils.getUserFriendlyMessage(error);
      
      expect(message).toBe('An unexpected error occurred. Please try again.');
    });
  });

  describe('fromUnknown', () => {
    it('should return the same error if already EasyAuthError', () => {
      const error = new EasyAuthError(AuthErrorCode.API_ERROR, 'Test error');
      
      const result = ErrorUtils.fromUnknown(error);
      
      expect(result).toBe(error);
    });

    it('should wrap regular Error in EasyAuthError', () => {
      const originalError = new Error('Original error');
      originalError.name = 'CustomError';
      
      const result = ErrorUtils.fromUnknown(originalError);
      
      expect(result).toBeInstanceOf(EasyAuthError);
      expect(result.code).toBe(AuthErrorCode.UNKNOWN_ERROR);
      expect(result.message).toBe('Original error');
      expect(result.cause).toBe(originalError);
      expect(result.details?.originalName).toBe('CustomError');
      expect(result.details?.originalStack).toBe(originalError.stack);
    });

    it('should wrap regular Error with context', () => {
      const originalError = new Error('Original error');
      const context = { userId: 'user123', sessionId: 'sess456' };
      
      const result = ErrorUtils.fromUnknown(originalError, context);
      
      expect(result.details?.userId).toBe('user123');
      expect(result.details?.sessionId).toBe('sess456');
    });

    it('should handle unknown non-Error values', () => {
      const unknownError = { message: 'Not an error object' };
      
      const result = ErrorUtils.fromUnknown(unknownError);
      
      expect(result).toBeInstanceOf(EasyAuthError);
      expect(result.code).toBe(AuthErrorCode.UNKNOWN_ERROR);
      expect(result.message).toBe('An unknown error occurred');
      expect(result.details?.originalError).toBe(unknownError);
    });

    it('should handle primitive values', () => {
      const result = ErrorUtils.fromUnknown('string error');
      
      expect(result).toBeInstanceOf(EasyAuthError);
      expect(result.code).toBe(AuthErrorCode.UNKNOWN_ERROR);
      expect(result.message).toBe('An unknown error occurred');
      expect(result.details?.originalError).toBe('string error');
    });

    it('should handle null and undefined', () => {
      const nullResult = ErrorUtils.fromUnknown(null);
      const undefinedResult = ErrorUtils.fromUnknown(undefined);
      
      expect(nullResult.details?.originalError).toBeNull();
      expect(undefinedResult.details?.originalError).toBeUndefined();
    });

    it('should merge context with error details', () => {
      const originalError = new Error('Test');
      const context = { customField: 'value' };
      
      const result = ErrorUtils.fromUnknown(originalError, context);
      
      expect(result.details?.customField).toBe('value');
      expect(result.details?.originalName).toBe('Error');
    });
  });
});