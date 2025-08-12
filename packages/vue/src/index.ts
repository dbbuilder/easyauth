// Main exports for @easyauth/vue
export * from './composables';
export * from './components';
export * from './types';
export { authApi } from './utils/api';
export { createAuthStorage } from './utils/storage';
export { EasyAuthPlugin } from './plugins/easyauth';

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
  UseAuthReturn,
  UseAuthQueryReturn,
  UseAuthQueryOptions,
} from './types';