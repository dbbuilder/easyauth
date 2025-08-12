# @easyauth/angular

Angular services, guards, and components for EasyAuth Framework - Zero-config authentication for Angular applications.

## Installation

```bash
npm install @easyauth/angular
```

## Quick Start

### 1. Import the Module

```typescript
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { EasyAuthModule } from '@easyauth/angular';

import { AppComponent } from './app.component';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    EasyAuthModule.forRoot({
      baseUrl: 'http://localhost:5000', // Your backend URL
      autoRefresh: true,
      debug: true
    })
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

### 2. Use in Components

```typescript
import { Component } from '@angular/core';
import { EasyAuthService } from '@easyauth/angular';

@Component({
  selector: 'app-home',
  template: `
    <div *ngIf="authService.isAuthenticated$ | async; else notAuthenticated">
      <h1>Welcome!</h1>
      <easy-user-profile [showRoles]="true"></easy-user-profile>
      <easy-logout-button></easy-logout-button>
    </div>
    
    <ng-template #notAuthenticated>
      <easy-login-button provider="google">Sign in with Google</easy-login-button>
      <easy-login-button provider="microsoft">Sign in with Microsoft</easy-login-button>
    </ng-template>
  `
})
export class HomeComponent {
  constructor(public authService: EasyAuthService) {}
}
```

### 3. Protect Routes

```typescript
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard, RoleGuard } from '@easyauth/angular';

const routes: Routes = [
  { path: 'public', component: PublicComponent },
  { 
    path: 'protected', 
    component: ProtectedComponent, 
    canActivate: [AuthGuard] 
  },
  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [RoleGuard],
    data: { 
      roles: ['admin'], 
      unauthorizedUrl: '/access-denied' 
    }
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
```

## API Reference

### EasyAuthService

Main service for authentication state management.

```typescript
class EasyAuthService {
  // Observables
  state$: Observable<AuthState>
  isAuthenticated$: Observable<boolean>
  user$: Observable<UserInfo | null>
  isLoading$: Observable<boolean>
  error$: Observable<string | null>

  // Methods
  configure(config: Partial<EasyAuthConfig>): void
  login(provider: string, returnUrl?: string): Observable<LoginResult>
  logout(): Observable<LogoutResult>
  refreshToken(): Observable<boolean>
  checkAuth(): Observable<boolean>
  clearError(): void
  getUserProfile(): Observable<UserInfo>

  // Authorization helpers
  hasRole(role: string): boolean
  hasPermission(permission: string): boolean
  hasAnyRole(roles: string[]): boolean
  hasAnyPermission(permissions: string[]): boolean
}
```

### Guards

#### AuthGuard
Protects routes requiring authentication:

```typescript
{
  path: 'protected',
  component: ProtectedComponent,
  canActivate: [AuthGuard],
  data: { loginUrl: '/login' } // Optional custom login route
}
```

#### RoleGuard
Protects routes requiring specific roles or permissions:

```typescript
{
  path: 'admin',
  component: AdminComponent,
  canActivate: [RoleGuard],
  data: {
    roles: ['admin', 'moderator'],     // Required roles
    permissions: ['write', 'delete'],  // Required permissions
    requireAll: false,                 // false = any, true = all
    unauthorizedUrl: '/forbidden'      // Redirect on insufficient permissions
  }
}
```

### Components

#### LoginButtonComponent
```html
<easy-login-button 
  provider="google"
  [returnUrl]="'/dashboard'"
  buttonText="Sign in with Google"
  buttonClass="btn btn-primary"
  (loginSuccess)="onLoginSuccess($event)"
  (loginError)="onLoginError($event)">
</easy-login-button>
```

#### LogoutButtonComponent
```html
<easy-logout-button
  buttonText="Sign Out"
  buttonClass="btn btn-secondary"
  (logoutSuccess)="onLogoutSuccess($event)">
</easy-logout-button>
```

#### UserProfileComponent
```html
<easy-user-profile
  [showAvatar]="true"
  [showName]="true"
  [showEmail]="true"
  [showRoles]="true"
  [showProvider]="false"
  [showStatus]="true"
  [showLastLogin]="false">
</easy-user-profile>
```

### Pipes

#### HasRolePipe
```html
<div *ngIf="'admin' | hasRole">Admin only content</div>
<div *ngIf="['admin', 'moderator'] | hasRole">Admin or moderator content</div>
<div *ngIf="['admin', 'moderator'] | hasRole:true">Admin AND moderator content</div>
```

#### HasPermissionPipe
```html
<button *ngIf="'write' | hasPermission">Create Post</button>
<button *ngIf="['write', 'publish'] | hasPermission">Publish</button>
```

## Configuration

```typescript
interface EasyAuthConfig {
  baseUrl?: string;                    // Backend API URL
  autoRefresh?: boolean;               // Auto-refresh tokens before expiry
  onTokenExpired?: () => void;         // Callback when token expires
  onLoginRequired?: () => void;        // Callback when login is required
  onError?: (error: any) => void;      // Global error handler
  storage?: 'localStorage' | 'sessionStorage' | 'memory';  // Token storage
  debug?: boolean;                     // Enable debug logging
}
```

## Advanced Usage

### Manual Authentication Check

```typescript
import { Component, OnInit } from '@angular/core';
import { EasyAuthService } from '@easyauth/angular';

@Component({
  selector: 'app-profile',
  template: `
    <div *ngIf="authService.state$ | async as authState">
      <div *ngIf="authState.isLoading">Loading...</div>
      <div *ngIf="authState.error">Error: {{ authState.error }}</div>
      <div *ngIf="authState.isAuthenticated">
        Welcome, {{ authState.user?.name }}!
      </div>
    </div>
  `
})
export class ProfileComponent implements OnInit {
  constructor(public authService: EasyAuthService) {}

  ngOnInit() {
    this.authService.checkAuth().subscribe(
      isAuthenticated => {
        if (isAuthenticated) {
          this.authService.getUserProfile().subscribe();
        }
      }
    );
  }
}
```

### Custom Error Handling

```typescript
EasyAuthModule.forRoot({
  baseUrl: 'https://api.example.com',
  onError: (error) => {
    console.error('Auth Error:', error);
    // Show toast notification
    this.toastr.error('Authentication error occurred');
  },
  onTokenExpired: () => {
    // Redirect to login
    this.router.navigate(['/login']);
  }
})
```

## Development

```bash
# Install dependencies
npm install

# Build the package
npm run build

# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Build and watch for changes
npm run build:watch
```

## License

MIT