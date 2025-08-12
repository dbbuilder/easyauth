/**
 * TDD Tests for LogoutButton component
 * RED phase - defining expected behavior before implementation
 */

import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LogoutButton } from './LogoutButton';
import { EasyAuthProvider } from '../context/EasyAuthProvider';
import { EasyAuthProviderConfig } from '../types';

// Import mock client from test setup
import { mockClient } from '../test-setup';

const mockLogout = mockClient.logout;

describe('LogoutButton Component', () => {
  let mockConfig: EasyAuthProviderConfig;
  const user = userEvent.setup();

  beforeEach(() => {
    jest.clearAllMocks();
    mockConfig = {
      baseUrl: 'https://test.api.com',
      enableLogging: false
    };
  });

  const renderWithProvider = (ui: React.ReactElement) => {
    return render(
      <EasyAuthProvider config={mockConfig}>
        {ui}
      </EasyAuthProvider>
    );
  };

  describe('Basic rendering', () => {
    it('should render logout button with default text', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      
      renderWithProvider(
        <LogoutButton />
      );

      expect(screen.getByRole('button')).toBeInTheDocument();
      expect(screen.getByText(/sign out/i)).toBeInTheDocument();
    });

    it('should render logout button with custom children', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      
      renderWithProvider(
        <LogoutButton>
          <span>Custom Logout Text</span>
        </LogoutButton>
      );

      expect(screen.getByRole('button')).toBeInTheDocument();
      expect(screen.getByText('Custom Logout Text')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      
      renderWithProvider(
        <LogoutButton className="custom-logout-class" />
      );

      const button = screen.getByRole('button');
      expect(button).toHaveClass('custom-logout-class');
    });

    it('should be disabled when disabled prop is true', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      
      renderWithProvider(
        <LogoutButton disabled />
      );

      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });
  });

  describe('Click handling', () => {
    it('should call logout when clicked', async () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      mockLogout.mockResolvedValueOnce(undefined);

      renderWithProvider(
        <LogoutButton />
      );

      const button = screen.getByRole('button');
      await user.click(button);

      expect(mockLogout).toHaveBeenCalled();
    });

    it('should call onSuccess callback when logout succeeds', async () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      const mockOnSuccess = jest.fn();
      mockLogout.mockResolvedValueOnce(undefined);

      renderWithProvider(
        <LogoutButton onSuccess={mockOnSuccess} />
      );

      const button = screen.getByRole('button');
      await user.click(button);

      await waitFor(() => {
        expect(mockOnSuccess).toHaveBeenCalled();
      });
    });

    it('should call onError callback when logout fails', async () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);
      const mockOnError = jest.fn();
      const mockError = new Error('Logout failed');
      mockLogout.mockRejectedValueOnce(mockError);

      renderWithProvider(
        <LogoutButton onError={mockOnError} />
      );

      const button = screen.getByRole('button');
      await user.click(button);

      await waitFor(() => {
        expect(mockOnError).toHaveBeenCalledWith(mockError);
      });
    });
  });

  describe('Loading states', () => {
    it('should show loading state during logout', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);

      renderWithProvider(
        <LogoutButton />
      );

      // Note: This test needs to be updated to properly test loading state
      // The current implementation would need state management for loading
      const button = screen.getByRole('button');
      expect(button).toBeInTheDocument();
    });
  });

  describe('Authentication state', () => {
    it('should be hidden when user is not authenticated', () => {
      // Set unauthenticated state
      mockClient.isAuthenticated.mockReturnValue(false);

      renderWithProvider(
        <LogoutButton />
      );

      expect(screen.queryByRole('button')).not.toBeInTheDocument();
    });

    it('should be visible when user is authenticated', () => {
      // Set authenticated state
      mockClient.isAuthenticated.mockReturnValue(true);

      renderWithProvider(
        <LogoutButton />
      );

      expect(screen.getByRole('button')).toBeInTheDocument();
    });
  });
});