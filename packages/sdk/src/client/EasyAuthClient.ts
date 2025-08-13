import {
  EasyAuthConfig,
  EasyAuthClient as IEasyAuthClient,
  AuthSession,
  LoginOptions,
  AuthResult,
  UserProfile,
  AuthEvent,
  AuthEventCallback,
  AuthEventData,
  AuthError,
  HttpClient,
  StorageAdapter,
  Logger
} from '../types';

import { DefaultHttpClient } from '../utils/http-client';
import { LocalStorageAdapter } from '../utils/storage';
import { ConsoleLogger } from '../utils/logger';
import { EventEmitter } from '../utils/event-emitter';

/**
 * Core EasyAuth client for handling authentication flows
 */
export class EasyAuthClient implements IEasyAuthClient {
  public readonly config: EasyAuthConfig;
  public session: AuthSession;
  
  protected readonly httpClient: HttpClient;
  protected readonly storage: StorageAdapter;
  protected readonly logger: Logger;
  protected readonly eventEmitter: EventEmitter;
  
  private static readonly STORAGE_KEY = 'easyauth_session';
  
  constructor(
    config: EasyAuthConfig,
    httpClient?: HttpClient,
    storage?: StorageAdapter,
    logger?: Logger
  ) {
    this.config = this.validateConfig(config);
    this.httpClient = httpClient ?? new DefaultHttpClient(this.config);
    this.storage = storage ?? new LocalStorageAdapter();
    this.logger = logger ?? new ConsoleLogger(this.config.enableLogging ?? false);
    this.eventEmitter = new EventEmitter();
    
    // Initialize session from storage
    this.session = this.loadSessionFromStorage();
    
    this.logger.info('EasyAuth client initialized', { baseUrl: this.config.baseUrl });
  }
  
  /**
   * Initiate login flow for specified provider
   */
  async login(options: LoginOptions): Promise<AuthResult> {
    this.logger.info('Starting login flow', { provider: options.provider });
    
    try {
      const response = await this.httpClient.post<{
        success: boolean;
        user?: UserProfile;
        token?: string;
        expiresAt?: string;
        error?: AuthError;
        redirectUrl?: string;
        message?: string;
      }>(`/auth/${options.provider}/login`, {
        returnUrl: options.returnUrl,
        state: options.state,
        parameters: options.parameters
      });

      if (response.data.success) {
        // Check if we have immediate user data (direct auth)
        if (response.data.user && response.data.token) {
          // Direct authentication - update session immediately
          this.session = {
            isAuthenticated: true,
            user: response.data.user,
            token: response.data.token,
            ...(response.data.expiresAt ? { expiresAt: new Date(response.data.expiresAt) } : {}),
            ...(this.session.refreshToken ? { refreshToken: this.session.refreshToken } : {})
          };

          this.saveSessionToStorage();
          this.logger.info('Login successful', { userId: response.data.user.id });

          // Emit login event
          this.emitEvent('login', {
            user: response.data.user,
            token: response.data.token
          });

          return {
            success: true,
            user: response.data.user,
            token: response.data.token
          };
        } 
        // Check if we have a redirect URL (OAuth flow)
        else if (response.data.redirectUrl) {
          this.logger.info('Redirecting to OAuth provider', { 
            provider: options.provider, 
            redirectUrl: response.data.redirectUrl 
          });

          // Handle OAuth redirect flow
          return await this.handleOAuthRedirect(response.data.redirectUrl, options.provider);
        }
        else {
          // Success but no user data or redirect - unexpected
          const authError = {
            code: 'INVALID_RESPONSE',
            message: 'Server returned success but no authentication data or redirect URL'
          };
          
          this.logger.warn('Invalid server response', authError);
          this.emitEvent('error', { error: authError });
          
          return { success: false, error: authError };
        }
      } else {
        // Handle API-level errors
        const authError = response.data.error || {
          code: 'LOGIN_FAILED',
          message: response.data.message || 'Login failed'
        };
        
        this.logger.warn('Login failed', authError);
        this.emitEvent('error', { error: authError });
        
        return { success: false, error: authError };
      }
    } catch (error) {
      const authError = this.handleError(error);
      this.logger.error('Login error', authError);
      this.emitEvent('error', { error: authError });
      return { success: false, error: authError };
    }
  }

  /**
   * Handle OAuth redirect flow by opening popup window
   */
  private async handleOAuthRedirect(redirectUrl: string, provider: string): Promise<AuthResult> {
    return new Promise((resolve) => {
      let authCompleted = false;
      
      // Open OAuth popup window
      const popup = window.open(
        redirectUrl,
        `oauth_${provider}`,
        'width=500,height=600,scrollbars=yes,resizable=yes'
      );

      if (!popup) {
        const error = {
          code: 'POPUP_BLOCKED',
          message: 'Popup was blocked. Please allow popups for authentication.'
        };
        
        this.logger.error('Popup blocked', error);
        this.emitEvent('error', { error });
        resolve({ success: false, error });
        return;
      }

      // Handle popup closed manually (user cancellation)
      const checkClosed = setInterval(() => {
        if (popup.closed) {
          clearInterval(checkClosed);
          window.removeEventListener('message', messageListener);
          
          // Only report cancellation if auth didn't complete successfully
          if (!authCompleted) {
            const error = {
              code: 'USER_CANCELLED',
              message: 'Authentication was cancelled by user'
            };
            
            this.logger.info('OAuth cancelled', error);
            resolve({ success: false, error });
          }
        }
      }, 1000);

      // Listen for messages from the popup
      const messageListener = (event: MessageEvent) => {
        // Validate origin for security (in development, allow localhost)
        const isValidOrigin = event.origin === window.location.origin || 
                             (process.env.NODE_ENV !== 'production' && event.origin.includes('localhost'));
        
        if (!isValidOrigin) {
          this.logger.warn('Message from invalid origin', { origin: event.origin, expected: window.location.origin });
          return;
        }

        this.logger.debug('Received message from popup', { origin: event.origin, type: event.data?.type });

        if (event.data && event.data.type === 'EASYAUTH_LOGIN_SUCCESS') {
          // Mark auth as completed and clean up
          authCompleted = true;
          clearInterval(checkClosed);
          window.removeEventListener('message', messageListener);
          popup.close();

          const authData = event.data.data;
          
          // Update session with successful login
          this.session = {
            isAuthenticated: true,
            user: authData.user,
            token: authData.token,
            ...(this.session.refreshToken ? { refreshToken: this.session.refreshToken } : {})
          };

          this.saveSessionToStorage();
          this.logger.info('OAuth login successful', { userId: authData.user.id });

          // Emit login event
          this.emitEvent('login', {
            user: authData.user,
            token: authData.token
          });

          resolve({
            success: true,
            user: authData.user,
            token: authData.token
          });
        }
      };

      // Add message listener
      window.addEventListener('message', messageListener);
    });
  }
  
  /**
   * Logout current user and clear session
   */
  async logout(): Promise<void> {
    this.logger.info('Logging out user');
    
    try {
      // Call logout endpoint
      await this.httpClient.post('/auth/logout', {
        token: this.session.token
      });

      this.logger.info('Logout successful');
      
      // Clear local session
      this.session = this.createEmptySession();
      this.saveSessionToStorage();
      
      // Emit logout event
      this.emitEvent('logout');
      
    } catch (error) {
      this.logger.error('Error during logout', error);
      
      // Even if API call fails, clear local session
      this.session = this.createEmptySession(); 
      this.saveSessionToStorage();
      this.emitEvent('logout');
    }
  }
  
  /**
   * Refresh authentication token
   */
  async refresh(): Promise<AuthResult> {
    this.logger.info('Refreshing authentication token');
    
    try {
      const response = await this.httpClient.post<{
        success: boolean;
        user?: UserProfile;
        token?: string;
        expiresAt?: string;
        refreshToken?: string;
        error?: AuthError;
      }>('/auth/refresh', {
        token: this.session.token,
        refreshToken: this.session.refreshToken
      });

      if (response.data.success && response.data.token) {
        // Update session with refreshed token
        this.session = {
          ...this.session,
          token: response.data.token,
          user: response.data.user || this.session.user,
          ...(response.data.expiresAt ? { expiresAt: new Date(response.data.expiresAt) } : {}),
          ...(response.data.refreshToken || this.session.refreshToken 
            ? { refreshToken: response.data.refreshToken || this.session.refreshToken } : {})
        };

        this.saveSessionToStorage();
        this.logger.info('Token refreshed successfully');

        // Emit token refresh event
        const refreshUser = response.data.user || this.session.user;
        this.emitEvent('token_refresh', {
          token: response.data.token,
          ...(refreshUser ? { user: refreshUser } : {})
        });

        const user = response.data.user || this.session.user;
        return {
          success: true,
          ...(user ? { user } : {}),
          token: response.data.token
        };
      } else {
        // Handle refresh failure
        const authError = response.data.error || {
          code: 'REFRESH_FAILED',
          message: 'Token refresh failed'
        };
        
        this.logger.warn('Token refresh failed', authError);
        this.emitEvent('error', { error: authError });
        
        return { success: false, error: authError };
      }
    } catch (error) {
      const authError = this.handleError(error);
      this.logger.error('Token refresh error', authError);
      this.emitEvent('error', { error: authError });
      return { success: false, error: authError };
    }
  }
  
  /**
   * Get current authentication session
   */
  getSession(): AuthSession {
    return { ...this.session };
  }
  
  /**
   * Check if user is currently authenticated
   */
  isAuthenticated(): boolean {
    return this.session.isAuthenticated && 
           this.session.user !== null &&
           !this.isTokenExpired();
  }
  
  /**
   * Get current authenticated user
   */
  getUser(): UserProfile | null {
    return this.isAuthenticated() ? this.session.user : null;
  }
  
  /**
   * Get current authentication token
   */
  getToken(): string | null {
    return this.isAuthenticated() ? this.session.token ?? null : null;
  }
  
  /**
   * Set authentication token manually
   */
  setToken(token: string): void {
    this.session.token = token;
    this.saveSessionToStorage();
    this.logger.debug('Token set manually');
  }
  
  /**
   * Clear authentication token
   */
  clearToken(): void {
    this.session = this.createEmptySession();
    this.saveSessionToStorage();
    this.logger.debug('Token cleared');
  }
  
  /**
   * Subscribe to authentication events
   */
  on(event: AuthEvent, callback: AuthEventCallback): void {
    this.eventEmitter.on(event, callback);
  }
  
  /**
   * Unsubscribe from authentication events
   */
  off(event: AuthEvent, callback: AuthEventCallback): void {
    this.eventEmitter.off(event, callback);
  }
  
  // Private helper methods
  
  private validateConfig(config: EasyAuthConfig): EasyAuthConfig {
    if (!config.baseUrl) {
      throw new Error('EasyAuth: baseUrl is required in configuration');
    }
    
    if (!config.baseUrl.startsWith('http://') && !config.baseUrl.startsWith('https://')) {
      throw new Error('EasyAuth: baseUrl must be a valid HTTP/HTTPS URL');
    }
    
    return {
      apiVersion: '1.0',
      timeout: 30000,
      retryAttempts: 3,
      enableLogging: false,
      ...config
    };
  }
  
  private loadSessionFromStorage(): AuthSession {
    try {
      const stored = this.storage.getItem(EasyAuthClient.STORAGE_KEY);
      if (stored) {
        const session = JSON.parse(stored) as AuthSession;
        // Convert date strings back to Date objects
        if (session.expiresAt) {
          session.expiresAt = new Date(session.expiresAt);
        }
        this.logger.debug('Session loaded from storage');
        return session;
      }
    } catch (error) {
      this.logger.warn('Failed to load session from storage', error);
    }
    
    this.logger.debug('No stored session found, initializing empty session');
    return this.createEmptySession();
  }
  
  private createEmptySession(): AuthSession {
    return {
      isAuthenticated: false,
      user: null
    };
  }
  
  private saveSessionToStorage(): void {
    try {
      this.storage.setItem(EasyAuthClient.STORAGE_KEY, JSON.stringify(this.session));
    } catch (error) {
      this.logger.warn('Failed to save session to storage', error);
    }
  }
  
  private isTokenExpired(): boolean {
    if (!this.session.expiresAt) {
      return false; // No expiration set
    }
    
    return new Date() >= this.session.expiresAt;
  }
  
  protected handleError(error: unknown): AuthError {
    if (error instanceof Error) {
      return {
        code: 'UNKNOWN_ERROR',
        message: error.message,
        details: { originalError: error.name }
      };
    }
    
    return {
      code: 'UNKNOWN_ERROR', 
      message: 'An unknown error occurred',
      details: { originalError: String(error) }
    };
  }
  
  protected emitEvent(event: AuthEvent, data?: AuthEventData): void {
    this.eventEmitter.emit(event, data);
  }
}