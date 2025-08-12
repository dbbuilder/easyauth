import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { EasyAuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate, CanActivateChild {
  constructor(
    private authService: EasyAuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkAuthentication(route, state);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkAuthentication(childRoute, state);
  }

  private checkAuthentication(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.authService.state$.pipe(
      take(1),
      map(authState => {
        if (authState.isLoading) {
          // If still loading, assume not authenticated for security
          return false;
        }

        if (authState.isAuthenticated) {
          return true;
        }

        // Store the attempted URL for redirecting after login
        sessionStorage.setItem('easyauth_redirect_url', state.url);
        
        // Redirect to login or trigger login flow
        const loginUrl = route.data?.['loginUrl'] || '/login';
        this.router.navigate([loginUrl]);
        
        return false;
      })
    );
  }
}