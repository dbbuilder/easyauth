/**
 * TDD Tests for useEasyAuth hook
 * RED phase - defining expected behavior before implementation
 */

import { renderHook, act } from '@testing-library/react';
import { ReactNode } from 'react';
import { useEasyAuth } from './useEasyAuth';
import { EasyAuthProvider } from '../context/EasyAuthProvider';
import { EasyAuthProviderConfig } from '../types';

// Import mock client from test setup
import { mockClient } from '../test-setup';

// Test wrapper component
const createWrapper = (config: EasyAuthProviderConfig) => {
  const TestWrapper = ({ children }: { children: ReactNode }) => (
    <EasyAuthProvider config={config}>
      {children}
    </EasyAuthProvider>
  );
  TestWrapper.displayName = 'TestWrapper';
  return TestWrapper;
};

describe('useEasyAuth Hook', () => {
  let mockConfig: EasyAuthProviderConfig;

  beforeEach(() => {
    jest.clearAllMocks();
    mockConfig = {
      baseUrl: 'https://test.api.com',
      enableLogging: false
    };
    
    // Setup default mock behaviors
    mockClient.getSession = jest.fn(() => ({ 
      isAuthenticated: false, 
      user: null 
    }));
    mockClient.isAuthenticated = jest.fn(() => false);
    mockClient.getUser = jest.fn(() => null);
    mockClient.getToken = jest.fn(() => null);
  });

  describe('Initial state', () => {
    it('should return initial unauthenticated state', () => {
      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.user).toBeNull();
      expect(result.current.token).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should provide session data from client', () => {
      const mockSession = {
        isAuthenticated: true,
        user: { id: 'user1', name: 'Test User', provider: 'google' as const },
        token: 'jwt-token-123'
      };

      mockClient.getSession = jest.fn(() => mockSession);
      mockClient.isAuthenticated = jest.fn(() => true);
      mockClient.getUser = jest.fn(() => mockSession.user);
      mockClient.getToken = jest.fn(() => mockSession.token);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockSession.user);
      expect(result.current.token).toBe(mockSession.token);
      expect(result.current.session).toEqual(mockSession);
    });
  });

  describe('Authentication actions', () => {
    it('should provide login function that calls client.login', async () => {
      const mockResult = { 
        success: true, 
        user: { id: 'user1', name: 'Test User', provider: 'google' as const },
        token: 'jwt-token-123'
      };
      mockClient.login = jest.fn().mockResolvedValue(mockResult);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      const loginOptions = { provider: 'google' as const };
      let authResult;

      await act(async () => {
        authResult = await result.current.login(loginOptions);
      });

      expect(mockClient.login).toHaveBeenCalledWith(loginOptions);
      expect(authResult).toEqual(mockResult);
    });

    it('should provide logout function that calls client.logout', async () => {
      mockClient.logout = jest.fn().mockResolvedValue(undefined);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      await act(async () => {
        await result.current.logout();
      });

      expect(mockClient.logout).toHaveBeenCalled();
    });

    it('should provide refresh function that calls client.refresh', async () => {
      const mockResult = { 
        success: true, 
        token: 'new-jwt-token-456'
      };
      mockClient.refresh = jest.fn().mockResolvedValue(mockResult);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      let refreshResult;

      await act(async () => {
        refreshResult = await result.current.refresh();
      });

      expect(mockClient.refresh).toHaveBeenCalled();
      expect(refreshResult).toEqual(mockResult);
    });
  });

  describe('Loading states', () => {
    it('should show loading during login operation', async () => {
      let resolveLogin: (value: any) => void;
      const loginPromise = new Promise(resolve => {
        resolveLogin = resolve;
      });
      mockClient.login = jest.fn().mockReturnValue(loginPromise);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      // Start login operation
      act(() => {
        result.current.login({ provider: 'google' });
      });

      // Should be loading
      expect(result.current.isLoading).toBe(true);

      // Complete login
      await act(async () => {
        resolveLogin!({ success: true });
      });

      // Should no longer be loading
      expect(result.current.isLoading).toBe(false);
    });

    it('should show loading during logout operation', async () => {
      let resolveLogout: () => void;
      const logoutPromise = new Promise<void>(resolve => {
        resolveLogout = resolve;
      });
      mockClient.logout = jest.fn().mockReturnValue(logoutPromise);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      // Start logout operation
      act(() => {
        result.current.logout();
      });

      // Should be loading
      expect(result.current.isLoading).toBe(true);

      // Complete logout
      await act(async () => {
        resolveLogout!();
      });

      // Should no longer be loading
      expect(result.current.isLoading).toBe(false);
    });
  });

  describe('Error handling', () => {
    it('should handle login errors', async () => {
      const mockError = new Error('Login failed');
      mockClient.login = jest.fn().mockRejectedValue(mockError);

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      await act(async () => {
        try {
          await result.current.login({ provider: 'google' });
        } catch {
          // Expected to throw
        }
      });

      expect(result.current.error).toBe('Login failed');
    });

    it('should provide clearError function', () => {
      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      // Simulate error state
      act(() => {
        // This will be implemented to set error state
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });
  });

  describe('Real-time updates', () => {
    it('should update state when auth events are received', () => {
      const mockNewSession = {
        isAuthenticated: true,
        user: { id: 'user1', name: 'Test User', provider: 'google' as const },
        token: 'jwt-token-123'
      };

      // Mock event registration
      let loginEventHandler: (data: any) => void;
      mockClient.on = jest.fn((event, handler) => {
        if (event === 'login') {
          loginEventHandler = handler;
        }
      });

      const wrapper = createWrapper(mockConfig);
      const { result } = renderHook(() => useEasyAuth(), { wrapper });

      // Simulate login event
      act(() => {
        if (loginEventHandler) {
          // Update mock client state
          mockClient.getSession = jest.fn(() => mockNewSession);
          mockClient.isAuthenticated = jest.fn(() => true);
          mockClient.getUser = jest.fn(() => mockNewSession.user);
          mockClient.getToken = jest.fn(() => mockNewSession.token);
          
          loginEventHandler({ user: mockNewSession.user, token: mockNewSession.token });
        }
      });

      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockNewSession.user);
    });
  });
});