/**
 * Provider implementations
 * OAuth 2.0 providers for various identity platforms
 */

// Import and export implemented providers
import { GoogleAuthProvider } from './GoogleAuthProvider';

export { GoogleAuthProvider };

// TODO: Implement remaining provider classes
// export { AppleAuthProvider } from './AppleAuthProvider';
// export { FacebookAuthProvider } from './FacebookAuthProvider';
// export { AzureB2CAuthProvider } from './AzureB2CAuthProvider';

// Export providers collection
export const providers = {
  GoogleAuthProvider,
};