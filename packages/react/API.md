# @easyauth/react API Reference

## Overview

The EasyAuth React package provides hooks and components for integrating authentication into React applications.

## Installation

```bash
npm install @easyauth/react
```

## Quick Start

```tsx
import { EasyAuthProvider, useAuth, LoginButton } from '@easyauth/react';

function App() {
  return (
    <EasyAuthProvider baseUrl="https://your-api.com">
      <AuthenticatedApp />
    </EasyAuthProvider>
  );
}

function AuthenticatedApp() {
  const { isAuthenticated, user } = useAuth();
  
  if (!isAuthenticated) {
    return <LoginButton provider="google" />;
  }
  
  return <div>Welcome, {user?.name}!</div>;
}
```

## Hooks

### useAuth()

Returns the authentication state and methods.

```tsx
const {
  // State
  isLoading,      // boolean
  isAuthenticated, // boolean  
  user,           // UserInfo | null
  error,          // string | null
  
  // Methods
  login,          // (provider: string, returnUrl?: string) => Promise<LoginResult>
  logout,         // () => Promise<LogoutResult>
  checkAuth,      // () => Promise<boolean>
  clearError      // () => void
} = useAuth();
```

### useAuthQuery(queryFn, options)

Hook for making authenticated API requests with caching and loading states.

```tsx
const { data, error, isLoading, refetch } = useAuthQuery(
  () => fetch('/api/user-profile').then(r => r.json()),
  {
    enabled: true,
    refetchOnWindowFocus: false,
    onSuccess: (data) => console.log('Success:', data),
    onError: (error) => console.error('Error:', error)
  }
);
```

### useEasyAuth(config)

Hook for configuring EasyAuth with custom settings.

```tsx
const auth = useEasyAuth({
  baseUrl: 'https://your-api.com',
  autoRefresh: true,
  onTokenExpired: () => console.log('Token expired'),
  onLoginRequired: () => console.log('Login required'),
  storage: 'localStorage'
});
```

## Components

### EasyAuthProvider

Context provider that wraps your app and provides authentication state.

```tsx
<EasyAuthProvider
  baseUrl="https://your-api.com"    // Required: API base URL
  autoRefresh={true}                // Optional: Auto-refresh tokens
  storage="localStorage"            // Optional: Storage type
  onTokenExpired={() => {}}         // Optional: Token expiry callback
  onLoginRequired={() => {}}        // Optional: Login required callback
  onError={(error) => {}}           // Optional: Error callback
>
  <App />
</EasyAuthProvider>
```

### AuthGuard

Component that conditionally renders content based on authentication status.

```tsx
<AuthGuard
  requiredRoles={['admin']}         // Optional: Required user roles
  requireAllRoles={false}           // Optional: Require all roles vs any
  fallback={<LoginForm />}          // Optional: Component when not authenticated
  loading={<Spinner />}             // Optional: Component while loading
  onUnauthorized={() => {}}         // Optional: Unauthorized callback
>
  <ProtectedContent />
</AuthGuard>
```

### LoginButton

Button component for initiating OAuth login flows.

```tsx
<LoginButton
  provider="google"                 // Required: OAuth provider
  returnUrl="/dashboard"            // Optional: Redirect after login
  disabled={false}                  // Optional: Disable button
  className="btn btn-primary"       // Optional: CSS classes
  loadingText="Logging in..."       // Optional: Loading state text
  onLoginStart={() => {}}           // Optional: Login start callback
  onLoginSuccess={(result) => {}}   // Optional: Login success callback
  onLoginError={(error) => {}}      // Optional: Login error callback
>
  Login with Google
</LoginButton>
```

### LogoutButton

Button component for logging out users.

```tsx
<LogoutButton
  disabled={false}                  // Optional: Disable button
  className="btn btn-secondary"     // Optional: CSS classes
  loadingText="Logging out..."      // Optional: Loading state text
  redirectAfterLogout={true}        // Optional: Redirect after logout
  onLogoutStart={() => {}}          // Optional: Logout start callback
  onLogoutComplete={() => {}}       // Optional: Logout complete callback
  onLogoutError={(error) => {}}     // Optional: Logout error callback
>
  Logout
</LogoutButton>
```

### UserProfile

Component for displaying user profile information.

```tsx
<UserProfile
  showEmail={true}                  // Optional: Show email
  showName={true}                   // Optional: Show name
  showAvatar={true}                 // Optional: Show profile picture
  avatarSize={40}                   // Optional: Avatar size in pixels
  className="user-profile"          // Optional: CSS classes
  onEdit={() => {}}                 // Optional: Edit profile callback
  loading={<Spinner />}             // Optional: Loading component
  error={<ErrorMessage />}          // Optional: Error component
/>
```

## Types

### UserInfo

```typescript
interface UserInfo {
  id: string;
  email?: string;
  name?: string;
  firstName?: string;
  lastName?: string;
  profilePicture?: string;
  provider?: string;
  roles: string[];
  permissions?: string[];
  lastLogin?: string;
  isVerified: boolean;
  locale?: string;
  timeZone?: string;
}
```

### LoginResult

```typescript
interface LoginResult {
  authUrl?: string;
  provider?: string;
  state?: string;
  redirectRequired: boolean;
}
```

### LogoutResult

```typescript
interface LogoutResult {
  loggedOut: boolean;
  redirectUrl?: string;
}
```

### EasyAuthConfig

```typescript
interface EasyAuthConfig {
  baseUrl?: string;
  autoRefresh?: boolean;
  onTokenExpired?: () => void;
  onLoginRequired?: () => void;
  onError?: (error: any) => void;
  storage?: 'localStorage' | 'sessionStorage' | 'memory';
  debug?: boolean;
}
```

## Error Handling

The package includes comprehensive error handling:

```tsx
const { error, clearError } = useAuth();

// Display errors
if (error) {
  return (
    <div className="error">
      Error: {error}
      <button onClick={clearError}>Dismiss</button>
    </div>
  );
}
```

## Storage Options

- `localStorage` (default): Persists across browser sessions
- `sessionStorage`: Cleared when tab is closed
- `memory`: Cleared when page is refreshed

## Security Considerations

- Tokens are automatically refreshed before expiration
- CSRF protection is built-in for OAuth flows
- Secure token storage with automatic cleanup
- HTTPS is required for production use

## Browser Support

- Chrome 60+
- Firefox 55+
- Safari 12+
- Edge 79+

## Bundle Size

- React package: ~25KB minified, ~7KB gzipped
- Zero dependencies beyond React
- Tree-shakeable exports