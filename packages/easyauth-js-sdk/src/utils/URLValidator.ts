/**
 * URL validation utilities
 */

export class URLValidator {
  private static readonly ALLOWED_PROTOCOLS = ['http:', 'https:'];
  private static readonly LOCALHOST_PATTERNS = [
    'localhost',
    '127.0.0.1',
    '::1',
  ];

  /**
   * Validate if a URL is properly formatted and secure
   */
  public static isValid(url: string): boolean {
    try {
      // Basic validation
      if (!url || typeof url !== 'string' || url.length === 0) {
        return false;
      }

      const parsed = new URL(url);
      
      // Check protocol
      if (!this.ALLOWED_PROTOCOLS.includes(parsed.protocol)) {
        return false;
      }

      // Check that hostname is not empty (catches malformed URLs like "http://")
      if (!parsed.hostname || parsed.hostname.length === 0) {
        return false;
      }

      // Check for obvious malformed patterns
      if (parsed.hostname === '.' || parsed.hostname === '..') {
        return false;
      }

      // In production, enforce HTTPS except for localhost
      if (this.isProduction() && parsed.protocol === 'http:') {
        return this.isLocalhost(parsed.hostname);
      }

      return true;
    } catch {
      return false;
    }
  }

  /**
   * Check if URL is using HTTPS
   */
  public static isSecure(url: string): boolean {
    try {
      const parsed = new URL(url);
      return parsed.protocol === 'https:';
    } catch {
      return false;
    }
  }

  /**
   * Check if URL is localhost
   */
  public static isLocalhost(hostname: string): boolean {
    return this.LOCALHOST_PATTERNS.includes(hostname.toLowerCase());
  }

  /**
   * Validate redirect URI against allowed patterns
   */
  public static isAllowedRedirect(url: string, allowedOrigins?: string[]): boolean {
    if (!this.isValid(url)) {
      return false;
    }

    if (!allowedOrigins || allowedOrigins.length === 0) {
      return true;
    }

    try {
      const parsed = new URL(url);
      const origin = parsed.origin;

      return allowedOrigins.some(allowedOrigin => {
        // Exact match
        if (origin === allowedOrigin) {
          return true;
        }

        // Wildcard subdomain match (e.g., "https://*.example.com")
        if (allowedOrigin.includes('*')) {
          const pattern = allowedOrigin.replace(/\*/g, '.*');
          const regex = new RegExp(`^${pattern}$`);
          return regex.test(origin);
        }

        return false;
      });
    } catch {
      return false;
    }
  }

  /**
   * Extract domain from URL
   */
  public static getDomain(url: string): string | null {
    try {
      // Basic validation first
      if (!url || typeof url !== 'string' || url.length === 0) {
        return null;
      }

      const parsed = new URL(url);
      
      // Return null for invalid/malformed hostnames
      if (!parsed.hostname || parsed.hostname.length === 0 || 
          parsed.hostname === '.' || parsed.hostname === '..') {
        return null;
      }

      return parsed.hostname;
    } catch {
      return null;
    }
  }

  /**
   * Check if we're in production environment
   */
  private static isProduction(): boolean {
    // Check Node.js environment first
    if (typeof process !== 'undefined' && process && process.env && process.env.NODE_ENV) {
      return process.env.NODE_ENV === 'production';
    }

    // Browser heuristic - check for location in different scopes
    let loc = null;
    
    // Try window.location first (browser)
    if (typeof window !== 'undefined' && window.location) {
      loc = window.location;
    }
    // Try global location (browser globals)
    else if (typeof location !== 'undefined') {
      loc = location;
    }
    // Try global.location (test environment)
    else if (typeof global !== 'undefined' && (global as any).location) {
      loc = (global as any).location;
    }
                
    if (loc && loc.protocol && loc.hostname) {
      return loc.protocol === 'https:' && !this.isLocalhost(loc.hostname);
    }

    return false;
  }

  /**
   * Normalize URL by removing trailing slashes and fragments
   */
  public static normalize(url: string): string {
    try {
      const parsed = new URL(url);
      parsed.hash = ''; // Remove fragment
      
      let normalized = parsed.toString();
      
      // Remove trailing slash from pathname (except root)
      if (parsed.pathname.length > 1 && parsed.pathname.endsWith('/')) {
        normalized = normalized.replace(/\/$/, '');
      }
      
      return normalized;
    } catch {
      return url;
    }
  }

  /**
   * Check if URL has suspicious patterns that might indicate phishing
   */
  public static isSuspicious(url: string): boolean {
    try {
      // Basic validation - invalid URLs are suspicious
      if (!url || typeof url !== 'string' || url.length === 0) {
        return true;
      }

      const parsed = new URL(url);
      
      // Invalid or malformed URLs are suspicious
      if (!parsed.hostname || parsed.hostname.length === 0 || 
          parsed.hostname === '.' || parsed.hostname === '..') {
        return true;
      }

      // Invalid protocols are suspicious
      if (!this.ALLOWED_PROTOCOLS.includes(parsed.protocol)) {
        return true;
      }

      const hostname = parsed.hostname.toLowerCase();

      // Check for suspicious patterns
      const suspiciousPatterns = [
        // Homograph attacks
        /[а-я]/i, // Cyrillic characters
        /[α-ω]/i, // Greek characters
        
        // Suspicious TLDs (extend as needed)
        /\.(tk|ml|ga|cf)$/,
        
        // Multiple subdomains that look like legitimate sites
        /\b(google|apple|facebook|microsoft|amazon)\b.*\b(google|apple|facebook|microsoft|amazon)\b/,
        
        // IP addresses instead of domains (sometimes suspicious)
        /^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/,
      ];

      return suspiciousPatterns.some(pattern => pattern.test(hostname));
    } catch {
      return true; // Invalid URLs are suspicious
    }
  }
}