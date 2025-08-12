/**
 * TDD Tests for LoginButton component
 * RED phase - defining expected behavior before implementation
 */

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import LoginButton from './LoginButton.vue'
import type { EasyAuthConfig } from '@easyauth/sdk'

// Mock the useEasyAuth composable
import { ref } from 'vue'
vi.mock('../composables/useEasyAuth', () => ({
  useEasyAuth: vi.fn(() => ({
    isAuthenticated: ref(false),
    isLoading: ref(false),
    user: ref(null),
    error: ref(null),
    login: vi.fn(),
    logout: vi.fn(),
    refresh: vi.fn()
  }))
}))

// Mock config injection
const mockConfig: EasyAuthConfig = {
  baseUrl: 'https://test-api.com',
  enableLogging: false
}

describe('LoginButton Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Rendering', () => {
    it('should render login button with default text', () => {
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      const button = wrapper.find('button')
      expect(button.exists()).toBe(true)
      expect(button.text()).toContain('Sign in with Google')
    })

    it('should render custom button text when provided', () => {
      const customText = 'Login with Google Account'
      
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        slots: {
          default: customText
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      expect(wrapper.text()).toContain(customText)
    })

    it('should apply custom CSS classes', () => {
      const customClass = 'my-custom-button-class'
      
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google',
          class: customClass
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      expect(wrapper.classes()).toContain(customClass)
    })

    it('should render button for different providers', () => {
      const providers = ['google', 'apple', 'facebook', 'azure-b2c'] as const

      providers.forEach(provider => {
        const wrapper = mount(LoginButton, {
          props: { provider },
          global: {
            provide: {
              easyAuthConfig: mockConfig
            }
          }
        })

        const expectedText = `Sign in with ${provider === 'azure-b2c' ? 'Azure' : 
          provider.charAt(0).toUpperCase() + provider.slice(1)}`
        
        expect(wrapper.text()).toContain(expectedText)
      })
    })
  })

  describe('Interaction', () => {
    it('should call login when button is clicked', async () => {
      const mockLogin = vi.fn()
      
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(false),
        user: ref(null),
        error: ref(null),
        login: mockLogin,
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      await wrapper.find('button').trigger('click')

      expect(mockLogin).toHaveBeenCalledWith({ provider: 'google' })
    })

    it('should pass additional login options when provided', async () => {
      const mockLogin = vi.fn()
      const loginOptions = {
        returnUrl: 'https://example.com/callback',
        state: 'custom-state'
      }
      
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(false),
        user: ref(null),
        error: ref(null),
        login: mockLogin,
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google',
          options: loginOptions
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      await wrapper.find('button').trigger('click')

      expect(mockLogin).toHaveBeenCalledWith({
        provider: 'google',
        ...loginOptions
      })
    })

    it('should be disabled when already authenticated', async () => {
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(true),
        isLoading: ref(false),
        user: ref(null),
        error: ref(null),
        login: vi.fn(),
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      const button = wrapper.find('button')
      expect(button.attributes('disabled')).toBeDefined()
    })

    it('should show loading state when authentication is in progress', async () => {
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(true),
        user: ref(null),
        error: ref(null),
        login: vi.fn(),
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      const button = wrapper.find('button')
      expect(button.attributes('disabled')).toBeDefined()
      expect(button.text()).toContain('Loading') // or spinner, will implement
    })
  })

  describe('Error handling', () => {
    it('should handle login errors gracefully', async () => {
      const mockLogin = vi.fn().mockRejectedValue(new Error('Login failed'))
      
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(false),
        user: ref(null),
        error: ref('Login failed'),
        login: mockLogin,
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      await wrapper.find('button').trigger('click')
      await nextTick()

      // Should not crash and should handle error appropriately
      expect(wrapper.exists()).toBe(true)
    })

    it('should emit error event when login fails', async () => {
      const mockLogin = vi.fn().mockRejectedValue(new Error('Login failed'))
      
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(false),
        user: ref(null),
        error: ref('Login failed'),
        login: mockLogin,
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      await wrapper.find('button').trigger('click')
      await nextTick()

      // Should emit error event (will implement)
      const errorEvents = wrapper.emitted('error')
      expect(errorEvents).toBeDefined() // Will implement error emission
    })
  })

  describe('Accessibility', () => {
    it('should have proper ARIA attributes', () => {
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      const button = wrapper.find('button')
      expect(button.attributes('type')).toBe('button')
      expect(button.attributes('aria-label')).toBeDefined()
    })

    it('should be keyboard accessible', async () => {
      const mockLogin = vi.fn()
      
      vi.mocked(await import('../composables/useEasyAuth')).useEasyAuth.mockReturnValue({
        isAuthenticated: ref(false),
        isLoading: ref(false),
        user: ref(null),
        error: ref(null),
        login: mockLogin,
        logout: vi.fn(),
        refresh: vi.fn()
      } as any)

      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      await wrapper.find('button').trigger('keydown.enter')
      
      // Should trigger login on Enter key
      expect(mockLogin).toHaveBeenCalledWith({ provider: 'google' })
    })
  })

  describe('Provider-specific behavior', () => {
    it('should use provider-specific styling', () => {
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'google'
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      // Should have provider-specific CSS class
      expect(wrapper.classes()).toContain('provider-google') // Will implement
    })

    it('should handle custom providers', () => {
      const wrapper = mount(LoginButton, {
        props: {
          provider: 'custom' as any
        },
        global: {
          provide: {
            easyAuthConfig: mockConfig
          }
        }
      })

      expect(wrapper.text()).toContain('Sign in with Custom')
    })
  })
})