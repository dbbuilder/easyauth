import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
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
} from '../models';
import { AuthStorageService } from './auth-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private baseUrl: string = '/api';
  private config: EasyAuthConfig = {};

  constructor(
    private http: HttpClient,
    private storage: AuthStorageService
  ) {}

  configure(config: Partial<EasyAuthConfig>): void {
    this.config = { ...this.config, ...config };
    if (config.baseUrl) {
      this.baseUrl = config.baseUrl.replace(/\/$/, '') + '/api';
    }
    if (config.storage) {
      this.storage.setStorageType(config.storage);
    }
  }

  private createHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-Correlation-ID': this.generateCorrelationId()
    });

    const accessToken = this.storage.getAccessToken();
    if (accessToken) {
      headers = headers.set('Authorization', `Bearer ${accessToken}`);
    }

    return headers;
  }

  private makeRequest<T>(
    endpoint: string, 
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'GET',
    body?: any
  ): Observable<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = this.createHeaders();

    if (this.config.debug) {
      console.log(`[EasyAuth Angular] ${method} ${url}`, { headers, body });
    }

    const request = this.http.request<ApiResponse<T>>(method, url, {
      headers,
      body: body ? JSON.stringify(body) : undefined
    }).pipe(
      map(response => {
        if (this.config.debug) {
          console.log(`[EasyAuth Angular] Response:`, response);
        }

        if (!response.success) {
          const error = new Error(response.message || 'API request failed');
          (error as any).code = response.error;
          (error as any).details = response.errorDetails;
          (error as any).correlationId = response.correlationId;
          throw error;
        }

        return response.data!;
      }),
      catchError((error: HttpErrorResponse) => {
        if (this.config.debug) {
          console.error(`[EasyAuth Angular] Request failed:`, error);
        }
        
        if (error.status === 0) {
          return throwError(() => new Error(`Unable to connect to authentication server at ${url}. Please check your configuration.`));
        }
        
        return throwError(() => error.error || error);
      })
    );

    return request;
  }

  private generateCorrelationId(): string {
    return `angular-${Math.random().toString(36).substr(2, 9)}-${Date.now().toString(36)}`;
  }

  checkAuth(): Observable<AuthStatus> {
    return this.makeRequest<AuthStatus>('/auth-check');
  }

  login(request: LoginRequest): Observable<LoginResult> {
    return this.makeRequest<LoginResult>('/login', 'POST', request);
  }

  logout(): Observable<LogoutResult> {
    return this.makeRequest<LogoutResult>('/logout', 'POST');
  }

  refreshToken(request: TokenRefreshRequest): Observable<TokenRefreshResult> {
    return this.makeRequest<TokenRefreshResult>('/refresh', 'POST', request);
  }

  getUserProfile(): Observable<UserInfo> {
    return this.makeRequest<UserInfo>('/user');
  }

  healthCheck(): Observable<{ status: string; service: string; timestamp: string; version: string }> {
    return this.makeRequest<{ status: string; service: string; timestamp: string; version: string }>('/health');
  }
}