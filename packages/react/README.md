# @easyauth/react

React hooks and components for EasyAuth Framework - Zero-config authentication for React applications.

## 🚀 Quick Start

```bash
npm install @easyauth/react
```

### 1. Setup AuthProvider

Wrap your app with the `AuthProvider`:

```tsx
import React from 'react';
import { AuthProvider } from '@easyauth/react';
import App from './App';

export default function Root() {
  return (
    <AuthProvider 
      config={{
        baseUrl: 'https://your-api.com', // Optional, defaults to current domain
        autoRefresh: true,
        onTokenExpired: () => {
          // Handle token expiry
        },
      }}
    >
      <App />
    </AuthProvider>
  );
}
```

### 2. Use Authentication

```tsx
import React from 'react';
import { useAuth, LoginButton, LogoutButton, UserProfile } from '@easyauth/react';

export default function App() {
  const { isAuthenticated, isLoading, user, error } = useAuth();

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      {isAuthenticated ? (
        <div>
          <h1>Welcome!</h1>
          <UserProfile showEmail showRoles />
          <LogoutButton />
        </div>
      ) : (
        <div>
          <h1>Please sign in</h1>
          <GoogleLoginButton />
          <FacebookLoginButton />
          <AppleLoginButton />
          <AzureB2CLoginButton />
        </div>
      )}
    </div>
  );
}
```

## 🪝 Hooks

### `useAuth()`

Main authentication hook providing complete auth state and actions:

```tsx
import { useAuth } from '@easyauth/react';

function MyComponent() {
  const {
    // State
    isAuthenticated,
    isLoading,
    user,
    error,
    tokenExpiry,
    sessionId,
    
    // Actions
    login,
    logout,
    refreshToken,
    checkAuth,
    clearError,
  } = useAuth();

  const handleLogin = async () => {
    try {
      await login('Google', '/dashboard');
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  return (
    <button onClick={handleLogin}>
      {isLoading ? 'Loading...' : 'Sign in with Google'}
    </button>
  );
}
```

### `useUserProfile()`

Fetch user profile data with automatic caching:

```tsx
import { useUserProfile } from '@easyauth/react';

function UserDetails() {
  const { data: profile, isLoading, error, refetch } = useUserProfile();

  if (isLoading) return <div>Loading profile...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      <h2>{profile.name}</h2>
      <p>{profile.email}</p>
      <button onClick={refetch}>Refresh Profile</button>
    </div>
  );
}
```

### `useAuthQuery()`

Generic hook for authenticated API calls:

```tsx
import { useAuthQuery } from '@easyauth/react';

function MyData() {
  const { data, isLoading, error } = useAuthQuery(
    () => fetch('/api/my-data').then(r => r.json()),
    {
      enabled: true,
      refetchOnWindowFocus: true,
      onError: (error) => console.error(error),
    }
  );

  // ... render logic
}
```

## 🧩 Components

### Login Buttons

Pre-built login buttons for different providers:

```tsx
import { 
  LoginButton, 
  GoogleLoginButton, 
  FacebookLoginButton, 
  AppleLoginButton,
  AzureB2CLoginButton 
} from '@easyauth/react';

function LoginPage() {
  return (
    <div>
      {/* Generic login button */}
      <LoginButton 
        provider="Custom"
        onLoginStart={() => console.log('Login started')}
        onLoginError={(error) => console.error(error)}
      >
        Sign in with Custom Provider
      </LoginButton>

      {/* Provider-specific buttons */}
      <GoogleLoginButton 
        returnUrl="/dashboard"
        className="btn-google"
      />
      
      <FacebookLoginButton />
      <AppleLoginButton />
      <AzureB2CLoginButton />
    </div>
  );
}
```

### Logout Button

```tsx
import { LogoutButton } from '@easyauth/react';

function Header() {
  return (
    <header>
      <LogoutButton 
        onLogoutComplete={() => window.location.href = '/'}
        className="btn-logout"
      >
        Sign Out
      </LogoutButton>
    </header>
  );
}
```

### AuthGuard

Protect components based on authentication status:

```tsx
import { AuthGuard, RequireAuth, RequireRoles } from '@easyauth/react';

function App() {
  return (
    <div>
      {/* Basic authentication required */}
      <RequireAuth fallback={<LoginPage />}>
        <Dashboard />
      </RequireAuth>

      {/* Role-based access */}
      <RequireRoles 
        roles={['admin', 'moderator']} 
        requireAll={false}
        fallback={<div>Access denied</div>}
      >
        <AdminPanel />
      </RequireRoles>

      {/* Advanced auth guard */}
      <AuthGuard
        requiredRoles={['admin']}
        onUnauthorized={() => {
          alert('Admin access required');
        }}
        redirectTo="/unauthorized"
      >
        <SuperSecretComponent />
      </AuthGuard>
    </div>
  );
}
```

### User Profile Components

Display user information:

```tsx
import { UserProfile, UserAvatar, UserName, UserEmail } from '@easyauth/react';

function Header() {
  return (
    <header>
      {/* Complete user profile */}
      <UserProfile 
        showAvatar 
        showEmail 
        showRoles
        className="user-profile"
      />

      {/* Individual components */}
      <UserAvatar size={40} onClick={() => openUserMenu()} />
      <UserName className="username" />
      <UserEmail className="user-email" />

      {/* Custom render prop */}
      <UserProfile>
        {(user) => (
          <div>
            <img src={user.profilePicture} />
            <span>{user.name} ({user.roles.join(', ')})</span>
          </div>
        )}
      </UserProfile>
    </header>
  );
}
```

## 🔧 Configuration

### AuthProvider Config

```tsx
import { AuthProvider } from '@easyauth/react';

<AuthProvider config={{
  // API base URL (optional, defaults to current domain)
  baseUrl: 'https://api.example.com',
  
  // Automatically refresh tokens before expiry
  autoRefresh: true,
  
  // Storage type for tokens
  storage: 'localStorage', // 'localStorage' | 'sessionStorage' | 'memory'
  
  // Debug mode
  debug: process.env.NODE_ENV === 'development',
  
  // Event handlers
  onTokenExpired: () => {
    console.log('Token expired, redirecting to login');
    window.location.href = '/login';
  },
  
  onLoginRequired: () => {
    console.log('Login required');
  },
  
  onError: (error) => {
    console.error('Auth error:', error);
  },
}}>
  <App />
</AuthProvider>
```

## 🎯 Zero-Config Features

### Automatic Backend Detection
The package automatically detects your backend URL and configures CORS:

```tsx
// No configuration needed!
// Automatically works with:
// - Same domain: https://myapp.com/api
// - Subdomain: https://api.myapp.com
// - Localhost development: http://localhost:3001/api
```

### Unified API Responses
All authentication endpoints return consistent response formats:

```json
{
  "success": true,
  "data": {
    "isAuthenticated": true,
    "user": { ... }
  },
  "message": "User is authenticated",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "abc123"
}
```

### Automatic Token Refresh
Tokens are automatically refreshed before expiry:

```tsx
// Automatic refresh happens transparently
// Your API calls just work without interruption
const { data } = useAuthQuery(() => fetch('/api/protected-data'));
```

## 🧪 Testing

Mock authentication for testing:

```tsx
import { AuthProvider } from '@easyauth/react';
import { render } from '@testing-library/react';

function TestWrapper({ children, mockUser = null }) {
  return (
    <AuthProvider config={{
      storage: 'memory', // Use memory storage for tests
      debug: false,
    }}>
      {/* Mock initial auth state */}
      {children}
    </AuthProvider>
  );
}

test('shows user profile when authenticated', () => {
  render(
    <TestWrapper mockUser={{ name: 'John Doe', email: 'john@example.com' }}>
      <MyComponent />
    </TestWrapper>
  );
  
  // ... test assertions
});
```

## 🔒 Security Features

- **Automatic token refresh** before expiry
- **Secure storage** options (localStorage, sessionStorage, memory)
- **CSRF protection** with correlation IDs
- **Request tracing** for debugging
- **Role-based access control** components

## 🌐 TypeScript Support

Full TypeScript support with comprehensive type definitions:

```tsx
import { UserInfo, AuthState, ApiResponse } from '@easyauth/react';

interface MyComponentProps {
  user: UserInfo;
}

function MyComponent({ user }: MyComponentProps) {
  // Full type safety
  const roles: string[] = user.roles;
  const isAdmin = roles.includes('admin');
  
  return <div>{user.name}</div>;
}
```

## 🔗 Integration with Other Libraries

### React Router

```tsx
import { Routes, Route, Navigate } from 'react-router-dom';
import { RequireAuth } from '@easyauth/react';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={
        <RequireAuth fallback={<Navigate to="/login" replace />}>
          <Dashboard />
        </RequireAuth>
      } />
    </Routes>
  );
}
```

### SWR Integration

```tsx
import useSWR from 'swr';
import { useAuth } from '@easyauth/react';

function useAuthenticatedSWR<T>(url: string) {
  const { isAuthenticated } = useAuth();
  
  return useSWR<T>(
    isAuthenticated ? url : null,
    (url) => fetch(url).then(r => r.json())
  );
}
```

## 📚 Examples

See the `/examples` directory for complete example applications:

- **Basic React App** - Simple authentication setup
- **React Router Integration** - Protected routes
- **TypeScript Example** - Full type safety
- **Custom Styling** - Styled components
- **Testing Examples** - Unit and integration tests

## 🤝 Contributing

This package is part of the EasyAuth Framework. See the main repository for contribution guidelines.

## 📄 License

MIT License - see LICENSE file for details.