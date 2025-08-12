import { 
  AuthState, 
  EasyAuthConfig, 
  LoginResult, 
  LogoutResult, 
  UserInfo, 
  AuthStateChangeCallback,
  AuthEventCallback
} from '../types';
import { AuthApi } from './api';
import { createAuthStorage, AuthStorage } from '../utils/storage';

export class EasyAuthClient extends EventTarget {
  private api: AuthApi;
  private storage: AuthStorage;
  private config: EasyAuthConfig = {};
  private state: AuthState = {
    isLoading: true,
    isAuthenticated: false,
    user: null,
    error: null,
    tokenExpiry: null,
    sessionId: null,
  };
  private refreshTokenTimeout?: number;
  private stateChangeCallbacks: Set<AuthStateChangeCallback> = new Set();

  constructor(config: Partial<EasyAuthConfig> = {}) {
    super();
    this.api = new AuthApi();
    this.storage = createAuthStorage(config.storage || 'localStorage');
    this.configure(config);
    
    // Auto-check authentication on initialization
    this.checkAuth().catch(() => {
      // Silently fail initial auth check
    });
  }

  configure(config: Partial<EasyAuthConfig>): void {
    this.config = { ...this.config, ...config };
    this.api.configure(config);
    
    if (config.storage) {
      this.storage = createAuthStorage(config.storage);
    }
  }

  // State management
  private updateState(updates: Partial<AuthState>): void {
    const previousState = { ...this.state };
    this.state = { ...this.state, ...updates };

    // Notify listeners
    this.stateChangeCallbacks.forEach(callback => {
      try {
        callback(this.state);
      } catch (error) {
        console.error('[EasyAuth] Error in state change callback:', error);
      }
    });

    // Dispatch custom event
    this.dispatchEvent(new CustomEvent('statechange', { detail: this.state }));

    // Handle side effects
    this.handleStateChange(this.state, previousState);
  }

  private handleStateChange(newState: AuthState, previousState: AuthState): void {
    // Handle authentication requirement
    if (!newState.isLoading && !newState.isAuthenticated && this.config.onLoginRequired) {
      this.config.onLoginRequired();
    }

    // Handle errors
    if (newState.error && this.config.onError) {
      this.config.onError(newState.error);
    }

    // Set up auto-refresh when token expiry changes
    if (newState.tokenExpiry !== previousState.tokenExpiry) {
      this.setupAutoRefresh(newState);
    }
  }

  private setupAutoRefresh(state: AuthState): void {
    // Clear existing timeout
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
    }

    if (!state.isAuthenticated || !state.tokenExpiry || !this.config.autoRefresh) {
      return;
    }

    const refreshTime = state.tokenExpiry.getTime() - Date.now() - 60000; // Refresh 1 minute before expiry
    
    if (refreshTime > 0) {
      this.refreshTokenTimeout = window.setTimeout(() => {
        this.refreshToken().catch(() => {
          // If refresh fails, trigger onTokenExpired callback
          this.config.onTokenExpired?.();
        });
      }, refreshTime);
    }
  }

  // Public API methods
  get currentState(): AuthState {
    return { ...this.state };
  }

  get isAuthenticated(): boolean {
    return this.state.isAuthenticated;
  }

  get user(): UserInfo | null {
    return this.state.user;
  }

  get isLoading(): boolean {
    return this.state.isLoading;
  }

  get error(): string | null {
    return this.state.error;
  }

  // Event listeners
  onStateChange(callback: AuthStateChangeCallback): () => void {
    this.stateChangeCallbacks.add(callback);
    
    // Return unsubscribe function
    return () => {
      this.stateChangeCallbacks.delete(callback);
    };
  }

  // Authentication methods
  async login(provider: string, returnUrl?: string): Promise<LoginResult> {
    try {
      this.updateState({ isLoading: true, error: null });

      this.dispatchEvent(new CustomEvent('loginstart', { 
        detail: { provider, returnUrl } 
      }));

      const result = await this.api.login({ provider, returnUrl });

      if (result.redirectRequired && result.authUrl) {
        // For OAuth flows, redirect to provider
        window.location.href = result.authUrl;
      }

      this.dispatchEvent(new CustomEvent('loginsuccess', { detail: result }));
      return result;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
      this.updateState({ 
        isLoading: false, 
        error: errorMessage 
      });
      
      this.dispatchEvent(new CustomEvent('loginerror', { detail: error }));
      throw error;
    } finally {
      this.updateState({ isLoading: false });
    }
  }

  async logout(): Promise<LogoutResult> {
    try {
      this.updateState({ isLoading: true });

      this.dispatchEvent(new CustomEvent('logoutstart'));

      const result = await this.api.logout();

      // Clear local storage
      this.storage.clear();
      
      // Clear auto-refresh timeout
      if (this.refreshTokenTimeout) {
        clearTimeout(this.refreshTokenTimeout);
      }

      this.updateState({
        isLoading: false,
        isAuthenticated: false,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null,
      });

      if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
      }

      this.dispatchEvent(new CustomEvent('logoutsuccess', { detail: result }));
      return result;
    } catch (error) {
      // Even if logout fails on server, clear local state
      this.storage.clear();
      if (this.refreshTokenTimeout) {
        clearTimeout(this.refreshTokenTimeout);
      }
      this.updateState({
        isLoading: false,
        isAuthenticated: false,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null,
      });

      this.dispatchEvent(new CustomEvent('logouterror', { detail: error }));
      return { loggedOut: true };
    }
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = this.storage.getRefreshToken();
      if (!refreshToken) {
        return false;
      }

      const result = await this.api.refreshToken({ refreshToken });

      if (result.accessToken) {
        this.storage.setAccessToken(result.accessToken);
        if (result.refreshToken) {
          this.storage.setRefreshToken(result.refreshToken);
        }

        // Re-check auth status after refresh
        await this.checkAuth();
        return true;
      }

      return false;
    } catch (error) {
      // If refresh fails, clear tokens and force re-login
      this.storage.clear();
      this.updateState({
        isLoading: false,
        isAuthenticated: false,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null,
      });
      return false;
    }
  }

  async checkAuth(): Promise<boolean> {
    try {
      this.updateState({ isLoading: true });

      const authStatus = await this.api.checkAuth();

      this.updateState({
        isLoading: false,
        isAuthenticated: authStatus.isAuthenticated,
        user: authStatus.user || null,
        tokenExpiry: authStatus.tokenExpiry ? new Date(authStatus.tokenExpiry) : null,
        sessionId: authStatus.sessionId || null,
        error: null,
      });

      return authStatus.isAuthenticated;
    } catch (error) {
      // Check if it's a token expired error
      if (error instanceof Error && error.message.includes('TOKEN_EXPIRED')) {
        const refreshSuccess = await this.refreshToken();
        if (refreshSuccess) {
          return await this.checkAuth();
        }
      }

      this.updateState({ 
        isLoading: false, 
        error: error instanceof Error ? error.message : 'Auth check failed' 
      });
      return false;
    }
  }

  clearError(): void {
    this.updateState({ error: null });
  }

  async getUserProfile(): Promise<UserInfo> {
    const user = await this.api.getUserProfile();
    this.updateState({ user });
    return user;
  }

  // Authorization helpers
  hasRole(role: string): boolean {
    return this.state.user ? this.state.user.roles.includes(role) : false;
  }

  hasPermission(permission: string): boolean {
    return this.state.user ? (this.state.user.permissions || []).includes(permission) : false;
  }

  hasAnyRole(roles: string[]): boolean {
    return this.state.user ? roles.some(role => this.state.user!.roles.includes(role)) : false;
  }

  hasAnyPermission(permissions: string[]): boolean {
    return this.state.user ? permissions.some(permission => (this.state.user!.permissions || []).includes(permission)) : false;
  }

  // Cleanup
  destroy(): void {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
    }
    this.stateChangeCallbacks.clear();
  }
}