import { Pipe, PipeTransform } from '@angular/core';
import { EasyAuthService } from '../services/auth.service';

@Pipe({
  name: 'hasRole',
  pure: false // Makes it impure to react to auth state changes
})
export class HasRolePipe implements PipeTransform {
  constructor(private authService: EasyAuthService) {}

  transform(role: string | string[], requireAll: boolean = false): boolean {
    if (!this.authService.currentState.isAuthenticated) {
      return false;
    }

    if (Array.isArray(role)) {
      return requireAll 
        ? role.every(r => this.authService.hasRole(r))
        : this.authService.hasAnyRole(role);
    }

    return this.authService.hasRole(role);
  }
}