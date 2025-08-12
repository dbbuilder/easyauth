import { Injectable } from '@angular/core';

export interface AuthStorage {
  getAccessToken(): string | null;
  setAccessToken(token: string): void;
  getRefreshToken(): string | null;
  setRefreshToken(token: string): void;
  getUserData(): any;
  setUserData(data: any): void;
  clear(): void;
}

class LocalStorageAuthStorage implements AuthStorage {
  private readonly ACCESS_TOKEN_KEY = 'easyauth_access_token';
  private readonly REFRESH_TOKEN_KEY = 'easyauth_refresh_token';
  private readonly USER_DATA_KEY = 'easyauth_user_data';

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  setAccessToken(token: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  setRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
  }

  getUserData(): any {
    const data = localStorage.getItem(this.USER_DATA_KEY);
    return data ? JSON.parse(data) : null;
  }

  setUserData(data: any): void {
    localStorage.setItem(this.USER_DATA_KEY, JSON.stringify(data));
  }

  clear(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_DATA_KEY);
  }
}

class SessionStorageAuthStorage implements AuthStorage {
  private readonly ACCESS_TOKEN_KEY = 'easyauth_access_token';
  private readonly REFRESH_TOKEN_KEY = 'easyauth_refresh_token';
  private readonly USER_DATA_KEY = 'easyauth_user_data';

  getAccessToken(): string | null {
    return sessionStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  setAccessToken(token: string): void {
    sessionStorage.setItem(this.ACCESS_TOKEN_KEY, token);
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  setRefreshToken(token: string): void {
    sessionStorage.setItem(this.REFRESH_TOKEN_KEY, token);
  }

  getUserData(): any {
    const data = sessionStorage.getItem(this.USER_DATA_KEY);
    return data ? JSON.parse(data) : null;
  }

  setUserData(data: any): void {
    sessionStorage.setItem(this.USER_DATA_KEY, JSON.stringify(data));
  }

  clear(): void {
    sessionStorage.removeItem(this.ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(this.REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(this.USER_DATA_KEY);
  }
}

class MemoryAuthStorage implements AuthStorage {
  private data: Record<string, string> = {};
  private userData: any = null;

  getAccessToken(): string | null {
    return this.data.access_token || null;
  }

  setAccessToken(token: string): void {
    this.data.access_token = token;
  }

  getRefreshToken(): string | null {
    return this.data.refresh_token || null;
  }

  setRefreshToken(token: string): void {
    this.data.refresh_token = token;
  }

  getUserData(): any {
    return this.userData;
  }

  setUserData(data: any): void {
    this.userData = data;
  }

  clear(): void {
    this.data = {};
    this.userData = null;
  }
}

@Injectable({
  providedIn: 'root'
})
export class AuthStorageService {
  private storage: AuthStorage;

  constructor() {
    this.storage = this.createAuthStorage('localStorage');
  }

  setStorageType(type: 'localStorage' | 'sessionStorage' | 'memory' = 'localStorage'): void {
    this.storage = this.createAuthStorage(type);
  }

  private createAuthStorage(type: 'localStorage' | 'sessionStorage' | 'memory' = 'localStorage'): AuthStorage {
    switch (type) {
      case 'sessionStorage':
        return new SessionStorageAuthStorage();
      case 'memory':
        return new MemoryAuthStorage();
      case 'localStorage':
      default:
        return new LocalStorageAuthStorage();
    }
  }

  getAccessToken(): string | null {
    return this.storage.getAccessToken();
  }

  setAccessToken(token: string): void {
    this.storage.setAccessToken(token);
  }

  getRefreshToken(): string | null {
    return this.storage.getRefreshToken();
  }

  setRefreshToken(token: string): void {
    this.storage.setRefreshToken(token);
  }

  getUserData(): any {
    return this.storage.getUserData();
  }

  setUserData(data: any): void {
    this.storage.setUserData(data);
  }

  clear(): void {
    this.storage.clear();
  }
}