# @easyauth/vue

Vue 3 composables and components for EasyAuth Framework - Zero-config authentication for Vue applications.

## 🚀 Quick Start

```bash
npm install @easyauth/vue
```

### 1. Setup with Plugin (Recommended)

```typescript
import { createApp } from 'vue';
import { EasyAuthPlugin } from '@easyauth/vue';
import App from './App.vue';

const app = createApp(App);

app.use(EasyAuthPlugin, {
  baseUrl: 'https://your-api.com', // Optional, defaults to current domain
  autoRefresh: true,
  registerComponents: true, // Auto-register components globally
  componentPrefix: 'EasyAuth', // Component prefix (EasyAuthLoginButton, etc.)
  onTokenExpired: () => {
    // Handle token expiry
  },
});

app.mount('#app');
```

### 2. Manual Setup with Composables

```vue
<template>
  <div>
    <div v-if="isLoading">Loading...</div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else-if="isAuthenticated">
      <h1>Welcome, {{ user.name }}!</h1>
      <LogoutButton />
    </div>
    <div v-else>
      <h1>Please sign in</h1>
      <LoginButton provider="Google" />
      <LoginButton provider="Facebook" />
    </div>
  </div>
</template>

<script setup>
import { createEasyAuth } from '@easyauth/vue';
import LoginButton from '@easyauth/vue/components/LoginButton.vue';
import LogoutButton from '@easyauth/vue/components/LogoutButton.vue';

// Initialize auth (call once in your root component)
const auth = createEasyAuth({
  baseUrl: 'https://your-api.com',
  autoRefresh: true,
});

const { isLoading, isAuthenticated, user, error } = auth;
</script>
```

## 🪝 Composables

### `useAuth()`

Main authentication composable providing complete auth state and actions:

```vue
<script setup>
import { useAuth } from '@easyauth/vue';

const {
  // State
  isLoading,
  isAuthenticated,
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
</script>

<template>
  <button @click="handleLogin" :disabled="isLoading">
    {{ isLoading ? 'Loading...' : 'Sign in with Google' }}
  </button>
</template>
```

### `useUserProfile()`

Fetch user profile data with automatic caching:

```vue
<script setup>
import { useUserProfile } from '@easyauth/vue';

const { data: profile, isLoading, error, refetch } = useUserProfile();
</script>

<template>
  <div v-if="isLoading">Loading profile...</div>
  <div v-else-if="error">Error: {{ error.message }}</div>
  <div v-else-if="profile">
    <h2>{{ profile.name }}</h2>
    <p>{{ profile.email }}</p>
    <button @click="refetch">Refresh Profile</button>
  </div>
</template>
```

### `useAuthQuery()`

Generic composable for authenticated API calls:

```vue
<script setup>
import { useAuthQuery } from '@easyauth/vue';

const { data, isLoading, error, refetch } = useAuthQuery(
  () => fetch('/api/my-data').then(r => r.json()),
  {
    enabled: true,
    refetchOnWindowFocus: true,
    onError: (error) => console.error(error),
  }
);
</script>

<template>
  <div v-if="isLoading">Loading...</div>
  <div v-else-if="error">Error: {{ error.message }}</div>
  <div v-else-if="data">{{ data }}</div>
</template>
```

## 🧩 Components

### LoginButton

```vue
<template>
  <div>
    <!-- Basic login button -->
    <LoginButton 
      provider="Google" 
      return-url="/dashboard"
      @login-success="onLoginSuccess"
      @login-error="onLoginError"
    />

    <!-- Custom styling -->
    <LoginButton 
      provider="Facebook"
      class="my-custom-button"
      loading-text="Signing in..."
    >
      Continue with Facebook
    </LoginButton>

    <!-- Event handlers -->
    <LoginButton 
      provider="Apple"
      :on-login-start="() => console.log('Login started')"
      :on-login-error="(error) => console.error(error)"
    />
  </div>
</template>
```

### LogoutButton

```vue
<template>
  <LogoutButton 
    class="logout-btn"
    loading-text="Signing out..."
    :redirect-after-logout="true"
    @logout-complete="onLogoutComplete"
  >
    Sign Out
  </LogoutButton>
</template>
```

### AuthGuard

Protect content based on authentication status:

```vue
<template>
  <div>
    <!-- Basic authentication required -->
    <AuthGuard>
      <template #fallback>
        <LoginPage />
      </template>
      <Dashboard />
    </AuthGuard>

    <!-- Role-based access -->
    <AuthGuard 
      :required-roles="['admin', 'moderator']"
      :require-all-roles="false"
      :on-unauthorized="() => alert('Access denied')"
      redirect-to="/unauthorized"
    >
      <template #unauthorized>
        <div>You don't have permission to view this content.</div>
      </template>
      <AdminPanel />
    </AuthGuard>
  </div>
</template>
```

### UserProfile

Display user information:

```vue
<template>
  <div>
    <!-- Complete user profile -->
    <UserProfile 
      :show-avatar="true"
      :show-email="true"
      :show-roles="true"
      class="user-profile"
    />

    <!-- Custom render with scoped slot -->
    <UserProfile v-slot="{ user }">
      <div class="custom-profile">
        <img :src="user.profilePicture" :alt="user.name" />
        <span>{{ user.name }} ({{ user.roles.join(', ') }})</span>
      </div>
    </UserProfile>

    <!-- Fallback content -->
    <UserProfile>
      <template #fallback>
        <div>Please log in to see your profile</div>
      </template>
    </UserProfile>
  </div>
</template>
```

## 🔧 Configuration

### Plugin Configuration

```typescript
app.use(EasyAuthPlugin, {
  // API base URL (optional, defaults to current domain)
  baseUrl: 'https://api.example.com',
  
  // Automatically refresh tokens before expiry
  autoRefresh: true,
  
  // Storage type for tokens
  storage: 'localStorage', // 'localStorage' | 'sessionStorage' | 'memory'
  
  // Debug mode
  debug: import.meta.env.DEV,
  
  // Component registration
  registerComponents: true,
  componentPrefix: 'EasyAuth', // Results in EasyAuthLoginButton, etc.
  
  // Event handlers
  onTokenExpired: () => {
    console.log('Token expired, redirecting to login');
    router.push('/login');
  },
  
  onLoginRequired: () => {
    console.log('Login required');
  },
  
  onError: (error) => {
    console.error('Auth error:', error);
  },
});
```

## 🎯 Zero-Config Features

### Automatic Backend Detection
The package automatically detects your backend URL:

```vue
<script setup>
// No configuration needed!
// Automatically works with:
// - Same domain: https://myapp.com/api
// - Subdomain: https://api.myapp.com
// - Localhost development: http://localhost:3001/api
import { useAuth } from '@easyauth/vue';
const auth = useAuth(); // Just works!
</script>
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

```vue
<script setup>
import { useAuth, useAuthQuery } from '@easyauth/vue';

// Set up auto-refresh
const auth = createEasyAuth({ autoRefresh: true });

// Your API calls just work without interruption
const { data } = useAuthQuery(() => fetch('/api/protected-data'));
</script>
```

## 🧪 Testing

Mock authentication for testing:

```typescript
import { mount } from '@vue/test-utils';
import { createEasyAuth } from '@easyauth/vue';

describe('MyComponent', () => {
  it('shows user profile when authenticated', () => {
    // Create auth with memory storage for tests
    createEasyAuth({ 
      storage: 'memory',
      debug: false 
    });
    
    const wrapper = mount(MyComponent);
    // ... test assertions
  });
});
```

## 🔒 Security Features

- **Automatic token refresh** before expiry
- **Secure storage** options (localStorage, sessionStorage, memory)
- **CSRF protection** with correlation IDs
- **Request tracing** for debugging
- **Role-based access control** components
- **Reactive security state** with Vue's reactivity system

## 🌐 TypeScript Support

Full TypeScript support with comprehensive type definitions:

```vue
<script setup lang="ts">
import type { UserInfo, AuthState } from '@easyauth/vue';

interface Props {
  user: UserInfo;
}

const props = defineProps<Props>();

// Full type safety
const roles: string[] = props.user.roles;
const isAdmin = roles.includes('admin');
</script>

<template>
  <div>{{ user.name }}</div>
</template>
```

## 🔗 Integration with Vue Ecosystem

### Vue Router

```vue
<script setup>
import { useAuth } from '@easyauth/vue';
import { useRouter } from 'vue-router';

const auth = useAuth();
const router = useRouter();

// Navigation guard
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return '/login';
  }
});
</script>
```

### Pinia Integration

```typescript
// stores/auth.ts
import { defineStore } from 'pinia';
import { useAuth } from '@easyauth/vue';

export const useAuthStore = defineStore('auth', () => {
  const easyAuth = useAuth();
  
  return {
    ...easyAuth,
    // Additional store logic
  };
});
```

### Nuxt 3 Plugin

```typescript
// plugins/easyauth.client.ts
import { EasyAuthPlugin } from '@easyauth/vue';

export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.vueApp.use(EasyAuthPlugin, {
    baseUrl: useRuntimeConfig().public.apiUrl,
    autoRefresh: true,
  });
});
```

## 📚 Examples

### Provider-Specific Buttons

```vue
<template>
  <div class="login-buttons">
    <!-- Google -->
    <LoginButton provider="Google" class="btn-google">
      <template>
        <GoogleIcon />
        Continue with Google
      </template>
    </LoginButton>

    <!-- Facebook -->
    <LoginButton provider="Facebook" class="btn-facebook" />

    <!-- Apple -->
    <LoginButton provider="Apple" class="btn-apple" />

    <!-- Azure B2C -->
    <LoginButton provider="AzureB2C" class="btn-azure" />
  </div>
</template>

<style scoped>
.btn-google { background: #4285f4; color: white; }
.btn-facebook { background: #1877f2; color: white; }
.btn-apple { background: #000; color: white; }
.btn-azure { background: #0078d4; color: white; }
</style>
```

### Custom Loading States

```vue
<script setup>
import { useAuth } from '@easyauth/vue';

const auth = useAuth();
</script>

<template>
  <div class="app">
    <div v-if="auth.isLoading" class="loading">
      <div class="spinner"></div>
      <p>Checking authentication...</p>
    </div>
    
    <router-view v-else />
  </div>
</template>
```

## 🤝 Contributing

This package is part of the EasyAuth Framework. See the main repository for contribution guidelines.

## 📄 License

MIT License - see LICENSE file for details.