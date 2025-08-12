import { Pipe, PipeTransform } from '@angular/core';
import { EasyAuthService } from '../services/auth.service';

@Pipe({
  name: 'hasPermission',
  pure: false // Makes it impure to react to auth state changes
})
export class HasPermissionPipe implements PipeTransform {
  constructor(private authService: EasyAuthService) {}

  transform(permission: string | string[], requireAll: boolean = false): boolean {
    if (!this.authService.currentState.isAuthenticated) {
      return false;
    }

    if (Array.isArray(permission)) {
      return requireAll 
        ? permission.every(p => this.authService.hasPermission(p))
        : this.authService.hasAnyPermission(permission);
    }

    return this.authService.hasPermission(permission);
  }
}