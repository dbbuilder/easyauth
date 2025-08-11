/**
 * Storage management for sessions and tokens
 * Supports multiple storage mechanisms
 */

import { SessionInfo } from '../types';

type StorageType = 'localStorage' | 'sessionStorage' | 'memory' | 'cookie';

export class StorageManager {
  private readonly storageType: StorageType;
  private readonly memoryStorage = new Map<string, string>();
  private readonly SESSION_KEY = 'easyauth_session';

  constructor(storageType: StorageType = 'localStorage') {
    this.storageType = storageType;
  }

  public initialize(): void {
    // Perform any initialization required for the storage type
    if (this.storageType === 'cookie') {
      // Ensure we can access document.cookie in browser environment
      if (typeof document === 'undefined') {
        throw new Error('Cookie storage requires a browser environment');
      }
    }
  }

  /**
   * Store session data
   */
  public async storeSession(session: SessionInfo): Promise<void> {
    const data = JSON.stringify({
      ...session,
      createdAt: session.createdAt.toISOString(),
      expiresAt: session.expiresAt.toISOString(),
      lastAccessedAt: session.lastAccessedAt.toISOString(),
      user: {
        ...session.user,
        createdAt: session.user.createdAt?.toISOString(),
        lastLoginAt: session.user.lastLoginAt?.toISOString(),
      },
    });

    await this.setItem(this.SESSION_KEY, data);
  }

  /**
   * Retrieve session data
   */
  public async getSession(): Promise<SessionInfo | null> {
    try {
      const data = await this.getItem(this.SESSION_KEY);
      
      if (!data) {
        return null;
      }

      const parsed = JSON.parse(data);
      
      // Convert ISO strings back to Date objects
      return {
        ...parsed,
        createdAt: new Date(parsed.createdAt),
        expiresAt: new Date(parsed.expiresAt),
        lastAccessedAt: new Date(parsed.lastAccessedAt),
        user: {
          ...parsed.user,
          createdAt: parsed.user.createdAt ? new Date(parsed.user.createdAt) : undefined,
          lastLoginAt: parsed.user.lastLoginAt ? new Date(parsed.user.lastLoginAt) : undefined,
        },
      };
    } catch (error) {
      // Clear corrupted session data
      await this.clearSession();
      return null;
    }
  }

  /**
   * Clear session data
   */
  public async clearSession(): Promise<void> {
    await this.removeItem(this.SESSION_KEY);
  }

  /**
   * Generic storage operations
   */
  private async setItem(key: string, value: string): Promise<void> {
    switch (this.storageType) {
      case 'localStorage':
        if (typeof localStorage !== 'undefined') {
          localStorage.setItem(key, value);
        }
        break;
      
      case 'sessionStorage':
        if (typeof sessionStorage !== 'undefined') {
          sessionStorage.setItem(key, value);
        }
        break;
      
      case 'memory':
        this.memoryStorage.set(key, value);
        break;
      
      case 'cookie':
        this.setCookie(key, value);
        break;
    }
  }

  private async getItem(key: string): Promise<string | null> {
    switch (this.storageType) {
      case 'localStorage':
        if (typeof localStorage !== 'undefined') {
          return localStorage.getItem(key);
        }
        return null;
      
      case 'sessionStorage':
        if (typeof sessionStorage !== 'undefined') {
          return sessionStorage.getItem(key);
        }
        return null;
      
      case 'memory':
        return this.memoryStorage.get(key) || null;
      
      case 'cookie':
        return this.getCookie(key);
      
      default:
        return null;
    }
  }

  private async removeItem(key: string): Promise<void> {
    switch (this.storageType) {
      case 'localStorage':
        if (typeof localStorage !== 'undefined') {
          localStorage.removeItem(key);
        }
        break;
      
      case 'sessionStorage':
        if (typeof sessionStorage !== 'undefined') {
          sessionStorage.removeItem(key);
        }
        break;
      
      case 'memory':
        this.memoryStorage.delete(key);
        break;
      
      case 'cookie':
        this.deleteCookie(key);
        break;
    }
  }

  /**
   * Cookie-specific operations
   */
  private setCookie(name: string, value: string): void {
    // Set secure cookie with appropriate flags
    const expires = new Date();
    expires.setTime(expires.getTime() + 24 * 60 * 60 * 1000); // 24 hours
    
    const cookieOptions = [
      `expires=${expires.toUTCString()}`,
      'path=/',
      'SameSite=Lax',
    ];

    // Add Secure flag if served over HTTPS
    if (typeof location !== 'undefined' && location.protocol === 'https:') {
      cookieOptions.push('Secure');
    }

    document.cookie = `${name}=${encodeURIComponent(value)}; ${cookieOptions.join('; ')}`;
  }

  private getCookie(name: string): string | null {
    const nameEQ = `${name}=`;
    const cookies = document.cookie.split(';');
    
    for (let cookie of cookies) {
      cookie = cookie.trim();
      if (cookie.indexOf(nameEQ) === 0) {
        return decodeURIComponent(cookie.substring(nameEQ.length));
      }
    }
    
    return null;
  }

  private deleteCookie(name: string): void {
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
  }
}