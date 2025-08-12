import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, timer, Subscription, throwError, of } from 'rxjs';
import { catchError, switchMap, tap, map } from 'rxjs/operators';
import { 
  AuthState, 
  EasyAuthConfig, 
  LoginResult, 
  LogoutResult, 
  UserInfo 
} from '../models';
import { AuthApiService } from './auth-api.service';
import { AuthStorageService } from './auth-storage.service';

@Injectable({
  providedIn: 'root'
})
export class EasyAuthService implements OnDestroy {
  private readonly initialState: AuthState = {
    isLoading: true,
    isAuthenticated: false,
    user: null,
    error: null,
    tokenExpiry: null,
    sessionId: null,
  };

  private authState$ = new BehaviorSubject<AuthState>(this.initialState);
  private config: EasyAuthConfig = {};
  private refreshTokenSubscription?: Subscription;

  constructor(
    private authApi: AuthApiService,
    private storage: AuthStorageService
  ) {}

  ngOnDestroy(): void {
    this.refreshTokenSubscription?.unsubscribe();
  }

  configure(config: Partial<EasyAuthConfig>): void {
    this.config = { ...this.config, ...config };
    this.authApi.configure(config);
    
    // Auto-check authentication on configuration
    this.checkAuth().subscribe();
  }

  get state$(): Observable<AuthState> {
    return this.authState$.asObservable();
  }

  get currentState(): AuthState {
    return this.authState$.value;
  }

  get isAuthenticated$(): Observable<boolean> {
    return this.state$.pipe(
      map(state => state.isAuthenticated)
    );
  }

  get user$(): Observable<UserInfo | null> {
    return this.state$.pipe(
      map(state => state.user)
    );
  }

  get isLoading$(): Observable<boolean> {
    return this.state$.pipe(
      map(state => state.isLoading)
    );
  }

  get error$(): Observable<string | null> {
    return this.state$.pipe(
      map(state => state.error)
    );
  }

  private updateState(updates: Partial<AuthState>): void {
    const currentState = this.authState$.value;
    const newState = { ...currentState, ...updates };
    this.authState$.next(newState);

    // Handle side effects
    this.handleStateChange(newState, currentState);
  }

  private handleStateChange(newState: AuthState, previousState: AuthState): void {
    // Handle authentication requirement
    if (!newState.isLoading && !newState.isAuthenticated && this.config.onLoginRequired) {
      this.config.onLoginRequired();
    }

    // Handle errors
    if (newState.error && this.config.onError) {
      this.config.onError(newState.error);
    }

    // Set up auto-refresh when token expiry changes
    if (newState.tokenExpiry !== previousState.tokenExpiry) {
      this.setupAutoRefresh(newState);
    }
  }

  private setupAutoRefresh(state: AuthState): void {
    // Clear existing subscription
    this.refreshTokenSubscription?.unsubscribe();

    if (!state.isAuthenticated || !state.tokenExpiry || !this.config.autoRefresh) {
      return;
    }

    const refreshTime = state.tokenExpiry.getTime() - Date.now() - 60000; // Refresh 1 minute before expiry
    
    if (refreshTime > 0) {
      this.refreshTokenSubscription = timer(refreshTime).pipe(
        switchMap(() => this.refreshToken()),
        catchError(() => {
          // If refresh fails, trigger onTokenExpired callback
          this.config.onTokenExpired?.();
          return throwError(() => new Error('Token refresh failed'));
        })
      ).subscribe();
    }
  }

  login(provider: string, returnUrl?: string): Observable<LoginResult> {
    this.updateState({ isLoading: true, error: null });

    return this.authApi.login({ provider, returnUrl }).pipe(
      tap(result => {
        if (result.redirectRequired && result.authUrl) {
          // For OAuth flows, redirect to provider
          window.location.href = result.authUrl;
        }
      }),
      tap(() => this.updateState({ isLoading: false })),
      catchError(error => {
        this.updateState({ 
          isLoading: false, 
          error: error instanceof Error ? error.message : 'Login failed' 
        });
        return throwError(() => error);
      })
    );
  }

  logout(): Observable<LogoutResult> {
    this.updateState({ isLoading: true });

    return this.authApi.logout().pipe(
      tap(result => {
        // Clear local storage
        this.storage.clear();
        this.refreshTokenSubscription?.unsubscribe();
        
        this.updateState({ 
          ...this.initialState, 
          isLoading: false 
        });
        
        if (result.redirectUrl) {
          window.location.href = result.redirectUrl;
        }
      }),
      catchError(() => {
        // Even if logout fails on server, clear local state
        this.storage.clear();
        this.refreshTokenSubscription?.unsubscribe();
        this.updateState({ 
          ...this.initialState, 
          isLoading: false 
        });
        
        // Return successful logout locally
        return of({ loggedOut: true });
      })
    );
  }

  refreshToken(): Observable<boolean> {
    const refreshToken = this.storage.getRefreshToken();
    if (!refreshToken) {
      return of(false);
    }

    return this.authApi.refreshToken({ refreshToken }).pipe(
      switchMap(result => {
        if (result.accessToken) {
          this.storage.setAccessToken(result.accessToken);
          if (result.refreshToken) {
            this.storage.setRefreshToken(result.refreshToken);
          }
          
          // Re-check auth status after refresh
          return this.checkAuth().pipe(
            map(() => true)
          );
        }
        
        return of(false);
      }),
      catchError(() => {
        // If refresh fails, clear tokens and force re-login
        this.storage.clear();
        this.updateState({ ...this.initialState, isLoading: false });
        return of(false);
      })
    );
  }

  checkAuth(): Observable<boolean> {
    this.updateState({ isLoading: true });

    return this.authApi.checkAuth().pipe(
      tap(authStatus => {
        this.updateState({
          isLoading: false,
          isAuthenticated: authStatus.isAuthenticated,
          user: authStatus.user || null,
          tokenExpiry: authStatus.tokenExpiry ? new Date(authStatus.tokenExpiry) : null,
          sessionId: authStatus.sessionId || null,
          error: null,
        });
      }),
      map(authStatus => authStatus.isAuthenticated),
      catchError(error => {
        // Check if it's a token expired error
        if (error instanceof Error && error.message.includes('TOKEN_EXPIRED')) {
          return this.refreshToken().pipe(
            switchMap(refreshSuccess => {
              if (refreshSuccess) {
                return this.checkAuth();
              }
              return of(false);
            })
          );
        }
        
        this.updateState({ 
          isLoading: false, 
          error: error instanceof Error ? error.message : 'Auth check failed' 
        });
        return of(false);
      })
    );
  }

  clearError(): void {
    this.updateState({ error: null });
  }

  getUserProfile(): Observable<UserInfo> {
    return this.authApi.getUserProfile().pipe(
      tap(user => this.updateState({ user }))
    );
  }

  hasRole(role: string): boolean {
    const user = this.currentState.user;
    return user ? user.roles.includes(role) : false;
  }

  hasPermission(permission: string): boolean {
    const user = this.currentState.user;
    return user ? (user.permissions || []).includes(permission) : false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.currentState.user;
    return user ? roles.some(role => user.roles.includes(role)) : false;
  }

  hasAnyPermission(permissions: string[]): boolean {
    const user = this.currentState.user;
    return user ? permissions.some(permission => (user.permissions || []).includes(permission)) : false;
  }
}