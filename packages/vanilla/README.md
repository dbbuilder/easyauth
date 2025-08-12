# @easyauth/vanilla

Vanilla JavaScript client for EasyAuth Framework - Zero-config authentication for any JavaScript framework or vanilla applications.

## Installation

```bash
npm install @easyauth/vanilla
```

Or via CDN:

```html
<script src="https://cdn.jsdelivr.net/npm/@easyauth/vanilla@latest/dist/index.umd.js"></script>
```

## Quick Start

### ES6 Modules

```javascript
import EasyAuth from '@easyauth/vanilla';

// Initialize the client
const auth = new EasyAuth({
  baseUrl: 'http://localhost:5000', // Your backend URL
  autoRefresh: true,
  debug: true
});

// Check authentication status
const isAuthenticated = await auth.checkAuth();

if (isAuthenticated) {
  console.log('User is authenticated:', auth.user);
} else {
  console.log('User is not authenticated');
}
```

### CommonJS

```javascript
const { EasyAuthClient } = require('@easyauth/vanilla');

const auth = new EasyAuthClient({
  baseUrl: 'http://localhost:5000'
});
```

### Browser (UMD)

```html
<script src="https://cdn.jsdelivr.net/npm/@easyauth/vanilla@latest/dist/index.umd.js"></script>
<script>
  const auth = new EasyAuth.EasyAuthClient({
    baseUrl: 'http://localhost:5000'
  });
</script>
```

## API Reference

### EasyAuthClient

Main authentication client class.

```javascript
const auth = new EasyAuthClient(config);
```

#### Configuration

```javascript
interface EasyAuthConfig {
  baseUrl?: string;                    // Backend API URL
  autoRefresh?: boolean;               // Auto-refresh tokens before expiry
  onTokenExpired?: () => void;         // Callback when token expires
  onLoginRequired?: () => void;        // Callback when login is required
  onError?: (error: any) => void;      // Global error handler
  storage?: 'localStorage' | 'sessionStorage' | 'memory';  // Token storage
  debug?: boolean;                     // Enable debug logging
}
```

#### Properties

```javascript
// Get current authentication state
const state = auth.currentState;

// Quick access properties
const isAuthenticated = auth.isAuthenticated;
const user = auth.user;
const isLoading = auth.isLoading;
const error = auth.error;
```

#### Methods

##### Authentication

```javascript
// Login with OAuth provider
const result = await auth.login('google', '/dashboard');

// Logout
const result = await auth.logout();

// Check authentication status
const isAuthenticated = await auth.checkAuth();

// Refresh access token
const success = await auth.refreshToken();

// Get user profile
const user = await auth.getUserProfile();

// Clear error state
auth.clearError();
```

##### Authorization

```javascript
// Check if user has specific role
const isAdmin = auth.hasRole('admin');

// Check if user has specific permission
const canWrite = auth.hasPermission('write');

// Check if user has any of the specified roles
const isModerator = auth.hasAnyRole(['admin', 'moderator']);

// Check if user has any of the specified permissions
const canPublish = auth.hasAnyPermission(['write', 'publish']);
```

##### Event Handling

```javascript
// Listen for state changes
const unsubscribe = auth.onStateChange((state) => {
  console.log('Auth state changed:', state);
});

// Custom events (using EventTarget)
auth.addEventListener('statechange', (event) => {
  console.log('State changed:', event.detail);
});

auth.addEventListener('loginstart', (event) => {
  console.log('Login started:', event.detail);
});

auth.addEventListener('loginsuccess', (event) => {
  console.log('Login successful:', event.detail);
});

auth.addEventListener('loginerror', (event) => {
  console.log('Login failed:', event.detail);
});

// Cleanup
unsubscribe();
```

### EasyAuthUI

Helper class for creating authentication UI elements.

```javascript
import { EasyAuthClient, EasyAuthUI } from '@easyauth/vanilla';

const auth = new EasyAuthClient({ baseUrl: 'http://localhost:5000' });
const ui = new EasyAuthUI(auth);
```

#### Login Button

```javascript
// Create login button
const loginElement = document.getElementById('login-button');
ui.createLoginButton(loginElement, {
  provider: 'google',
  returnUrl: '/dashboard',
  text: 'Sign in with Google',
  loadingText: 'Signing in...',
  className: 'btn btn-primary',
  disabled: false,
  loadingSpinner: '⏳'
});
```

#### Logout Button

```javascript
// Create logout button
const logoutElement = document.getElementById('logout-button');
ui.createLogoutButton(logoutElement, {
  text: 'Sign Out',
  loadingText: 'Signing out...',
  className: 'btn btn-secondary'
});
```

#### User Profile

```javascript
// Render user profile
const profileElement = document.getElementById('user-profile');
ui.renderUserProfile(profileElement, {
  showAvatar: true,
  showName: true,
  showEmail: true,
  showRoles: true,
  showProvider: false,
  showStatus: true,
  showLastLogin: false,
  className: 'user-profile'
});
```

## Usage Examples

### Complete Authentication Flow

```html
<!DOCTYPE html>
<html>
<head>
  <title>EasyAuth Vanilla Example</title>
  <style>
    .user-profile {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin: 10px 0;
    }
    
    .avatar-image, .avatar-fallback {
      width: 40px;
      height: 40px;
      border-radius: 50%;
    }
    
    .avatar-fallback {
      background: #007bff;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
    }
    
    .role-badge {
      background: #f0f0f0;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 12px;
      margin-right: 5px;
    }
    
    .btn {
      padding: 10px 15px;
      margin: 5px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    
    .btn-primary { background: #007bff; color: white; }
    .btn-secondary { background: #6c757d; color: white; }
  </style>
</head>
<body>
  <div id="app">
    <h1>EasyAuth Vanilla Example</h1>
    
    <!-- Authentication status -->
    <div id="auth-status"></div>
    
    <!-- Login buttons -->
    <div id="login-section" style="display: none;">
      <h2>Sign In</h2>
      <button id="google-login">Sign in with Google</button>
      <button id="microsoft-login">Sign in with Microsoft</button>
    </div>
    
    <!-- User section -->
    <div id="user-section" style="display: none;">
      <h2>Welcome!</h2>
      <div id="user-profile"></div>
      <button id="logout-button">Sign Out</button>
      
      <!-- Protected content -->
      <div id="admin-section" style="display: none;">
        <h3>Admin Only</h3>
        <p>This content is only visible to admins.</p>
      </div>
    </div>
  </div>

  <script src="https://cdn.jsdelivr.net/npm/@easyauth/vanilla@latest/dist/index.umd.js"></script>
  <script>
    // Initialize EasyAuth
    const auth = new EasyAuth.EasyAuthClient({
      baseUrl: 'http://localhost:5000',
      autoRefresh: true,
      debug: true,
      onLoginRequired: () => {
        showLoginSection();
      },
      onError: (error) => {
        console.error('Auth Error:', error);
      }
    });

    // Initialize UI helper
    const ui = new EasyAuth.EasyAuthUI(auth);

    // Elements
    const authStatus = document.getElementById('auth-status');
    const loginSection = document.getElementById('login-section');
    const userSection = document.getElementById('user-section');
    const adminSection = document.getElementById('admin-section');

    // Setup UI components
    ui.createLoginButton(document.getElementById('google-login'), {
      provider: 'google',
      returnUrl: '/',
      text: 'Sign in with Google'
    });

    ui.createLoginButton(document.getElementById('microsoft-login'), {
      provider: 'microsoft',
      returnUrl: '/',
      text: 'Sign in with Microsoft'
    });

    ui.createLogoutButton(document.getElementById('logout-button'));

    ui.renderUserProfile(document.getElementById('user-profile'), {
      showAvatar: true,
      showName: true,
      showEmail: true,
      showRoles: true,
      showStatus: true
    });

    // State management
    function updateUI() {
      const { isAuthenticated, isLoading, user, error } = auth.currentState;

      if (isLoading) {
        authStatus.innerHTML = '⏳ Loading...';
        return;
      }

      if (error) {
        authStatus.innerHTML = `❌ Error: ${error}`;
        return;
      }

      if (isAuthenticated && user) {
        authStatus.innerHTML = `✅ Authenticated as ${user.name || user.email}`;
        showUserSection();
        
        // Show admin section if user has admin role
        if (auth.hasRole('admin')) {
          adminSection.style.display = 'block';
        } else {
          adminSection.style.display = 'none';
        }
      } else {
        authStatus.innerHTML = '❌ Not authenticated';
        showLoginSection();
      }
    }

    function showLoginSection() {
      loginSection.style.display = 'block';
      userSection.style.display = 'none';
    }

    function showUserSection() {
      loginSection.style.display = 'none';
      userSection.style.display = 'block';
    }

    // Listen for state changes
    auth.onStateChange(updateUI);

    // Initial state update
    updateUI();

    // Check authentication on page load
    auth.checkAuth().catch(console.error);
  </script>
</body>
</html>
```

### Framework Integration Examples

#### With jQuery

```javascript
$(document).ready(function() {
  const auth = new EasyAuth.EasyAuthClient({
    baseUrl: 'http://localhost:5000'
  });

  // Update UI on state change
  auth.onStateChange((state) => {
    if (state.isAuthenticated) {
      $('#login-section').hide();
      $('#user-section').show();
      $('#username').text(state.user?.name || 'User');
    } else {
      $('#login-section').show();
      $('#user-section').hide();
    }
  });

  // Login button
  $('#login-btn').click(() => {
    auth.login('google');
  });

  // Logout button
  $('#logout-btn').click(() => {
    auth.logout();
  });
});
```

#### With Alpine.js

```html
<div x-data="authData()">
  <div x-show="!isAuthenticated">
    <button @click="login('google')">Sign in with Google</button>
  </div>
  
  <div x-show="isAuthenticated">
    <p x-text="`Welcome, ${user?.name}!`"></p>
    <button @click="logout()">Sign Out</button>
  </div>
</div>

<script>
  function authData() {
    const auth = new EasyAuth.EasyAuthClient({
      baseUrl: 'http://localhost:5000'
    });

    return {
      isAuthenticated: false,
      user: null,
      
      init() {
        auth.onStateChange((state) => {
          this.isAuthenticated = state.isAuthenticated;
          this.user = state.user;
        });
        auth.checkAuth();
      },
      
      async login(provider) {
        await auth.login(provider);
      },
      
      async logout() {
        await auth.logout();
      }
    };
  }
</script>
```

#### With Web Components

```javascript
class AuthComponent extends HTMLElement {
  constructor() {
    super();
    this.auth = new EasyAuth.EasyAuthClient({
      baseUrl: 'http://localhost:5000'
    });
  }

  connectedCallback() {
    this.render();
    
    this.auth.onStateChange(() => {
      this.render();
    });
    
    this.auth.checkAuth();
  }

  render() {
    const { isAuthenticated, user } = this.auth.currentState;
    
    if (isAuthenticated) {
      this.innerHTML = `
        <div>
          <p>Welcome, ${user?.name || 'User'}!</p>
          <button onclick="this.parentElement.parentElement.logout()">Sign Out</button>
        </div>
      `;
    } else {
      this.innerHTML = `
        <button onclick="this.parentElement.login()">Sign In</button>
      `;
    }
  }

  async login() {
    await this.auth.login('google');
  }

  async logout() {
    await this.auth.logout();
  }
}

customElements.define('auth-component', AuthComponent);
```

## CSS Styling

The UI components use semantic class names that you can style:

```css
/* Login/Logout buttons */
.easyauth-login-btn,
.easyauth-logout-btn {
  padding: 10px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.easyauth-login-btn {
  background: #007bff;
  color: white;
}

.easyauth-logout-btn {
  background: #6c757d;
  color: white;
}

/* User profile */
.easyauth-user-profile {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
}

.user-avatar {
  flex-shrink: 0;
}

.avatar-image {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: cover;
}

.avatar-fallback {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #007bff;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
}

.user-info {
  flex: 1;
}

.user-name {
  font-weight: 600;
  margin-bottom: 4px;
}

.user-email {
  color: #666;
  font-size: 14px;
  margin-bottom: 8px;
}

.role-badge {
  display: inline-block;
  background: #f0f0f0;
  color: #333;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  margin-right: 6px;
}

.user-status.verified {
  background: #d4edda;
  color: #155724;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
}

.user-status.unverified {
  background: #f8d7da;
  color: #721c24;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
}

/* Loading states */
.user-profile-loading {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
}

.loading-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #f0f0f0;
  animation: pulse 1.5s ease-in-out infinite;
}

.loading-line {
  height: 12px;
  background: #f0f0f0;
  border-radius: 6px;
  margin-bottom: 8px;
  animation: pulse 1.5s ease-in-out infinite;
}

.loading-line.short {
  width: 60%;
}

@keyframes pulse {
  0% { opacity: 1; }
  50% { opacity: 0.5; }
  100% { opacity: 1; }
}
```

## Browser Support

- Chrome 80+
- Firefox 74+
- Safari 13.1+
- Edge 80+

## Development

```bash
# Install dependencies
npm install

# Build the package
npm run build

# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Build and watch for changes
npm run build:watch
```

## License

MIT