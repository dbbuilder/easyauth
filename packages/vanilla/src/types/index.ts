// Core API types that match the backend
export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
  errorDetails?: any;
  timestamp: string;
  correlationId?: string;
  version: string;
  meta?: Record<string, any>;
}

export interface AuthStatus {
  isAuthenticated: boolean;
  user?: UserInfo;
  tokenExpiry?: string;
  sessionId?: string;
}

export interface UserInfo {
  id: string;
  email?: string;
  name?: string;
  firstName?: string;
  lastName?: string;
  profilePicture?: string;
  provider?: string;
  roles: string[];
  permissions?: string[];
  lastLogin?: string;
  isVerified: boolean;
  locale?: string;
  timeZone?: string;
}

export interface LoginRequest {
  provider: string;
  email?: string;
  returnUrl?: string;
}

export interface LoginResult {
  authUrl?: string;
  provider?: string;
  state?: string;
  redirectRequired: boolean;
}

export interface TokenRefreshRequest {
  refreshToken: string;
}

export interface TokenRefreshResult {
  accessToken?: string;
  refreshToken?: string;
  tokenType: string;
  expiresIn: number;
}

export interface LogoutResult {
  loggedOut: boolean;
  redirectUrl?: string;
}

export interface EasyAuthConfig {
  baseUrl?: string;
  autoRefresh?: boolean;
  onTokenExpired?: () => void;
  onLoginRequired?: () => void;
  onError?: (error: any) => void;
  storage?: 'localStorage' | 'sessionStorage' | 'memory';
  debug?: boolean;
}

export interface AuthState {
  isLoading: boolean;
  isAuthenticated: boolean;
  user: UserInfo | null;
  error: string | null;
  tokenExpiry: Date | null;
  sessionId: string | null;
}

// Event types for vanilla JS
export interface AuthStateChangeEvent extends CustomEvent {
  detail: AuthState;
}

export interface LoginEvent extends CustomEvent {
  detail: { provider: string; returnUrl?: string };
}

export interface AuthErrorEvent extends CustomEvent {
  detail: { error: string; details?: any };
}

// Callback types
export type AuthStateChangeCallback = (state: AuthState) => void;
export type AuthEventCallback<T = any> = (data: T) => void;