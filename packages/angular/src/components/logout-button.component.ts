import { Component, Input, Output, EventEmitter } from '@angular/core';
import { EasyAuthService } from '../services/auth.service';
import { LogoutResult } from '../models';

@Component({
  selector: 'easy-logout-button',
  template: `
    <button 
      [disabled]="isLoading" 
      [class]="buttonClass"
      (click)="handleLogout()"
    >
      <span *ngIf="isLoading" class="spinner" [innerHTML]="loadingSpinner"></span>
      <span [class.loading]="isLoading">{{ isLoading ? loadingText : buttonText }}</span>
    </button>
  `,
  styles: [`
    .spinner {
      display: inline-block;
      margin-right: 8px;
    }
    
    .loading {
      opacity: 0.7;
    }
    
    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }
  `]
})
export class LogoutButtonComponent {
  @Input() buttonText: string = 'Sign Out';
  @Input() loadingText: string = 'Signing out...';
  @Input() buttonClass: string = 'easy-auth-btn easy-auth-btn-secondary';
  @Input() disabled: boolean = false;
  @Input() loadingSpinner: string = '⏳';

  @Output() logoutStart = new EventEmitter<void>();
  @Output() logoutSuccess = new EventEmitter<LogoutResult>();
  @Output() logoutError = new EventEmitter<Error>();

  isLoading = false;

  constructor(private authService: EasyAuthService) {}

  async handleLogout(): Promise<void> {
    if (this.disabled || this.isLoading) {
      return;
    }

    try {
      this.isLoading = true;
      this.logoutStart.emit();

      const result = await this.authService.logout().toPromise();
      this.logoutSuccess.emit(result);
    } catch (error) {
      this.logoutError.emit(error instanceof Error ? error : new Error('Logout failed'));
    } finally {
      this.isLoading = false;
    }
  }
}