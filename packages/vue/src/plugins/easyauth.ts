import type { App, Plugin } from 'vue';
import type { EasyAuthConfig } from '../types';
import { createEasyAuth } from '../composables/useAuth';
import LoginButton from '../components/LoginButton.vue';
import LogoutButton from '../components/LogoutButton.vue';
import AuthGuard from '../components/AuthGuard.vue';
import UserProfile from '../components/UserProfile.vue';

export interface EasyAuthPluginOptions extends Partial<EasyAuthConfig> {
  registerComponents?: boolean;
  componentPrefix?: string;
}

export const EasyAuthPlugin: Plugin = {
  install(app: App, options: EasyAuthPluginOptions = {}) {
    // Create the auth instance
    createEasyAuth(options);

    // Register components globally if requested
    if (options.registerComponents !== false) {
      const prefix = options.componentPrefix || 'EasyAuth';
      
      app.component(`${prefix}LoginButton`, LoginButton);
      app.component(`${prefix}LogoutButton`, LogoutButton);
      app.component(`${prefix}AuthGuard`, AuthGuard);
      app.component(`${prefix}UserProfile`, UserProfile);
    }

    // Provide global properties
    app.config.globalProperties.$easyauth = {
      // Any global methods can be added here
    };
  }
};

// For easier imports
export default EasyAuthPlugin;