# EasyAuth JavaScript/TypeScript SDK

[![npm version](https://badge.fury.io/js/@easyauth%2Fjs-sdk.svg)](https://www.npmjs.com/package/@easyauth/js-sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![TypeScript](https://img.shields.io/badge/%3C%2F%3E-TypeScript-%230074c1.svg)](http://www.typescriptlang.org/)

Universal authentication SDK for JavaScript and TypeScript applications. Supports multiple OAuth providers including Google, Apple, Facebook, and Azure B2C.

## 🚀 Features

- **Universal Support**: Works with React, Vue, Angular, Svelte, and vanilla JavaScript
- **TypeScript First**: Full type safety with comprehensive TypeScript definitions
- **Multiple Providers**: Google, Apple, Facebook, Azure B2C, and custom OAuth2/OIDC
- **Multiple Formats**: ESM, CommonJS, and UMD builds for maximum compatibility
- **Security Built-in**: CSRF protection, PKCE support, and secure token storage
- **Framework Agnostic**: Works in any JavaScript environment
- **Modern Standards**: OAuth 2.0, OpenID Connect, and PKCE support

## 📦 Installation

```bash
npm install @easyauth/js-sdk
```

## 🔧 Quick Start

### Basic Setup

```typescript
import { EasyAuthClient } from '@easyauth/js-sdk';

const client = new EasyAuthClient({
  apiBaseUrl: 'https://your-api.example.com',
  providers: {
    google: {
      clientId: 'your-google-client-id',
      enabled: true,
    },
    facebook: {
      clientId: 'your-facebook-client-id', 
      enabled: true,
    },
  },
  defaultProvider: 'google',
});
```

### Initiate Login

```typescript
const loginResult = await client.initiateLogin({
  provider: 'google',
  returnUrl: 'https://your-app.com/callback',
  scopes: ['openid', 'email', 'profile'],
});

if (loginResult.success) {
  // Redirect user to authentication provider
  window.location.href = loginResult.authUrl!;
}
```

### Handle Callback

```typescript
// In your callback handler
const urlParams = new URLSearchParams(window.location.search);
const callbackData = {
  code: urlParams.get('code'),
  state: urlParams.get('state'),
  provider: 'google', // or determine from your route
};

const authResult = await client.handleCallback(callbackData);

if (authResult.success) {
  console.log('User authenticated:', authResult.user);
  console.log('Session created:', authResult.session);
} else {
  console.error('Authentication failed:', authResult.error);
}
```

### Check Authentication Status

```typescript
const isLoggedIn = await client.isLoggedIn();
const currentUser = await client.getUser();
const currentSession = await client.getCurrentSession();
```

### Sign Out

```typescript
const signOutSuccess = await client.signOut();
```

## 🎯 Framework Integration Examples

### React

```tsx
import { useEffect, useState } from 'react';
import { EasyAuthClient, UserInfo } from '@easyauth/js-sdk';

const client = new EasyAuthClient(config);

function App() {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    client.getUser().then(user => {
      setUser(user);
      setLoading(false);
    });
  }, []);

  const handleLogin = async () => {
    const result = await client.initiateLogin({
      provider: 'google',
      returnUrl: window.location.origin + '/callback',
    });
    
    if (result.success) {
      window.location.href = result.authUrl!;
    }
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      {user ? (
        <div>
          <p>Welcome, {user.name}!</p>
          <button onClick={() => client.signOut()}>Sign Out</button>
        </div>
      ) : (
        <button onClick={handleLogin}>Sign In with Google</button>
      )}
    </div>
  );
}
```

### Vue 3

```vue
<template>
  <div>
    <div v-if="user">
      <p>Welcome, {{ user.name }}!</p>
      <button @click="signOut">Sign Out</button>
    </div>
    <button v-else @click="signIn">Sign In with Google</button>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { EasyAuthClient } from '@easyauth/js-sdk';

const client = new EasyAuthClient(config);
const user = ref(null);

onMounted(async () => {
  user.value = await client.getUser();
});

const signIn = async () => {
  const result = await client.initiateLogin({
    provider: 'google',
    returnUrl: window.location.origin + '/callback',
  });
  
  if (result.success) {
    window.location.href = result.authUrl;
  }
};

const signOut = async () => {
  await client.signOut();
  user.value = null;
};
</script>
```

## ⚙️ Configuration Options

### Full Configuration Example

```typescript
const config = {
  apiBaseUrl: 'https://your-api.example.com',
  
  providers: {
    google: {
      clientId: 'your-google-client-id',
      enabled: true,
      scopes: ['openid', 'email', 'profile'],
    },
    apple: {
      clientId: 'your-apple-client-id',
      teamId: 'your-team-id',
      keyId: 'your-key-id',
      privateKey: 'your-private-key',
      enabled: true,
    },
    facebook: {
      clientId: 'your-facebook-client-id',
      appSecret: 'your-app-secret',
      enabled: true,
    },
    'azure-b2c': {
      clientId: 'your-azure-client-id',
      tenantId: 'your-tenant-id',
      tenantName: 'your-tenant-name',
      signInPolicy: 'B2C_1_SignIn',
      enabled: true,
    },
  },
  
  defaultProvider: 'google',
  
  session: {
    storage: 'localStorage', // 'localStorage' | 'sessionStorage' | 'memory' | 'cookie'
    expirationMinutes: 60 * 24, // 24 hours
    autoRefresh: true,
    persistAcrossTabs: true,
  },
  
  security: {
    enableCSRF: true,
    stateParameterEntropy: 32,
    pkceEnabled: true,
    httpsOnly: true,
    sameSiteCookie: 'lax',
  },
};
```

## 🔒 Security Features

- **CSRF Protection**: Automatic state parameter generation and validation
- **PKCE Support**: Proof Key for Code Exchange for enhanced security
- **Secure Storage**: Configurable token storage with secure defaults
- **URL Validation**: Automatic validation of redirect URLs
- **Token Management**: Automatic refresh and secure storage

## 🧪 Testing

```bash
# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Run tests in watch mode
npm run test:watch
```

## 🛠️ Development

```bash
# Install dependencies
npm install

# Run development build
npm run dev

# Build for production
npm run build

# Lint code
npm run lint

# Type check
npm run typecheck
```

## 📚 API Reference

### EasyAuthClient

Main client class for authentication operations.

#### Constructor

```typescript
new EasyAuthClient(config: AuthConfig)
```

#### Methods

- `getAvailableProviders(): Promise<ProviderInfo[]>`
- `getProviderInfo(provider: AuthProvider): Promise<ProviderInfo | null>`
- `initiateLogin(request: LoginRequest): Promise<AuthResult>`
- `handleCallback(data: CallbackData): Promise<AuthResult>`
- `getCurrentSession(): Promise<SessionInfo | null>`
- `validateSession(sessionId?: string): Promise<SessionValidationResult>`
- `refreshSession(): Promise<TokenRefreshResult>`
- `signOut(): Promise<boolean>`
- `isLoggedIn(): Promise<boolean>`
- `getUser(): Promise<UserInfo | null>`
- `getAccessToken(): Promise<string | null>`

#### Events

```typescript
client.addEventListener('login_initiated', (event) => {
  console.log('Login initiated:', event);
});

client.addEventListener('login_completed', (event) => {
  console.log('Login completed:', event);
});

client.addEventListener('logout_completed', (event) => {
  console.log('Logout completed:', event);
});
```

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📖 [Documentation](https://github.com/dbbuilder/easyauth#readme)
- 🐛 [Issue Tracker](https://github.com/dbbuilder/easyauth/issues)
- 💬 [Discussions](https://github.com/dbbuilder/easyauth/discussions)

## 🗺️ Roadmap

- [ ] React hooks package (`@easyauth/react`)
- [ ] Vue composables package (`@easyauth/vue`)  
- [ ] Angular services package (`@easyauth/angular`)
- [ ] Svelte stores package (`@easyauth/svelte`)
- [ ] Next.js integration package (`@easyauth/nextjs`)
- [ ] Provider-specific optimizations
- [ ] Advanced security features
- [ ] Analytics and monitoring

---

**Made with ❤️ by the EasyAuth team**