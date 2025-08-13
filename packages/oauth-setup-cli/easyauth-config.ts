// EasyAuth Configuration
// Generated on 2025-08-13T06:01:03.012Z

import { EnhancedEasyAuthClient } from '@easyauth/sdk';

export const easyAuthConfig = {
  baseUrl: process.env.EASYAUTH_BASE_URL || 'https://test.com',
  environment: process.env.EASYAUTH_ENVIRONMENT as 'development' | 'staging' | 'production' || 'production',
  
  // Provider credentials from environment variables
  providers: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!
    }
  }
};

// Initialize EasyAuth client
export const easyAuthClient = new EnhancedEasyAuthClient(easyAuthConfig);

// Export for convenience
export default easyAuthClient;
