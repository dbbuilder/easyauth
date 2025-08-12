# EasyAuth Integration Guide for Claude Code

This guide contains pre-configured instructions for Claude Code to rapidly integrate EasyAuth into new projects.

## Quick Integration Commands

When a user asks to "add EasyAuth to my project" or "integrate authentication", use these steps:

### 1. **React/Next.js Projects**

```bash
# Install the SDK
npm install @easyauth/js-sdk

# Create auth configuration (update with actual values)
cat > src/auth/config.js << 'EOF'
import { EasyAuthClient } from '@easyauth/js-sdk';

export const authClient = new EasyAuthClient({
  apiBaseUrl: process.env.REACT_APP_API_URL || 'https://your-api.example.com',
  providers: {
    google: {
      clientId: process.env.REACT_APP_GOOGLE_CLIENT_ID,
      enabled: true,
      scopes: ['openid', 'email', 'profile']
    },
    facebook: {
      clientId: process.env.REACT_APP_FACEBOOK_CLIENT_ID,
      appSecret: process.env.REACT_APP_FACEBOOK_APP_SECRET,
      enabled: true
    }
  },
  defaultProvider: 'google',
  session: {
    storage: 'localStorage',
    expirationMinutes: 60 * 24, // 24 hours
    autoRefresh: true,
    persistAcrossTabs: true
  },
  security: {
    enableCSRF: true,
    pkceEnabled: true,
    httpsOnly: process.env.NODE_ENV === 'production'
  }
});
EOF
```

### 2. **Environment Variables Template**

```bash
# Create .env template
cat > .env.example << 'EOF'
# EasyAuth Configuration
REACT_APP_API_URL=https://your-api.example.com
REACT_APP_GOOGLE_CLIENT_ID=your-google-client-id
REACT_APP_FACEBOOK_CLIENT_ID=your-facebook-client-id
REACT_APP_FACEBOOK_APP_SECRET=your-facebook-app-secret

# For production deployments
NODE_ENV=production
EOF

cp .env.example .env
```

### 3. **React Hook Template**

```bash
# Create useAuth hook
mkdir -p src/hooks
cat > src/hooks/useAuth.js << 'EOF'
import { useState, useEffect, createContext, useContext } from 'react';
import { authClient } from '../auth/config';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [session, setSession] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      try {
        const currentUser = await authClient.getUser();
        const currentSession = await authClient.getCurrentSession();
        
        setUser(currentUser);
        setSession(currentSession);
      } catch (error) {
        console.error('Failed to initialize auth:', error);
      } finally {
        setLoading(false);
      }
    };

    initAuth();
  }, []);

  const login = async (provider = 'google') => {
    const result = await authClient.initiateLogin({
      provider,
      returnUrl: `${window.location.origin}/auth/callback`
    });

    if (result.success && result.authUrl) {
      window.location.href = result.authUrl;
    } else {
      throw new Error(result.error || 'Failed to initiate login');
    }
  };

  const logout = async () => {
    await authClient.signOut();
    setUser(null);
    setSession(null);
    window.location.href = '/login';
  };

  const value = {
    user,
    session,
    loading,
    login,
    logout,
    isAuthenticated: !!user
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
EOF
```

### 4. **Route Protection Component**

```bash
# Create protected route component
mkdir -p src/components
cat > src/components/ProtectedRoute.jsx << 'EOF'
import { useAuth } from '../hooks/useAuth';
import { Navigate } from 'react-router-dom';

export const ProtectedRoute = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return children;
};
EOF
```

### 5. **Callback Handler**

```bash
# Create auth callback page
mkdir -p src/pages/auth
cat > src/pages/auth/CallbackPage.jsx << 'EOF'
import { useEffect, useState } from 'react';
import { authClient } from '../../auth/config';

export const CallbackPage = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        const urlParams = new URLSearchParams(window.location.search);
        const code = urlParams.get('code');
        const state = urlParams.get('state');
        const provider = urlParams.get('provider') || 'google';

        if (!code || !state) {
          throw new Error('Missing authorization code or state parameter');
        }

        const result = await authClient.handleCallback({
          code,
          state,
          provider
        });

        if (result.success) {
          window.location.href = '/dashboard';
        } else {
          throw new Error(result.error || 'Authentication failed');
        }
      } catch (err) {
        setError(err.message);
        setLoading(false);
      }
    };

    handleCallback();
  }, []);

  if (loading) {
    return <div>Completing authentication...</div>;
  }

  if (error) {
    return (
      <div>
        <h2>Authentication Failed</h2>
        <p>{error}</p>
        <a href="/login">Try Again</a>
      </div>
    );
  }

  return null;
};
EOF
```

### 6. **Router Setup**

```bash
# Add routes to your App.js
cat >> src/App.js << 'EOF'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider, ProtectedRoute } from './components';
import { CallbackPage } from './pages/auth/CallbackPage';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/auth/callback" element={<CallbackPage />} />
          <Route 
            path="/dashboard" 
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } 
          />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
}
EOF
```

## OAuth Provider Setup Checklist

### Google OAuth Setup
```bash
echo "Google OAuth Setup:"
echo "1. Go to Google Cloud Console"
echo "2. Create/select project"
echo "3. Enable Google+ API"
echo "4. Create OAuth 2.0 credentials"
echo "5. Add authorized redirect URIs:"
echo "   - http://localhost:3000/auth/callback (dev)"
echo "   - https://yourdomain.com/auth/callback (prod)"
echo "6. Copy Client ID to REACT_APP_GOOGLE_CLIENT_ID"
```

### Facebook OAuth Setup
```bash
echo "Facebook OAuth Setup:"
echo "1. Go to Facebook Developers"
echo "2. Create App"
echo "3. Add Facebook Login product"
echo "4. Configure OAuth redirect URIs:"
echo "   - http://localhost:3000/auth/callback (dev)"
echo "   - https://yourdomain.com/auth/callback (prod)"
echo "5. Copy App ID and App Secret to env vars"
```

## Backend Requirements

The frontend expects these API endpoints:

- `POST /auth/initiate` - Start OAuth flow
- `POST /auth/callback` - Handle OAuth callback
- `GET /auth/user` - Get current user info
- `GET /auth/session` - Get current session
- `POST /auth/refresh` - Refresh tokens
- `POST /auth/logout` - Sign out user

## Deployment Checklist

```bash
# Production deployment checklist
echo "✅ Environment variables configured"
echo "✅ OAuth providers configured with production URLs"
echo "✅ Backend API deployed and accessible"
echo "✅ CORS configured to allow frontend domain"
echo "✅ SSL/HTTPS enabled"
echo "✅ Error monitoring set up"
```

## Common Integration Issues

### CORS Issues
```bash
# Add to backend startup.cs or equivalent
echo "app.UseCors(builder => builder"
echo "    .WithOrigins(\"http://localhost:3000\", \"https://yourdomain.com\")"
echo "    .AllowAnyMethod()"
echo "    .AllowAnyHeader()"
echo "    .AllowCredentials());"
```

### Environment Variable Loading
```bash
# For Vite projects, prefix with VITE_
echo "VITE_API_URL=https://your-api.example.com"
echo "VITE_GOOGLE_CLIENT_ID=your-google-client-id"

# Update config to use VITE_ prefix
sed -i 's/REACT_APP_/VITE_/g' src/auth/config.js
```

### Docker Deployment
```bash
# Create Dockerfile for React app
cat > Dockerfile << 'EOF'
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build
FROM nginx:alpine
COPY --from=0 /app/dist /usr/share/nginx/html
EOF
```

## Testing Integration

```bash
# Test authentication flow
npm run dev
echo "1. Navigate to /login"
echo "2. Click login with Google/Facebook"
echo "3. Complete OAuth flow"
echo "4. Verify redirect to /dashboard"
echo "5. Test logout functionality"
echo "6. Test page refresh (session persistence)"
```

## Security Checklist

```bash
echo "🔒 Security Checklist:"
echo "✅ HTTPS enabled in production"
echo "✅ OAuth redirect URIs whitelisted"
echo "✅ Environment variables secured"
echo "✅ CSRF protection enabled"
echo "✅ PKCE enabled for OAuth"
echo "✅ Session timeout configured"
echo "✅ Secure cookie settings"
```

This guide provides copy-paste ready code for rapid EasyAuth integration. Adjust URLs, provider configurations, and styling as needed for specific projects.