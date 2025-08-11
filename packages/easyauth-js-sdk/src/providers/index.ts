/**
 * Provider implementations
 * OAuth 2.0 providers for various identity platforms
 */

// Export implemented providers
export { GoogleAuthProvider } from './GoogleAuthProvider';

// TODO: Implement remaining provider classes
// export { AppleAuthProvider } from './AppleAuthProvider';
// export { FacebookAuthProvider } from './FacebookAuthProvider';
// export { AzureB2CAuthProvider } from './AzureB2CAuthProvider';

// Export providers collection
export const providers = {
  GoogleAuthProvider,
};