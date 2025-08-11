/**
 * Authentication-specific types
 */

import { AuthProvider, UserInfo, TokenSet, AuthErrorCode } from './index';

// OAuth2 specific types
export interface OAuth2Config {
  clientId: string;
  clientSecret?: string;
  redirectUri: string;
  scopes?: string[];
  responseType: 'code' | 'token' | 'id_token';
  responseMode?: 'query' | 'fragment' | 'form_post';
  prompt?: 'none' | 'login' | 'consent' | 'select_account';
  maxAge?: number;
  uiLocales?: string[];
  acrValues?: string[];
  display?: 'page' | 'popup' | 'touch' | 'wap';
}

// PKCE (Proof Key for Code Exchange) types
export interface PKCEChallenge {
  codeVerifier: string;
  codeChallenge: string;
  codeChallengeMethod: 'S256' | 'plain';
}

// OpenID Connect specific types
export interface OpenIDConnectConfig extends OAuth2Config {
  issuer: string;
  authorizationEndpoint: string;
  tokenEndpoint: string;
  userInfoEndpoint?: string;
  jwksUri?: string;
  endSessionEndpoint?: string;
  revocationEndpoint?: string;
  introspectionEndpoint?: string;
  supportedScopes: string[];
  supportedResponseTypes: string[];
  supportedGrantTypes: string[];
  supportedSubjectTypes: string[];
  supportedIdTokenSigningAlgValues: string[];
}

// JWT Token types
export interface JWTHeader {
  alg: string;
  typ: string;
  kid?: string;
}

export interface JWTPayload {
  iss: string;
  sub: string;
  aud: string | string[];
  exp: number;
  iat: number;
  nbf?: number;
  jti?: string;
  azp?: string;
  scope?: string;
  email?: string;
  email_verified?: boolean;
  name?: string;
  given_name?: string;
  family_name?: string;
  picture?: string;
  locale?: string;
  [key: string]: unknown;
}

export interface DecodedJWT {
  header: JWTHeader;
  payload: JWTPayload;
  signature: string;
  raw: string;
}

// Authentication state management
export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserInfo | null;
  tokens: TokenSet | null;
  error: AuthErrorCode | null;
  provider: AuthProvider | null;
  sessionId: string | null;
}

// Authentication context
export interface AuthContext {
  state: AuthState;
  login: (provider: AuthProvider, options?: LoginOptions) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  getAccessToken: () => Promise<string | null>;
  isTokenExpired: () => boolean;
  updateUser: (user: Partial<UserInfo>) => void;
}

export interface LoginOptions {
  returnUrl?: string;
  scopes?: string[];
  prompt?: string;
  customParams?: Record<string, string>;
  locale?: string;
  hint?: string;
}

// Biometric authentication types (for mobile/web authn)
export interface BiometricConfig {
  enabled: boolean;
  allowFallback: boolean;
  promptMessage?: string;
  cancelTitle?: string;
  fallbackTitle?: string;
  disableBackup?: boolean;
}

// Multi-factor authentication types
export interface MFAConfig {
  enabled: boolean;
  requiredFactors: MFAFactor[];
  optionalFactors: MFAFactor[];
  gracePeriodHours?: number;
}

export type MFAFactor = 
  | 'sms'
  | 'email'
  | 'totp'
  | 'push'
  | 'biometric'
  | 'backup_codes';

export interface MFAChallenge {
  challengeId: string;
  factorType: MFAFactor;
  deliveryMethod?: string;
  expiresAt: Date;
  remainingAttempts: number;
}

export interface MFAVerification {
  challengeId: string;
  code: string;
  rememberDevice?: boolean;
}

// Device registration for remember me functionality
export interface DeviceInfo {
  deviceId: string;
  deviceName: string;
  deviceType: 'mobile' | 'tablet' | 'desktop' | 'unknown';
  os: string;
  browser: string;
  ipAddress: string;
  userAgent: string;
  registeredAt: Date;
  lastUsedAt: Date;
  isTrusted: boolean;
}

// Social login specific types
export interface SocialProfile {
  provider: AuthProvider;
  providerId: string;
  email?: string;
  name?: string;
  firstName?: string;
  lastName?: string;
  avatar?: string;
  locale?: string;
  verified?: boolean;
  raw?: Record<string, unknown>;
}

// Password-based authentication (for custom providers)
export interface PasswordCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface PasswordResetRequest {
  email: string;
  callbackUrl: string;
}

export interface PasswordChangeRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// Account linking
export interface AccountLinkRequest {
  primaryProvider: AuthProvider;
  primaryUserId: string;
  secondaryProvider: AuthProvider;
  secondaryUserId: string;
}

export interface LinkedAccount {
  provider: AuthProvider;
  providerId: string;
  email?: string;
  linkedAt: Date;
  isPrimary: boolean;
}

// Audit and security
export interface LoginAttempt {
  timestamp: Date;
  provider: AuthProvider;
  ipAddress: string;
  userAgent: string;
  success: boolean;
  failureReason?: string;
  location?: {
    country: string;
    region: string;
    city: string;
  };
}

export interface SecurityEvent {
  type: SecurityEventType;
  severity: 'low' | 'medium' | 'high' | 'critical';
  timestamp: Date;
  userId?: string;
  sessionId?: string;
  details: Record<string, unknown>;
  ipAddress?: string;
  userAgent?: string;
}

export type SecurityEventType = 
  | 'suspicious_login'
  | 'account_locked'
  | 'password_changed'
  | 'mfa_enabled'
  | 'mfa_disabled'
  | 'device_registered'
  | 'account_linked'
  | 'data_export'
  | 'account_deleted';