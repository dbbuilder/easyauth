/**
 * EasyAuth JavaScript/TypeScript SDK
 * Universal authentication for web applications
 * 
 * @packageDocumentation
 */

// Export main client class
export { EasyAuthClient } from './auth/EasyAuthClient';

// Export all types
export * from './types';

// Export provider implementations
export * from './providers';

// Export utility functions
export * from './utils';

// Export error classes
export * from './types/errors';

// Version information
export const SDK_VERSION = '1.0.0-alpha.1';
export const SDK_NAME = '@easyauth/js-sdk';

// Default export for convenience
import { EasyAuthClient } from './auth/EasyAuthClient';
export default EasyAuthClient;