/**
 * Core types for EasyAuth SDK
 */

export interface EasyAuthConfig {
  baseUrl: string;
  apiVersion?: string;
  timeout?: number;
  retryAttempts?: number;
  enableLogging?: boolean;
  
  // Enhanced configuration options
  environment?: 'development' | 'staging' | 'production';
  useDevCredentials?: boolean;
  enableStateManagement?: boolean;
  stateManagementType?: 'redux' | 'zustand' | 'pinia' | 'context';
  
  // OAuth proxy configuration for simplified setup
  useOAuthProxy?: boolean;
  proxyConfig?: {
    url: string;
    apiKey?: string;
    timeout?: number;
  };
  
  // Advanced features
  enableTokenAutoRefresh?: boolean;
  refreshThresholdMinutes?: number;
  enableSessionPersistence?: boolean;
  storageType?: 'localStorage' | 'sessionStorage' | 'memory';
}

export interface UserProfile {
  id: string;
  email?: string;
  name?: string;
  profilePicture?: string;
  provider: AuthProvider;
  metadata?: Record<string, unknown>;
  roles?: string[];
  permissions?: string[];
}

export interface AuthSession {
  isAuthenticated: boolean;
  user: UserProfile | null;
  token?: string;
  expiresAt?: Date;
  refreshToken?: string;
}

export interface LoginOptions {
  provider: AuthProvider;
  returnUrl?: string;
  state?: string;
  parameters?: Record<string, string>;
}

export interface AuthResult {
  success: boolean;
  user?: UserProfile;
  token?: string;
  error?: AuthError;
  redirectUrl?: string;
}

export interface AuthError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
  statusCode?: number;
}

export type AuthProvider = 'google' | 'apple' | 'facebook' | 'azure-b2c' | 'custom';

// Enhanced provider-specific types
export interface GoogleUserProfile extends UserProfile {
  provider: 'google';
  googleId: string;
  familyName?: string;
  givenName?: string;
  locale?: string;
  picture?: string;
  verifiedEmail?: boolean;
}

export interface FacebookUserProfile extends UserProfile {
  provider: 'facebook';
  facebookId: string;
  firstName?: string;
  lastName?: string;
  locale?: string;
  timezone?: number;
  gender?: string;
  ageRange?: {
    min: number;
    max?: number;
  };
}

export interface AppleUserProfile extends UserProfile {
  provider: 'apple';
  appleId: string;
  isPrivateEmail?: boolean;
  realUserStatus?: 'likelyReal' | 'unknown' | 'unsupported';
}

export interface AzureB2CUserProfile extends UserProfile {
  provider: 'azure-b2c';
  azureId: string;
  tenantId?: string;
  userPrincipalName?: string;
  displayName?: string;
  jobTitle?: string;
  department?: string;
  companyName?: string;
  country?: string;
  city?: string;
  postalCode?: string;
}

export interface ProviderConfig {
  clientId: string;
  redirectUri?: string;
  scopes?: string[];
  customParameters?: Record<string, string>;
}

export interface EasyAuthClient {
  config: EasyAuthConfig;
  session: AuthSession;
  
  // Authentication methods
  login(options: LoginOptions): Promise<AuthResult>;
  logout(): Promise<void>;
  refresh(): Promise<AuthResult>;
  
  // Session management
  getSession(): AuthSession;
  isAuthenticated(): boolean;
  getUser(): UserProfile | null;
  
  // Token management
  getToken(): string | null;
  setToken(token: string): void;
  clearToken(): void;
  
  // Event handling
  on(event: AuthEvent, callback: AuthEventCallback): void;
  off(event: AuthEvent, callback: AuthEventCallback): void;
}

export type AuthEvent = 
  | 'login'
  | 'logout' 
  | 'token_refresh'
  | 'session_expired'
  | 'error';

export type AuthEventCallback = (data?: unknown) => void;

export interface AuthEventData {
  user?: UserProfile;
  error?: AuthError;
  token?: string;
}

// HTTP client interfaces
export interface HttpClient {
  get<T>(url: string, config?: RequestConfig): Promise<ApiResponse<T>>;
  post<T>(url: string, data?: unknown, config?: RequestConfig): Promise<ApiResponse<T>>;
  put<T>(url: string, data?: unknown, config?: RequestConfig): Promise<ApiResponse<T>>;
  delete<T>(url: string, config?: RequestConfig): Promise<ApiResponse<T>>;
}

export interface RequestConfig {
  headers?: Record<string, string>;
  timeout?: number;
  withCredentials?: boolean;
}

export interface ApiResponse<T> {
  data: T;
  status: number;
  statusText: string;
  headers: Record<string, string>;
}

// Storage interfaces
export interface StorageAdapter {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
  clear(): void;
}

// Logger interface
export interface Logger {
  debug(message: string, ...args: unknown[]): void;
  info(message: string, ...args: unknown[]): void;
  warn(message: string, ...args: unknown[]): void;
  error(message: string, ...args: unknown[]): void;
}