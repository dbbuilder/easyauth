// Reuse the same types from React package - unified API structure
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

// Vue-specific types
export interface UseAuthReturn {
  // Reactive state
  isLoading: Ref<boolean>;
  isAuthenticated: Ref<boolean>;
  user: Ref<UserInfo | null>;
  error: Ref<string | null>;
  tokenExpiry: Ref<Date | null>;
  sessionId: Ref<string | null>;
  // Methods
  login: (provider: string, returnUrl?: string) => Promise<LoginResult>;
  logout: () => Promise<LogoutResult>;
  refreshToken: () => Promise<boolean>;
  checkAuth: () => Promise<boolean>;
  clearError: () => void;
}

export interface UseAuthQueryOptions<T> {
  enabled?: boolean;
  refetchOnWindowFocus?: boolean;
  staleTime?: number;
  onError?: (error: any) => void;
  onSuccess?: (data: T) => void;
}

export interface UseAuthQueryReturn<T> {
  data: Ref<T | undefined>;
  error: Ref<Error | null>;
  isLoading: Ref<boolean>;
  isError: Ref<boolean>;
  isSuccess: Ref<boolean>;
  refetch: () => Promise<void>;
}

// Import Ref type for Vue composables
import type { Ref } from 'vue';