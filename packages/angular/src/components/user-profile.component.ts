import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { EasyAuthService } from '../services/auth.service';
import { UserInfo } from '../models';

@Component({
  selector: 'easy-user-profile',
  template: `
    <div class="easy-user-profile" *ngIf="user$ | async as user">
      <!-- Avatar/Profile Picture -->
      <div class="profile-avatar" *ngIf="showAvatar">
        <img 
          *ngIf="user.profilePicture; else avatarFallback" 
          [src]="user.profilePicture" 
          [alt]="user.name || user.email || 'User'"
          class="avatar-image"
        />
        <ng-template #avatarFallback>
          <div class="avatar-fallback">
            {{ getInitials(user) }}
          </div>
        </ng-template>
      </div>

      <!-- User Information -->
      <div class="profile-info">
        <div class="user-name" *ngIf="showName && (user.name || user.firstName)">
          {{ user.name || (user.firstName + ' ' + (user.lastName || '')) }}
        </div>
        
        <div class="user-email" *ngIf="showEmail && user.email">
          {{ user.email }}
        </div>
        
        <div class="user-roles" *ngIf="showRoles && user.roles?.length">
          <span class="role-badge" *ngFor="let role of user.roles">
            {{ role }}
          </span>
        </div>
        
        <div class="user-provider" *ngIf="showProvider && user.provider">
          <span class="provider-text">via {{ user.provider }}</span>
        </div>
        
        <div class="user-status" *ngIf="showStatus">
          <span 
            class="status-badge" 
            [class.verified]="user.isVerified"
            [class.unverified]="!user.isVerified"
          >
            {{ user.isVerified ? 'Verified' : 'Unverified' }}
          </span>
        </div>

        <div class="last-login" *ngIf="showLastLogin && user.lastLogin">
          <small>Last login: {{ formatDate(user.lastLogin) }}</small>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div class="easy-user-profile-loading" *ngIf="(authService.isLoading$ | async)">
      <div class="loading-placeholder">
        <div class="loading-avatar"></div>
        <div class="loading-text">
          <div class="loading-line"></div>
          <div class="loading-line short"></div>
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div class="easy-user-profile-error" *ngIf="(authService.error$ | async) as error">
      <div class="error-message">
        Failed to load user profile: {{ error }}
      </div>
    </div>
  `,
  styles: [`
    .easy-user-profile {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
    }

    .profile-avatar {
      flex-shrink: 0;
    }

    .avatar-image {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      object-fit: cover;
    }

    .avatar-fallback {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #007bff;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      font-size: 14px;
    }

    .profile-info {
      flex: 1;
      min-width: 0;
    }

    .user-name {
      font-weight: 600;
      font-size: 16px;
      color: #333;
      margin-bottom: 4px;
    }

    .user-email {
      font-size: 14px;
      color: #666;
      margin-bottom: 8px;
    }

    .user-roles {
      margin-bottom: 6px;
    }

    .role-badge {
      display: inline-block;
      background: #f0f0f0;
      color: #333;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 12px;
      margin-right: 6px;
      margin-bottom: 4px;
    }

    .provider-text {
      font-size: 12px;
      color: #888;
      font-style: italic;
    }

    .status-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
    }

    .status-badge.verified {
      background: #d4edda;
      color: #155724;
    }

    .status-badge.unverified {
      background: #f8d7da;
      color: #721c24;
    }

    .last-login {
      margin-top: 8px;
      font-size: 12px;
      color: #888;
    }

    /* Loading States */
    .easy-user-profile-loading {
      padding: 12px;
    }

    .loading-placeholder {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .loading-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #f0f0f0;
      animation: pulse 1.5s ease-in-out infinite;
    }

    .loading-text {
      flex: 1;
    }

    .loading-line {
      height: 12px;
      background: #f0f0f0;
      border-radius: 6px;
      margin-bottom: 8px;
      animation: pulse 1.5s ease-in-out infinite;
    }

    .loading-line.short {
      width: 60%;
    }

    .error-message {
      color: #dc3545;
      font-size: 14px;
      padding: 12px;
      background: #f8d7da;
      border-radius: 4px;
    }

    @keyframes pulse {
      0% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
      100% {
        opacity: 1;
      }
    }
  `]
})
export class UserProfileComponent implements OnInit, OnDestroy {
  @Input() showAvatar: boolean = true;
  @Input() showName: boolean = true;
  @Input() showEmail: boolean = true;
  @Input() showRoles: boolean = false;
  @Input() showProvider: boolean = false;
  @Input() showStatus: boolean = false;
  @Input() showLastLogin: boolean = false;

  user$: Observable<UserInfo | null>;
  private subscription?: Subscription;

  constructor(public authService: EasyAuthService) {
    this.user$ = this.authService.user$;
  }

  ngOnInit(): void {
    // Refresh user profile on component init if authenticated
    this.subscription = this.authService.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.authService.getUserProfile().subscribe();
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  getInitials(user: UserInfo): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
    }
    if (user.name) {
      const parts = user.name.split(' ');
      if (parts.length >= 2) {
        return `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase();
      }
      return user.name.charAt(0).toUpperCase();
    }
    if (user.email) {
      return user.email.charAt(0).toUpperCase();
    }
    return '?';
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}