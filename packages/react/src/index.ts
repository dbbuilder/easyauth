// Main exports for @easyauth/react
export { AuthProvider, useAuthContext } from './context/AuthContext';
export * from './hooks';
export * from './components';
export * from './types/index';
export { authApi } from './utils/api';

// Re-export commonly used types for convenience
export type {
  ApiResponse,
  AuthStatus,
  UserInfo,
  LoginRequest,
  LoginResult,
  TokenRefreshRequest,
  TokenRefreshResult,
  LogoutResult,
  EasyAuthConfig,
  AuthState,
  AuthActions,
} from './types';