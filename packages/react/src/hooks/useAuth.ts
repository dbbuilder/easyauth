import { useCallback } from 'react';
import { useAuthContext } from '../context/AuthContext';
import { AuthState, AuthActions } from '../types';

export interface UseAuthReturn extends AuthState {
  login: AuthActions['login'];
  logout: AuthActions['logout'];
  refreshToken: AuthActions['refreshToken'];
  checkAuth: AuthActions['checkAuth'];
  clearError: AuthActions['clearError'];
}

export function useAuth(): UseAuthReturn {
  const { actions, ...state } = useAuthContext();

  const login = useCallback(
    (provider: string, returnUrl?: string) => actions.login(provider, returnUrl),
    [actions.login]
  );

  const logout = useCallback(
    () => actions.logout(),
    [actions.logout]
  );

  const refreshToken = useCallback(
    () => actions.refreshToken(),
    [actions.refreshToken]
  );

  const checkAuth = useCallback(
    () => actions.checkAuth(),
    [actions.checkAuth]
  );

  const clearError = useCallback(
    () => actions.clearError(),
    [actions.clearError]
  );

  return {
    ...state,
    login,
    logout,
    refreshToken,
    checkAuth,
    clearError,
  };
}