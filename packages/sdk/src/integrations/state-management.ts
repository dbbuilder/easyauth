/**
 * State Management Framework Integrations
 */

import { AuthSession, UserProfile, AuthEvent, AuthEventData } from '../types';

// Redux Integration
export interface ReduxAuthState {
  session: AuthSession;
  isLoading: boolean;
  error: string | null;
}

export interface ReduxAuthActions {
  LOGIN_START: 'EASYAUTH_LOGIN_START';
  LOGIN_SUCCESS: 'EASYAUTH_LOGIN_SUCCESS'; 
  LOGIN_FAILURE: 'EASYAUTH_LOGIN_FAILURE';
  LOGOUT: 'EASYAUTH_LOGOUT';
  TOKEN_REFRESH: 'EASYAUTH_TOKEN_REFRESH';
  CLEAR_ERROR: 'EASYAUTH_CLEAR_ERROR';
}

export const reduxAuthActions: ReduxAuthActions = {
  LOGIN_START: 'EASYAUTH_LOGIN_START',
  LOGIN_SUCCESS: 'EASYAUTH_LOGIN_SUCCESS',
  LOGIN_FAILURE: 'EASYAUTH_LOGIN_FAILURE',
  LOGOUT: 'EASYAUTH_LOGOUT',
  TOKEN_REFRESH: 'EASYAUTH_TOKEN_REFRESH',
  CLEAR_ERROR: 'EASYAUTH_CLEAR_ERROR'
};

export function createReduxReducer() {
  const initialState: ReduxAuthState = {
    session: { isAuthenticated: false, user: null },
    isLoading: false,
    error: null
  };

  return function authReducer(state = initialState, action: any): ReduxAuthState {
    switch (action.type) {
      case reduxAuthActions.LOGIN_START:
        return {
          ...state,
          isLoading: true,
          error: null
        };

      case reduxAuthActions.LOGIN_SUCCESS:
        return {
          ...state,
          isLoading: false,
          session: action.payload.session,
          error: null
        };

      case reduxAuthActions.LOGIN_FAILURE:
        return {
          ...state,
          isLoading: false,
          error: action.payload.error,
          session: { isAuthenticated: false, user: null }
        };

      case reduxAuthActions.LOGOUT:
        return {
          ...state,
          session: { isAuthenticated: false, user: null },
          error: null
        };

      case reduxAuthActions.TOKEN_REFRESH:
        return {
          ...state,
          session: action.payload.session
        };

      case reduxAuthActions.CLEAR_ERROR:
        return {
          ...state,
          error: null
        };

      default:
        return state;
    }
  };
}

// Zustand Integration
export interface ZustandAuthStore {
  session: AuthSession;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setSession: (session: AuthSession) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearAuth: () => void;
}

export function createZustandStore(): ZustandAuthStore {
  return {
    session: { isAuthenticated: false, user: null },
    isLoading: false,
    error: null,
    
    setSession: (session: AuthSession) => ({ session }),
    setLoading: (isLoading: boolean) => ({ isLoading }),
    setError: (error: string | null) => ({ error }),
    clearAuth: () => ({
      session: { isAuthenticated: false, user: null },
      error: null
    })
  };
}

// Pinia Integration (Vue)
export interface PiniaAuthState {
  session: AuthSession;
  isLoading: boolean;
  error: string | null;
}

export function createPiniaStore() {
  return {
    id: 'easyauth',
    state: (): PiniaAuthState => ({
      session: { isAuthenticated: false, user: null },
      isLoading: false,
      error: null
    }),
    
    getters: {
      isAuthenticated: (state: PiniaAuthState) => state.session.isAuthenticated,
      user: (state: PiniaAuthState) => state.session.user,
      hasError: (state: PiniaAuthState) => state.error !== null
    },
    
    actions: {
      setSession(session: AuthSession) {
        this.session = session;
      },
      
      setLoading(loading: boolean) {
        this.isLoading = loading;
      },
      
      setError(error: string | null) {
        this.error = error;
      },
      
      clearAuth() {
        this.session = { isAuthenticated: false, user: null };
        this.error = null;
      }
    }
  };
}

// React Context Integration
export interface ReactAuthContextValue {
  session: AuthSession;
  isLoading: boolean;
  error: string | null;
  login: (provider: string, options?: any) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
}

// Event Handler Factory for connecting EasyAuth to state management
export class StateManagementConnector {
  private storeType: 'redux' | 'zustand' | 'pinia' | 'context';
  private store: any;
  private dispatch?: (action: any) => void;
  
  constructor(storeType: 'redux' | 'zustand' | 'pinia' | 'context', store: any, dispatch?: (action: any) => void) {
    this.storeType = storeType;
    this.store = store;
    this.dispatch = dispatch;
  }
  
  /**
   * Create event handlers that sync EasyAuth state with your store
   */
  createEventHandlers() {
    return {
      onLogin: (data: AuthEventData) => {
        this.updateAuthState({
          session: {
            isAuthenticated: true,
            user: data.user || null,
            token: data.token
          },
          isLoading: false,
          error: null
        });
      },
      
      onLogout: () => {
        this.updateAuthState({
          session: { isAuthenticated: false, user: null },
          isLoading: false,
          error: null
        });
      },
      
      onTokenRefresh: (data: AuthEventData) => {
        this.updateAuthState({
          session: {
            isAuthenticated: true,
            user: data.user || this.getCurrentUser(),
            token: data.token
          }
        });
      },
      
      onError: (data: AuthEventData) => {
        this.updateAuthState({
          isLoading: false,
          error: data.error?.message || 'Authentication error occurred'
        });
      },
      
      onLoadingStart: () => {
        this.updateAuthState({ isLoading: true, error: null });
      }
    };
  }
  
  private updateAuthState(updates: Partial<{ session: AuthSession; isLoading: boolean; error: string | null }>) {
    switch (this.storeType) {
      case 'redux':
        if (this.dispatch) {
          if (updates.session) {
            this.dispatch({
              type: reduxAuthActions.LOGIN_SUCCESS,
              payload: { session: updates.session }
            });
          }
          if (updates.error) {
            this.dispatch({
              type: reduxAuthActions.LOGIN_FAILURE,
              payload: { error: updates.error }
            });
          }
          if (updates.isLoading !== undefined) {
            this.dispatch({
              type: reduxAuthActions.LOGIN_START
            });
          }
        }
        break;
        
      case 'zustand':
        if (updates.session) this.store.setSession(updates.session);
        if (updates.isLoading !== undefined) this.store.setLoading(updates.isLoading);
        if (updates.error !== undefined) this.store.setError(updates.error);
        break;
        
      case 'pinia':
        if (updates.session) this.store.setSession(updates.session);
        if (updates.isLoading !== undefined) this.store.setLoading(updates.isLoading);
        if (updates.error !== undefined) this.store.setError(updates.error);
        break;
        
      case 'context':
        // For React context, updates would be handled by setState
        // This would be implemented in the specific React hook
        break;
    }
  }
  
  private getCurrentUser(): UserProfile | null {
    switch (this.storeType) {
      case 'redux':
        return this.store.getState?.()?.auth?.session?.user || null;
      case 'zustand':
        return this.store.session?.user || null;
      case 'pinia':
        return this.store.session?.user || null;
      default:
        return null;
    }
  }
}

// Type exports for external use
export type StateStore = ZustandAuthStore | any; // any for Redux/Pinia stores
export type StateDispatch = ((action: any) => void) | undefined;