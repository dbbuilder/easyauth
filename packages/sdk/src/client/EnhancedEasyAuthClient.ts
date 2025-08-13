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

import { EasyAuthClient } from './EasyAuthClient';
import { CredentialHelper, createQuickSetup } from '../utils/credential-helper';
import { detectEnvironment, getOptimalStorageAdapter, supportsOAuthPopup } from '../utils/environment';
import { StateManagementConnector } from '../integrations/state-management';
import { LocalStorageAdapter, SessionStorageAdapter, MemoryStorageAdapter } from '../utils/storage';

/**
 * Enhanced EasyAuth client with advanced features
 */
export class EnhancedEasyAuthClient extends EasyAuthClient {
  private credentialHelper?: CredentialHelper;
  private stateConnector?: StateManagementConnector;
  private autoRefreshInterval?: NodeJS.Timeout;
  private environment = detectEnvironment();
  
  constructor(
    config: EasyAuthConfig,
    httpClient?: HttpClient,
    storage?: StorageAdapter,
    logger?: Logger
  ) {
    // Enhanced configuration with smart defaults
    const enhancedConfig = EnhancedEasyAuthClient.enhanceConfig(config);
    
    // Auto-select optimal storage if not provided
    const optimalStorage = storage || EnhancedEasyAuthClient.createOptimalStorage(enhancedConfig);
    
    super(enhancedConfig, httpClient, optimalStorage, logger);
    
    // Initialize enhanced features
    this.initializeEnhancedFeatures(enhancedConfig);
  }
  
  /**
   * Quick setup factory for new projects
   */
  static quickSetup(options: {
    baseUrl: string;
    providers: string[];
    isDevelopment?: boolean;
    useOAuthProxy?: boolean;
    stateManagement?: 'redux' | 'zustand' | 'pinia' | 'context';
    store?: any;
    dispatch?: (action: any) => void;
  }): {
    client: EnhancedEasyAuthClient;
    setupInstructions: string;
    credentials: any;
  } {
    const { credentials, setupInstructions, helper } = createQuickSetup({
      providers: options.providers,
      isDevelopment: options.isDevelopment ?? process.env.NODE_ENV === 'development',
      useProxy: options.useOAuthProxy
    });
    
    const config: EasyAuthConfig = {
      baseUrl: options.baseUrl,
      environment: options.isDevelopment ? 'development' : 'production',
      useDevCredentials: options.isDevelopment,
      useOAuthProxy: options.useOAuthProxy,
      enableStateManagement: !!options.stateManagement,
      stateManagementType: options.stateManagement,
      enableTokenAutoRefresh: true,
      enableSessionPersistence: true
    };
    
    const client = new EnhancedEasyAuthClient(config);
    
    // Connect to state management if configured
    if (options.stateManagement && options.store) {
      client.connectToStateManagement(options.stateManagement, options.store, options.dispatch);
    }
    
    return {
      client,
      setupInstructions,
      credentials
    };
  }
  
  /**
   * Connect EasyAuth to state management framework
   */
  connectToStateManagement(
    type: 'redux' | 'zustand' | 'pinia' | 'context',
    store: any,
    dispatch?: (action: any) => void
  ): void {
    this.stateConnector = new StateManagementConnector(type, store, dispatch);
    const handlers = this.stateConnector.createEventHandlers();
    
    // Connect event handlers
    this.on('login', handlers.onLogin);
    this.on('logout', handlers.onLogout);
    this.on('token_refresh', handlers.onTokenRefresh);
    this.on('error', handlers.onError);
  }
  
  /**
   * Enhanced login with environment-aware flow selection
   */
  async login(options: LoginOptions): Promise<AuthResult> {
    // Emit loading start for state management
    this.stateConnector?.createEventHandlers().onLoadingStart();
    
    // Auto-select authentication flow based on environment
    const enhancedOptions = this.enhanceLoginOptions(options);
    
    try {
      const result = await super.login(enhancedOptions);
      
      // Start auto-refresh if successful and enabled
      if (result.success && this.config.enableTokenAutoRefresh) {
        this.startAutoRefresh();
      }
      
      return result;
    } catch (error) {
      // Enhanced error handling
      return this.handleEnhancedError(error);
    }
  }
  
  /**
   * Enhanced logout with cleanup
   */
  async logout(): Promise<void> {
    // Stop auto-refresh
    this.stopAutoRefresh();
    
    // Call parent logout
    await super.logout();
  }
  
  /**
   * Get environment information
   */
  getEnvironmentInfo() {
    return this.environment;
  }
  
  /**
   * Validate production readiness
   */
  validateProductionReadiness(): {
    isReady: boolean;
    warnings: string[];
    recommendations: string[];
  } {
    const warnings: string[] = [];
    const recommendations: string[] = [];
    
    // Check environment
    if (this.config.environment === 'development') {
      warnings.push('Client is configured for development environment');
    }
    
    // Check OAuth setup
    if (this.config.useDevCredentials) {
      warnings.push('Using development OAuth credentials');
      recommendations.push('Configure production OAuth applications for each provider');
    }
    
    // Check security settings
    if (!this.config.enableSessionPersistence) {
      recommendations.push('Consider enabling session persistence for better UX');
    }
    
    if (!this.config.enableTokenAutoRefresh) {
      recommendations.push('Enable automatic token refresh for seamless authentication');
    }
    
    // Check environment compatibility
    if (!this.environment.supportsLocalStorage && this.config.storageType === 'localStorage') {
      warnings.push('localStorage not available in current environment');
      recommendations.push('Use sessionStorage or memory storage adapter');
    }
    
    return {
      isReady: warnings.length === 0,
      warnings,
      recommendations
    };
  }
  
  /**
   * Health check for authentication service
   */
  async healthCheck(): Promise<{
    status: 'healthy' | 'degraded' | 'unhealthy';
    details: {
      apiEndpoint: boolean;
      oauthProviders: Record<string, boolean>;
      tokenValidation: boolean;
      storageAccess: boolean;
    };
    latency: number;
  }> {
    const startTime = Date.now();
    
    const details = {
      apiEndpoint: false,
      oauthProviders: {} as Record<string, boolean>,
      tokenValidation: false,
      storageAccess: false
    };
    
    try {
      // Test API endpoint
      try {
        await this.httpClient.get('/auth/health');
        details.apiEndpoint = true;
      } catch {
        details.apiEndpoint = false;
      }
      
      // Test storage access
      try {
        this.storage.setItem('easyauth_health_test', 'test');
        const retrieved = this.storage.getItem('easyauth_health_test');
        details.storageAccess = retrieved === 'test';
        this.storage.removeItem('easyauth_health_test');
      } catch {
        details.storageAccess = false;
      }
      
      // Test token validation if authenticated
      if (this.isAuthenticated()) {
        try {
          const refreshResult = await this.refresh();
          details.tokenValidation = refreshResult.success;
        } catch {
          details.tokenValidation = false;
        }
      } else {
        details.tokenValidation = true; // N/A when not authenticated
      }
      
      const latency = Date.now() - startTime;
      
      // Determine overall status
      const healthyCount = Object.values(details).filter(Boolean).length;
      const totalChecks = Object.keys(details).length - Object.keys(details.oauthProviders).length + 1;
      
      let status: 'healthy' | 'degraded' | 'unhealthy';
      if (healthyCount === totalChecks) {
        status = 'healthy';
      } else if (healthyCount >= totalChecks * 0.5) {
        status = 'degraded';
      } else {
        status = 'unhealthy';
      }
      
      return { status, details, latency };
      
    } catch (error) {
      return {
        status: 'unhealthy',
        details,
        latency: Date.now() - startTime
      };
    }
  }
  
  // Private helper methods
  
  private static enhanceConfig(config: EasyAuthConfig): EasyAuthConfig {
    const env = detectEnvironment();
    
    return {
      // Defaults
      apiVersion: '1.0',
      timeout: 30000,
      retryAttempts: 3,
      enableLogging: false,
      environment: process.env.NODE_ENV as any || 'development',
      useDevCredentials: false,
      enableStateManagement: false,
      enableTokenAutoRefresh: true,
      refreshThresholdMinutes: 5,
      enableSessionPersistence: true,
      storageType: getOptimalStorageAdapter(),
      
      // User config overrides
      ...config
    };
  }
  
  private static createOptimalStorage(config: EasyAuthConfig): StorageAdapter {
    switch (config.storageType) {
      case 'localStorage':
        return new LocalStorageAdapter();
      case 'sessionStorage':
        return new SessionStorageAdapter();
      case 'memory':
        return new MemoryStorageAdapter();
      default:
        return new LocalStorageAdapter();
    }
  }
  
  private initializeEnhancedFeatures(config: EasyAuthConfig): void {
    // Initialize credential helper
    if (config.useDevCredentials || config.useOAuthProxy) {
      this.credentialHelper = new CredentialHelper(
        config.environment === 'development',
        config.useOAuthProxy ? config.proxyConfig : undefined
      );
    }
    
    // Initialize auto-refresh if session exists and is authenticated
    if (config.enableTokenAutoRefresh && this.isAuthenticated()) {
      this.startAutoRefresh();
    }
  }
  
  private enhanceLoginOptions(options: LoginOptions): LoginOptions {
    // Auto-select flow based on environment capabilities
    if (!this.environment.supportsPopup && !options.returnUrl) {
      // Fallback to redirect flow if popup not supported
      options.returnUrl = window?.location?.href || options.returnUrl;
    }
    
    return options;
  }
  
  private handleEnhancedError(error: unknown): AuthResult {
    let authError: AuthError;
    
    if (error instanceof Error) {
      // Provide enhanced error context
      authError = {
        code: 'ENHANCED_AUTH_ERROR',
        message: error.message,
        details: {
          originalError: error.name,
          environment: this.environment,
          config: {
            useProxy: this.config.useOAuthProxy,
            environment: this.config.environment
          }
        }
      };
    } else {
      authError = {
        code: 'UNKNOWN_ENHANCED_ERROR',
        message: 'An unknown error occurred during authentication',
        details: { originalError: String(error) }
      };
    }
    
    return { success: false, error: authError };
  }
  
  private startAutoRefresh(): void {
    if (this.autoRefreshInterval) {
      clearInterval(this.autoRefreshInterval);
    }
    
    const refreshThreshold = (this.config.refreshThresholdMinutes || 5) * 60 * 1000;
    
    this.autoRefreshInterval = setInterval(async () => {
      if (this.isAuthenticated() && this.shouldRefreshToken()) {
        try {
          await this.refresh();
        } catch (error) {
          this.logger.warn('Auto-refresh failed', error);
          // Emit session expired event
          this.emitEvent('session_expired', { error: this.handleError(error) });
        }
      }
    }, refreshThreshold);
  }
  
  private stopAutoRefresh(): void {
    if (this.autoRefreshInterval) {
      clearInterval(this.autoRefreshInterval);
      this.autoRefreshInterval = undefined;
    }
  }
  
  private shouldRefreshToken(): boolean {
    if (!this.session.expiresAt) {
      return false;
    }
    
    const now = new Date();
    const threshold = (this.config.refreshThresholdMinutes || 5) * 60 * 1000;
    const timeUntilExpiry = this.session.expiresAt.getTime() - now.getTime();
    
    return timeUntilExpiry <= threshold;
  }
  
  // Use parent's protected emitEvent method
}