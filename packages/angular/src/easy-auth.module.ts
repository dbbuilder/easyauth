import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

// Services
import { EasyAuthService } from './services/auth.service';
import { AuthApiService } from './services/auth-api.service';
import { AuthStorageService } from './services/auth-storage.service';

// Guards
import { AuthGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';

// Components
import { LoginButtonComponent } from './components/login-button.component';
import { LogoutButtonComponent } from './components/logout-button.component';
import { UserProfileComponent } from './components/user-profile.component';

// Pipes
import { HasRolePipe } from './pipes/has-role.pipe';
import { HasPermissionPipe } from './pipes/has-permission.pipe';

// Models
import { EasyAuthConfig } from './models';

@NgModule({
  declarations: [
    LoginButtonComponent,
    LogoutButtonComponent,
    UserProfileComponent,
    HasRolePipe,
    HasPermissionPipe
  ],
  imports: [
    CommonModule,
    HttpClientModule
  ],
  exports: [
    LoginButtonComponent,
    LogoutButtonComponent,
    UserProfileComponent,
    HasRolePipe,
    HasPermissionPipe
  ],
  providers: [
    EasyAuthService,
    AuthApiService,
    AuthStorageService,
    AuthGuard,
    RoleGuard
  ]
})
export class EasyAuthModule {
  static forRoot(config?: Partial<EasyAuthConfig>): ModuleWithProviders<EasyAuthModule> {
    return {
      ngModule: EasyAuthModule,
      providers: [
        EasyAuthService,
        AuthApiService,
        AuthStorageService,
        AuthGuard,
        RoleGuard,
        {
          provide: 'EASY_AUTH_CONFIG',
          useValue: config || {}
        }
      ]
    };
  }
}