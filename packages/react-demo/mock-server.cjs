#!/usr/bin/env node

/**
 * Mock Authentication Server for EasyAuth Demo
 * Simulates OAuth provider responses for testing purposes
 */

const express = require('express');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = 5200;

// Enable CORS for all routes
app.use(cors({
  origin: ['http://localhost:5173', 'http://localhost:5174', 'http://localhost:5175', 'http://localhost:5176', 'http://localhost:5177', 'http://localhost:5178', 'http://localhost:5179', 'http://localhost:5180'],
  credentials: true
}));

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Mock user database
const mockUsers = {
  google: {
    id: 'google_123456',
    email: 'john.doe@gmail.com',
    name: 'John Doe',
    profilePicture: 'https://avatar.iran.liara.run/public/boy?username=john',
    provider: 'google'
  },
  apple: {
    id: 'apple_789012',
    email: 'jane.smith@icloud.com',
    name: 'Jane Smith',
    profilePicture: 'https://avatar.iran.liara.run/public/girl?username=jane',
    provider: 'apple'
  },
  facebook: {
    id: 'fb_345678',
    email: 'mike.wilson@facebook.com',
    name: 'Mike Wilson',
    profilePicture: 'https://avatar.iran.liara.run/public/boy?username=mike',
    provider: 'facebook'
  },
  'azure-b2c': {
    id: 'azure_901234',
    email: 'sarah.johnson@company.com',
    name: 'Sarah Johnson',
    profilePicture: 'https://avatar.iran.liara.run/public/girl?username=sarah',
    provider: 'azure-b2c'
  }
};

// Mock JWT token generation
function generateMockToken(user) {
  const payload = {
    sub: user.id,
    email: user.email,
    name: user.name,
    provider: user.provider,
    iat: Math.floor(Date.now() / 1000),
    exp: Math.floor(Date.now() / 1000) + (60 * 60) // 1 hour
  };
  
  // In a real implementation, this would be a proper JWT
  return `mock_token_${Buffer.from(JSON.stringify(payload)).toString('base64')}`;
}

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'OK', 
    message: 'EasyAuth Mock Server is running',
    timestamp: new Date().toISOString()
  });
});

// OAuth initiation endpoint
app.post('/auth/:provider/login', (req, res) => {
  const { provider } = req.params;
  const { returnUrl } = req.body;
  
  console.log(`🚀 Login request for provider: ${provider}`);
  
  if (!mockUsers[provider]) {
    return res.status(400).json({
      error: 'unsupported_provider',
      message: `Provider '${provider}' is not supported`
    });
  }
  
  // Simulate OAuth redirect URL (in real implementation this would redirect to OAuth provider)
  const redirectUrl = `http://localhost:${PORT}/auth/${provider}/callback?code=mock_auth_code_${provider}&state=${Date.now()}`;
  
  res.json({
    success: true,
    redirectUrl,
    message: `Redirecting to ${provider} OAuth...`
  });
});

// OAuth callback endpoint (simulates provider callback)
app.get('/auth/:provider/callback', (req, res) => {
  const { provider } = req.params;
  const { code, state } = req.query;
  
  console.log(`✅ OAuth callback for provider: ${provider}, code: ${code}`);
  
  if (!mockUsers[provider]) {
    return res.status(400).send('Invalid provider');
  }
  
  const user = mockUsers[provider];
  const token = generateMockToken(user);
  
  // In a real implementation, this would redirect back to the client app
  // For demo purposes, we'll show a success page with post message
  const html = `
    <!DOCTYPE html>
    <html>
    <head>
      <title>Authentication Success</title>
      <style>
        body { 
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
          max-width: 500px; 
          margin: 100px auto; 
          padding: 20px;
          text-align: center;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          min-height: 100vh;
          color: white;
        }
        .card {
          background: rgba(255,255,255,0.1);
          backdrop-filter: blur(10px);
          border-radius: 16px;
          padding: 2rem;
          border: 1px solid rgba(255,255,255,0.2);
        }
        .avatar {
          width: 80px;
          height: 80px;
          border-radius: 50%;
          margin: 0 auto 1rem;
          border: 3px solid rgba(255,255,255,0.3);
        }
        h1 { color: #4ade80; margin-bottom: 0.5rem; }
        p { margin: 0.5rem 0; opacity: 0.9; }
        .provider { 
          background: rgba(255,255,255,0.1);
          padding: 0.5rem 1rem;
          border-radius: 20px;
          display: inline-block;
          margin: 1rem 0;
          font-weight: 600;
        }
        .close-btn {
          background: #4ade80;
          color: white;
          border: none;
          padding: 0.75rem 2rem;
          border-radius: 8px;
          cursor: pointer;
          font-weight: 600;
          margin-top: 1rem;
        }
      </style>
    </head>
    <body>
      <div class="card">
        <img src="${user.profilePicture}" alt="Profile" class="avatar" />
        <h1>✅ Authentication Successful!</h1>
        <p><strong>${user.name}</strong></p>
        <p>${user.email}</p>
        <div class="provider">${provider.toUpperCase()} Account</div>
        <p style="font-size: 0.9em; opacity: 0.7;">This window will close automatically in 3 seconds.</p>
        <button class="close-btn" onclick="closeWindow()">Close Window</button>
      </div>
      
      <script>
        // Post authentication result to parent window
        const authResult = {
          success: true,
          user: ${JSON.stringify(user)},
          token: "${token}",
          provider: "${provider}"
        };
        
        console.log('🔄 Attempting to post message to parent window', authResult);
        
        // Try to post to parent window (for popup flow)
        try {
          if (window.opener) {
            console.log('📤 Posting message to window.opener');
            window.opener.postMessage({ type: 'EASYAUTH_LOGIN_SUCCESS', data: authResult }, '*');
          }
          if (window.parent && window.parent !== window) {
            console.log('📤 Posting message to window.parent');
            window.parent.postMessage({ type: 'EASYAUTH_LOGIN_SUCCESS', data: authResult }, '*');
          }
        } catch (e) {
          console.error('❌ Could not post message to parent:', e);
        }
        
        function closeWindow() {
          if (window.opener) {
            window.close();
          } else {
            window.history.back();
          }
        }
        
        // Auto-close after 3 seconds
        setTimeout(closeWindow, 3000);
      </script>
    </body>
    </html>
  `;
  
  res.send(html);
});

// User profile endpoint
app.get('/auth/me', (req, res) => {
  const authHeader = req.headers.authorization;
  
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({
      error: 'unauthorized',
      message: 'Missing or invalid authorization header'
    });
  }
  
  const token = authHeader.substring(7);
  
  if (!token.startsWith('mock_token_')) {
    return res.status(401).json({
      error: 'invalid_token',
      message: 'Invalid token format'
    });
  }
  
  try {
    const payloadBase64 = token.replace('mock_token_', '');
    const payload = JSON.parse(Buffer.from(payloadBase64, 'base64').toString());
    
    // Check if token is expired
    if (payload.exp < Math.floor(Date.now() / 1000)) {
      return res.status(401).json({
        error: 'token_expired',
        message: 'Token has expired'
      });
    }
    
    const user = mockUsers[payload.provider];
    if (!user) {
      return res.status(404).json({
        error: 'user_not_found',
        message: 'User not found'
      });
    }
    
    console.log(`👤 Profile request for user: ${user.name} (${user.provider})`);
    
    res.json({
      success: true,
      user: user
    });
    
  } catch (error) {
    res.status(401).json({
      error: 'invalid_token',
      message: 'Could not parse token'
    });
  }
});

// Logout endpoint
app.post('/auth/logout', (req, res) => {
  const authHeader = req.headers.authorization;
  console.log('🚪 Logout request');
  
  // In a real implementation, you'd invalidate the token
  res.json({
    success: true,
    message: 'Logged out successfully'
  });
});

// Token refresh endpoint
app.post('/auth/refresh', (req, res) => {
  const { refresh_token } = req.body;
  console.log('🔄 Token refresh request');
  
  // For demo purposes, just return a new token
  res.json({
    success: true,
    token: `mock_token_${Buffer.from(JSON.stringify({
      sub: 'refreshed_user',
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + (60 * 60)
    })).toString('base64')}`,
    expires_in: 3600
  });
});

// Static file serving for any additional assets
app.use('/static', express.static(path.join(__dirname, 'public')));

// 404 handler
app.use((req, res) => {
  res.status(404).json({
    error: 'not_found',
    message: `Endpoint ${req.method} ${req.path} not found`,
    available_endpoints: [
      'GET /health',
      'POST /auth/:provider/login',
      'GET /auth/:provider/callback',
      'GET /auth/me',
      'POST /auth/logout',
      'POST /auth/refresh'
    ]
  });
});

// Error handler
app.use((error, req, res, next) => {
  console.error('❌ Server error:', error);
  res.status(500).json({
    error: 'server_error',
    message: 'Internal server error',
    details: process.env.NODE_ENV === 'development' ? error.message : undefined
  });
});

// Start server
app.listen(PORT, () => {
  console.log('🎯 EasyAuth Mock Server started!');
  console.log(`📍 Server running at: http://localhost:${PORT}`);
  console.log(`🏥 Health check: http://localhost:${PORT}/health`);
  console.log('📱 Available providers: google, apple, facebook, azure-b2c');
  console.log('');
  console.log('🚀 Ready to serve authentication requests!');
});

// Handle process termination
process.on('SIGTERM', () => {
  console.log('👋 Mock server shutting down...');
  process.exit(0);
});

process.on('SIGINT', () => {
  console.log('👋 Mock server shutting down...');
  process.exit(0);
});