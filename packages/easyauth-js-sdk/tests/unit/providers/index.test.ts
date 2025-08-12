/**
 * Tests for providers index exports
 */

import { GoogleAuthProvider, providers } from '../../../src/providers/index';
import { GoogleAuthProvider as GoogleAuthProviderDirect } from '../../../src/providers/GoogleAuthProvider';

describe('Providers Index', () => {
  describe('exports', () => {
    it('should export GoogleAuthProvider', () => {
      expect(GoogleAuthProvider).toBeDefined();
      expect(GoogleAuthProvider).toBe(GoogleAuthProviderDirect);
    });

    it('should export providers collection', () => {
      expect(providers).toBeDefined();
      expect(providers.GoogleAuthProvider).toBe(GoogleAuthProvider);
    });

    it('should have providers object with correct structure', () => {
      expect(typeof providers).toBe('object');
      expect(providers).toHaveProperty('GoogleAuthProvider');
      expect(providers.GoogleAuthProvider).toBe(GoogleAuthProvider);
    });
  });
});