import { StorageAdapter } from '../types';

/**
 * LocalStorage implementation of StorageAdapter
 */
export class LocalStorageAdapter implements StorageAdapter {
  getItem(key: string): string | null {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        return window.localStorage.getItem(key);
      }
    } catch (error) {
      // localStorage might be disabled
      console.warn('localStorage not available:', error);
    }
    return null;
  }
  
  setItem(key: string, value: string): void {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.setItem(key, value);
      }
    } catch (error) {
      // localStorage might be disabled or full
      console.warn('Failed to write to localStorage:', error);
    }
  }
  
  removeItem(key: string): void {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.removeItem(key);
      }
    } catch (error) {
      console.warn('Failed to remove from localStorage:', error);
    }
  }
  
  clear(): void {
    try {
      if (typeof window !== 'undefined' && window.localStorage) {
        window.localStorage.clear();
      }
    } catch (error) {
      console.warn('Failed to clear localStorage:', error);
    }
  }
}

/**
 * SessionStorage implementation of StorageAdapter
 */
export class SessionStorageAdapter implements StorageAdapter {
  getItem(key: string): string | null {
    try {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        return window.sessionStorage.getItem(key);
      }
    } catch (error) {
      console.warn('sessionStorage not available:', error);
    }
    return null;
  }
  
  setItem(key: string, value: string): void {
    try {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        window.sessionStorage.setItem(key, value);
      }
    } catch (error) {
      console.warn('Failed to write to sessionStorage:', error);
    }
  }
  
  removeItem(key: string): void {
    try {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        window.sessionStorage.removeItem(key);
      }
    } catch (error) {
      console.warn('Failed to remove from sessionStorage:', error);
    }
  }
  
  clear(): void {
    try {
      if (typeof window !== 'undefined' && window.sessionStorage) {
        window.sessionStorage.clear();
      }
    } catch (error) {
      console.warn('Failed to clear sessionStorage:', error);
    }
  }
}

/**
 * In-memory storage implementation (fallback for SSR)
 */
export class MemoryStorageAdapter implements StorageAdapter {
  private storage = new Map<string, string>();
  
  getItem(key: string): string | null {
    return this.storage.get(key) ?? null;
  }
  
  setItem(key: string, value: string): void {
    this.storage.set(key, value);
  }
  
  removeItem(key: string): void {
    this.storage.delete(key);
  }
  
  clear(): void {
    this.storage.clear();
  }
}