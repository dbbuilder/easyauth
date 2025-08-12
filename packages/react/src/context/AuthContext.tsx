import { createContext, useContext, useReducer, useEffect, ReactNode } from 'react';
import { AuthState, AuthActions, EasyAuthConfig } from '../types';
import { authApi } from '../utils/api';
import { createAuthStorage } from '../utils/storage';

interface AuthContextValue extends AuthState {
  actions: AuthActions;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthAction {
  type: 'SET_LOADING' | 'SET_AUTHENTICATED' | 'SET_ERROR' | 'CLEAR_ERROR' | 'SET_USER' | 'LOGOUT';
  payload?: any;
}

const initialState: AuthState = {
  isLoading: true,
  isAuthenticated: false,
  user: null,
  error: null,
  tokenExpiry: null,
  sessionId: null,
};

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
      
    case 'SET_AUTHENTICATED':
      return {
        ...state,
        isLoading: false,
        isAuthenticated: action.payload.isAuthenticated,
        user: action.payload.user || null,
        tokenExpiry: action.payload.tokenExpiry ? new Date(action.payload.tokenExpiry) : null,
        sessionId: action.payload.sessionId || null,
        error: null,
      };
      
    case 'SET_ERROR':
      return {
        ...state,
        isLoading: false,
        error: action.payload,
      };
      
    case 'CLEAR_ERROR':
      return { ...state, error: null };
      
    case 'SET_USER':
      return { ...state, user: action.payload };
      
    case 'LOGOUT':
      return {
        ...initialState,
        isLoading: false,
      };
      
    default:
      return state;
  }
}

interface AuthProviderProps {
  children: ReactNode;
  config?: Partial<EasyAuthConfig>;
}

export function AuthProvider({ children, config = {} }: AuthProviderProps) {
  const [state, dispatch] = useReducer(authReducer, initialState);
  const storage = createAuthStorage(config.storage || 'localStorage');

  // Initialize API with config
  authApi.configure(config);

  const actions: AuthActions = {
    login: async (provider: string, returnUrl?: string) => {
      try {
        dispatch({ type: 'SET_LOADING', payload: true });
        dispatch({ type: 'CLEAR_ERROR' });
        
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
        dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Login failed' });
        throw error;
      } finally {
        dispatch({ type: 'SET_LOADING', payload: false });
      }
    },

    logout: async () => {
      try {
        dispatch({ type: 'SET_LOADING', payload: true });
        
        const result = await authApi.logout();
        
        // Clear local storage
        storage.clear();
        
        dispatch({ type: 'LOGOUT' });
        
        if (result.redirectUrl) {
          window.location.href = result.redirectUrl;
        }
        
        return result;
      } catch (error) {
        // Even if logout fails on server, clear local state
        storage.clear();
        dispatch({ type: 'LOGOUT' });
        return { loggedOut: true };
      } finally {
        dispatch({ type: 'SET_LOADING', payload: false });
      }
    },

    refreshToken: async (): Promise<boolean> => {
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
          await actions.checkAuth();
          return true;
        }
        
        return false;
      } catch (error) {
        // If refresh fails, clear tokens and force re-login
        storage.clear();
        dispatch({ type: 'LOGOUT' });
        return false;
      }
    },

    checkAuth: async (): Promise<boolean> => {
      try {
        dispatch({ type: 'SET_LOADING', payload: true });
        
        const authStatus = await authApi.checkAuth();
        
        dispatch({ 
          type: 'SET_AUTHENTICATED', 
          payload: authStatus
        });
        
        return authStatus.isAuthenticated;
      } catch (error) {
        // Check if it's a token expired error
        if (error instanceof Error && error.message.includes('TOKEN_EXPIRED')) {
          const refreshSuccess = await actions.refreshToken();
          if (refreshSuccess) {
            return await actions.checkAuth();
          }
        }
        
        dispatch({ type: 'SET_ERROR', payload: error instanceof Error ? error.message : 'Auth check failed' });
        return false;
      } finally {
        dispatch({ type: 'SET_LOADING', payload: false });
      }
    },

    clearError: () => {
      dispatch({ type: 'CLEAR_ERROR' });
    },
  };

  // Auto-check authentication on mount
  useEffect(() => {
    actions.checkAuth();
  }, []);

  // Auto-refresh tokens before expiry
  useEffect(() => {
    if (!state.isAuthenticated || !state.tokenExpiry || !config.autoRefresh) {
      return;
    }

    const refreshTime = state.tokenExpiry.getTime() - Date.now() - 60000; // Refresh 1 minute before expiry
    
    if (refreshTime > 0) {
      const timeout = setTimeout(() => {
        actions.refreshToken().catch(() => {
          // If refresh fails, trigger onTokenExpired callback
          config.onTokenExpired?.();
        });
      }, refreshTime);

      return () => clearTimeout(timeout);
    }
    
    return; // Return undefined for consistency
  }, [state.tokenExpiry, state.isAuthenticated, config.autoRefresh, config.onTokenExpired]);

  // Handle authentication requirement
  useEffect(() => {
    if (!state.isLoading && !state.isAuthenticated && config.onLoginRequired) {
      config.onLoginRequired();
    }
  }, [state.isLoading, state.isAuthenticated, config.onLoginRequired]);

  // Handle errors
  useEffect(() => {
    if (state.error && config.onError) {
      config.onError(state.error);
    }
  }, [state.error, config.onError]);

  const contextValue: AuthContextValue = {
    ...state,
    actions,
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuthContext(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuthContext must be used within an AuthProvider');
  }
  return context;
}