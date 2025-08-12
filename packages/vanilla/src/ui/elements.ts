import { EasyAuthClient } from '../core/client';
import { UserInfo } from '../types';

export interface LoginButtonOptions {
  provider: string;
  returnUrl?: string;
  text?: string;
  loadingText?: string;
  className?: string;
  disabled?: boolean;
  loadingSpinner?: string;
}

export interface LogoutButtonOptions {
  text?: string;
  loadingText?: string;
  className?: string;
  disabled?: boolean;
  loadingSpinner?: string;
}

export interface UserProfileOptions {
  showAvatar?: boolean;
  showName?: boolean;
  showEmail?: boolean;
  showRoles?: boolean;
  showProvider?: boolean;
  showStatus?: boolean;
  showLastLogin?: boolean;
  className?: string;
}

export class EasyAuthUI {
  constructor(private client: EasyAuthClient) {}

  createLoginButton(element: HTMLElement, options: LoginButtonOptions): void {
    const {
      provider,
      returnUrl,
      text = 'Sign In',
      loadingText = 'Signing in...',
      className = 'easyauth-login-btn',
      disabled = false,
      loadingSpinner = '⏳'
    } = options;

    let isLoading = false;

    const button = element.tagName === 'BUTTON' 
      ? element as HTMLButtonElement 
      : element.querySelector('button') || this.createButton();

    button.className = className;
    button.disabled = disabled;
    this.updateButtonText(button, text);

    const handleLogin = async () => {
      if (disabled || isLoading) return;

      try {
        isLoading = true;
        button.disabled = true;
        this.updateButtonText(button, loadingSpinner + ' ' + loadingText);

        await this.client.login(provider, returnUrl);
      } catch (error) {
        console.error('Login failed:', error);
      } finally {
        isLoading = false;
        button.disabled = disabled;
        this.updateButtonText(button, text);
      }
    };

    button.addEventListener('click', handleLogin);

    // Add to element if not already a button
    if (element.tagName !== 'BUTTON') {
      element.appendChild(button);
    }
  }

  createLogoutButton(element: HTMLElement, options: LogoutButtonOptions = {}): void {
    const {
      text = 'Sign Out',
      loadingText = 'Signing out...',
      className = 'easyauth-logout-btn',
      disabled = false,
      loadingSpinner = '⏳'
    } = options;

    let isLoading = false;

    const button = element.tagName === 'BUTTON' 
      ? element as HTMLButtonElement 
      : element.querySelector('button') || this.createButton();

    button.className = className;
    button.disabled = disabled;
    this.updateButtonText(button, text);

    const handleLogout = async () => {
      if (disabled || isLoading) return;

      try {
        isLoading = true;
        button.disabled = true;
        this.updateButtonText(button, loadingSpinner + ' ' + loadingText);

        await this.client.logout();
      } catch (error) {
        console.error('Logout failed:', error);
      } finally {
        isLoading = false;
        button.disabled = disabled;
        this.updateButtonText(button, text);
      }
    };

    button.addEventListener('click', handleLogout);

    // Add to element if not already a button
    if (element.tagName !== 'BUTTON') {
      element.appendChild(button);
    }
  }

  renderUserProfile(element: HTMLElement, options: UserProfileOptions = {}): void {
    const {
      showAvatar = true,
      showName = true,
      showEmail = true,
      showRoles = false,
      showProvider = false,
      showStatus = false,
      showLastLogin = false,
      className = 'easyauth-user-profile'
    } = options;

    const updateProfile = () => {
      const { user, isLoading, error } = this.client.currentState;

      // Clear existing content
      element.innerHTML = '';
      element.className = className;

      if (isLoading) {
        this.renderLoadingProfile(element);
        return;
      }

      if (error) {
        this.renderErrorProfile(element, error);
        return;
      }

      if (!user) {
        element.innerHTML = '<div class="no-user">Not authenticated</div>';
        return;
      }

      this.renderUserDetails(element, user, {
        showAvatar,
        showName,
        showEmail,
        showRoles,
        showProvider,
        showStatus,
        showLastLogin
      });
    };

    // Initial render
    updateProfile();

    // Listen for state changes
    this.client.onStateChange(updateProfile);
  }

  private renderUserDetails(
    element: HTMLElement, 
    user: UserInfo, 
    options: Required<Omit<UserProfileOptions, 'className'>>
  ): void {
    const container = this.createElement('div', 'user-details');

    // Avatar
    if (options.showAvatar) {
      const avatarContainer = this.createElement('div', 'user-avatar');
      
      if (user.profilePicture) {
        const img = this.createElement('img', 'avatar-image') as HTMLImageElement;
        img.src = user.profilePicture;
        img.alt = user.name || user.email || 'User';
        avatarContainer.appendChild(img);
      } else {
        const fallback = this.createElement('div', 'avatar-fallback');
        fallback.textContent = this.getInitials(user);
        avatarContainer.appendChild(fallback);
      }
      
      container.appendChild(avatarContainer);
    }

    const infoContainer = this.createElement('div', 'user-info');

    // Name
    if (options.showName && (user.name || user.firstName)) {
      const nameEl = this.createElement('div', 'user-name');
      nameEl.textContent = user.name || `${user.firstName} ${user.lastName || ''}`.trim();
      infoContainer.appendChild(nameEl);
    }

    // Email
    if (options.showEmail && user.email) {
      const emailEl = this.createElement('div', 'user-email');
      emailEl.textContent = user.email;
      infoContainer.appendChild(emailEl);
    }

    // Roles
    if (options.showRoles && user.roles?.length) {
      const rolesContainer = this.createElement('div', 'user-roles');
      user.roles.forEach(role => {
        const roleEl = this.createElement('span', 'role-badge');
        roleEl.textContent = role;
        rolesContainer.appendChild(roleEl);
      });
      infoContainer.appendChild(rolesContainer);
    }

    // Provider
    if (options.showProvider && user.provider) {
      const providerEl = this.createElement('div', 'user-provider');
      providerEl.textContent = `via ${user.provider}`;
      infoContainer.appendChild(providerEl);
    }

    // Status
    if (options.showStatus) {
      const statusEl = this.createElement('span', 'user-status');
      statusEl.className += user.isVerified ? ' verified' : ' unverified';
      statusEl.textContent = user.isVerified ? 'Verified' : 'Unverified';
      infoContainer.appendChild(statusEl);
    }

    // Last login
    if (options.showLastLogin && user.lastLogin) {
      const lastLoginEl = this.createElement('div', 'user-last-login');
      lastLoginEl.textContent = `Last login: ${this.formatDate(user.lastLogin)}`;
      infoContainer.appendChild(lastLoginEl);
    }

    container.appendChild(infoContainer);
    element.appendChild(container);
  }

  private renderLoadingProfile(element: HTMLElement): void {
    const container = this.createElement('div', 'user-profile-loading');
    
    const avatarPlaceholder = this.createElement('div', 'loading-avatar');
    const textPlaceholder = this.createElement('div', 'loading-text');
    const line1 = this.createElement('div', 'loading-line');
    const line2 = this.createElement('div', 'loading-line short');
    
    textPlaceholder.appendChild(line1);
    textPlaceholder.appendChild(line2);
    
    container.appendChild(avatarPlaceholder);
    container.appendChild(textPlaceholder);
    element.appendChild(container);
  }

  private renderErrorProfile(element: HTMLElement, error: string): void {
    const errorEl = this.createElement('div', 'user-profile-error');
    errorEl.textContent = `Failed to load user profile: ${error}`;
    element.appendChild(errorEl);
  }

  private createElement(tag: string, className?: string): HTMLElement {
    const element = document.createElement(tag);
    if (className) {
      element.className = className;
    }
    return element;
  }

  private createButton(className?: string): HTMLButtonElement {
    const button = document.createElement('button');
    if (className) {
      button.className = className;
    }
    return button;
  }

  private updateButtonText(button: HTMLButtonElement, text: string): void {
    button.textContent = text;
  }

  private getInitials(user: UserInfo): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
    }
    if (user.name) {
      const parts = user.name.split(' ');
      if (parts.length >= 2) {
        return `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase();
      }
      return user.name.charAt(0).toUpperCase();
    }
    if (user.email) {
      return user.email.charAt(0).toUpperCase();
    }
    return '?';
  }

  private formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}