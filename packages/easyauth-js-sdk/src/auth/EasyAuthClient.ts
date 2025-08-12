/**
 * Core EasyAuth client implementation
 * Main entry point for authentication operations
 */

import {
  IEasyAuthClient,
  AuthConfig,
  LoginRequest,
  AuthResult,
  CallbackData,
  SessionInfo,
  AuthProvider,
  ProviderInfo,
  ProviderCapability,
  GoogleProviderConfig,
  AppleProviderConfig,
  FacebookProviderConfig,
  AzureB2CProviderConfig,
  CustomProviderConfig,
  SessionValidationResult,
  TokenRefreshResult,
  UserInfo,
  HealthCheckResult,
  AuthEventType,
  AuthEventHandler,
  AuthEvent,
  AuthErrorCode,
} from '../types';

import { EasyAuthError, ConfigurationError, SecurityError, SessionError } from '../types/errors';
import { StateManager } from '../utils/StateManager';
import { StorageManager } from '../utils/StorageManager';
import { EventEmitter } from '../utils/EventEmitter';
import { URLValidator } from '../utils/URLValidator';
import { CryptoUtils } from '../utils/CryptoUtils';

/**
 * Main EasyAuth client class implementing the IEasyAuthClient interface
 * Provides authentication functionality for web applications
 */
export class EasyAuthClient implements IEasyAuthClient {
  private readonly _config: AuthConfig;
  private readonly _stateManager: StateManager;
  private readonly _storageManager: StorageManager;
  private readonly _eventEmitter: EventEmitter;
  private _currentSession: SessionInfo | null = null;
  private _isInitialized = false;

  constructor(config: AuthConfig) {
    // Validate configuration
    this.validateConfiguration(config);
    
    this._config = { ...config };
    this._stateManager = new StateManager();
    this._storageManager = new StorageManager(config.session?.storage || 'localStorage');
    this._eventEmitter = new EventEmitter();
    
    // Initialize the client
    this.initialize();
  }

  // #region Properties

  public get isInitialized(): boolean {
    return this._isInitialized;
  }

  public get config(): AuthConfig {
    return { ...this._config };
  }

  // #endregion

  // #region Provider Management

  public async getAvailableProviders(): Promise<ProviderInfo[]> {
    const providers: ProviderInfo[] = [];
    
    for (const [name, config] of Object.entries(this._config.providers)) {
      // Skip if config is undefined or not enabled
      if (!config || !this.isConfigEnabled(config)) {
        continue;
      }

      // For custom providers, iterate through each one
      if (name === 'custom' && typeof config === 'object' && !('clientId' in config)) {
        for (const [customName, customConfig] of Object.entries(config)) {
          if (customConfig.enabled) {
            providers.push({
              name: 'custom' as AuthProvider,
              displayName: customName,
              isEnabled: true,
              configuration: customConfig,
              capabilities: this.getProviderCapabilities('custom' as AuthProvider),
              healthStatus: {
                isHealthy: true, // TODO: Implement actual health check
                lastChecked: new Date(),
              },
            });
          }
        }
      } else if ('clientId' in config && config.enabled) {
        providers.push({
          name: name as AuthProvider,
          displayName: this.getProviderDisplayName(name as AuthProvider),
          isEnabled: true,
          configuration: config as GoogleProviderConfig | AppleProviderConfig | FacebookProviderConfig | AzureB2CProviderConfig | CustomProviderConfig,
          capabilities: this.getProviderCapabilities(name as AuthProvider),
          healthStatus: {
            isHealthy: true, // TODO: Implement actual health check
            lastChecked: new Date(),
          },
        });
      }
    }
    
    return providers;
  }

  public async getProviderInfo(provider: AuthProvider): Promise<ProviderInfo | null> {
    const config = this._config.providers[provider];
    if (!config || !this.isConfigEnabled(config)) {
      return null;
    }

    // Handle custom providers
    if (provider === 'custom' && typeof config === 'object' && !('clientId' in config)) {
      // For custom providers, return info about the first enabled one
      const firstCustom = Object.entries(config).find(([, customConfig]) => customConfig.enabled);
      if (firstCustom) {
        const [customName, customConfig] = firstCustom;
        return {
          name: provider,
          displayName: customName,
          isEnabled: true,
          configuration: customConfig,
          capabilities: this.getProviderCapabilities(provider),
          healthStatus: {
            isHealthy: true, // TODO: Implement actual health check
            lastChecked: new Date(),
          },
        };
      }
      return null;
    }

    // Handle standard providers
    if ('clientId' in config) {
      return {
        name: provider,
        displayName: this.getProviderDisplayName(provider),
        isEnabled: config.enabled as boolean,
        configuration: config as GoogleProviderConfig | AppleProviderConfig | FacebookProviderConfig | AzureB2CProviderConfig | CustomProviderConfig,
        capabilities: this.getProviderCapabilities(provider),
        healthStatus: {
          isHealthy: true, // TODO: Implement actual health check
          lastChecked: new Date(),
        },
      };
    }

    return null;
  }

  private isConfigEnabled(config: any): boolean {
    if (!config) return false;
    
    // For standard provider configs
    if ('enabled' in config) {
      return config.enabled;
    }
    
    // For custom provider configs (Record<string, CustomProviderConfig>)
    if (typeof config === 'object' && !('clientId' in config)) {
      return Object.values(config).some((customConfig: any) => customConfig.enabled);
    }
    
    return false;
  }

  // #endregion

  // #region Authentication Flow

  public async initiateLogin(request: LoginRequest): Promise<AuthResult> {
    try {
      // Validate request
      if (!this.validateLoginRequest(request)) {
        return {
          success: false,
          error: 'Invalid return URL format',
          errorCode: AuthErrorCode.VALIDATION_ERROR,
        };
      }

      // Determine provider
      const provider = request.provider || this._config.defaultProvider;
      if (!provider) {
        return {
          success: false,
          error: 'No provider specified and no default provider configured',
          errorCode: AuthErrorCode.PROVIDER_NOT_FOUND,
        };
      }

      // Check if provider is available
      const providerConfig = this._config.providers[provider];
      if (!providerConfig || !providerConfig.enabled) {
        return {
          success: false,
          error: `Provider "${provider}" is not available`,
          errorCode: AuthErrorCode.PROVIDER_NOT_FOUND,
        };
      }

      // Generate state parameter for CSRF protection
      const state = await CryptoUtils.generateState();
      
      // Store state and return URL
      this._stateManager.storeState(state, {
        provider,
        returnUrl: request.returnUrl,
        customParams: request.customParams,
        scopes: request.scopes,
      });

      // Build authorization URL
      const authUrl = await this.buildAuthorizationUrl(provider, request, state);

      // Emit event
      this.emitEvent('login_initiated', { provider, returnUrl: request.returnUrl });

      return {
        success: true,
        authUrl,
        state,
      };

    } catch (error) {
      this.emitEvent('login_failed', { error: error instanceof Error ? error.message : 'Unknown error' });
      
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Login initiation failed',
        errorCode: AuthErrorCode.UNKNOWN_ERROR,
      };
    }
  }

  public async handleCallback(data: CallbackData): Promise<AuthResult> {
    try {
      // Check for provider errors
      if (data.error) {
        const errorMessage = data.error_description || data.error;
        this.emitEvent('login_failed', { error: errorMessage, provider: data.provider });
        
        return {
          success: false,
          error: errorMessage,
          errorCode: data.error === 'access_denied' ? AuthErrorCode.ACCESS_DENIED : AuthErrorCode.API_ERROR,
        };
      }

      // Validate state parameter
      if (!this._stateManager.validateState(data.state)) {
        this.emitEvent('login_failed', { error: 'Invalid state parameter', provider: data.provider });
        
        throw new SecurityError(
          AuthErrorCode.CSRF_ERROR,
          'Invalid state parameter - possible CSRF attack',
          'high',
          { provider: data.provider }
        );
      }

      // Get stored state data
      const stateData = this._stateManager.getStateData(data.state);
      if (!stateData) {
        throw new SecurityError(
          AuthErrorCode.INVALID_STATE,
          'State parameter not found',
          'medium',
          { provider: data.provider }
        );
      }

      // Exchange code for tokens (mock implementation)
      const tokens = await this.exchangeCodeForTokens(data.code!, data.provider, stateData);
      
      // Get user info (mock implementation)
      const user = await this.getUserInfo(tokens.accessToken, data.provider);

      // Create session
      const session = await this.createSession(user, tokens, data.provider);

      // Store session
      this._currentSession = session;
      await this._storageManager.storeSession(session);

      // Clean up state
      this._stateManager.clearState(data.state);

      // Emit event
      this.emitEvent('login_completed', { 
        provider: data.provider, 
        userId: user.id,
        sessionId: session.sessionId,
      });

      return {
        success: true,
        session,
        user,
        tokens,
      };

    } catch (error) {
      this.emitEvent('login_failed', { 
        error: error instanceof Error ? error.message : 'Unknown error',
        provider: data.provider,
      });

      if (error instanceof EasyAuthError) {
        return {
          success: false,
          error: error.message,
          errorCode: error.code,
        };
      }

      return {
        success: false,
        error: 'Callback handling failed',
        errorCode: AuthErrorCode.UNKNOWN_ERROR,
      };
    }
  }

  // #endregion

  // #region Session Management

  public async getCurrentSession(): Promise<SessionInfo | null> {
    // Return cached session if valid
    if (this._currentSession && this.isSessionValid(this._currentSession)) {
      return this._currentSession;
    }

    // Try to load session from storage
    const storedSession = await this._storageManager.getSession();
    if (storedSession && this.isSessionValid(storedSession)) {
      this._currentSession = storedSession;
      return storedSession;
    }

    // Clear invalid session
    this._currentSession = null;
    await this._storageManager.clearSession();
    
    return null;
  }

  public async validateSession(sessionId?: string): Promise<SessionValidationResult> {
    try {
      const session = sessionId 
        ? await this.getSessionById(sessionId)
        : await this.getCurrentSession();

      if (!session) {
        return { isValid: false, error: 'No session found' };
      }

      if (!this.isSessionValid(session)) {
        return { 
          isValid: false, 
          error: 'Session expired',
          refreshRequired: !!session.refreshToken,
        };
      }

      return { isValid: true, session };

    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Session validation failed',
      };
    }
  }

  public async refreshSession(): Promise<TokenRefreshResult> {
    try {
      const session = await this.getCurrentSession();
      if (!session?.refreshToken) {
        throw new SessionError(
          AuthErrorCode.INVALID_SESSION,
          'No refresh token available',
          session?.sessionId
        );
      }

      // Mock token refresh - in real implementation, call API
      const newTokens = await this.refreshTokens(session.refreshToken, session.provider);
      
      // Update session
      const updatedSession: SessionInfo = {
        ...session,
        expiresAt: newTokens.expiresAt,
        lastAccessedAt: new Date(),
      };

      this._currentSession = updatedSession;
      await this._storageManager.storeSession(updatedSession);

      this.emitEvent('session_refreshed', { 
        sessionId: session.sessionId,
        provider: session.provider,
      });

      return {
        success: true,
        tokens: newTokens,
        session: updatedSession,
      };

    } catch (error) {
      if (error instanceof EasyAuthError) {
        return {
          success: false,
          error: error.message,
          errorCode: error.code,
        };
      }

      return {
        success: false,
        error: 'Token refresh failed',
        errorCode: AuthErrorCode.UNKNOWN_ERROR,
      };
    }
  }

  public async signOut(): Promise<boolean> {
    try {
      const session = this._currentSession;
      
      this.emitEvent('logout_initiated', {
        sessionId: session?.sessionId,
        provider: session?.provider,
      });

      // Clear session data
      this._currentSession = null;
      await this._storageManager.clearSession();
      this._stateManager.clearAllStates();

      // TODO: Revoke tokens on server - could potentially fail and return false

      this.emitEvent('logout_completed', {
        sessionId: session?.sessionId,
        provider: session?.provider,
      });

      // Always return true for local logout - local cleanup should always succeed
      return true;

    } catch (error) {
      // Even if logout fails, we should clear local session
      this._currentSession = null;
      await this._storageManager.clearSession();
      
      // Always return true since local cleanup is the primary goal
      // Future enhancement: return false if server token revocation fails
      return true;
    }
  }

  // #endregion

  // #region Event Handling

  public addEventListener(type: AuthEventType, handler: AuthEventHandler): void {
    this._eventEmitter.on(type, handler);
  }

  public removeEventListener(type: AuthEventType, handler: AuthEventHandler): void {
    this._eventEmitter.off(type, handler);
  }

  // #endregion

  // #region Utility Methods

  public async isLoggedIn(): Promise<boolean> {
    const session = await this.getCurrentSession();
    return session !== null;
  }

  public async getUser(): Promise<UserInfo | null> {
    const session = await this.getCurrentSession();
    return session?.user || null;
  }

  public async getAccessToken(): Promise<string | null> {
    // In a real implementation, this would extract the access token from the session
    const session = await this.getCurrentSession();
    return session ? 'mock-access-token' : null;
  }

  public async getHealthStatus(): Promise<HealthCheckResult> {
    // Mock implementation - in real implementation, check API health
    return {
      status: 'healthy',
      checks: {
        api: {
          status: 'healthy',
          responseTime: 100,
        },
        providers: {
          status: 'healthy',
          responseTime: 50,
        },
      },
      timestamp: new Date(),
    };
  }

  // #endregion

  // #region Private Methods

  private validateConfiguration(config: AuthConfig): void {
    if (!config) {
      throw new ConfigurationError('Configuration is required');
    }

    if (!config.apiBaseUrl) {
      throw new ConfigurationError('API base URL is required');
    }

    if (!config.providers || Object.keys(config.providers).length === 0) {
      throw new ConfigurationError('At least one provider must be configured');
    }

    // Validate individual providers
    for (const [name, providerConfig] of Object.entries(config.providers)) {
      if (providerConfig.enabled && !providerConfig.clientId) {
        throw new ConfigurationError(`${this.capitalizeFirst(name)} provider client ID is required`);
      }
    }
  }

  private validateLoginRequest(request: LoginRequest): boolean {
    return URLValidator.isValid(request.returnUrl);
  }

  private initialize(): void {
    // Initialize storage manager
    this._storageManager.initialize();
    
    // Load existing session if available
    this._storageManager.getSession().then(session => {
      if (session && this.isSessionValid(session)) {
        this._currentSession = session;
      }
    }).catch(() => {
      // Ignore errors during initialization
    });

    this._isInitialized = true;
  }

  private async buildAuthorizationUrl(
    provider: AuthProvider, 
    request: LoginRequest, 
    state: string
  ): Promise<string> {
    const config = this._config.providers[provider]!;
    const baseUrl = this.getProviderAuthUrl(provider);
    
    const clientId = typeof config === 'object' && 'clientId' in config 
      ? String(config.clientId) 
      : '';
    
    const params = new URLSearchParams({
      client_id: clientId,
      redirect_uri: request.returnUrl,
      response_type: 'code',
      scope: (request.scopes || ['openid', 'email', 'profile']).join(' '),
      state,
    });

    // Add custom parameters
    if (request.customParams) {
      for (const [key, value] of Object.entries(request.customParams)) {
        params.set(key, value);
      }
    }

    return `${baseUrl}?${params.toString()}`;
  }

  private getProviderAuthUrl(provider: AuthProvider): string {
    switch (provider) {
      case 'google':
        return 'https://accounts.google.com/oauth2/v2/auth';
      case 'facebook':
        return 'https://www.facebook.com/v18.0/dialog/oauth';
      case 'apple':
        return 'https://appleid.apple.com/auth/authorize';
      case 'azure-b2c':
        // TODO: Build B2C URL from config
        return 'https://login.microsoftonline.com/oauth2/v2.0/authorize';
      default:
        throw new Error(`Unsupported provider: ${provider}`);
    }
  }

  private getProviderDisplayName(provider: AuthProvider): string {
    switch (provider) {
      case 'google':
        return 'Google';
      case 'facebook':
        return 'Facebook';
      case 'apple':
        return 'Apple';
      case 'azure-b2c':
        return 'Microsoft';
      default:
        return this.capitalizeFirst(provider);
    }
  }

  private getProviderCapabilities(provider: AuthProvider): ProviderCapability[] {
    const baseCapabilities: ProviderCapability[] = ['oauth2', 'refresh_token', 'user_info'];
    
    switch (provider) {
      case 'google':
      case 'apple':
      case 'azure-b2c':
        return [...baseCapabilities, 'openid_connect'];
      case 'facebook':
        return baseCapabilities;
      default:
        return baseCapabilities;
    }
  }

  private async exchangeCodeForTokens(
    _code: string, 
    _provider: AuthProvider, 
    _stateData: any
  ): Promise<any> {
    // Mock implementation - in real implementation, call token endpoint
    // These parameters are intentionally unused in the mock
    void _code; void _provider; void _stateData;
    return {
      accessToken: 'mock-access-token',
      refreshToken: 'mock-refresh-token',
      idToken: 'mock-id-token',
      tokenType: 'Bearer',
      expiresIn: 3600,
      expiresAt: new Date(Date.now() + 3600 * 1000),
    };
  }

  private async getUserInfo(accessToken: string, provider: AuthProvider): Promise<UserInfo> {
    // Mock implementation - in real implementation, call userinfo endpoint
    return {
      id: 'user-123',
      email: 'user@example.com',
      emailVerified: true,
      name: 'Test User',
      givenName: 'Test',
      familyName: 'User',
      picture: 'https://example.com/avatar.jpg',
      locale: 'en',
      provider,
      providerUserId: 'provider-user-123',
      createdAt: new Date(),
      lastLoginAt: new Date(),
    };
  }

  private async createSession(
    user: UserInfo, 
    tokens: any, 
    provider: AuthProvider
  ): Promise<SessionInfo> {
    const sessionId = await CryptoUtils.generateSessionId();
    
    return {
      sessionId,
      user,
      isValid: true,
      createdAt: new Date(),
      expiresAt: tokens.expiresAt,
      lastAccessedAt: new Date(),
      provider,
      refreshToken: tokens.refreshToken,
    };
  }

  private isSessionValid(session: SessionInfo): boolean {
    return session.isValid && session.expiresAt > new Date();
  }

  private async getSessionById(sessionId: string): Promise<SessionInfo | null> {
    // In a real implementation, this would call the API
    if (this._currentSession?.sessionId === sessionId) {
      return this._currentSession;
    }
    return null;
  }

  // eslint-disable-next-line no-unused-vars
  private async refreshTokens(_refreshToken: string, _provider: AuthProvider): Promise<any> {
    // Mock implementation - in real implementation, call refresh endpoint
    return {
      accessToken: 'new-mock-access-token',
      refreshToken: 'new-mock-refresh-token',
      tokenType: 'Bearer',
      expiresIn: 3600,
      expiresAt: new Date(Date.now() + 3600 * 1000),
    };
  }

  private emitEvent(type: AuthEventType, data?: Record<string, unknown>): void {
    const event: AuthEvent = {
      type,
      timestamp: new Date(),
      data,
    };
    
    this._eventEmitter.emit(type, event);
  }

  private capitalizeFirst(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  // #endregion
}