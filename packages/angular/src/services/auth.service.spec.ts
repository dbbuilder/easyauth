import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { EasyAuthService } from './auth.service';
import { AuthApiService } from './auth-api.service';
import { AuthStorageService } from './auth-storage.service';

describe('EasyAuthService', () => {
  let service: EasyAuthService;
  let authApiService: jasmine.SpyObj<AuthApiService>;
  let storageService: jasmine.SpyObj<AuthStorageService>;

  beforeEach(() => {
    const authApiSpy = jasmine.createSpyObj('AuthApiService', ['checkAuth', 'login', 'logout', 'refreshToken', 'getUserProfile', 'configure']);
    const storageSpy = jasmine.createSpyObj('AuthStorageService', ['getAccessToken', 'getRefreshToken', 'clear']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        EasyAuthService,
        { provide: AuthApiService, useValue: authApiSpy },
        { provide: AuthStorageService, useValue: storageSpy }
      ]
    });

    service = TestBed.inject(EasyAuthService);
    authApiService = TestBed.inject(AuthApiService) as jasmine.SpyObj<AuthApiService>;
    storageService = TestBed.inject(AuthStorageService) as jasmine.SpyObj<AuthStorageService>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have initial state', () => {
    const initialState = service.currentState;
    expect(initialState.isLoading).toBe(true);
    expect(initialState.isAuthenticated).toBe(false);
    expect(initialState.user).toBeNull();
    expect(initialState.error).toBeNull();
  });

  it('should configure auth api service when configured', () => {
    const config = { baseUrl: 'https://test.example.com', debug: true };
    
    service.configure(config);
    
    expect(authApiService.configure).toHaveBeenCalledWith(config);
  });

  it('should clear error when clearError is called', () => {
    // Set initial error state
    (service as any).updateState({ error: 'Test error' });
    expect(service.currentState.error).toBe('Test error');
    
    service.clearError();
    
    expect(service.currentState.error).toBeNull();
  });

  it('should check if user has role', () => {
    const mockUser = {
      id: '1',
      roles: ['admin', 'user'],
      isVerified: true
    };

    (service as any).updateState({ 
      isAuthenticated: true, 
      user: mockUser 
    });

    expect(service.hasRole('admin')).toBe(true);
    expect(service.hasRole('moderator')).toBe(false);
  });

  it('should check if user has any of multiple roles', () => {
    const mockUser = {
      id: '1',
      roles: ['user'],
      isVerified: true
    };

    (service as any).updateState({ 
      isAuthenticated: true, 
      user: mockUser 
    });

    expect(service.hasAnyRole(['admin', 'user'])).toBe(true);
    expect(service.hasAnyRole(['admin', 'moderator'])).toBe(false);
  });

  it('should return false for role checks when not authenticated', () => {
    (service as any).updateState({ 
      isAuthenticated: false, 
      user: null 
    });

    expect(service.hasRole('admin')).toBe(false);
    expect(service.hasAnyRole(['admin', 'user'])).toBe(false);
  });
});