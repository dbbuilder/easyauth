# EasyAuth React Demo

This is an interactive demonstration of the EasyAuth Framework React integration. It showcases authentication flows with multiple OAuth providers and provides real-time examples of the EasyAuth React components in action.

## 🎯 Features

- **Multi-Provider Authentication**: Google, Apple, Facebook, Azure B2C
- **Real-time Authentication State**: Live updates of login/logout status
- **Interactive Components**: Working LoginButton and LogoutButton components
- **User Profile Display**: Shows authenticated user information
- **Code Examples**: Live code snippets for developers
- **Responsive Design**: Works on desktop and mobile
- **Mock Authentication Server**: Built-in server for testing without real OAuth setup

## 🚀 Quick Start

### Prerequisites

- Node.js 16+ installed
- npm or yarn package manager

### Running the Demo

#### Option 1: Full Demo with Mock Server (Recommended)

```bash
# Install dependencies
npm install

# Start both the mock authentication server and React demo
npm run dev:full
```

This will start:
- **Mock Server**: http://localhost:3001 (handles authentication)
- **React Demo**: http://localhost:5177 (the demo app)

#### Option 2: Demo Only

```bash
# Start just the React demo (you'll need your own auth server)
npm run dev
```

#### Option 3: Mock Server Only

```bash
# Start just the mock authentication server
npm run mock-server
```

## 🎮 Using the Demo

1. **Open the Demo**: Navigate to http://localhost:5177
2. **Try Authentication**: Click any of the provider login buttons
3. **See Mock Flow**: The mock server will simulate OAuth and return mock user data
4. **Explore Features**: View user profile, logout, and see real-time state changes

### Mock Authentication Data

The demo uses realistic mock data for each provider:

- **Google**: john.doe@gmail.com (John Doe)
- **Apple**: jane.smith@icloud.com (Jane Smith)  
- **Facebook**: mike.wilson@facebook.com (Mike Wilson)
- **Azure B2C**: sarah.johnson@company.com (Sarah Johnson)

## 🏗️ Architecture

```
┌─────────────────┐    HTTP     ┌──────────────────┐
│  React Demo     │◄───────────►│  Mock Auth       │
│  (Port 5177)    │   Requests  │  Server          │
│                 │             │  (Port 3001)     │
├─────────────────┤             ├──────────────────┤
│ @easyauth/react │             │ Express.js       │
│ - useEasyAuth   │             │ - OAuth Simulation│
│ - LoginButton   │             │ - User Profiles  │
│ - LogoutButton  │             │ - JWT Tokens     │
│ - EasyAuthProvider│           │ - CORS Support   │
└─────────────────┘             └──────────────────┘
```

## 📝 Available Scripts

- `npm run dev` - Start React development server
- `npm run build` - Build for production  
- `npm run preview` - Preview production build
- `npm run test` - Run test suite
- `npm run test:ui` - Run tests with UI
- `npm run mock-server` - Start mock authentication server
- `npm run dev:full` - Start both servers concurrently
- `npm run lint` - Lint code

## 🧪 Testing

Run the comprehensive test suite:

```bash
npm test
```

Tests cover:
- Component rendering
- Authentication state management
- User interface interactions
- Integration with EasyAuth provider
- Error handling scenarios

## 🔧 Development

### Local Package Dependencies

The demo uses local file references to the EasyAuth packages:

- `@easyauth/react` → `file:../react`
- `@easyauth/sdk` → `file:../sdk` (indirect)

This allows testing the latest development versions without publishing.

### Mock Server Endpoints

The mock server provides these endpoints:

- `GET /health` - Health check
- `POST /auth/:provider/login` - Initiate OAuth flow
- `GET /auth/:provider/callback` - OAuth callback handler
- `GET /auth/me` - Get user profile
- `POST /auth/logout` - Logout user
- `POST /auth/refresh` - Refresh token

### Configuration

Update the demo configuration in `src/App.tsx`:

```typescript
const config = {
  baseUrl: 'http://localhost:3001', // Mock server URL
  enableLogging: true               // Enable debug logging
};
```

## 🎨 Styling

The demo uses modern CSS with:
- CSS Grid and Flexbox layouts
- Gradient backgrounds and glass morphism effects
- Smooth animations and hover effects
- Responsive breakpoints for mobile devices
- Custom CSS properties for easy theming

## 📱 Mobile Support

The demo is fully responsive and works on:
- Desktop browsers (Chrome, Firefox, Safari, Edge)
- Mobile devices (iOS Safari, Chrome Mobile)
- Tablet devices (iPad, Android tablets)

## 🔒 Security Notes

⚠️ **This is a demo application with a mock authentication server. Do not use in production.**

The mock server:
- Uses fake JWT tokens for demonstration
- Does not implement real OAuth security
- Stores no persistent data
- Is intended for development and testing only

For production use, replace the mock server with:
- Real OAuth provider integrations
- Secure token handling
- Proper session management
- HTTPS endpoints

## 🐛 Troubleshooting

### Port Already in Use
If ports 3001 or 5177 are in use, the servers will automatically try alternative ports.

### CORS Issues
The mock server is configured to accept requests from common Vite dev server ports. If you see CORS errors, check the CORS configuration in `mock-server.cjs`.

### TypeScript Errors
Ensure you have the latest TypeScript definitions:
```bash
npm install --save-dev @types/node
```

### React Version Conflicts
The demo uses React 18.2.0. If you encounter version conflicts, check that all packages are using compatible React versions.

## 📚 Documentation

For more information about the EasyAuth Framework:

- [EasyAuth SDK Documentation](../sdk/README.md)
- [EasyAuth React Package Documentation](../react/README.md)
- [Main Project Documentation](../../README.md)

## 🤝 Contributing

This demo is part of the larger EasyAuth Framework project. See the main project README for contribution guidelines.

## 📄 License

MIT License - see LICENSE file in the project root.