/**
 * EasyAuth SDK - TypeScript authentication client
 * 
 * @version 1.0.0
 * @author EasyAuth Development Team
 * @license MIT
 */

// Main client
export { EasyAuthClient } from './client/EasyAuthClient';

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
  Logger
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

// Default export for convenient usage
export { EasyAuthClient as default } from './client/EasyAuthClient';