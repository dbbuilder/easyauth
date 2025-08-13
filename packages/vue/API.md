# @easyauth/vue API Reference

## Overview

The EasyAuth Vue package provides composables and components for integrating authentication into Vue 3 applications.

## Installation

```bash
npm install @easyauth/vue
```

## Quick Start

```vue
<template>
  <div v-if="!isAuthenticated">
    <LoginButton provider="google" />
  </div>
  <div v-else>
    Welcome, {{ user?.name }}!
    <LogoutButton />
  </div>
</template>

<script setup>
import { useAuth, LoginButton, LogoutButton } from '@easyauth/vue';

const { isAuthenticated, user } = useAuth();
</script>
```

## Plugin Installation

```javascript
import { createApp } from 'vue';
import { createEasyAuth } from '@easyauth/vue';
import App from './App.vue';

const app = createApp(App);

// Install EasyAuth plugin
app.use(createEasyAuth({
  config: {
    baseUrl: 'https://your-api.com',
    autoRefresh: true
  }
}));

app.mount('#app');
```

## Composables

### useAuth()

Returns reactive authentication state and methods.

```vue
<script setup>
import { useAuth } from '@easyauth/vue';

const {
  // Reactive state
  isLoading,      // Ref<boolean>
  isAuthenticated, // Ref<boolean>
  user,           // Ref<UserInfo | null>
  error,          // Ref<string | null>
  tokenExpiry,    // Ref<Date | null>
  sessionId,      // Ref<string | null>
  
  // Methods
  login,          // (provider: string, returnUrl?: string) => Promise<LoginResult>
  logout,         // () => Promise<LogoutResult>
  refreshToken,   // () => Promise<boolean>
  checkAuth,      // () => Promise<boolean>
  clearError      // () => void
} = useAuth();
</script>
```

### useAuthQuery(queryFn, options)

Composable for making authenticated API requests with reactive state.

```vue
<script setup>
import { useAuthQuery } from '@easyauth/vue';

const { data, error, isLoading, refetch } = useAuthQuery(
  () => fetch('/api/user-profile').then(r => r.json()),
  {
    enabled: true,
    refetchOnWindowFocus: false,
    onSuccess: (data) => console.log('Success:', data),
    onError: (error) => console.error('Error:', error)
  }
);
</script>
```

### useEasyAuth(config)

Composable for configuring EasyAuth with custom settings.

```vue
<script setup>
import { useEasyAuth } from '@easyauth/vue';

const auth = useEasyAuth({
  apiUrl: 'https://your-api.com',
  providers: ['google', 'facebook']
});
</script>
```

### useUserProfile()

Convenient composable for fetching user profile data.

```vue
<script setup>
import { useUserProfile } from '@easyauth/vue';

const { data: profile, isLoading, error } = useUserProfile();
</script>
```

## Components

### AuthGuard

Component that conditionally renders content based on authentication status.

```vue
<template>
  <AuthGuard
    :required-roles="['admin']"       <!-- Optional: Required user roles -->
    :require-all-roles="false"        <!-- Optional: Require all roles vs any -->
    :redirect-to="/login"             <!-- Optional: Redirect URL when unauthorized -->
    @unauthorized="handleUnauthorized" <!-- Optional: Unauthorized event -->
  >
    <template #loading>
      <div>Loading...</div>
    </template>
    
    <template #fallback>
      <div>Authentication required</div>
    </template>
    
    <template #unauthorized>
      <div>Insufficient permissions</div>
    </template>
    
    <!-- Protected content -->
    <ProtectedContent />
  </AuthGuard>
</template>
```

### LoginButton

Button component for initiating OAuth login flows.

```vue
<template>
  <LoginButton
    provider="google"                 <!-- Required: OAuth provider -->
    :return-url="'/dashboard'"        <!-- Optional: Redirect after login -->
    :disabled="false"                 <!-- Optional: Disable button -->
    class="btn btn-primary"           <!-- Optional: CSS classes -->
    loading-text="Logging in..."      <!-- Optional: Loading state text -->
    @login-start="onLoginStart"       <!-- Optional: Login start event -->
    @login-success="onLoginSuccess"   <!-- Optional: Login success event -->
    @login-error="onLoginError"       <!-- Optional: Login error event -->
  >
    Login with Google
  </LoginButton>
</template>
```

### LogoutButton

Button component for logging out users.

```vue
<template>
  <LogoutButton
    :disabled="false"                 <!-- Optional: Disable button -->
    class="btn btn-secondary"         <!-- Optional: CSS classes -->
    loading-text="Logging out..."     <!-- Optional: Loading state text -->
    :redirect-after-logout="true"     <!-- Optional: Redirect after logout -->
    @logout-start="onLogoutStart"     <!-- Optional: Logout start event -->
    @logout-complete="onLogoutComplete" <!-- Optional: Logout complete event -->
    @logout-error="onLogoutError"     <!-- Optional: Logout error event -->
  >
    Logout
  </LogoutButton>
</template>
```

### UserProfile

Component for displaying user profile information.

```vue
<template>
  <UserProfile
    :show-email="true"                <!-- Optional: Show email -->
    :show-name="true"                 <!-- Optional: Show name -->
    :show-avatar="true"               <!-- Optional: Show profile picture -->
    :avatar-size="40"                 <!-- Optional: Avatar size in pixels -->
    class="user-profile"              <!-- Optional: CSS classes -->
    @edit="handleEdit"                <!-- Optional: Edit profile event -->
  >
    <template #loading>
      <div>Loading profile...</div>
    </template>
    
    <template #error="{ error }">
      <div>Error: {{ error }}</div>
    </template>
  </UserProfile>
</template>
```

## Plugin Options

```typescript
interface EasyAuthPluginOptions {
  config: EasyAuthConfig;
  components?: {
    LoginButton?: string;    // Global component name
    LogoutButton?: string;   // Global component name
  };
}
```

## Types

### EasyAuthConfig

```typescript
interface EasyAuthConfig {
  apiUrl?: string;
  providers?: string[];
}
```

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

## Error Handling

```vue
<template>
  <div v-if="error" class="error">
    Error: {{ error }}
    <button @click="clearError">Dismiss</button>
  </div>
</template>

<script setup>
import { useAuth } from '@easyauth/vue';

const { error, clearError } = useAuth();
</script>
```

## Reactivity

All authentication state is reactive and will automatically update your UI:

```vue
<template>
  <div>
    <p v-if="isLoading">Loading...</p>
    <p v-else-if="isAuthenticated">Welcome, {{ user?.name }}!</p>
    <p v-else>Please log in</p>
  </div>
</template>

<script setup>
import { useAuth } from '@easyauth/vue';

const { isLoading, isAuthenticated, user } = useAuth();
</script>
```

## Styling

The package includes default CSS classes for components:

```css
/* Default styles are included */
.easyauth-login-button {
  /* Button styling */
}

.easyauth-logout-button {
  /* Logout button styling */
}

/* Provider-specific styling */
.provider-google { /* Google colors */ }
.provider-facebook { /* Facebook colors */ }
.provider-apple { /* Apple colors */ }
```

## Security Considerations

- Tokens are automatically refreshed before expiration
- CSRF protection is built-in for OAuth flows
- Secure token storage with automatic cleanup
- HTTPS is required for production use

## Vue Compatibility

- Vue 3.0+
- Composition API support
- TypeScript support included
- SSR compatible

## Bundle Size

- Vue package: ~17KB minified, ~5KB gzipped
- CSS included: ~4KB
- Tree-shakeable exports