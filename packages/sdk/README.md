# EasyAuth TypeScript SDK v2.0

🚀 **Universal Authentication Client** for TypeScript/JavaScript applications with multi-environment support, state management integrations, and simplified OAuth setup.

[![npm version](https://badge.fury.io/js/%40easyauth%2Fsdk.svg)](https://badge.fury.io/js/%40easyauth%2Fsdk)
[![TypeScript](https://img.shields.io/badge/%3C%2F%3E-TypeScript-%230074c1.svg)](http://www.typescriptlang.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🎯 Quick Start

### Installation

```bash
npm install @easyauth/sdk
# or
yarn add @easyauth/sdk
# or  
pnpm add @easyauth/sdk
```

### 5-Minute Setup

```typescript
import { EnhancedEasyAuthClient } from '@easyauth/sdk';

// Quick setup for new projects (uses development credentials)
const { client, setupInstructions } = EnhancedEasyAuthClient.quickSetup({
  baseUrl: 'https://your-api.com',
  providers: ['google', 'facebook', 'apple'],
  isDevelopment: true, // Uses shared dev credentials for localhost
  stateManagement: 'react' // Optional: auto-connect to React context
});

// Login with any provider
const result = await client.login({ provider: 'google' });
if (result.success) {
  console.log('Welcome,', result.user?.name);
}
```

## 🌟 Key Features

### ✨ **Zero Configuration OAuth**
No need to set up OAuth apps during development! The SDK includes shared development credentials that work on localhost, so you can start coding immediately.

```typescript
// Works instantly on localhost - no OAuth app setup required!
const client = new EnhancedEasyAuthClient({
  baseUrl: 'https://your-api.com',
  useDevCredentials: true // Magic! ✨
});
```

### 🔄 **Universal Environment Support**
Works seamlessly across all JavaScript environments:

- **Browser** - Full OAuth popup/redirect flows
- **Node.js** - Server-side authentication 
- **React Native** - Mobile OAuth flows
- **SSR/SSG** - Next.js, Nuxt.js, SvelteKit
- **Web Workers** - Background authentication

### 🗄️ **State Management Ready**
First-class integrations with popular state management libraries:

```typescript
// Redux Integration
import { createReduxReducer, reduxAuthActions } from '@easyauth/sdk';

// Zustand Integration  
import { createZustandStore } from '@easyauth/sdk';

// Pinia Integration (Vue)
import { createPiniaStore } from '@easyauth/sdk';

// React Context
const { client } = EnhancedEasyAuthClient.quickSetup({
  baseUrl: 'https://api.example.com',
  providers: ['google'],
  stateManagement: 'context',
  store: yourReactContext
});
```

### 🛡️ **Production Security**
Enterprise-grade security with zero configuration:

- **CSRF Protection** - Automatic token validation
- **XSS Prevention** - Secure token storage
- **Auto Token Refresh** - Seamless session management
- **Environment Detection** - Optimal security per platform

## 📚 Usage Examples

### Basic Authentication

```typescript
import EasyAuth from '@easyauth/sdk';

const client = new EasyAuth({
  baseUrl: 'https://your-api.com'
});

// Login
const result = await client.login({ provider: 'google' });
if (result.success) {
  console.log('User:', result.user);
  console.log('Token:', result.token);
}

// Check authentication status
if (client.isAuthenticated()) {
  const user = client.getUser();
  console.log('Current user:', user?.name);
}

// Logout
await client.logout();
```

### React Integration

```tsx
import { EnhancedEasyAuthClient } from '@easyauth/sdk';
import { createContext, useContext, useState, useEffect } from 'react';

// Create auth context
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [client] = useState(() => new EnhancedEasyAuthClient({
    baseUrl: process.env.REACT_APP_API_URL,
    useDevCredentials: process.env.NODE_ENV === 'development'
  }));
  
  const [session, setSession] = useState(client.getSession());

  useEffect(() => {
    // Listen for auth changes
    client.on('login', () => setSession(client.getSession()));
    client.on('logout', () => setSession(client.getSession()));
    client.on('token_refresh', () => setSession(client.getSession()));
    
    return () => {
      client.off('login', setSession);
      client.off('logout', setSession);
      client.off('token_refresh', setSession);
    };
  }, [client]);

  return (
    <AuthContext.Provider value={{ client, session }}>
      {children}
    </AuthContext.Provider>
  );
}

// Usage in components
export function LoginButton() {
  const { client } = useContext(AuthContext);
  
  const handleLogin = async () => {
    await client.login({ provider: 'google' });
  };

  return <button onClick={handleLogin}>Login with Google</button>;
}
```

### Vue 3 Integration

```vue
<template>
  <div>
    <button v-if="!session.isAuthenticated" @click="login">
      Login with Google
    </button>
    <div v-else>
      Welcome, {{ session.user?.name }}
      <button @click="logout">Logout</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { EnhancedEasyAuthClient } from '@easyauth/sdk';

const client = new EnhancedEasyAuthClient({
  baseUrl: import.meta.env.VITE_API_URL,
  useDevCredentials: import.meta.env.DEV
});

const session = ref(client.getSession());

onMounted(() => {
  client.on('login', () => session.value = client.getSession());
  client.on('logout', () => session.value = client.getSession());
});

const login = () => client.login({ provider: 'google' });
const logout = () => client.logout();
</script>
```

### Next.js App Router Integration

```typescript
// app/providers.tsx
'use client';

import { EnhancedEasyAuthClient } from '@easyauth/sdk';
import { createContext, useContext } from 'react';

const AuthContext = createContext<EnhancedEasyAuthClient | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const client = new EnhancedEasyAuthClient({
    baseUrl: process.env.NEXT_PUBLIC_API_URL!,
    useDevCredentials: process.env.NODE_ENV === 'development',
    enableSessionPersistence: true
  });

  return (
    <AuthContext.Provider value={client}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const client = useContext(AuthContext);
  if (!client) throw new Error('useAuth must be used within AuthProvider');
  return client;
};
```

## 🔧 Advanced Configuration

### Multi-Environment Setup

```typescript
const client = new EnhancedEasyAuthClient({
  baseUrl: 'https://your-api.com',
  
  // Environment settings
  environment: 'production', // 'development' | 'staging' | 'production'
  useDevCredentials: false,
  
  // OAuth proxy for simplified setup
  useOAuthProxy: true,
  proxyConfig: {
    url: 'https://oauth-proxy.your-company.com',
    apiKey: 'your-proxy-api-key'
  },
  
  // Advanced features
  enableTokenAutoRefresh: true,
  refreshThresholdMinutes: 5,
  enableSessionPersistence: true,
  storageType: 'localStorage', // 'localStorage' | 'sessionStorage' | 'memory'
  
  // State management
  enableStateManagement: true,
  stateManagementType: 'redux'
});
```

### Provider-Specific Configuration

```typescript
import type { 
  GoogleUserProfile, 
  FacebookUserProfile,
  AppleUserProfile 
} from '@easyauth/sdk';

const client = new EnhancedEasyAuthClient({
  baseUrl: 'https://your-api.com',
  providers: {
    google: {
      clientId: 'your-google-client-id',
      scopes: ['profile', 'email']
    },
    facebook: {
      appId: 'your-facebook-app-id',
      scopes: ['email', 'public_profile']
    },
    apple: {
      clientId: 'your-apple-service-id'
    }
  }
});

// Handle provider-specific user data
client.on('login', (data) => {
  const user = data.user;
  
  switch (user?.provider) {
    case 'google':
      const googleUser = user as GoogleUserProfile;
      console.log('Google ID:', googleUser.googleId);
      break;
      
    case 'facebook':
      const facebookUser = user as FacebookUserProfile;
      console.log('Facebook ID:', facebookUser.facebookId);
      break;
      
    case 'apple':
      const appleUser = user as AppleUserProfile;
      console.log('Apple ID:', appleUser.appleId);
      break;
  }
});
```

## 🔒 Security Best Practices

### Production Readiness Check

```typescript
const client = new EnhancedEasyAuthClient({
  baseUrl: 'https://your-api.com'
});

// Validate production configuration
const validation = client.validateProductionReadiness();

if (!validation.isReady) {
  console.warn('⚠️ Production Issues:');
  validation.warnings.forEach(warning => console.warn(`- ${warning}`));
  
  console.info('💡 Recommendations:');
  validation.recommendations.forEach(rec => console.info(`- ${rec}`));
}
```

### Health Monitoring

```typescript
// Monitor authentication service health
const health = await client.healthCheck();

console.log(`Status: ${health.status}`);
console.log(`Latency: ${health.latency}ms`);
console.log('Details:', health.details);

// Status can be: 'healthy' | 'degraded' | 'unhealthy'
if (health.status === 'unhealthy') {
  // Implement fallback authentication or show maintenance message
}
```

## 🎨 State Management Integrations

### Redux Toolkit

```typescript
import { configureStore } from '@reduxjs/toolkit';
import { createReduxReducer, reduxAuthActions } from '@easyauth/sdk';

const authReducer = createReduxReducer();

const store = configureStore({
  reducer: {
    auth: authReducer
  }
});

// Connect EasyAuth to Redux
client.connectToStateManagement('redux', store, store.dispatch);

// Use in components
const isAuthenticated = useSelector(state => state.auth.session.isAuthenticated);
const user = useSelector(state => state.auth.session.user);
```

### Zustand

```typescript
import { create } from 'zustand';
import { createZustandStore } from '@easyauth/sdk';

const useAuthStore = create(() => createZustandStore());

// Connect EasyAuth to Zustand
client.connectToStateManagement('zustand', useAuthStore.getState());

// Use in components
function Profile() {
  const { session, isLoading } = useAuthStore();
  
  if (isLoading) return <div>Loading...</div>;
  if (!session.isAuthenticated) return <div>Please login</div>;
  
  return <div>Welcome, {session.user?.name}!</div>;
}
```

### Pinia (Vue)

```typescript
import { defineStore } from 'pinia';
import { createPiniaStore } from '@easyauth/sdk';

export const useAuthStore = defineStore(createPiniaStore());

// Connect EasyAuth to Pinia
client.connectToStateManagement('pinia', useAuthStore());

// Use in Vue components
<script setup>
const authStore = useAuthStore();
const { isAuthenticated, user } = storeToRefs(authStore);
</script>
```

## 🛠️ Development vs Production

### Development Mode
```typescript
// Automatic during development
const { client, setupInstructions } = EnhancedEasyAuthClient.quickSetup({
  baseUrl: 'http://localhost:3000',
  providers: ['google', 'facebook'],
  isDevelopment: true // Uses shared dev credentials
});

// Print setup instructions for production
console.log(setupInstructions);
```

### Production Mode
```typescript
const client = new EnhancedEasyAuthClient({
  baseUrl: 'https://api.yourapp.com',
  environment: 'production',
  
  // Your production OAuth credentials
  providers: {
    google: { clientId: process.env.GOOGLE_CLIENT_ID },
    facebook: { appId: process.env.FACEBOOK_APP_ID }
  }
});
```

## 📖 API Reference

### Core Methods

#### `login(options: LoginOptions): Promise<AuthResult>`
Initiate authentication with specified provider.

#### `logout(): Promise<void>`
End user session and clear tokens.

#### `refresh(): Promise<AuthResult>`
Refresh authentication token.

#### `isAuthenticated(): boolean`
Check if user is currently authenticated.

#### `getUser(): UserProfile | null`
Get current authenticated user.

#### `getSession(): AuthSession`
Get complete authentication session.

### Event Handling

```typescript
// Available events
client.on('login', (data) => {
  console.log('User logged in:', data.user);
});

client.on('logout', () => {
  console.log('User logged out');
});

client.on('token_refresh', (data) => {
  console.log('Token refreshed:', data.token);
});

client.on('session_expired', (data) => {
  console.log('Session expired:', data.error);
});

client.on('error', (data) => {
  console.error('Auth error:', data.error);
});
```

### Utility Methods

#### `detectEnvironment()`
Returns information about the current runtime environment.

#### `getOptimalStorageAdapter()`
Returns the best storage adapter for the current environment.

#### `supportsOAuthPopup()`
Check if OAuth popup flows are supported.

#### `createQuickSetup(options)`
Factory function for rapid development setup.

## 🔗 OAuth Provider Setup

While development credentials work on localhost, you'll need to set up OAuth applications for production:

### Google OAuth Setup
1. Go to [Google Cloud Console](https://console.developers.google.com/)
2. Create project and enable Google+ API
3. Create OAuth 2.0 Client ID
4. Add your domain to authorized origins
5. Add redirect URI: `https://yourdomain.com/auth/google/callback`

### Facebook Login Setup
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create new app and add Facebook Login
3. Configure Valid OAuth Redirect URIs
4. Add: `https://yourdomain.com/auth/facebook/callback`
5. Set app to Live mode

### Apple Sign-In Setup
1. Go to [Apple Developer Account](https://developer.apple.com/account/)
2. Register new Service ID
3. Configure domains and return URL: `https://yourdomain.com/auth/apple/callback`
4. Create private key for server authentication

### Azure B2C Setup
1. Create Azure B2C tenant
2. Register application 
3. Configure redirect URI: `https://yourdomain.com/auth/azure-b2c/callback`
4. Create user flows for sign-up/sign-in

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## 📄 License

MIT © [EasyAuth Development Team](https://github.com/dbbuilder/easyauth)

## 🆘 Support

- 📖 [Documentation](https://docs.easyauth.dev)
- 🐛 [Bug Reports](https://github.com/dbbuilder/easyauth/issues)
- 💬 [Discussions](https://github.com/dbbuilder/easyauth/discussions)
- 📧 [Email Support](mailto:support@easyauth.dev)

---

**🚀 Start building secure authentication today!**

```bash
npm install @easyauth/sdk
```