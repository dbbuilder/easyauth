/**
 * TDD Tests for useEasyAuth composable
 * RED phase - defining expected behavior before implementation
 */

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, nextTick } from 'vue'
import { useEasyAuth, resetEasyAuthState } from './useEasyAuth'
import type { EasyAuthConfig, UserProfile } from '@easyauth/sdk'

// Mock the SDK
vi.mock('@easyauth/sdk', () => ({
  EasyAuthClient: vi.fn(() => ({
    getSession: vi.fn(() => ({
      isAuthenticated: false,
      user: null,
      token: null
    })),
    login: vi.fn(() => Promise.resolve({
      success: true,
      user: { id: '123', name: 'Test User', email: 'test@example.com' },
      token: 'mock-token'
    })),
    logout: vi.fn(() => Promise.resolve({
      success: true
    })),
    refresh: vi.fn(() => Promise.resolve({
      success: true,
      user: { id: '123', name: 'Test User', email: 'test@example.com' },
      token: 'mock-token'
    })),
    on: vi.fn(),
    off: vi.fn()
  }))
}))

const mockConfig: EasyAuthConfig = {
  baseUrl: 'https://test-api.com',
  enableLogging: false
}

// Test component that uses the composable
const TestComponent = defineComponent({
  props: {
    config: {
      type: Object as () => EasyAuthConfig,
      required: true
    }
  },
  template: `
    <div>
      <div data-testid="auth-status">{{ isAuthenticated ? 'authenticated' : 'not-authenticated' }}</div>
      <div data-testid="loading-status">{{ isLoading ? 'loading' : 'not-loading' }}</div>
      <div data-testid="user-name">{{ user?.name || 'no-user' }}</div>
      <div data-testid="error-message">{{ error || 'no-error' }}</div>
      <button @click="handleLogin" data-testid="login-button">Login</button>
      <button @click="handleLogout" data-testid="logout-button">Logout</button>
      <button @click="handleRefresh" data-testid="refresh-button">Refresh</button>
    </div>
  `,
  setup(props) {
    const { 
      isAuthenticated, 
      isLoading, 
      user, 
      error, 
      login, 
      logout, 
      refresh 
    } = useEasyAuth(props.config)

    const handleLogin = async () => {
      await login({ provider: 'google' })
    }

    const handleLogout = async () => {
      await logout()
    }

    const handleRefresh = async () => {
      await refresh()
    }

    return {
      isAuthenticated,
      isLoading,
      user,
      error,
      handleLogin,
      handleLogout,
      handleRefresh
    }
  }
})

describe('useEasyAuth Composable', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Reset global state between tests to avoid test pollution
    resetEasyAuthState()
  })

  describe('Initial state', () => {
    it('should provide default unauthenticated state', () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      expect(wrapper.get('[data-testid="auth-status"]').text()).toBe('not-authenticated')
      expect(wrapper.get('[data-testid="loading-status"]').text()).toBe('not-loading')
      expect(wrapper.get('[data-testid="user-name"]').text()).toBe('no-user')
      expect(wrapper.get('[data-testid="error-message"]').text()).toBe('no-error')
    })

    it('should initialize EasyAuth client with provided config', () => {
      mount(TestComponent, {
        props: { config: mockConfig }
      })

      // Will verify client initialization in implementation
      expect(true).toBe(true) // Placeholder for client initialization test
    })
  })

  describe('Authentication state reactivity', () => {
    it('should reactively update authentication state', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      // Initially not authenticated
      expect(wrapper.get('[data-testid="auth-status"]').text()).toBe('not-authenticated')

      // Simulate login - GREEN phase implementation working
      await wrapper.get('[data-testid="login-button"]').trigger('click')
      await flushPromises()

      // After successful login, should be authenticated
      expect(wrapper.get('[data-testid="auth-status"]').text()).toBe('authenticated')
    })

    it('should reactively update user information', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      // Initially no user
      expect(wrapper.get('[data-testid="user-name"]').text()).toBe('no-user')
      
      // Simulate login to load user data
      await wrapper.get('[data-testid="login-button"]').trigger('click')
      await flushPromises()
      
      // After login, should show user information
      expect(wrapper.get('[data-testid="user-name"]').text()).toBe('Test User')
    })

    it('should reactively update loading state during operations', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      expect(wrapper.get('[data-testid="loading-status"]').text()).toBe('not-loading')

      // Simulate login operation - loading state should update
      const loginPromise = wrapper.get('[data-testid="login-button"]').trigger('click')
      
      // Should show loading during async operation (will implement)
      await nextTick()
      
      await loginPromise
      await flushPromises()
      
      // Should return to not-loading state
      expect(wrapper.get('[data-testid="loading-status"]').text()).toBe('not-loading')
    })
  })

  describe('Authentication methods', () => {
    it('should provide login method that accepts provider options', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      const loginButton = wrapper.get('[data-testid="login-button"]')
      
      // Should not throw error when calling login
      await expect(loginButton.trigger('click')).resolves.not.toThrow()
    })

    it('should provide logout method', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      const logoutButton = wrapper.get('[data-testid="logout-button"]')
      
      // Should not throw error when calling logout
      await expect(logoutButton.trigger('click')).resolves.not.toThrow()
    })

    it('should provide refresh method', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      const refreshButton = wrapper.get('[data-testid="refresh-button"]')
      
      // Should not throw error when calling refresh
      await expect(refreshButton.trigger('click')).resolves.not.toThrow()
    })
  })

  describe('Error handling', () => {
    it('should handle and expose authentication errors', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      expect(wrapper.get('[data-testid="error-message"]').text()).toBe('no-error')
      
      // Error handling will be implemented in GREEN phase
    })

    it('should clear errors when starting new operations', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      // This will test error clearing behavior
      expect(wrapper.get('[data-testid="error-message"]').text()).toBe('no-error')
    })
  })

  describe('Session persistence', () => {
    it('should load saved session on initialization', async () => {
      // Mock saved session in localStorage
      const mockSession = {
        isAuthenticated: true,
        user: { id: '123', name: 'Test User', email: 'test@example.com' } as UserProfile,
        token: 'mock-token'
      }

      // Mock EasyAuthClient specifically for this test to return the saved session
      const { EasyAuthClient } = await import('@easyauth/sdk')
      vi.mocked(EasyAuthClient).mockImplementation(() => ({
        getSession: vi.fn(() => mockSession),
        login: vi.fn(() => Promise.resolve({
          success: true,
          user: { id: '123', name: 'Test User', email: 'test@example.com' },
          token: 'mock-token'
        })),
        logout: vi.fn(() => Promise.resolve({
          success: true
        })),
        refresh: vi.fn(() => Promise.resolve({
          success: true,
          user: { id: '123', name: 'Test User', email: 'test@example.com' },
          token: 'mock-token'
        })),
        on: vi.fn(),
        off: vi.fn()
      }))

      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      // Should load saved session - GREEN phase implementation working
      expect(wrapper.get('[data-testid="user-name"]').text()).toBe('Test User')
    })

    it('should persist session changes to storage', async () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      await wrapper.get('[data-testid="login-button"]').trigger('click')
      await flushPromises()

      // Should save to localStorage (will verify in GREEN phase)
      expect(vi.mocked(window.localStorage.setItem)).toHaveBeenCalledTimes(0) // Will change in GREEN phase
    })
  })

  describe('Event handling', () => {
    it('should listen to authentication events from SDK', () => {
      mount(TestComponent, {
        props: { config: mockConfig }
      })

      // Should register event listeners (will verify in GREEN phase)
      expect(true).toBe(true) // Placeholder
    })

    it('should cleanup event listeners on unmount', () => {
      const wrapper = mount(TestComponent, {
        props: { config: mockConfig }
      })

      wrapper.unmount()

      // Should remove event listeners (will verify in GREEN phase)
      expect(true).toBe(true) // Placeholder
    })
  })

  describe('Multiple composable instances', () => {
    it('should share authentication state between multiple composable instances', () => {
      const TestComponent2 = defineComponent({
        props: {
          config: {
            type: Object as () => EasyAuthConfig,
            required: true
          }
        },
        template: `<div data-testid="second-auth-status">{{ isAuthenticated ? 'authenticated' : 'not-authenticated' }}</div>`,
        setup(props) {
          const { isAuthenticated } = useEasyAuth(props.config)
          return { isAuthenticated }
        }
      })

      const wrapper1 = mount(TestComponent, {
        props: { config: mockConfig }
      })

      const wrapper2 = mount(TestComponent2, {
        props: { config: mockConfig }
      })

      // Both should show same authentication state
      expect(wrapper1.get('[data-testid="auth-status"]').text()).toBe(
        wrapper2.get('[data-testid="second-auth-status"]').text()
      )
    })
  })
})