import { reactive, computed, watch, inject, provide } from 'vue';
import type { 
  UseAuthReturn, 
  AuthState, 
  EasyAuthConfig, 
  LoginResult, 
  LogoutResult 
} from '../types';
import { authApi } from '../utils/api';
import { createAuthStorage } from '../utils/storage';

const AUTH_INJECTION_KEY = Symbol('EasyAuth');

// Global auth state - shared across all components
const globalAuthState = reactive<AuthState>({
  isLoading: true,
  isAuthenticated: false,
  user: null,
  error: null,
  tokenExpiry: null,
  sessionId: null,
});

let isInitialized = false;
let config: Partial<EasyAuthConfig> = {};
let storage = createAuthStorage('localStorage');

export function createEasyAuth(authConfig: Partial<EasyAuthConfig> = {}) {
  config = { ...config, ...authConfig };
  storage = createAuthStorage(config.storage || 'localStorage');
  authApi.configure(config);
  
  // Auto-check authentication on initialization
  if (!isInitialized) {
    isInitialized = true;
    checkAuth();
  }

  const authMethods = {
    async login(provider: string, returnUrl?: string): Promise<LoginResult> {
      try {
        globalAuthState.isLoading = true;
        globalAuthState.error = null;
        
        const result = await authApi.login({ 
          provider, 
          ...(returnUrl && { returnUrl })
        });
        
        if (result.redirectRequired && result.authUrl) {
          // For OAuth flows, redirect to provider
          window.location.href = result.authUrl;
        }
        
        return result;
      } catch (error) {
        globalAuthState.error = error instanceof Error ? error.message : 'Login failed';
        throw error;
      } finally {
        globalAuthState.isLoading = false;
      }
    },

    async logout(): Promise<LogoutResult> {
      try {
        globalAuthState.isLoading = true;
        
        const result = await authApi.logout();
        
        // Clear local storage
        storage.clear();
        
        // Reset state
        globalAuthState.isAuthenticated = false;
        globalAuthState.user = null;
        globalAuthState.tokenExpiry = null;
        globalAuthState.sessionId = null;
        globalAuthState.error = null;
        
        if (result.redirectUrl) {
          window.location.href = result.redirectUrl;
        }
        
        return result;
      } catch (error) {
        // Even if logout fails on server, clear local state
        storage.clear();
        globalAuthState.isAuthenticated = false;
        globalAuthState.user = null;
        globalAuthState.tokenExpiry = null;
        globalAuthState.sessionId = null;
        return { loggedOut: true };
      } finally {
        globalAuthState.isLoading = false;
      }
    },

    async refreshToken(): Promise<boolean> {
      try {
        const refreshToken = storage.getRefreshToken();
        if (!refreshToken) {
          return false;
        }

        const result = await authApi.refreshToken({ refreshToken });
        
        if (result.accessToken) {
          storage.setAccessToken(result.accessToken);
          if (result.refreshToken) {
            storage.setRefreshToken(result.refreshToken);
          }
          
          // Re-check auth status after refresh
          await checkAuth();
          return true;
        }
        
        return false;
      } catch (error) {
        // If refresh fails, clear tokens and force re-login
        storage.clear();
        globalAuthState.isAuthenticated = false;
        globalAuthState.user = null;
        globalAuthState.tokenExpiry = null;
        globalAuthState.sessionId = null;
        return false;
      }
    },

    async checkAuth(): Promise<boolean> {
      try {
        globalAuthState.isLoading = true;
        
        const authStatus = await authApi.checkAuth();
        
        globalAuthState.isAuthenticated = authStatus.isAuthenticated;
        globalAuthState.user = authStatus.user || null;
        globalAuthState.tokenExpiry = authStatus.tokenExpiry ? new Date(authStatus.tokenExpiry) : null;
        globalAuthState.sessionId = authStatus.sessionId || null;
        globalAuthState.error = null;
        
        return authStatus.isAuthenticated;
      } catch (error) {
        // Check if it's a token expired error
        if (error instanceof Error && error.message.includes('TOKEN_EXPIRED')) {
          const refreshSuccess = await authMethods.refreshToken();
          if (refreshSuccess) {
            return await authMethods.checkAuth();
          }
        }
        
        globalAuthState.error = error instanceof Error ? error.message : 'Auth check failed';
        globalAuthState.isAuthenticated = false;
        globalAuthState.user = null;
        return false;
      } finally {
        globalAuthState.isLoading = false;
      }
    },

    clearError() {
      globalAuthState.error = null;
    }
  };

  // Set up auto-refresh
  if (config.autoRefresh) {
    watch(() => globalAuthState.tokenExpiry, (newExpiry) => {
      if (!newExpiry || !globalAuthState.isAuthenticated) return;
      
      const refreshTime = newExpiry.getTime() - Date.now() - 60000; // Refresh 1 minute before expiry
      
      if (refreshTime > 0) {
        setTimeout(() => {
          authMethods.refreshToken().catch(() => {
            config.onTokenExpired?.();
          });
        }, refreshTime);
      }
    });
  }

  // Watch for auth state changes
  watch(() => globalAuthState.isAuthenticated, (isAuth) => {
    if (!globalAuthState.isLoading && !isAuth && config.onLoginRequired) {
      config.onLoginRequired();
    }
  });

  watch(() => globalAuthState.error, (error) => {
    if (error && config.onError) {
      config.onError(error);
    }
  });

  // Convert reactive state to refs for Vue composition API
  const authReturn: UseAuthReturn = {
    // Reactive state as refs
    isLoading: computed(() => globalAuthState.isLoading),
    isAuthenticated: computed(() => globalAuthState.isAuthenticated),
    user: computed(() => globalAuthState.user),
    error: computed(() => globalAuthState.error),
    tokenExpiry: computed(() => globalAuthState.tokenExpiry),
    sessionId: computed(() => globalAuthState.sessionId),
    // Methods
    ...authMethods,
  };

  // Provide auth context for injection
  provide(AUTH_INJECTION_KEY, authReturn);
  
  return authReturn;
}

export function useAuth(): UseAuthReturn {
  const auth = inject<UseAuthReturn>(AUTH_INJECTION_KEY);
  
  if (!auth) {
    // If no injected auth, create a standalone instance
    return createEasyAuth();
  }
  
  return auth;
}

// Helper function for initial auth check
async function checkAuth() {
  try {
    globalAuthState.isLoading = true;
    
    const authStatus = await authApi.checkAuth();
    
    globalAuthState.isAuthenticated = authStatus.isAuthenticated;
    globalAuthState.user = authStatus.user || null;
    globalAuthState.tokenExpiry = authStatus.tokenExpiry ? new Date(authStatus.tokenExpiry) : null;
    globalAuthState.sessionId = authStatus.sessionId || null;
    globalAuthState.error = null;
    
  } catch (error) {
    globalAuthState.error = error instanceof Error ? error.message : 'Auth check failed';
    globalAuthState.isAuthenticated = false;
    globalAuthState.user = null;
  } finally {
    globalAuthState.isLoading = false;
  }
}