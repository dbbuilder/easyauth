import { 
  ApiResponse, 
  AuthStatus, 
  LoginRequest, 
  LoginResult, 
  TokenRefreshRequest, 
  TokenRefreshResult, 
  LogoutResult,
  UserInfo,
  EasyAuthConfig
} from '../types';
import { createAuthStorage } from './storage';

class AuthApi {
  private baseUrl: string = '/api';
  private config: EasyAuthConfig = {};
  private storage = createAuthStorage('localStorage');

  configure(config: Partial<EasyAuthConfig>) {
    this.config = { ...this.config, ...config };
    if (config.baseUrl) {
      this.baseUrl = config.baseUrl.replace(/\/$/, '') + '/api';
    }
    if (config.storage) {
      this.storage = createAuthStorage(config.storage);
    }
  }

  private async makeRequest<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    // Add auth headers if available
    const accessToken = this.storage.getAccessToken();
    const headers = new Headers(options.headers);
    
    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`);
    }
    
    // Add correlation ID for tracing
    if (!headers.has('X-Correlation-ID')) {
      headers.set('X-Correlation-ID', this.generateCorrelationId());
    }

    headers.set('Content-Type', 'application/json');

    const config: RequestInit = {
      ...options,
      headers,
    };

    if (this.config.debug) {
      console.log(`[EasyAuth] ${config.method || 'GET'} ${url}`, config);
    }

    try {
      const response = await fetch(url, config);
      const data: ApiResponse<T> = await response.json();

      if (this.config.debug) {
        console.log(`[EasyAuth] Response:`, data);
      }

      if (!data.success) {
        const error = new Error(data.message || 'API request failed');
        (error as any).code = data.error;
        (error as any).details = data.errorDetails;
        (error as any).correlationId = data.correlationId;
        throw error;
      }

      return data.data!;
    } catch (error) {
      if (this.config.debug) {
        console.error(`[EasyAuth] Request failed:`, error);
      }
      
      if (error instanceof TypeError && error.message.includes('Failed to fetch')) {
        throw new Error(`Unable to connect to authentication server at ${url}. Please check your configuration.`);
      }
      
      throw error;
    }
  }

  private generateCorrelationId(): string {
    return `react-${Math.random().toString(36).substr(2, 9)}-${Date.now().toString(36)}`;
  }

  async checkAuth(): Promise<AuthStatus> {
    return this.makeRequest<AuthStatus>('/auth-check');
  }

  async login(request: LoginRequest): Promise<LoginResult> {
    return this.makeRequest<LoginResult>('/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async logout(): Promise<LogoutResult> {
    return this.makeRequest<LogoutResult>('/logout', {
      method: 'POST',
    });
  }

  async refreshToken(request: TokenRefreshRequest): Promise<TokenRefreshResult> {
    return this.makeRequest<TokenRefreshResult>('/refresh', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getUserProfile(): Promise<UserInfo> {
    return this.makeRequest<UserInfo>('/user');
  }

  async healthCheck(): Promise<{ status: string; service: string; timestamp: string; version: string }> {
    return this.makeRequest('/health');
  }
}

export const authApi = new AuthApi();