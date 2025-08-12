/**
 * AuthDemo component - demonstrates EasyAuth React integration
 * TDD GREEN phase - implementing to make tests pass
 */

import { useEasyAuth, LoginButton, LogoutButton } from '@easyauth/react';

export function AuthDemo() {
  const { isAuthenticated, user, isLoading, error } = useEasyAuth();

  return (
    <div className="auth-demo">
      <header>
        <h1>EasyAuth React Demo</h1>
        <p>Interactive demonstration of EasyAuth Framework integration</p>
      </header>

      <main>
        <section className="auth-state">
          <h2>Authentication State</h2>
          <div className="status">
            {isLoading && <p>Loading...</p>}
            {error && <p className="error">Error: {error}</p>}
            {isAuthenticated ? (
              <div>
                <p>✅ Authenticated</p>
                <LogoutButton />
              </div>
            ) : (
              <p>❌ Not authenticated</p>
            )}
          </div>
        </section>

        {isAuthenticated && user && (
          <section className="user-profile">
            <h2>User Profile</h2>
            <div>
              <p><strong>Name:</strong> {user.name}</p>
              <p><strong>Email:</strong> {user.email}</p>
              <p><strong>Provider:</strong> {user.provider}</p>
            </div>
          </section>
        )}

        {!isAuthenticated && (
          <section className="login-options">
            <h2>Sign In Options</h2>
            <div className="login-buttons">
              <LoginButton provider="google" />
              <LoginButton provider="apple" />
              <LoginButton provider="facebook" />
              <LoginButton provider="azure-b2c">Sign in with Azure</LoginButton>
            </div>
          </section>
        )}

        <section className="api-config">
          <h2>API Configuration</h2>
          <div className="config-info">
            <p>Base URL: http://localhost:5200</p>
            <p>Logging: Enabled</p>
          </div>
        </section>

        <section className="example-code">
          <h2>Example Code</h2>
          <pre>
            <code>{`
import { EasyAuthProvider, useEasyAuth, LoginButton } from '@easyauth/react';

function App() {
  return (
    <EasyAuthProvider config={{ baseUrl: 'https://your-api.com' }}>
      <MyComponent />
    </EasyAuthProvider>
  );
}

function MyComponent() {
  const { isAuthenticated, user } = useEasyAuth();
  
  return (
    <div>
      {isAuthenticated ? (
        <p>Welcome {user?.displayName}</p>
      ) : (
        <LoginButton provider="google" />
      )}
    </div>
  );
}
            `}</code>
          </pre>
        </section>
      </main>
    </div>
  );
}