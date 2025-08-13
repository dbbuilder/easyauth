/**
 * Vue 3 composable for EasyAuth integration
 * Provides reactive authentication state and methods
 */

import { ref, onUnmounted } from 'vue'
// Note: @easyauth/sdk is not yet implemented, using placeholder types
interface EasyAuthConfig {
  apiUrl?: string;
  providers?: string[];
}

interface LoginOptions {
  provider: string;
  returnUrl?: string;
}

interface UserProfile {
  id: string;
  email: string;
  name?: string;
}

interface AuthResult {
  success: boolean;
  user: UserProfile;
  error?: string;
}

// Global state management for sharing between multiple composable instances
let globalClient: any | null = null
let globalState: {
  isAuthenticated: boolean
  isLoading: boolean
  user: UserProfile | null
  error: string | null
} | null = null

const subscribers = new Set<() => void>()

function notifySubscribers() {
  subscribers.forEach(callback => callback())
}

function initializeGlobalClient(_config: EasyAuthConfig) {
  if (!globalClient) {
    globalClient = {} // Placeholder for EasyAuthClient implementation
    globalState = {
      isAuthenticated: false,
      isLoading: false,
      user: null,
      error: null
    }

    // Load saved session on initialization
    const session = globalClient.getSession()
    if (session) {
      globalState.isAuthenticated = session.isAuthenticated
      globalState.user = session.user
    }

    // Listen to authentication events from SDK
    globalClient.on('login', (result: AuthResult) => {
      if (globalState) {
        globalState.isAuthenticated = true
        globalState.user = result.user
        globalState.error = null
        globalState.isLoading = false
        notifySubscribers()
      }
    })

    globalClient.on('logout', () => {
      if (globalState) {
        globalState.isAuthenticated = false
        globalState.user = null
        globalState.error = null
        globalState.isLoading = false
        notifySubscribers()
      }
    })

    globalClient.on('error', (error: string) => {
      if (globalState) {
        globalState.error = error
        globalState.isLoading = false
        notifySubscribers()
      }
    })

    globalClient.on('session-expired', () => {
      if (globalState) {
        globalState.isAuthenticated = false
        globalState.user = null
        globalState.error = 'Session expired'
        globalState.isLoading = false
        notifySubscribers()
      }
    })
  }
}

// Export a function to reset global state (for testing)
export function resetEasyAuthState() {
  globalClient = null
  globalState = null
  subscribers.clear()
}

export function useEasyAuth(config: EasyAuthConfig) {
  // Initialize global client if not already done
  initializeGlobalClient(config)

  // Create reactive refs
  const isAuthenticated = ref(globalState?.isAuthenticated ?? false)
  const isLoading = ref(globalState?.isLoading ?? false)
  const user = ref(globalState?.user ?? null)
  const error = ref(globalState?.error ?? null)

  // Subscribe to global state changes
  const updateLocalState = () => {
    if (globalState) {
      isAuthenticated.value = globalState.isAuthenticated
      isLoading.value = globalState.isLoading
      user.value = globalState.user
      error.value = globalState.error
    }
  }

  subscribers.add(updateLocalState)

  // Authentication methods
  const login = async (options: LoginOptions): Promise<void> => {
    if (!globalClient || !globalState) return

    try {
      globalState.isLoading = true
      globalState.error = null
      notifySubscribers()

      const result = await globalClient.login(options)
      
      if (result.success) {
        globalState.isAuthenticated = true
        globalState.user = result.user
        globalState.error = null
      } else {
        globalState.error = result.error || 'Login failed'
      }
    } catch (err) {
      globalState.error = err instanceof Error ? err.message : 'Login failed'
    } finally {
      globalState.isLoading = false
      notifySubscribers()
    }
  }

  const logout = async (): Promise<void> => {
    if (!globalClient || !globalState) return

    try {
      globalState.isLoading = true
      globalState.error = null
      notifySubscribers()

      await globalClient.logout()
      
      globalState.isAuthenticated = false
      globalState.user = null
      globalState.error = null
    } catch (err) {
      globalState.error = err instanceof Error ? err.message : 'Logout failed'
    } finally {
      globalState.isLoading = false
      notifySubscribers()
    }
  }

  const refresh = async (): Promise<void> => {
    if (!globalClient || !globalState) return

    try {
      globalState.isLoading = true
      globalState.error = null
      notifySubscribers()

      const result = await globalClient.refresh()
      
      if (result.success) {
        globalState.isAuthenticated = true
        globalState.user = result.user
        globalState.error = null
      } else {
        globalState.error = result.error || 'Refresh failed'
        globalState.isAuthenticated = false
        globalState.user = null
      }
    } catch (err) {
      globalState.error = err instanceof Error ? err.message : 'Refresh failed'
      globalState.isAuthenticated = false
      globalState.user = null
    } finally {
      globalState.isLoading = false
      notifySubscribers()
    }
  }

  // Cleanup on unmount
  onUnmounted(() => {
    subscribers.delete(updateLocalState)
  })

  return {
    // Reactive state refs
    isAuthenticated,
    isLoading,
    user,
    error,
    
    // Authentication methods
    login,
    logout,
    refresh
  }
}