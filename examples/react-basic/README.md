# EasyAuth React Integration Example

This is a comprehensive React example demonstrating how to integrate the EasyAuth JavaScript SDK with a React application using TypeScript, React Router, and Tailwind CSS.

## 🚀 Features

- **Complete Authentication Flow**: Login, logout, dashboard, and callback handling
- **Multiple OAuth Providers**: Google and Facebook authentication
- **Modern React Patterns**: React Router v6, TypeScript, and React Context
- **Responsive UI**: Tailwind CSS for modern styling
- **Security Best Practices**: CSRF protection, PKCE, secure session management

## 📦 Quick Start

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Environment Variables

Copy the example environment file and configure your OAuth providers:

```bash
cp .env.example .env
```

Edit `.env` with your actual configuration:

```env
REACT_APP_API_URL=https://your-api.example.com
REACT_APP_GOOGLE_CLIENT_ID=your-google-client-id
REACT_APP_FACEBOOK_CLIENT_ID=your-facebook-client-id
REACT_APP_FACEBOOK_APP_SECRET=your-facebook-app-secret
```

### 3. Start Development Server

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the application.

## 🏗️ Project Structure

```
src/
├── App.tsx           # Main application component with routing
├── main.tsx         # React DOM entry point
└── index.css        # Tailwind CSS imports and global styles
```

## 🔧 Key Components

### AuthProvider Context

Provides authentication state and methods throughout the application:

- `user`: Current user information
- `session`: Current session details
- `loading`: Authentication loading state
- `login(provider)`: Initiate login flow
- `logout()`: Sign out user

### Routes

- `/login` - Authentication page with provider selection
- `/callback` - OAuth callback handler
- `/dashboard` - Protected user dashboard
- `/` - Redirects to dashboard

### Components

1. **LoginPage**: Provider selection and authentication initiation
2. **Dashboard**: Protected route displaying user and session information
3. **CallbackPage**: Handles OAuth provider callbacks and redirects

## 🔒 Security Features

- **CSRF Protection**: Automatic state parameter generation and validation
- **PKCE Support**: Proof Key for Code Exchange for enhanced security
- **Secure Storage**: localStorage with configurable options
- **URL Validation**: Automatic validation of redirect URLs
- **Error Handling**: Comprehensive error handling and user feedback

## 🧪 Testing

```bash
# Type checking
npm run type-check

# Linting
npm run lint

# Build for production
npm run build

# Preview production build
npm run preview
```

## 🚀 Production Deployment

1. **Build the application**:
   ```bash
   npm run build
   ```

2. **Configure production environment variables**
3. **Deploy the `dist` folder** to your preferred hosting platform
4. **Configure OAuth redirect URIs** in your provider settings

## 🛠️ Customization

### Styling

This example uses Tailwind CSS. You can customize the design by:

1. Modifying Tailwind classes in components
2. Adding custom CSS in `index.css`
3. Configuring Tailwind in `tailwind.config.js` (if needed)

### Authentication Providers

Add more providers by updating the EasyAuthClient configuration in `App.tsx`:

```typescript
const authClient = new EasyAuthClient({
  apiBaseUrl: process.env.REACT_APP_API_URL,
  providers: {
    google: { /* config */ },
    facebook: { /* config */ },
    apple: { /* config */ },    // Add Apple
    'azure-b2c': { /* config */ }  // Add Azure B2C
  }
});
```

### Routes and Navigation

Add new routes by updating the Routes component in `App.tsx`:

```tsx
<Routes>
  <Route path="/login" element={<LoginPage />} />
  <Route path="/callback" element={<CallbackPage />} />
  <Route path="/dashboard" element={<Dashboard />} />
  <Route path="/profile" element={<ProfilePage />} />  {/* New route */}
  <Route path="/" element={<Navigate to="/dashboard" replace />} />
</Routes>
```

## 📚 Learn More

- [EasyAuth Documentation](https://github.com/dbbuilder/easyauth#readme)
- [React Documentation](https://reactjs.org/)
- [React Router Documentation](https://reactrouter.com/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)

## 🐛 Troubleshooting

### Common Issues

1. **OAuth Configuration**: Ensure your OAuth client IDs and redirect URIs are correctly configured
2. **CORS Issues**: Configure your API server to allow requests from your React app's origin
3. **Environment Variables**: Make sure all required environment variables are set

### Debug Mode

Enable debug mode by setting `NODE_ENV=development` to see detailed error messages and logs.

## 🤝 Contributing

This example is part of the EasyAuth project. See the main project's contributing guidelines for how to contribute improvements.