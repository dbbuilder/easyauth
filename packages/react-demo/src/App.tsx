/**
 * Main App component for EasyAuth React Demo  
 * TDD GREEN phase - implementing to make tests pass
 */

import { EasyAuthProvider } from '@easyauth/react';
import { AuthDemo } from './components/AuthDemo';
import './App.css'

const config = {
  baseUrl: 'http://localhost:5200',
  enableLogging: true
};

function App() {
  return (
    <EasyAuthProvider config={config}>
      <div className="app">
        <nav role="navigation">
          <div className="nav-brand">
            <h1>EasyAuth Demo</h1>
          </div>
          <div className="nav-links">
            <span>Interactive Demo</span>
            <span>Documentation</span>
          </div>
        </nav>

        <main className="main-content">
          <AuthDemo />
        </main>

        <footer>
          <p>&copy; 2024 EasyAuth Framework. Built with React & TypeScript.</p>
        </footer>
      </div>
    </EasyAuthProvider>
  );
}

export default App
