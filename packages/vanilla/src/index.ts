// Core exports
export { EasyAuthClient } from './core/client';
export { AuthApi } from './core/api';

// UI utilities
export { EasyAuthUI } from './ui/elements';
export type { 
  LoginButtonOptions, 
  LogoutButtonOptions, 
  UserProfileOptions 
} from './ui/elements';

// Storage utilities
export { createAuthStorage } from './utils/storage';
export type { AuthStorage } from './utils/storage';

// Types
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
  AuthStateChangeEvent,
  LoginEvent,
  AuthErrorEvent,
  AuthStateChangeCallback,
  AuthEventCallback
} from './types';

// Default export for convenience
export { EasyAuthClient as default } from './core/client';