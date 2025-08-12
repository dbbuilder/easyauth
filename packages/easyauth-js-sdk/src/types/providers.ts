/**
 * Provider-specific types and configurations
 */
/* eslint-disable no-unused-vars */

import { ProviderConfig, AuthProvider } from './index';

// Base provider interface
export interface IAuthProvider {
  readonly name: AuthProvider;
  readonly displayName: string;
  readonly isEnabled: boolean;
  
  getAuthorizationUrl(request: AuthorizationRequest): Promise<string>;
  exchangeCodeForTokens(code: string, state: string): Promise<TokenExchangeResult>;
  getUserInfo(accessToken: string): Promise<ProviderUserInfo>;
  refreshTokens(refreshToken: string): Promise<TokenExchangeResult>;
  revokeTokens?(tokens: string[]): Promise<boolean>;
  validateConfiguration(): Promise<boolean>;
  getHealthStatus(): Promise<ProviderHealthCheck>;
}

// Authorization request
export interface AuthorizationRequest {
  returnUrl: string;
  state: string;
  scopes?: string[];
  customParams?: Record<string, string>;
  pkceChallenge?: string;
  nonce?: string;
}

// Token exchange result
export interface TokenExchangeResult {
  success: boolean;
  accessToken?: string;
  refreshToken?: string;
  idToken?: string;
  tokenType?: string;
  expiresIn?: number;
  scope?: string;
  error?: string;
}

// Provider user information
export interface ProviderUserInfo {
  id: string;
  email?: string;
  emailVerified?: boolean;
  name?: string;
  givenName?: string;
  familyName?: string;
  profilePictureUrl?: string;
  picture?: string;
  locale?: string;
  phone?: string;
  phoneVerified?: boolean;
  birthdate?: string;
  gender?: string;
  provider: string;
  address?: ProviderAddress;
  customClaims?: Record<string, unknown>;
}

export interface ProviderAddress {
  street?: string;
  locality?: string;
  region?: string;
  postalCode?: string;
  country?: string;
  formatted?: string;
}

// Provider health check
export interface ProviderHealthCheck {
  provider: string;
  isHealthy: boolean;
  responseTime: number;
  status: string;
  lastChecked?: Date;
  endpoints?: Record<string, EndpointHealth>;
  error?: string;
}

export interface EndpointHealth {
  url: string;
  status: number;
  responseTime: number;
  isHealthy: boolean;
  error?: string;
}

// Google-specific types
export interface GoogleUserInfo extends ProviderUserInfo {
  hd?: string; // Hosted domain
  sub: string; // Subject identifier
}

export interface GoogleTokenInfo {
  azp: string;
  aud: string;
  sub: string;
  scope: string;
  exp: string;
  expires_in: string;
  email?: string;
  email_verified?: string;
  access_type?: string;
}

// Google-specific configuration
export interface GoogleConfig {
  clientId: string;
  clientSecret?: string;
  redirectUri: string;
  scopes?: string[];
  enabled?: boolean;
  hostedDomain?: string;
}

// Apple-specific types
export interface AppleUserInfo extends ProviderUserInfo {
  sub: string;
  email?: string;
  email_verified?: boolean;
  is_private_email?: boolean;
  real_user_status?: number;
}

export interface AppleClientSecret {
  iss: string;
  iat: number;
  exp: number;
  aud: string;
  sub: string;
}

export interface AppleTokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token: string;
}

// Facebook-specific types
export interface FacebookUserInfo extends Omit<ProviderUserInfo, 'picture'> {
  id: string;
  name?: string;
  email?: string;
  first_name?: string;
  last_name?: string;
  picture?: FacebookPicture;
  locale?: string;
  timezone?: number;
  verified?: boolean;
  link?: string;
}

export interface FacebookPicture {
  data: {
    height: number;
    is_silhouette: boolean;
    url: string;
    width: number;
  };
}

export interface FacebookTokenInfo {
  app_id: string;
  type: string;
  application: string;
  data_access_expires_at: number;
  expires_at: number;
  is_valid: boolean;
  issued_at: number;
  scopes: string[];
  user_id: string;
}

// Azure B2C specific types
export interface AzureB2CUserInfo extends ProviderUserInfo {
  oid: string; // Object identifier
  sub: string; // Subject
  ver: string; // Version
  iss: string; // Issuer
  aud: string; // Audience
  exp: number; // Expiration time
  iat: number; // Issued at
  nbf: number; // Not before
  name?: string;
  emails?: string[];
  tfp?: string; // Trust framework policy (user flow)
  at_hash?: string;
  c_hash?: string;
  extension_Department?: string;
  extension_ObjectId?: string;
  [key: string]: unknown; // Allow custom attributes
}

export interface AzureB2CConfig extends ProviderConfig {
  tenantName: string;
  tenantId: string;
  signInPolicy: string;
  signUpSignInPolicy?: string;
  resetPasswordPolicy?: string;
  editProfilePolicy?: string;
  validateAuthority?: boolean;
  knownAuthorities?: string[];
  cloudDiscoveryMetadata?: string;
  authorityMetadata?: string;
}

// Custom provider types
export interface CustomProviderConfig extends ProviderConfig {
  authorizationUrl: string;
  tokenUrl: string;
  userInfoUrl?: string;
  revokeUrl?: string;
  jwksUri?: string;
  issuer?: string;
  supportedScopes?: string[];
  customHeaders?: Record<string, string>;
  tokenExchangeMethod: 'POST' | 'GET';
  userInfoMethod: 'POST' | 'GET';
  tokenLocation: 'header' | 'body' | 'query';
}

// SAML provider types (for enterprise SSO)
export interface SAMLConfig extends ProviderConfig {
  entryPoint: string;
  issuer: string;
  cert: string | string[];
  privateCert?: string;
  decryptionPvk?: string;
  signatureAlgorithm?: 'sha1' | 'sha256';
  digestAlgorithm?: 'sha1' | 'sha256';
  nameIdFormat?: string;
  authnContextClassRef?: string;
  attributeConsumingServiceIndex?: string;
  disableRequestedAuthnContext?: boolean;
  forceAuthn?: boolean;
  skipRequestCompression?: boolean;
  acceptedClockSkewMs?: number;
  attributeStatementTimeout?: number;
}

// OIDC Discovery document
export interface OIDCDiscoveryDocument {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  userinfo_endpoint?: string;
  jwks_uri: string;
  registration_endpoint?: string;
  scopes_supported?: string[];
  response_types_supported: string[];
  response_modes_supported?: string[];
  grant_types_supported?: string[];
  acr_values_supported?: string[];
  subject_types_supported: string[];
  id_token_signing_alg_values_supported: string[];
  id_token_encryption_alg_values_supported?: string[];
  id_token_encryption_enc_values_supported?: string[];
  userinfo_signing_alg_values_supported?: string[];
  userinfo_encryption_alg_values_supported?: string[];
  userinfo_encryption_enc_values_supported?: string[];
  request_object_signing_alg_values_supported?: string[];
  request_object_encryption_alg_values_supported?: string[];
  request_object_encryption_enc_values_supported?: string[];
  token_endpoint_auth_methods_supported?: string[];
  token_endpoint_auth_signing_alg_values_supported?: string[];
  display_values_supported?: string[];
  claim_types_supported?: string[];
  claims_supported?: string[];
  service_documentation?: string;
  claims_locales_supported?: string[];
  ui_locales_supported?: string[];
  claims_parameter_supported?: boolean;
  request_parameter_supported?: boolean;
  request_uri_parameter_supported?: boolean;
  require_request_uri_registration?: boolean;
  op_policy_uri?: string;
  op_tos_uri?: string;
  revocation_endpoint?: string;
  revocation_endpoint_auth_methods_supported?: string[];
  revocation_endpoint_auth_signing_alg_values_supported?: string[];
  introspection_endpoint?: string;
  introspection_endpoint_auth_methods_supported?: string[];
  introspection_endpoint_auth_signing_alg_values_supported?: string[];
  code_challenge_methods_supported?: string[];
}

// Provider registry for dynamic provider loading
export interface ProviderRegistry {
  registerProvider(name: string, provider: IAuthProvider): void;
  unregisterProvider(name: string): void;
  getProvider(name: string): IAuthProvider | null;
  getAllProviders(): IAuthProvider[];
  getEnabledProviders(): IAuthProvider[];
}

// Provider factory
export interface ProviderFactory {
  createProvider(type: AuthProvider, config: ProviderConfig): Promise<IAuthProvider>;
  getSupportedProviders(): AuthProvider[];
  validateConfig(type: AuthProvider, config: ProviderConfig): Promise<boolean>;
}