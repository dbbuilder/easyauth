import { Component, Input, Output, EventEmitter } from '@angular/core';
import { EasyAuthService } from '../services/auth.service';
import { LoginResult } from '../models';

@Component({
  selector: 'easy-login-button',
  template: `
    <button 
      [disabled]="isLoading" 
      [class]="buttonClass"
      (click)="handleLogin()"
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
export class LoginButtonComponent {
  @Input() provider: string = 'google';
  @Input() returnUrl?: string;
  @Input() buttonText: string = 'Sign In';
  @Input() loadingText: string = 'Signing in...';
  @Input() buttonClass: string = 'easy-auth-btn easy-auth-btn-primary';
  @Input() disabled: boolean = false;
  @Input() loadingSpinner: string = '⏳';

  @Output() loginStart = new EventEmitter<{ provider: string; returnUrl?: string }>();
  @Output() loginSuccess = new EventEmitter<LoginResult>();
  @Output() loginError = new EventEmitter<Error>();

  isLoading = false;

  constructor(private authService: EasyAuthService) {}

  async handleLogin(): Promise<void> {
    if (this.disabled || this.isLoading) {
      return;
    }

    try {
      this.isLoading = true;
      this.loginStart.emit({ provider: this.provider, returnUrl: this.returnUrl });

      const result = await this.authService.login(this.provider, this.returnUrl).toPromise();
      this.loginSuccess.emit(result);
    } catch (error) {
      this.loginError.emit(error instanceof Error ? error : new Error('Login failed'));
    } finally {
      this.isLoading = false;
    }
  }
}