# EasyAuth API Response Format Guide

EasyAuth provides **unified, consistent API responses** across all endpoints, eliminating the inconsistent response formats that caused confusion in the QuizGenerator project.

## 🎯 Unified Response Structure

All EasyAuth API endpoints return responses in this consistent format:

```json
{
  "success": true,
  "data": { /* your actual data */ },
  "message": "Operation completed successfully",
  "error": null,
  "errorDetails": null,
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "abc123def456",
  "version": "1.0.0",
  "meta": {}
}
```

## ✅ Success Response Examples

### Authentication Status Check
```json
GET /api/auth-check
{
  "success": true,
  "data": {
    "isAuthenticated": true,
    "user": {
      "id": "user123",
      "email": "user@example.com",
      "name": "John Doe",
      "firstName": "John",
      "lastName": "Doe",
      "profilePicture": "https://example.com/avatar.jpg",
      "provider": "Google",
      "roles": ["User"],
      "permissions": [],
      "lastLogin": "2024-01-15T09:15:30.000Z",
      "isVerified": true,
      "locale": "en-US",
      "timeZone": "America/New_York"
    },
    "tokenExpiry": "2024-01-15T11:30:45.000Z",
    "sessionId": "session789"
  },
  "message": "User is authenticated",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "auth-check-001",
  "version": "1.0.0"
}
```

### Login Initiation
```json
POST /api/login
{
  "success": true,
  "data": {
    "authUrl": "https://accounts.google.com/oauth/authorize?...",
    "provider": "Google",
    "state": "csrf-state-token-123",
    "redirectRequired": true
  },
  "message": "Redirect to OAuth provider",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "login-init-001"
}
```

### Token Refresh
```json
POST /api/refresh
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh_token_here...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  },
  "message": "Token refreshed successfully",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "token-refresh-001"
}
```

## ❌ Error Response Examples

### Missing Parameter
```json
POST /api/login (missing provider)
{
  "success": false,
  "data": null,
  "message": "Provider is required",
  "error": "MISSING_PARAMETER",
  "errorDetails": {
    "parameter": "provider",
    "supportedProviders": ["Google", "Facebook", "AzureB2C", "Apple"]
  },
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "login-error-001"
}
```

### Authentication Required
```json
GET /api/user (without authentication)
{
  "success": false,
  "data": null,
  "message": "Authentication required",
  "error": "UNAUTHORIZED",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "auth-required-001"
}
```

### Token Expired
```json
POST /api/refresh (with expired token)
{
  "success": false,
  "data": null,
  "message": "Invalid or expired refresh token. Please log in again.",
  "error": "TOKEN_EXPIRED",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "token-expired-001"
}
```

### Internal Server Error
```json
GET /api/auth-check (server error)
{
  "success": false,
  "data": null,
  "message": "Failed to check authentication status",
  "error": "INTERNAL_ERROR",
  "errorDetails": {
    "exception": "Database connection timeout"
  },
  "timestamp": "2024-01-15T10:30:45.123Z",
  "correlationId": "server-error-001"
}
```

## 🏷️ Standard Error Codes

EasyAuth uses consistent error codes across all endpoints:

### Authentication Errors
- `UNAUTHORIZED` - Authentication required
- `INVALID_CREDENTIALS` - Wrong username/password  
- `TOKEN_EXPIRED` - JWT token has expired
- `TOKEN_INVALID` - JWT token is malformed
- `SESSION_EXPIRED` - User session has expired

### Authorization Errors  
- `FORBIDDEN` - Access denied
- `INSUFFICIENT_PERMISSIONS` - User lacks required permissions

### Validation Errors
- `VALIDATION_ERROR` - Input validation failed
- `INVALID_REQUEST` - Malformed request
- `MISSING_PARAMETER` - Required parameter missing
- `INVALID_PARAMETER` - Parameter value is invalid

### Provider Errors
- `PROVIDER_ERROR` - OAuth provider returned error
- `PROVIDER_UNAVAILABLE` - OAuth provider is down
- `INVALID_PROVIDER` - Unsupported provider specified
- `PROVIDER_CONFIGURATION_ERROR` - Provider misconfigured

### System Errors
- `INTERNAL_ERROR` - Unexpected server error
- `SERVICE_UNAVAILABLE` - Service temporarily unavailable
- `DATABASE_ERROR` - Database operation failed
- `NETWORK_ERROR` - External network call failed

## 🔍 Request Tracing

Every response includes a `correlationId` for request tracing:

```javascript
// Frontend: Send correlation ID with requests
fetch('/api/auth-check', {
    headers: {
        'X-Correlation-ID': 'frontend-req-123'
    }
});

// Response will include the same correlation ID
{
    "success": true,
    "correlationId": "frontend-req-123",
    // ... rest of response
}
```

## 📱 Frontend Integration Examples

### React/TypeScript
```typescript
interface ApiResponse<T = any> {
    success: boolean;
    data?: T;
    message?: string;
    error?: string;
    errorDetails?: any;
    timestamp: string;
    correlationId?: string;
    version: string;
}

// Type-safe API calls
const checkAuth = async (): Promise<ApiResponse<AuthStatus>> => {
    const response = await fetch('/api/auth-check');
    return response.json();
};

// Usage
const authResult = await checkAuth();
if (authResult.success) {
    console.log('User:', authResult.data?.user);
} else {
    console.error('Auth error:', authResult.error, authResult.message);
}
```

### Vue.js Composable
```javascript
export function useEasyAuth() {
    const checkAuthentication = async () => {
        try {
            const response = await $fetch('/api/auth-check');
            
            if (response.success) {
                return {
                    isAuthenticated: response.data.isAuthenticated,
                    user: response.data.user
                };
            } else {
                throw new Error(response.message || 'Authentication check failed');
            }
        } catch (error) {
            console.error('Auth check error:', error);
            return { isAuthenticated: false, user: null };
        }
    };

    return { checkAuthentication };
}
```

### Angular Service
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
    constructor(private http: HttpClient) {}

    checkAuthStatus(): Observable<AuthStatus> {
        return this.http.get<ApiResponse<AuthStatus>>('/api/auth-check')
            .pipe(
                map(response => {
                    if (response.success) {
                        return response.data!;
                    }
                    throw new Error(response.message || 'Auth check failed');
                })
            );
    }
}
```

## 🎨 Error Handling Best Practices

### Generic Error Handler
```javascript
function handleApiError(response) {
    const { error, message, errorDetails, correlationId } = response;
    
    switch (error) {
        case 'UNAUTHORIZED':
        case 'TOKEN_EXPIRED':
        case 'SESSION_EXPIRED':
            // Redirect to login
            window.location.href = '/login';
            break;
            
        case 'FORBIDDEN':
            showNotification('Access denied', 'error');
            break;
            
        case 'VALIDATION_ERROR':
            // Show field-specific errors
            if (errorDetails) {
                showFieldErrors(errorDetails);
            }
            break;
            
        case 'PROVIDER_UNAVAILABLE':
            showNotification('Login service temporarily unavailable', 'warning');
            break;
            
        default:
            showNotification(message || 'An unexpected error occurred', 'error');
            console.error(`API Error [${correlationId}]:`, error, errorDetails);
    }
}
```

## 🔧 Migration from Inconsistent APIs

### Before (Inconsistent)
```javascript
// Different endpoints returned different formats
const authCheck = await fetch('/api/auth-check');
// Returns: { authenticated: true, userInfo: {...} }

const login = await fetch('/api/login', { ... });
// Returns: { success: true, redirect_url: '...' }

const profile = await fetch('/api/profile');
// Returns: { user: {...}, status: 'ok' }
```

### After (Consistent)
```javascript
// All endpoints use the same format
const authResult = await fetch('/api/auth-check').then(r => r.json());
// Returns: { success: true, data: { isAuthenticated: true, user: {...} }, ... }

const loginResult = await fetch('/api/login', { ... }).then(r => r.json());
// Returns: { success: true, data: { authUrl: '...', provider: '...' }, ... }

const profileResult = await fetch('/api/user').then(r => r.json());
// Returns: { success: true, data: { id: '...', email: '...' }, ... }

// Same error handling for all endpoints
if (!result.success) {
    handleApiError(result);
}
```

## ✨ Benefits

1. **Predictable Structure**: Every response follows the same format
2. **Better Error Handling**: Consistent error codes and messages
3. **Request Tracing**: Built-in correlation IDs for debugging
4. **Type Safety**: Easy to create TypeScript interfaces
5. **Frontend Simplification**: Single error handling strategy
6. **Debugging**: Structured error details with correlation IDs
7. **Monitoring**: Consistent logging and metrics collection

This unified approach eliminates the API response confusion that plagued QuizGenerator and provides a rock-solid foundation for any frontend application!