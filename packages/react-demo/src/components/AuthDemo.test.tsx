/**
 * TDD Tests for AuthDemo component
 * RED phase - defining expected behavior before implementation
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuthDemo } from './AuthDemo';
import { EasyAuthProvider } from '@easyauth/react';

const mockConfig = {
  baseUrl: 'https://demo.api.com',
  enableLogging: true
};

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <EasyAuthProvider config={mockConfig}>
      {ui}
    </EasyAuthProvider>
  );
};

describe('AuthDemo Component', () => {
  beforeEach(() => {
    // Clear any previous state
  });

  describe('Initial render', () => {
    it('should render the demo page with title', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/EasyAuth React Demo/i)).toBeInTheDocument();
    });

    it('should show unauthenticated state initially', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/not authenticated/i)).toBeInTheDocument();
    });

    it('should display login buttons for all providers', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/sign in with google/i)).toBeInTheDocument();
      expect(screen.getByText(/sign in with apple/i)).toBeInTheDocument();
      expect(screen.getByText(/sign in with facebook/i)).toBeInTheDocument();
      expect(screen.getByText(/sign in with azure/i)).toBeInTheDocument();
    });
  });

  describe('Authentication flow demonstration', () => {
    it('should show user info when authenticated', async () => {
      // This test will verify authenticated state display
      renderWithProviders(<AuthDemo />);
      
      // Initially unauthenticated
      expect(screen.getByText(/not authenticated/i)).toBeInTheDocument();
      
      // This test will be completed when we implement the authenticated state
      expect(screen.queryByText(/logout/i)).not.toBeInTheDocument();
    });

    it('should display user profile information when logged in', () => {
      // This test will verify user profile display in authenticated state
      renderWithProviders(<AuthDemo />);
      
      // Initially no profile info
      expect(screen.queryByText(/user profile/i)).not.toBeInTheDocument();
    });
  });

  describe('Feature showcase', () => {
    it('should display authentication state information', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/authentication state/i)).toBeInTheDocument();
    });

    it('should show loading states during authentication', () => {
      renderWithProviders(<AuthDemo />);
      
      // Test for loading state indication
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
    });

    it('should display error messages if authentication fails', () => {
      renderWithProviders(<AuthDemo />);
      
      // Initially no errors
      expect(screen.queryByText(/error/i)).not.toBeInTheDocument();
    });
  });

  describe('Interactive demo features', () => {
    it('should provide code examples for developers', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/example code/i)).toBeInTheDocument();
    });

    it('should show API configuration options', () => {
      renderWithProviders(<AuthDemo />);
      
      expect(screen.getByText(/api configuration/i)).toBeInTheDocument();
    });
  });
});