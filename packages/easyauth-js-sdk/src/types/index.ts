/**
 * Core TypeScript definitions for EasyAuth JS SDK
 * Comprehensive type definitions for authentication flows
 */

// #region Provider Types

export type AuthProvider = 
  | 'google'
  | 'apple' 
  | 'facebook'
  | 'azure-b2c'
  | 'custom';

export interface ProviderConfig {
  clientId: string;
  clientSecret?: string;
  enabled: boolean;
  scopes?: string[];
  customParams?: Record<string, string>;
}

export interface GoogleProviderConfig extends ProviderConfig {
  hostedDomain?: string;
  includeGrantedScopes?: boolean;
}

export interface AppleProviderConfig extends ProviderConfig {
  teamId: string;
  keyId: string;
  privateKey: string;
  bundleId?: string;
}

export interface FacebookProviderConfig extends ProviderConfig {
  appSecret: string;
  graphApiVersion?: string;
}

export interface AzureB2CProviderConfig extends ProviderConfig {
  tenantId: string;
  tenantName: string;
  signInPolicy: string;
  resetPasswordPolicy?: string;
  editProfilePolicy?: string;
}

export interface CustomProviderConfig extends ProviderConfig {
  authorizationUrl: string;
  tokenUrl: string;
  userInfoUrl?: string;
  revokeUrl?: string;
}

export type ProviderConfigs = {
  google?: GoogleProviderConfig;
  apple?: AppleProviderConfig;
  facebook?: FacebookProviderConfig;
  'azure-b2c'?: AzureB2CProviderConfig;
  custom?: Record<string, CustomProviderConfig>;
};

// #endregion

// #region Configuration Types

export interface AuthConfig {
  apiBaseUrl: string;
  providers: ProviderConfigs;
  defaultProvider?: AuthProvider;
  session?: SessionConfig;
  security?: SecurityConfig;
  ui?: UIConfig;
}

export interface SessionConfig {
  storage: 'localStorage' | 'sessionStorage' | 'memory' | 'cookie';
  expirationMinutes?: number;
  refreshThresholdMinutes?: number;
  autoRefresh?: boolean;
  persistAcrossTabs?: boolean;
}

export interface SecurityConfig {
  enableCSRF: boolean;
  stateParameterEntropy?: number;
  pkceEnabled?: boolean;
  httpsOnly?: boolean;
  sameSiteCookie?: 'strict' | 'lax' | 'none';
}

export interface UIConfig {
  theme?: 'light' | 'dark' | 'auto';
  locale?: string;
  customStyles?: Record<string, string>;
}

// #endregion

// #region Authentication Flow Types

export interface LoginRequest {
  provider?: AuthProvider;
  returnUrl: string;
  state?: string;
  customParams?: Record<string, string>;
  scopes?: string[];
}

export interface AuthResult {
  success: boolean;
  authUrl?: string;
  state?: string;
  session?: SessionInfo;
  user?: UserInfo;
  tokens?: TokenSet;
  error?: string;
  errorCode?: AuthErrorCode;
}

export interface CallbackData {
  code?: string;
  error?: string;
  error_description?: string;
  state: string;
  provider: AuthProvider;
}

export interface TokenSet {
  accessToken: string;
  refreshToken?: string;
  idToken?: string;
  tokenType: string;
  expiresIn: number;
  expiresAt: Date;
  scopes?: string[];
}

export interface TokenRefreshResult {
  success: boolean;
  tokens?: TokenSet;
  session?: SessionInfo;
  error?: string;
  errorCode?: AuthErrorCode;
}

// #endregion

// #region User and Session Types

export interface UserInfo {
  id: string;
  email?: string;
  emailVerified?: boolean;
  name?: string;
  givenName?: string;
  familyName?: string;
  picture?: string;
  locale?: string;
  provider: AuthProvider;
  providerUserId: string;
  customClaims?: Record<string, unknown>;
  roles?: string[];
  permissions?: string[];
  createdAt?: Date;
  lastLoginAt?: Date;
}

export interface SessionInfo {
  sessionId: string;
  user: UserInfo;
  isValid: boolean;
  createdAt: Date;
  expiresAt: Date;
  lastAccessedAt: Date;
  provider: AuthProvider;
  ipAddress?: string;
  userAgent?: string;
  refreshToken?: string;
}

export interface SessionValidationResult {
  isValid: boolean;
  session?: SessionInfo;
  error?: string;
  refreshRequired?: boolean;
}

// #endregion

// #region Provider Information Types

export interface ProviderInfo {
  name: AuthProvider;
  displayName: string;
  iconUrl?: string;
  description?: string;
  capabilities: ProviderCapability[];
  isEnabled: boolean;
  configuration: ProviderConfig;
  healthStatus: ProviderHealthStatus;
}

export type ProviderCapability = 
  | 'oauth2'
  | 'openid_connect'
  | 'refresh_token'
  | 'revoke_token'
  | 'user_info'
  | 'email_verification'
  | 'password_reset'
  | 'profile_edit';

export interface ProviderHealthStatus {
  isHealthy: boolean;
  lastChecked: Date;
  responseTime?: number;
  error?: string;
}

// #endregion

// #region Error Types

export enum AuthErrorCode {
  // Configuration errors
  INVALID_CONFIG = 'INVALID_CONFIG',
  PROVIDER_NOT_FOUND = 'PROVIDER_NOT_FOUND',
  PROVIDER_DISABLED = 'PROVIDER_DISABLED',
  
  // Authentication errors
  INVALID_CREDENTIALS = 'INVALID_CREDENTIALS',
  ACCESS_DENIED = 'ACCESS_DENIED',
  EXPIRED_TOKEN = 'EXPIRED_TOKEN',
  INVALID_TOKEN = 'INVALID_TOKEN',
  INVALID_STATE = 'INVALID_STATE',
  
  // Session errors
  SESSION_EXPIRED = 'SESSION_EXPIRED',
  SESSION_NOT_FOUND = 'SESSION_NOT_FOUND',
  INVALID_SESSION = 'INVALID_SESSION',
  
  // Network errors
  NETWORK_ERROR = 'NETWORK_ERROR',
  API_ERROR = 'API_ERROR',
  TIMEOUT_ERROR = 'TIMEOUT_ERROR',
  
  // Security errors
  CSRF_ERROR = 'CSRF_ERROR',
  INVALID_REDIRECT_URI = 'INVALID_REDIRECT_URI',
  INSECURE_CONNECTION = 'INSECURE_CONNECTION',
  
  // Generic errors
  UNKNOWN_ERROR = 'UNKNOWN_ERROR',
  VALIDATION_ERROR = 'VALIDATION_ERROR'
}

export interface AuthError {
  code: AuthErrorCode;
  message: string;
  details?: Record<string, unknown>;
  provider?: AuthProvider;
  timestamp: Date;
  requestId?: string;
}

// #endregion

// #region Event Types

export type AuthEventType = 
  | 'login_initiated'
  | 'login_completed'
  | 'login_failed'
  | 'logout_initiated'
  | 'logout_completed'
  | 'session_refreshed'
  | 'session_expired'
  | 'provider_error'
  | 'config_updated';

export interface AuthEvent {
  type: AuthEventType;
  timestamp: Date;
  data?: Record<string, unknown>;
  provider?: AuthProvider;
  sessionId?: string;
  userId?: string;
}

export type AuthEventHandler = (event: AuthEvent) => void;

// #endregion

// #region Utility Types

export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: AuthError;
  metadata?: {
    requestId: string;
    timestamp: Date;
    version: string;
  };
}

export interface PaginatedResponse<T> extends ApiResponse<T[]> {
  pagination?: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
}

export interface HealthCheckResult {
  status: 'healthy' | 'unhealthy' | 'degraded';
  checks: Record<string, {
    status: 'healthy' | 'unhealthy';
    description?: string;
    responseTime?: number;
    error?: string;
  }>;
  timestamp: Date;
}

// #endregion

// #region Client Interface Types

export interface IEasyAuthClient {
  // Core properties
  readonly isInitialized: boolean;
  readonly config: AuthConfig;
  
  // Provider management
  getAvailableProviders(): Promise<ProviderInfo[]>;
  getProviderInfo(provider: AuthProvider): Promise<ProviderInfo | null>;
  
  // Authentication flow
  initiateLogin(request: LoginRequest): Promise<AuthResult>;
  handleCallback(data: CallbackData): Promise<AuthResult>;
  
  // Session management
  getCurrentSession(): Promise<SessionInfo | null>;
  validateSession(sessionId?: string): Promise<SessionValidationResult>;
  refreshSession(): Promise<TokenRefreshResult>;
  signOut(): Promise<boolean>;
  
  // Event handling
  addEventListener(type: AuthEventType, handler: AuthEventHandler): void;
  removeEventListener(type: AuthEventType, handler: AuthEventHandler): void;
  
  // Utility methods
  isLoggedIn(): Promise<boolean>;
  getUser(): Promise<UserInfo | null>;
  getAccessToken(): Promise<string | null>;
  
  // Health and diagnostics
  getHealthStatus(): Promise<HealthCheckResult>;
}

// #endregion

// Re-export everything for convenience
export * from './auth';
export * from './providers';
export * from './errors';