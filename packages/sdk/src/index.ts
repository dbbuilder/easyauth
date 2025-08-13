/**
 * EasyAuth SDK - TypeScript authentication client
 * 
 * @version 2.0.0
 * @author EasyAuth Development Team
 * @license MIT
 */

// Main clients
export { EasyAuthClient } from './client/EasyAuthClient';
export { EnhancedEasyAuthClient } from './client/EnhancedEasyAuthClient';

// Core types
export type {
  EasyAuthConfig,
  UserProfile,
  AuthSession,
  LoginOptions,
  AuthResult,
  AuthError,
  AuthProvider,
  ProviderConfig,
  EasyAuthClient as IEasyAuthClient,
  AuthEvent,
  AuthEventCallback,
  AuthEventData,
  HttpClient,
  RequestConfig,
  ApiResponse,
  StorageAdapter,
  Logger,
  
  // Enhanced provider-specific types
  GoogleUserProfile,
  FacebookUserProfile,
  AppleUserProfile,
  AzureB2CUserProfile
} from './types';

// Utility classes for advanced usage
export { DefaultHttpClient } from './utils/http-client';
export { 
  LocalStorageAdapter,
  SessionStorageAdapter,
  MemoryStorageAdapter
} from './utils/storage';
export { ConsoleLogger, NoOpLogger } from './utils/logger';
export { EventEmitter } from './utils/event-emitter';

// Enhanced utilities
export { 
  detectEnvironment,
  getOptimalStorageAdapter,
  supportsOAuthPopup,
  supportsOAuthRedirect
} from './utils/environment';

export {
  CredentialHelper,
  createQuickSetup
} from './utils/credential-helper';

// State management integrations
export {
  StateManagementConnector,
  createReduxReducer,
  createZustandStore,
  createPiniaStore,
  reduxAuthActions
} from './integrations/state-management';

export type {
  ReduxAuthState,
  ZustandAuthStore,
  PiniaAuthState,
  ReactAuthContextValue,
  StateStore,
  StateDispatch
} from './integrations/state-management';

// Default export for convenient usage (Enhanced client)
export { EnhancedEasyAuthClient as default } from './client/EnhancedEasyAuthClient';