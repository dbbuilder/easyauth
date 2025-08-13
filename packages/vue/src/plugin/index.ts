/**
 * EasyAuth Vue 3 Plugin
 * Provides global configuration and easy installation
 */

import type { App } from 'vue'
// Using local types since @easyauth/sdk is not yet implemented
interface EasyAuthConfig {
  apiUrl?: string;
  providers?: string[];
}
import { useEasyAuth } from '../composables/useEasyAuth'
import LoginButton from '../components/LoginButton.vue'
import LogoutButton from '../components/LogoutButton.vue'

// Plugin options interface
export interface EasyAuthPluginOptions {
  config: EasyAuthConfig
  components?: {
    LoginButton?: string
    LogoutButton?: string
  }
}

// Plugin installation function
export function createEasyAuth(options: EasyAuthPluginOptions) {
  return {
    install(app: App) {
      // Provide the config globally
      app.provide('easyAuthConfig', options.config)
      
      // Register components globally if desired
      const componentNames = options.components || {
        LoginButton: 'LoginButton',
        LogoutButton: 'LogoutButton'
      }

      if (componentNames.LoginButton) {
        app.component(componentNames.LoginButton, LoginButton)
      }
      
      if (componentNames.LogoutButton) {
        app.component(componentNames.LogoutButton, LogoutButton)
      }

      // Add global properties (optional - for Options API compatibility)
      app.config.globalProperties.$easyAuth = {
        useEasyAuth: () => useEasyAuth(options.config)
      }
    }
  }
}

// Export types for TypeScript users
export type { EasyAuthConfig }

// Default export for easier importing
export default createEasyAuth