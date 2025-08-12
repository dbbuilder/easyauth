# @easyauth/sdk

Universal TypeScript SDK for EasyAuth Framework - Secure authentication client for modern web applications.

## Features

- **Universal Compatibility**: Works with React, Vue, Next.js, Angular, Svelte, and vanilla JavaScript
- **TypeScript First**: Full TypeScript support with comprehensive type definitions
- **Provider Agnostic**: Support for Google, Apple, Facebook, Azure B2C, and custom OAuth providers
- **Secure by Default**: Automatic token management, secure storage, and session handling
- **Event-Driven**: React to authentication events in real-time
- **Framework Independent**: Core client with no framework dependencies
- **Tree Shakeable**: Import only what you need for optimal bundle size
- **SSR Compatible**: Works in browser, Node.js, and server-side rendering environments

## Installation

```bash
npm install @easyauth/sdk
# or
yarn add @easyauth/sdk
# or
pnpm add @easyauth/sdk
```

## Quick Start

### Basic Usage

```typescript
import { EasyAuthClient } from '@easyauth/sdk';

// Initialize the client
const authClient = new EasyAuthClient({
  baseUrl: 'https://your-api.com',
  enableLogging: true // Enable for development
});

// Check authentication status
if (authClient.isAuthenticated()) {
  const user = authClient.getUser();
  console.log('Logged in as:', user?.name);
}

// Login with Google
try {
  const result = await authClient.login({
    provider: 'google',
    returnUrl: window.location.href
  });

  if (result.success) {
    console.log('Login successful:', result.user);
  }
} catch (error) {
  console.error('Login failed:', error);
}
```

### Event Handling

```typescript
// Listen for authentication events
authClient.on('login', (data) => {
  console.log('User logged in:', data.user);
});

authClient.on('logout', () => {
  console.log('User logged out');
  // Redirect to login page
});

authClient.on('token_refresh', (data) => {
  console.log('Token refreshed:', data.token);
});

authClient.on('session_expired', () => {
  console.log('Session expired');
  // Handle expired session
});

authClient.on('error', (data) => {
  console.error('Auth error:', data.error);
});
```

## Configuration

```typescript
interface EasyAuthConfig {
  baseUrl: string;              // Your EasyAuth API endpoint
  apiVersion?: string;          // API version (default: '1.0')
  timeout?: number;             // Request timeout in ms (default: 30000)
  retryAttempts?: number;       // Retry failed requests (default: 3)
  enableLogging?: boolean;      // Enable debug logging (default: false)
}
```

## API Reference

### EasyAuthClient

#### Methods

- `login(options: LoginOptions): Promise<AuthResult>` - Initiate login flow
- `logout(): Promise<void>` - Logout and clear session
- `refresh(): Promise<AuthResult>` - Refresh authentication token
- `getSession(): AuthSession` - Get current session state
- `isAuthenticated(): boolean` - Check if user is authenticated
- `getUser(): UserProfile | null` - Get current user profile
- `getToken(): string | null` - Get authentication token
- `setToken(token: string): void` - Set token manually
- `clearToken(): void` - Clear authentication data
- `on(event: AuthEvent, callback: AuthEventCallback): void` - Subscribe to events
- `off(event: AuthEvent, callback: AuthEventCallback): void` - Unsubscribe from events

#### Types

```typescript
interface LoginOptions {
  provider: 'google' | 'apple' | 'facebook' | 'azure-b2c' | 'custom';
  returnUrl?: string;
  state?: string;
  parameters?: Record<string, string>;
}

interface AuthResult {
  success: boolean;
  user?: UserProfile;
  token?: string;
  error?: AuthError;
  redirectUrl?: string;
}

interface UserProfile {
  id: string;
  email?: string;
  name?: string;
  profilePicture?: string;
  provider: AuthProvider;
  metadata?: Record<string, unknown>;
  roles?: string[];
  permissions?: string[];
}
```

## Advanced Usage

### Custom Storage

```typescript
import { EasyAuthClient, StorageAdapter } from '@easyauth/sdk';

class CustomStorageAdapter implements StorageAdapter {
  getItem(key: string): string | null {
    // Custom storage implementation
    return null;
  }

  setItem(key: string, value: string): void {
    // Custom storage implementation
  }

  removeItem(key: string): void {
    // Custom storage implementation
  }

  clear(): void {
    // Custom storage implementation
  }
}

const client = new EasyAuthClient(
  config,
  undefined, // HTTP client
  new CustomStorageAdapter(), // Custom storage
  undefined  // Logger
);
```

### Custom HTTP Client

```typescript
import { EasyAuthClient, HttpClient } from '@easyauth/sdk';

class CustomHttpClient implements HttpClient {
  async get<T>(url: string, config?: RequestConfig): Promise<ApiResponse<T>> {
    // Custom HTTP implementation
  }

  // ... other methods
}

const client = new EasyAuthClient(
  config,
  new CustomHttpClient(),
  undefined, // Storage
  undefined  // Logger
);
```

## Framework Integrations

This SDK serves as the foundation for framework-specific packages:

- **React**: `@easyauth/react` - React hooks and components
- **Vue**: `@easyauth/vue` - Vue composables and components  
- **Next.js**: `@easyauth/nextjs` - Next.js App Router integration
- **Angular**: `@easyauth/angular` - Angular services and guards
- **Svelte**: `@easyauth/svelte` - Svelte stores and actions

## Development

### Building

```bash
npm run build
```

### Testing

```bash
npm test
npm run test:watch
npm run test:coverage
```

### Linting

```bash
npm run lint
npm run lint:fix
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## License

MIT License - see the [LICENSE](../../LICENSE) file for details.

## Support

- 📖 [Documentation](https://docs.easyauth.dev)
- 💬 [GitHub Discussions](https://github.com/dbbuilder/easyauth/discussions)
- 🐛 [Issue Tracker](https://github.com/dbbuilder/easyauth/issues)