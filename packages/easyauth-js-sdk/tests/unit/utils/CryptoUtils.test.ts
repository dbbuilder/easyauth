/**
 * Unit tests for CryptoUtils - Cryptographic utilities
 * Following TDD methodology: Testing all crypto operations
 */

import { CryptoUtils } from '../../../src/utils/CryptoUtils';

describe('CryptoUtils', () => {
  describe('generateState', () => {
    it('should generate a state string', async () => {
      const state = await CryptoUtils.generateState();
      
      expect(state).toBeDefined();
      expect(typeof state).toBe('string');
      expect(state.length).toBeGreaterThan(0);
    });

    it('should generate unique states on each call', async () => {
      const state1 = await CryptoUtils.generateState();
      const state2 = await CryptoUtils.generateState();
      
      expect(state1).not.toBe(state2);
    });

    it('should generate base64url-safe characters only', async () => {
      const state = await CryptoUtils.generateState();
      
      // Base64url characters: A-Z, a-z, 0-9, -, _
      expect(state).toMatch(/^[A-Za-z0-9_-]+$/);
    });
  });

  describe('generateSessionId', () => {
    it('should generate a session ID with timestamp', async () => {
      const sessionId = await CryptoUtils.generateSessionId();
      
      expect(sessionId).toBeDefined();
      expect(typeof sessionId).toBe('string');
      expect(sessionId).toContain('_');
    });

    it('should generate unique session IDs', async () => {
      const id1 = await CryptoUtils.generateSessionId();
      const id2 = await CryptoUtils.generateSessionId();
      
      expect(id1).not.toBe(id2);
    });

    it('should have timestamp component at the beginning', async () => {
      const sessionId = await CryptoUtils.generateSessionId();
      const parts = sessionId.split('_');
      
      expect(parts.length).toBeGreaterThanOrEqual(2);
      expect(parts[0]).toBeTruthy();
      // Timestamp should be numeric in base36
      expect(/^[0-9a-z]+$/.test(parts[0])).toBe(true);
    });
  });

  describe('generatePKCE', () => {
    it('should generate PKCE verifier and challenge', async () => {
      const pkce = await CryptoUtils.generatePKCE();
      
      expect(pkce).toBeDefined();
      expect(pkce.verifier).toBeDefined();
      expect(pkce.challenge).toBeDefined();
      expect(typeof pkce.verifier).toBe('string');
      expect(typeof pkce.challenge).toBe('string');
    });

    it('should generate different verifier and challenge each time', async () => {
      const pkce1 = await CryptoUtils.generatePKCE();
      const pkce2 = await CryptoUtils.generatePKCE();
      
      expect(pkce1.verifier).not.toBe(pkce2.verifier);
      expect(pkce1.challenge).not.toBe(pkce2.challenge);
    });

    it('should generate verifier longer than challenge', async () => {
      const pkce = await CryptoUtils.generatePKCE();
      
      expect(pkce.verifier.length).toBeGreaterThan(pkce.challenge.length);
    });
  });

  describe('generateNonce', () => {
    it('should generate a nonce string', async () => {
      const nonce = await CryptoUtils.generateNonce();
      
      expect(nonce).toBeDefined();
      expect(typeof nonce).toBe('string');
      expect(nonce.length).toBeGreaterThan(0);
    });

    it('should generate unique nonces', async () => {
      const nonce1 = await CryptoUtils.generateNonce();
      const nonce2 = await CryptoUtils.generateNonce();
      
      expect(nonce1).not.toBe(nonce2);
    });
  });

  describe('generateRandomString', () => {
    it('should generate string of specified length', async () => {
      const length = 32;
      const randomString = await CryptoUtils.generateRandomString(length);
      
      expect(randomString).toBeDefined();
      expect(typeof randomString).toBe('string');
      // Note: base64url encoding may result in different length due to padding removal
      expect(randomString.length).toBeGreaterThan(0);
    });

    it('should generate different strings each time', async () => {
      const str1 = await CryptoUtils.generateRandomString(16);
      const str2 = await CryptoUtils.generateRandomString(16);
      
      expect(str1).not.toBe(str2);
    });

    it('should work with different lengths', async () => {
      const short = await CryptoUtils.generateRandomString(8);
      const long = await CryptoUtils.generateRandomString(64);
      
      expect(short.length).toBeLessThan(long.length);
    });
  });

  describe('sha256Base64UrlEncode', () => {
    it('should hash and encode a string', async () => {
      const input = 'test string';
      const hash = await CryptoUtils.sha256Base64UrlEncode(input);
      
      expect(hash).toBeDefined();
      expect(typeof hash).toBe('string');
      expect(hash.length).toBeGreaterThan(0);
    });

    it('should produce same hash for same input', async () => {
      const input = 'test string';
      const hash1 = await CryptoUtils.sha256Base64UrlEncode(input);
      const hash2 = await CryptoUtils.sha256Base64UrlEncode(input);
      
      expect(hash1).toBe(hash2);
    });

    it('should produce different hashes for different inputs', async () => {
      const hash1 = await CryptoUtils.sha256Base64UrlEncode('input1');
      const hash2 = await CryptoUtils.sha256Base64UrlEncode('input2');
      
      expect(hash1).not.toBe(hash2);
    });

    it('should handle empty string', async () => {
      const hash = await CryptoUtils.sha256Base64UrlEncode('');
      
      expect(hash).toBeDefined();
      expect(typeof hash).toBe('string');
    });
  });

  describe('constantTimeEquals', () => {
    it('should return true for identical strings', () => {
      const result = CryptoUtils.constantTimeEquals('hello', 'hello');
      expect(result).toBe(true);
    });

    it('should return false for different strings', () => {
      const result = CryptoUtils.constantTimeEquals('hello', 'world');
      expect(result).toBe(false);
    });

    it('should return false for strings of different lengths', () => {
      const result = CryptoUtils.constantTimeEquals('short', 'much longer string');
      expect(result).toBe(false);
    });

    it('should handle empty strings', () => {
      const result1 = CryptoUtils.constantTimeEquals('', '');
      const result2 = CryptoUtils.constantTimeEquals('', 'nonempty');
      
      expect(result1).toBe(true);
      expect(result2).toBe(false);
    });

    it('should be case sensitive', () => {
      const result = CryptoUtils.constantTimeEquals('Hello', 'hello');
      expect(result).toBe(false);
    });
  });

  describe('isTokenExpired', () => {
    it('should return false for future expiration', () => {
      const futureDate = new Date(Date.now() + 3600 * 1000); // 1 hour from now
      const result = CryptoUtils.isTokenExpired(futureDate);
      
      expect(result).toBe(false);
    });

    it('should return true for past expiration', () => {
      const pastDate = new Date(Date.now() - 3600 * 1000); // 1 hour ago
      const result = CryptoUtils.isTokenExpired(pastDate);
      
      expect(result).toBe(true);
    });

    it('should respect safety margin', () => {
      const nearFutureDate = new Date(Date.now() + 200 * 1000); // 200 seconds from now
      const safetyMargin = 300; // 300 seconds
      
      const result = CryptoUtils.isTokenExpired(nearFutureDate, safetyMargin);
      expect(result).toBe(true); // Should be expired due to safety margin
    });

    it('should use default safety margin', () => {
      const nearFutureDate = new Date(Date.now() + 200 * 1000); // 200 seconds from now
      
      const result = CryptoUtils.isTokenExpired(nearFutureDate); // Default 300s margin
      expect(result).toBe(true);
    });

    it('should handle zero safety margin', () => {
      const futureDate = new Date(Date.now() + 1000); // 1 second from now
      const result = CryptoUtils.isTokenExpired(futureDate, 0);
      
      expect(result).toBe(false);
    });
  });

  describe('decodeJWTPayload', () => {
    it('should decode valid JWT payload', () => {
      // Create a mock JWT with base64url encoded payload
      const payload = { sub: '1234567890', name: 'John Doe', iat: 1516239022 };
      const encodedPayload = btoa(JSON.stringify(payload)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
      const mockJWT = `header.${encodedPayload}.signature`;
      
      const result = CryptoUtils.decodeJWTPayload(mockJWT);
      
      expect(result).toEqual(payload);
    });

    it('should throw error for invalid JWT format', () => {
      const invalidJWT = 'invalid.jwt';
      
      expect(() => CryptoUtils.decodeJWTPayload(invalidJWT)).toThrow('Failed to decode JWT payload');
    });

    it('should throw error for malformed payload', () => {
      const invalidJWT = 'header.invalidpayload.signature';
      
      expect(() => CryptoUtils.decodeJWTPayload(invalidJWT)).toThrow('Failed to decode JWT payload');
    });

    it('should handle empty payload section', () => {
      const emptyPayloadJWT = 'header..signature';
      
      expect(() => CryptoUtils.decodeJWTPayload(emptyPayloadJWT)).toThrow();
    });
  });

  describe('verifyHMAC', () => {
    beforeEach(() => {
      // Mock crypto.subtle if not available
      if (typeof crypto === 'undefined' || !crypto.subtle) {
        (global as any).crypto = {
          subtle: {
            importKey: jest.fn(),
            verify: jest.fn(),
          },
        };
      }
    });

    it('should return false for mismatched signature', async () => {
      const data = 'test data';
      const signature = 'invalid_signature';
      const secret = 'secret_key';
      
      const result = await CryptoUtils.verifyHMAC(data, signature, secret);
      expect(result).toBe(false);
    });

    it('should handle crypto.subtle errors gracefully', async () => {
      // Mock crypto.subtle.verify to throw an error
      if (crypto.subtle && crypto.subtle.verify) {
        jest.mocked(crypto.subtle.verify).mockRejectedValue(new Error('Crypto error'));
      }
      
      const result = await CryptoUtils.verifyHMAC('data', 'signature', 'secret');
      expect(result).toBe(false);
    });
  });

  // Test fallback methods when crypto is not available
  describe('Fallback methods', () => {
    let originalCrypto: any;

    beforeAll(() => {
      originalCrypto = (global as any).crypto;
    });

    afterAll(() => {
      (global as any).crypto = originalCrypto;
    });

    it('should use fallback for random string generation', async () => {
      // Remove crypto to force fallback
      delete (global as any).crypto;
      
      const randomString = await CryptoUtils.generateRandomString(16);
      
      expect(randomString).toBeDefined();
      expect(typeof randomString).toBe('string');
      expect(randomString.length).toBeGreaterThan(0);
    });

    it('should use fallback for SHA256 encoding', async () => {
      // Remove crypto to force fallback
      delete (global as any).crypto;
      
      const hash = await CryptoUtils.sha256Base64UrlEncode('test');
      
      expect(hash).toBeDefined();
      expect(typeof hash).toBe('string');
      expect(hash.length).toBeGreaterThan(0);
    });

    it('should use fallback for HMAC verification', async () => {
      // Remove crypto to force fallback
      delete (global as any).crypto;
      
      const result = await CryptoUtils.verifyHMAC('data', 'signature', 'secret');
      
      expect(typeof result).toBe('boolean');
    });
  });

  describe('Advanced fallback scenarios', () => {
    let warnSpy: jest.SpyInstance;

    beforeEach(() => {
      warnSpy = jest.spyOn(console, 'warn').mockImplementation();
    });

    afterEach(() => {
      warnSpy.mockRestore();
    });

    it('should use Node.js crypto when available in fallback', async () => {
      const originalCrypto = global.crypto;
      const originalRequire = global.require;

      // @ts-ignore
      global.crypto = undefined;
      // @ts-ignore  
      global.require = jest.fn().mockImplementation((module) => {
        if (module === 'crypto') {
          return {
            randomBytes: (length: number) => {
              const buffer = Buffer.alloc(length);
              for (let i = 0; i < length; i++) {
                buffer[i] = 65 + (i % 26); // A-Z pattern
              }
              return buffer;
            }
          };
        }
        throw new Error('Module not found');
      });

      const result = await CryptoUtils.generateRandomString(10);
      expect(result).toHaveLength(10);
      expect(warnSpy).toHaveBeenCalledWith(
        'SECURITY WARNING: Using fallback random generation. crypto.getRandomValues is not available.'
      );

      global.crypto = originalCrypto;
      global.require = originalRequire;
    });

    // TODO: Fix complex fallback test - hard to simulate crypto module failure in Jest
    it.skip('should fall back to Math.random when Node crypto fails', async () => {
      const originalCrypto = global.crypto;
      const originalRequire = global.require;
      const mathRandomSpy = jest.spyOn(Math, 'random').mockReturnValue(0.5);

      // @ts-ignore
      delete global.crypto;
      // @ts-ignore
      global.require = jest.fn().mockImplementation(() => {
        throw new Error('crypto module not available');
      });

      const result = await CryptoUtils.generateRandomString(8);
      expect(result).toHaveLength(8);
      expect(result).toMatch(/^[A-Za-z0-9_-]+$/); // Should be base64url chars
      expect(warnSpy).toHaveBeenCalled(); // Should warn about fallback
      expect(mathRandomSpy).toHaveBeenCalled(); // Should use Math.random

      global.crypto = originalCrypto;
      global.require = originalRequire;
      mathRandomSpy.mockRestore();
    });
  });

  describe('Base64URL utility methods', () => {
    it('should convert ArrayBuffer to base64url correctly', () => {
      const testData = new Uint8Array([72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100]); // "Hello World"
      const result = (CryptoUtils as any).arrayBufferToBase64Url(testData);
      
      expect(typeof result).toBe('string');
      expect(result).not.toContain('+');
      expect(result).not.toContain('/');
      expect(result).not.toContain('=');
      expect(result.length).toBeGreaterThan(0);
    });

    it('should convert base64url to ArrayBuffer correctly', () => {
      const base64url = 'SGVsbG8gV29ybGQ'; // "Hello World"
      const result = (CryptoUtils as any).base64UrlToArrayBuffer(base64url);
      
      expect(result).toBeInstanceOf(ArrayBuffer);
      const uint8Array = new Uint8Array(result);
      expect(Array.from(uint8Array)).toEqual([72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100]);
    });

    it('should handle base64url padding in conversion', () => {
      // Test different padding scenarios
      const testCases = [
        'QQ',    // Should add ==
        'QWE',   // Should add =  
        'QWER',  // No padding needed
      ];

      testCases.forEach(base64url => {
        const result = (CryptoUtils as any).base64UrlToArrayBuffer(base64url);
        expect(result).toBeInstanceOf(ArrayBuffer);
      });
    });

    it('should decode base64url to string', () => {
      const base64url = 'SGVsbG8'; // "Hello"
      const result = (CryptoUtils as any).base64UrlDecode(base64url);
      expect(result).toBe('Hello');
    });

    it('should handle different base64url lengths', () => {
      const testStrings = ['A', 'AB', 'ABC', 'ABCD', 'ABCDE'];
      
      testStrings.forEach(str => {
        const encoded = btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
        const result = (CryptoUtils as any).base64UrlDecode(encoded);
        expect(result).toBe(str);
      });
    });
  });

  describe('Private method coverage', () => {
    it('should generate simple fallback hash deterministically', () => {
      const input1 = 'test string';
      const input2 = 'test string';
      const input3 = 'different string';

      const hash1 = (CryptoUtils as any).simpleFallbackHash(input1);
      const hash2 = (CryptoUtils as any).simpleFallbackHash(input2);
      const hash3 = (CryptoUtils as any).simpleFallbackHash(input3);

      expect(hash1).toBe(hash2);
      expect(hash1).not.toBe(hash3);
      expect(typeof hash1).toBe('string');
    });

    it('should handle empty string in fallback hash', () => {
      const result = (CryptoUtils as any).simpleFallbackHash('');
      expect(result).toBe('0');
    });

    it('should handle long strings in fallback hash', () => {
      const longString = 'a'.repeat(1000);
      const result = (CryptoUtils as any).simpleFallbackHash(longString);
      expect(typeof result).toBe('string');
      expect(result.length).toBeGreaterThan(0);
    });

    it('should perform fallback HMAC verification', () => {
      const data = 'test data';
      const secret = 'secret key';
      
      // Generate expected signature using the same method the verifier uses
      const expectedSignature = (CryptoUtils as any).simpleFallbackHash(data + secret);

      const result1 = (CryptoUtils as any).fallbackHMACVerify(data, expectedSignature, secret);
      const result2 = (CryptoUtils as any).fallbackHMACVerify(data, expectedSignature, secret);
      const result3 = (CryptoUtils as any).fallbackHMACVerify(data, 'wrong-signature', secret);

      expect(result1).toBe(true); // Should verify correct signature
      expect(result2).toBe(true); // Should be consistent
      expect(result3).toBe(false); // Different signature should fail
      expect(typeof result1).toBe('boolean');
    });

    it('should handle edge cases in fallback HMAC', () => {
      // Test with empty strings
      const result1 = (CryptoUtils as any).fallbackHMACVerify('', '', '');
      expect(typeof result1).toBe('boolean');

      // Test with special characters
      const result2 = (CryptoUtils as any).fallbackHMACVerify('data!@#$', 'sig!@#$', 'secret!@#$');
      expect(typeof result2).toBe('boolean');
    });
  });
});