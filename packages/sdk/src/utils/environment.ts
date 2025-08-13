/**
 * Environment detection and configuration utilities
 */

export interface EnvironmentInfo {
  isBrowser: boolean;
  isNode: boolean;
  isReactNative: boolean;
  isWebWorker: boolean;
  supportsLocalStorage: boolean;
  supportsSessionStorage: boolean;
  supportsPopup: boolean;
}

/**
 * Detect the current runtime environment
 */
export function detectEnvironment(): EnvironmentInfo {
  const isBrowser = typeof window !== 'undefined' && typeof window.document !== 'undefined';
  const isNode = typeof process !== 'undefined' && process.versions?.node;
  const isWebWorker = typeof self !== 'undefined' && typeof importScripts === 'function';
  const isReactNative = typeof navigator !== 'undefined' && navigator.product === 'ReactNative';
  
  return {
    isBrowser,
    isNode: !!isNode,
    isReactNative,
    isWebWorker,
    supportsLocalStorage: isBrowser && typeof localStorage !== 'undefined',
    supportsSessionStorage: isBrowser && typeof sessionStorage !== 'undefined',
    supportsPopup: isBrowser && typeof window.open === 'function'
  };
}

/**
 * Get the best storage adapter for the current environment
 */
export function getOptimalStorageAdapter(): 'localStorage' | 'sessionStorage' | 'memory' {
  const env = detectEnvironment();
  
  if (env.supportsLocalStorage) {
    return 'localStorage';
  }
  
  if (env.supportsSessionStorage) {
    return 'sessionStorage';
  }
  
  return 'memory';
}

/**
 * Check if OAuth popup flows are supported
 */
export function supportsOAuthPopup(): boolean {
  const env = detectEnvironment();
  return env.supportsPopup && !env.isReactNative;
}

/**
 * Check if OAuth redirect flows are supported
 */
export function supportsOAuthRedirect(): boolean {
  const env = detectEnvironment();
  return env.isBrowser || env.isReactNative;
}