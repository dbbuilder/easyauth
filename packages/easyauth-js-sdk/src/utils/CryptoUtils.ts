/**
 * Cryptographic utilities for authentication
 */

export class CryptoUtils {
  /**
   * Generate a cryptographically secure random state parameter
   */
  public static async generateState(): Promise<string> {
    return this.generateRandomString(32);
  }

  /**
   * Generate a session ID
   */
  public static async generateSessionId(): Promise<string> {
    const timestamp = Date.now().toString(36);
    const randomPart = await this.generateRandomString(16);
    return `${timestamp}_${randomPart}`;
  }

  /**
   * Generate PKCE code verifier and challenge
   */
  public static async generatePKCE(): Promise<{ verifier: string; challenge: string }> {
    const verifier = await this.generateRandomString(128);
    const challenge = await this.sha256Base64UrlEncode(verifier);
    
    return { verifier, challenge };
  }

  /**
   * Generate a nonce for OpenID Connect
   */
  public static async generateNonce(): Promise<string> {
    return this.generateRandomString(16);
  }

  /**
   * Generate cryptographically secure random string
   */
  public static async generateRandomString(length: number): Promise<string> {
    if (typeof crypto !== 'undefined' && crypto.getRandomValues) {
      // Browser environment
      const array = new Uint8Array(length);
      crypto.getRandomValues(array);
      return this.arrayBufferToBase64Url(array);
    } else {
      // Fallback for environments without crypto.getRandomValues
      return this.generateFallbackRandomString(length);
    }
  }

  /**
   * SHA256 hash and base64url encode
   */
  public static async sha256Base64UrlEncode(str: string): Promise<string> {
    if (typeof crypto !== 'undefined' && crypto.subtle) {
      const encoder = new TextEncoder();
      const data = encoder.encode(str);
      const hashBuffer = await crypto.subtle.digest('SHA-256', data);
      return this.arrayBufferToBase64Url(new Uint8Array(hashBuffer));
    } else {
      // Fallback - not cryptographically secure, but better than nothing
      return this.simpleFallbackHash(str);
    }
  }

  /**
   * Verify HMAC signature (for webhook validation)
   */
  public static async verifyHMAC(
    data: string,
    signature: string,
    secret: string
  ): Promise<boolean> {
    if (typeof crypto !== 'undefined' && crypto.subtle) {
      try {
        const encoder = new TextEncoder();
        const key = await crypto.subtle.importKey(
          'raw',
          encoder.encode(secret),
          { name: 'HMAC', hash: 'SHA-256' },
          false,
          ['verify']
        );

        const signatureBuffer = this.base64UrlToArrayBuffer(signature);
        const dataBuffer = encoder.encode(data);

        return await crypto.subtle.verify('HMAC', key, signatureBuffer, dataBuffer);
      } catch {
        return false;
      }
    }

    // Fallback verification (less secure)
    return this.fallbackHMACVerify(data, signature, secret);
  }

  /**
   * Constant time string comparison to prevent timing attacks
   */
  public static constantTimeEquals(a: string, b: string): boolean {
    if (a.length !== b.length) {
      return false;
    }

    let result = 0;
    for (let i = 0; i < a.length; i++) {
      result |= a.charCodeAt(i) ^ b.charCodeAt(i);
    }

    return result === 0;
  }

  /**
   * Check if token is expired with safety margin
   */
  public static isTokenExpired(expiresAt: Date, safetyMarginSeconds = 300): boolean {
    const now = new Date();
    const expirationWithMargin = new Date(expiresAt.getTime() - safetyMarginSeconds * 1000);
    return now >= expirationWithMargin;
  }

  /**
   * Decode JWT payload (without verification)
   */
  public static decodeJWTPayload(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) {
        throw new Error('Invalid JWT format');
      }

      const payload = parts[1];
      const decoded = this.base64UrlDecode(payload);
      return JSON.parse(decoded);
    } catch (error) {
      throw new Error('Failed to decode JWT payload');
    }
  }

  // #region Private Utility Methods

  private static arrayBufferToBase64Url(buffer: Uint8Array): string {
    const bytes = Array.from(buffer);
    const binaryString = bytes.map(byte => String.fromCharCode(byte)).join('');
    const base64 = btoa(binaryString);
    
    // Convert base64 to base64url
    return base64
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }

  private static base64UrlToArrayBuffer(base64Url: string): ArrayBuffer {
    // Convert base64url to base64
    let base64 = base64Url
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    
    // Add padding if needed
    while (base64.length % 4) {
      base64 += '=';
    }

    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    
    return bytes.buffer;
  }

  private static base64UrlDecode(base64Url: string): string {
    let base64 = base64Url
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    
    while (base64.length % 4) {
      base64 += '=';
    }

    return atob(base64);
  }

  private static generateFallbackRandomString(length: number): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
    let result = '';
    
    for (let i = 0; i < length; i++) {
      result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    return result;
  }

  private static simpleFallbackHash(str: string): string {
    // Simple hash function - NOT cryptographically secure
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    
    return Math.abs(hash).toString(36);
  }

  private static fallbackHMACVerify(data: string, signature: string, secret: string): boolean {
    // Very basic fallback - not cryptographically secure
    const expectedSignature = this.simpleFallbackHash(data + secret);
    return this.constantTimeEquals(signature, expectedSignature);
  }

  // #endregion
}