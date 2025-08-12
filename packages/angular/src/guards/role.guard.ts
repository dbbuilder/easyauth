import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { EasyAuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate, CanActivateChild {
  constructor(
    private authService: EasyAuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkRoles(route, state);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.checkRoles(childRoute, state);
  }

  private checkRoles(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.authService.state$.pipe(
      take(1),
      map(authState => {
        if (authState.isLoading) {
          return false;
        }

        if (!authState.isAuthenticated) {
          // Store the attempted URL for redirecting after login
          sessionStorage.setItem('easyauth_redirect_url', state.url);
          
          const loginUrl = route.data?.['loginUrl'] || '/login';
          this.router.navigate([loginUrl]);
          return false;
        }

        const requiredRoles = route.data?.['roles'] as string[];
        const requiredPermissions = route.data?.['permissions'] as string[];
        const requireAll = route.data?.['requireAll'] === true; // Default is 'any'

        // Check roles
        if (requiredRoles && requiredRoles.length > 0) {
          const hasRequiredRoles = requireAll 
            ? requiredRoles.every(role => this.authService.hasRole(role))
            : this.authService.hasAnyRole(requiredRoles);

          if (!hasRequiredRoles) {
            const unauthorizedUrl = route.data?.['unauthorizedUrl'] || '/unauthorized';
            this.router.navigate([unauthorizedUrl]);
            return false;
          }
        }

        // Check permissions
        if (requiredPermissions && requiredPermissions.length > 0) {
          const hasRequiredPermissions = requireAll
            ? requiredPermissions.every(permission => this.authService.hasPermission(permission))
            : this.authService.hasAnyPermission(requiredPermissions);

          if (!hasRequiredPermissions) {
            const unauthorizedUrl = route.data?.['unauthorizedUrl'] || '/unauthorized';
            this.router.navigate([unauthorizedUrl]);
            return false;
          }
        }

        return true;
      })
    );
  }
}