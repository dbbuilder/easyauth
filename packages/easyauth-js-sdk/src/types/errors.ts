/**
 * Error handling types and definitions
 */

import { AuthProvider, AuthErrorCode } from './index';

// Base error class for all authentication errors
export class EasyAuthError extends Error {
  public readonly code: AuthErrorCode;
  public readonly provider?: AuthProvider;
  public readonly details?: Record<string, unknown>;
  public readonly timestamp: Date;
  public readonly requestId?: string;
  public readonly isRetryable: boolean;

  constructor(
    code: AuthErrorCode,
    message: string,
    options?: {
      provider?: AuthProvider;
      details?: Record<string, unknown>;
      cause?: Error;
      requestId?: string;
      isRetryable?: boolean;
    }
  ) {
    super(message);
    this.name = 'EasyAuthError';
    this.code = code;
    this.provider = options?.provider;
    this.details = options?.details;
    this.timestamp = new Date();
    this.requestId = options?.requestId;
    this.isRetryable = options?.isRetryable ?? false;

    // Maintain proper stack trace
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, EasyAuthError);
    }

    // Set the cause if provided (for error chaining)
    if (options?.cause) {
      this.cause = options.cause;
    }
  }

  // Convert to a plain object for serialization
  public toJSON(): SerializedError {
    return {
      name: this.name,
      code: this.code,
      message: this.message,
      provider: this.provider,
      details: this.details,
      timestamp: this.timestamp,
      requestId: this.requestId,
      isRetryable: this.isRetryable,
      stack: this.stack,
    };
  }

  // Create from a plain object (for deserialization)
  public static fromJSON(data: SerializedError): EasyAuthError {
    const error = new EasyAuthError(data.code, data.message, {
      provider: data.provider,
      details: data.details,
      requestId: data.requestId,
      isRetryable: data.isRetryable,
    });
    error.stack = data.stack;
    return error;
  }
}

// Serialized error format
export interface SerializedError {
  name: string;
  code: AuthErrorCode;
  message: string;
  provider?: AuthProvider;
  details?: Record<string, unknown>;
  timestamp: Date;
  requestId?: string;
  isRetryable: boolean;
  stack?: string;
}

// Specific error classes for different categories
export class ConfigurationError extends EasyAuthError {
  constructor(message: string, details?: Record<string, unknown>) {
    super(AuthErrorCode.INVALID_CONFIG, message, { details, isRetryable: false });
    this.name = 'ConfigurationError';
  }
}

export class NetworkError extends EasyAuthError {
  public readonly statusCode?: number;
  public readonly responseBody?: string;

  constructor(
    message: string,
    statusCode?: number,
    options?: {
      provider?: AuthProvider;
      responseBody?: string;
      requestId?: string;
    }
  ) {
    super(AuthErrorCode.NETWORK_ERROR, message, {
      provider: options?.provider,
      requestId: options?.requestId,
      isRetryable: true,
      details: {
        statusCode,
        responseBody: options?.responseBody,
      },
    });
    this.name = 'NetworkError';
    this.statusCode = statusCode;
    this.responseBody = options?.responseBody;
  }
}

export class SessionError extends EasyAuthError {
  public readonly sessionId?: string;

  constructor(
    code: AuthErrorCode,
    message: string,
    sessionId?: string,
    options?: {
      provider?: AuthProvider;
      details?: Record<string, unknown>;
    }
  ) {
    super(code, message, {
      provider: options?.provider,
      details: { sessionId, ...options?.details },
      isRetryable: code === AuthErrorCode.SESSION_EXPIRED,
    });
    this.name = 'SessionError';
    this.sessionId = sessionId;
  }
}

export class SecurityError extends EasyAuthError {
  public readonly securityLevel: 'low' | 'medium' | 'high' | 'critical';

  constructor(
    code: AuthErrorCode,
    message: string,
    securityLevel: 'low' | 'medium' | 'high' | 'critical' = 'medium',
    options?: {
      provider?: AuthProvider;
      details?: Record<string, unknown>;
    }
  ) {
    super(code, message, {
      provider: options?.provider,
      details: { securityLevel, ...options?.details },
      isRetryable: false,
    });
    this.name = 'SecurityError';
    this.securityLevel = securityLevel;
  }
}

export class ProviderError extends EasyAuthError {
  public readonly providerErrorCode?: string;
  public readonly providerErrorDescription?: string;

  constructor(
    provider: AuthProvider,
    message: string,
    code: AuthErrorCode = AuthErrorCode.API_ERROR,
    options?: {
      providerErrorCode?: string;
      providerErrorDescription?: string;
      details?: Record<string, unknown>;
      isRetryable?: boolean;
    }
  ) {
    super(code, message, {
      provider,
      details: {
        providerErrorCode: options?.providerErrorCode,
        providerErrorDescription: options?.providerErrorDescription,
        ...options?.details,
      },
      isRetryable: options?.isRetryable ?? false,
    });
    this.name = 'ProviderError';
    this.providerErrorCode = options?.providerErrorCode;
    this.providerErrorDescription = options?.providerErrorDescription;
  }
}

// Error handler interface
export interface ErrorHandler {
  handleError(error: EasyAuthError): Promise<void> | void;
  shouldRetry(error: EasyAuthError, attemptCount: number): boolean;
  getRetryDelay(error: EasyAuthError, attemptCount: number): number;
}

// Default error handler implementation
export class DefaultErrorHandler implements ErrorHandler {
  private readonly maxRetries: number;
  private readonly baseDelay: number;
  private readonly maxDelay: number;

  constructor(options?: {
    maxRetries?: number;
    baseDelay?: number;
    maxDelay?: number;
  }) {
    this.maxRetries = options?.maxRetries ?? 3;
    this.baseDelay = options?.baseDelay ?? 1000;
    this.maxDelay = options?.maxDelay ?? 30000;
  }

  public handleError(error: EasyAuthError): void {
    // Log the error (in a real implementation, this would use a proper logger)
    console.error('EasyAuth Error:', error.toJSON());

    // Report critical security errors
    if (error instanceof SecurityError && error.securityLevel === 'critical') {
      this.reportSecurityIncident(error);
    }
  }

  public shouldRetry(error: EasyAuthError, attemptCount: number): boolean {
    if (attemptCount >= this.maxRetries) {
      return false;
    }

    return error.isRetryable;
  }

  public getRetryDelay(error: EasyAuthError, attemptCount: number): number {
    // Exponential backoff with jitter
    const delay = Math.min(
      this.baseDelay * Math.pow(2, attemptCount),
      this.maxDelay
    );

    // Add jitter (±25%)
    const jitter = delay * 0.25 * (Math.random() * 2 - 1);
    return Math.max(0, delay + jitter);
  }

  private reportSecurityIncident(error: SecurityError): void {
    // In a real implementation, this would report to a security incident system
    console.warn('SECURITY INCIDENT:', error.toJSON());
  }
}

// Error boundary for React integration
export interface ErrorBoundaryState {
  hasError: boolean;
  error?: EasyAuthError;
  errorId?: string;
}

// Error reporting interface
export interface ErrorReporter {
  report(error: EasyAuthError, context?: Record<string, unknown>): Promise<void>;
  setUser(userId: string, userInfo?: Record<string, unknown>): void;
  addBreadcrumb(message: string, category?: string, level?: 'info' | 'warning' | 'error'): void;
  clearBreadcrumbs(): void;
}

// Error context for debugging
export interface ErrorContext {
  userId?: string;
  sessionId?: string;
  provider?: AuthProvider;
  userAgent?: string;
  url?: string;
  timestamp: Date;
  buildVersion?: string;
  environment?: string;
  breadcrumbs?: ErrorBreadcrumb[];
}

export interface ErrorBreadcrumb {
  timestamp: Date;
  message: string;
  category?: string;
  level: 'info' | 'warning' | 'error';
  data?: Record<string, unknown>;
}

// Utility functions for error handling
export const ErrorUtils = {
  // Check if error is retryable
  isRetryable: (error: Error): boolean => {
    if (error instanceof EasyAuthError) {
      return error.isRetryable;
    }
    return false;
  },

  // Check if error is network related
  isNetworkError: (error: Error): boolean => {
    return error instanceof NetworkError;
  },

  // Check if error is security related
  isSecurityError: (error: Error): boolean => {
    return error instanceof SecurityError;
  },

  // Get error message for display to users
  getUserFriendlyMessage: (error: Error): string => {
    if (error instanceof EasyAuthError) {
      switch (error.code) {
        case AuthErrorCode.NETWORK_ERROR:
          return 'Unable to connect to authentication service. Please check your internet connection and try again.';
        case AuthErrorCode.ACCESS_DENIED:
          return 'Access denied. Please check your credentials and try again.';
        case AuthErrorCode.SESSION_EXPIRED:
          return 'Your session has expired. Please log in again.';
        case AuthErrorCode.INVALID_CONFIG:
          return 'Authentication service is not properly configured. Please contact support.';
        default:
          return 'An authentication error occurred. Please try again.';
      }
    }
    return 'An unexpected error occurred. Please try again.';
  },

  // Create error from unknown source
  fromUnknown: (error: unknown, context?: Partial<ErrorContext>): EasyAuthError => {
    if (error instanceof EasyAuthError) {
      return error;
    }

    if (error instanceof Error) {
      return new EasyAuthError(
        AuthErrorCode.UNKNOWN_ERROR,
        error.message,
        {
          details: {
            originalName: error.name,
            originalStack: error.stack,
            ...context,
          },
          cause: error,
        }
      );
    }

    return new EasyAuthError(
      AuthErrorCode.UNKNOWN_ERROR,
      'An unknown error occurred',
      {
        details: {
          originalError: error,
          ...context,
        },
      }
    );
  },
};