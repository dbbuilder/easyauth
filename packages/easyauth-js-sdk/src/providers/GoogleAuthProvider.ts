/**
 * Google OAuth 2.0 Authentication Provider
 * Implements Google Sign-In with OAuth 2.0 and PKCE support
 */

import {
  IAuthProvider,
  AuthorizationRequest,
  TokenExchangeResult,
  ProviderUserInfo,
  ProviderHealthCheck,
  GoogleConfig
} from '../types/providers';
import { CryptoUtils } from '../utils/CryptoUtils';

export class GoogleAuthProvider implements IAuthProvider {
  public readonly name = 'google' as const;
  public readonly displayName = 'Google';
  
  private readonly config: GoogleConfig;
  private readonly authBaseUrl = 'https://accounts.google.com/o/oauth2/v2/auth';
  private readonly tokenEndpoint = 'https://oauth2.googleapis.com/token';
  private readonly userInfoEndpoint = 'https://www.googleapis.com/oauth2/v2/userinfo';
  private readonly revokeEndpoint = 'https://oauth2.googleapis.com/revoke';
  private readonly discoveryEndpoint = 'https://accounts.google.com/.well-known/openid_configuration';
  
  private readonly defaultScopes = ['openid', 'profile', 'email'];
  
  constructor(config: GoogleConfig) {
    // Basic validation in constructor
    if (!config.clientId) {
      throw new Error('Google client ID is required');
    }
    if (!config.redirectUri) {
      throw new Error('Redirect URI is required');
    }
    
    // Basic format validation - must end with googleusercontent.com
    if (!config.clientId.endsWith('.googleusercontent.com')) {
      throw new Error('Invalid Google client ID format');
    }
    
    this.config = { ...config };
  }

  public get isEnabled(): boolean {
    return this.config.enabled !== false;
  }


  /**
   * Validate configuration asynchronously
   */
  public async validateConfiguration(): Promise<boolean> {
    try {
      if (!this.config.clientId) {
        return false;
      }

      // Validate Google client ID format - should not start/end with special chars or have consecutive special chars
      const googleClientIdPattern = /^[a-zA-Z0-9]+([._-][a-zA-Z0-9]+)*\.googleusercontent\.com$/;
      if (!googleClientIdPattern.test(this.config.clientId)) {
        return false;
      }

      if (!this.config.redirectUri) {
        return false;
      }
      
      // Additional async validation could go here
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Generate authorization URL with PKCE support
   */
  public async getAuthorizationUrl(request: AuthorizationRequest): Promise<string> {
    const params = new URLSearchParams();
    
    // Required OAuth 2.0 parameters
    params.append('client_id', this.config.clientId);
    params.append('redirect_uri', this.config.redirectUri);
    params.append('response_type', 'code');
    params.append('scope', this.getScopes(request.scopes).join(' '));
    params.append('state', request.state);
    
    // PKCE parameters
    if (request.pkceChallenge) {
      params.append('code_challenge', request.pkceChallenge);
      params.append('code_challenge_method', 'S256');
    }
    
    // Optional parameters
    if (request.nonce) {
      params.append('nonce', request.nonce);
    }
    
    // Custom parameters (Google-specific)
    if (request.customParams) {
      Object.entries(request.customParams).forEach(([key, value]) => {
        params.append(key, value);
      });
    }
    
    // Use proper percent encoding instead of + for spaces
    return `${this.authBaseUrl}?${params.toString().replace(/\+/g, '%20')}`;
  }

  /**
   * Exchange authorization code for tokens
   */
  public async exchangeCodeForTokens(code: string, state: string): Promise<TokenExchangeResult> {
    try {
      const body = new URLSearchParams();
      body.append('grant_type', 'authorization_code');
      body.append('code', code);
      body.append('client_id', this.config.clientId);
      body.append('redirect_uri', this.config.redirectUri);
      
      if (this.config.clientSecret) {
        body.append('client_secret', this.config.clientSecret);
      }

      const response = await fetch(this.tokenEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: body.toString(),
      });

      const data = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: data.error || 'Token exchange failed',
        };
      }

      return {
        success: true,
        accessToken: data.access_token,
        refreshToken: data.refresh_token,
        idToken: data.id_token,
        tokenType: data.token_type,
        expiresIn: data.expires_in,
        scope: data.scope,
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error during token exchange',
      };
    }
  }

  /**
   * Get user information using access token
   */
  public async getUserInfo(accessToken: string): Promise<ProviderUserInfo> {
    const response = await fetch(this.userInfoEndpoint, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user info');
    }

    const data = await response.json();

    return {
      id: data.sub,
      email: data.email,
      emailVerified: data.email_verified,
      name: data.name,
      givenName: data.given_name,
      familyName: data.family_name,
      profilePictureUrl: data.picture,
      locale: data.locale,
      provider: 'google',
    };
  }

  /**
   * Refresh access token using refresh token
   */
  public async refreshTokens(refreshToken: string): Promise<TokenExchangeResult> {
    try {
      const body = new URLSearchParams();
      body.append('grant_type', 'refresh_token');
      body.append('refresh_token', refreshToken);
      body.append('client_id', this.config.clientId);
      
      if (this.config.clientSecret) {
        body.append('client_secret', this.config.clientSecret);
      }

      const response = await fetch(this.tokenEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: body.toString(),
      });

      const data = await response.json();

      if (!response.ok) {
        return {
          success: false,
          error: data.error || 'Token refresh failed',
        };
      }

      return {
        success: true,
        accessToken: data.access_token,
        refreshToken: data.refresh_token || refreshToken, // Google may not return new refresh token
        tokenType: data.token_type,
        expiresIn: data.expires_in,
        scope: data.scope,
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error during token refresh',
      };
    }
  }

  /**
   * Revoke access and refresh tokens
   */
  public async revokeTokens(tokens: string[]): Promise<boolean> {
    try {
      const revokePromises = tokens.map(async (token) => {
        const response = await fetch(`${this.revokeEndpoint}?token=${encodeURIComponent(token)}`, {
          method: 'POST',
        });
        return response.ok;
      });

      const results = await Promise.all(revokePromises);
      return results.every(result => result);
    } catch {
      return false;
    }
  }

  /**
   * Check provider health status
   */
  public async getHealthStatus(): Promise<ProviderHealthCheck> {
    const startTime = Date.now();
    
    try {
      const response = await fetch(this.discoveryEndpoint);
      const responseTime = Math.max(1, Date.now() - startTime); // Ensure minimum 1ms
      
      if (response.ok) {
        return {
          provider: 'google',
          isHealthy: true,
          responseTime,
          status: 'Available',
        };
      } else {
        return {
          provider: 'google',
          isHealthy: false,
          responseTime,
          status: 'Degraded',
          error: `HTTP ${response.status}`,
        };
      }
    } catch (error) {
      const responseTime = Math.max(1, Date.now() - startTime); // Ensure minimum 1ms
      return {
        provider: 'google',
        isHealthy: false,
        responseTime,
        status: 'Unavailable',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Get effective scopes (default + requested)
   */
  private getScopes(requestedScopes?: string[]): string[] {
    if (requestedScopes && requestedScopes.length > 0) {
      return requestedScopes;
    }
    return this.config.scopes || this.defaultScopes;
  }
}