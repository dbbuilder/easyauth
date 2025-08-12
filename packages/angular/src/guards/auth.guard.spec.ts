import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { of } from 'rxjs';

import { AuthGuard } from './auth.guard';
import { EasyAuthService } from '../services/auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<EasyAuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('EasyAuthService', [], {
      state$: of({
        isLoading: false,
        isAuthenticated: true,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null
      })
    });

    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: EasyAuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(EasyAuthService) as jasmine.SpyObj<EasyAuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should allow access when authenticated', (done) => {
    const route = new ActivatedRouteSnapshot();
    const state = { url: '/protected' } as RouterStateSnapshot;

    guard.canActivate(route, state).subscribe(result => {
      expect(result).toBe(true);
      done();
    });
  });

  it('should deny access and redirect when not authenticated', (done) => {
    Object.defineProperty(authService, 'state$', {
      writable: true,
      value: of({
        isLoading: false,
        isAuthenticated: false,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null
      })
    });

    const route = new ActivatedRouteSnapshot();
    const state = { url: '/protected' } as RouterStateSnapshot;

    guard.canActivate(route, state).subscribe(result => {
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
      done();
    });
  });

  it('should use custom login URL from route data', (done) => {
    Object.defineProperty(authService, 'state$', {
      writable: true,
      value: of({
        isLoading: false,
        isAuthenticated: false,
        user: null,
        error: null,
        tokenExpiry: null,
        sessionId: null
      })
    });

    const route = new ActivatedRouteSnapshot();
    route.data = { loginUrl: '/custom-login' };
    const state = { url: '/protected' } as RouterStateSnapshot;

    guard.canActivate(route, state).subscribe(result => {
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/custom-login']);
      done();
    });
  });
});