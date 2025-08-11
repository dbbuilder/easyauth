/**
 * Unit tests for StateManager - OAuth state management
 * Following TDD methodology: Testing CSRF protection and state handling
 */

import { StateManager } from '../../../src/utils/StateManager';

describe('StateManager', () => {
  let stateManager: StateManager;
  const mockStateData = {
    provider: 'google',
    returnUrl: 'https://example.com/callback',
    customParams: { prompt: 'consent' },
    scopes: ['openid', 'email', 'profile'],
  };

  beforeEach(() => {
    stateManager = new StateManager();
    jest.clearAllMocks();
  });

  describe('storeState', () => {
    it('should store state data successfully', () => {
      const state = 'test_state_123';
      
      stateManager.storeState(state, mockStateData);
      
      // Verify state can be validated
      const isValid = stateManager.validateState(state);
      expect(isValid).toBe(true);
    });

    it('should add timestamp to stored data', () => {
      const state = 'test_state_123';
      const beforeStore = Date.now();
      
      stateManager.storeState(state, mockStateData);
      
      const storedData = stateManager.getStateData(state);
      expect(storedData).toBeDefined();
      expect(storedData!.timestamp).toBeGreaterThanOrEqual(beforeStore);
      expect(storedData!.timestamp).toBeLessThanOrEqual(Date.now());
    });

    it('should store all provided data fields', () => {
      const state = 'test_state_123';
      
      stateManager.storeState(state, mockStateData);
      
      const storedData = stateManager.getStateData(state);
      expect(storedData).toBeDefined();
      expect(storedData!.provider).toBe(mockStateData.provider);
      expect(storedData!.returnUrl).toBe(mockStateData.returnUrl);
      expect(storedData!.customParams).toEqual(mockStateData.customParams);
      expect(storedData!.scopes).toEqual(mockStateData.scopes);
    });

    it('should handle optional fields', () => {
      const state = 'test_state_123';
      const minimalData = {
        provider: 'facebook',
        returnUrl: 'https://example.com/callback',
      };
      
      stateManager.storeState(state, minimalData);
      
      const storedData = stateManager.getStateData(state);
      expect(storedData).toBeDefined();
      expect(storedData!.provider).toBe(minimalData.provider);
      expect(storedData!.returnUrl).toBe(minimalData.returnUrl);
      expect(storedData!.customParams).toBeUndefined();
      expect(storedData!.scopes).toBeUndefined();
    });

    it('should replace existing state data', () => {
      const state = 'test_state_123';
      const newData = { ...mockStateData, provider: 'apple' };
      
      stateManager.storeState(state, mockStateData);
      stateManager.storeState(state, newData);
      
      const storedData = stateManager.getStateData(state);
      expect(storedData!.provider).toBe('apple');
    });
  });

  describe('validateState', () => {
    it('should return true for valid, non-expired state', () => {
      const state = 'test_state_123';
      stateManager.storeState(state, mockStateData);
      
      const isValid = stateManager.validateState(state);
      expect(isValid).toBe(true);
    });

    it('should return false for non-existent state', () => {
      const isValid = stateManager.validateState('non_existent_state');
      expect(isValid).toBe(false);
    });

    it('should return false for expired state', () => {
      const state = 'test_state_123';
      
      // Store state
      stateManager.storeState(state, mockStateData);
      
      // Mock expired timestamp by manipulating the internal state
      const statesMap = (stateManager as any).states;
      const stateData = statesMap.get(state);
      stateData.timestamp = Date.now() - 11 * 60 * 1000; // 11 minutes ago (past expiration)
      
      const isValid = stateManager.validateState(state);
      expect(isValid).toBe(false);
    });

    it('should clean up expired state when validating', () => {
      const state = 'test_state_123';
      
      // Store state
      stateManager.storeState(state, mockStateData);
      
      // Mock expired timestamp
      const statesMap = (stateManager as any).states;
      const stateData = statesMap.get(state);
      stateData.timestamp = Date.now() - 11 * 60 * 1000; // 11 minutes ago
      
      // Validate should return false and clean up
      stateManager.validateState(state);
      
      // State should be removed
      const isStillThere = statesMap.has(state);
      expect(isStillThere).toBe(false);
    });

    it('should handle edge case of exactly expired state', () => {
      const state = 'test_state_123';
      
      // Store state
      stateManager.storeState(state, mockStateData);
      
      // Mock timestamp at exact expiration boundary
      const statesMap = (stateManager as any).states;
      const stateData = statesMap.get(state);
      stateData.timestamp = Date.now() - 10 * 60 * 1000; // Exactly 10 minutes ago
      
      const isValid = stateManager.validateState(state);
      expect(isValid).toBe(false);
    });
  });

  describe('getStateData', () => {
    it('should return state data for valid state', () => {
      const state = 'test_state_123';
      stateManager.storeState(state, mockStateData);
      
      const retrievedData = stateManager.getStateData(state);
      
      expect(retrievedData).toBeDefined();
      expect(retrievedData!.provider).toBe(mockStateData.provider);
      expect(retrievedData!.returnUrl).toBe(mockStateData.returnUrl);
      expect(retrievedData!.customParams).toEqual(mockStateData.customParams);
      expect(retrievedData!.scopes).toEqual(mockStateData.scopes);
      expect(retrievedData!.timestamp).toBeDefined();
    });

    it('should return null for invalid state', () => {
      const retrievedData = stateManager.getStateData('non_existent_state');
      expect(retrievedData).toBeNull();
    });

    it('should return null for expired state', () => {
      const state = 'test_state_123';
      
      // Store state
      stateManager.storeState(state, mockStateData);
      
      // Mock expired timestamp
      const statesMap = (stateManager as any).states;
      const stateData = statesMap.get(state);
      stateData.timestamp = Date.now() - 11 * 60 * 1000; // 11 minutes ago
      
      const retrievedData = stateManager.getStateData(state);
      expect(retrievedData).toBeNull();
    });

    it('should validate state before returning data', () => {
      const state = 'test_state_123';
      stateManager.storeState(state, mockStateData);
      
      // Spy on validateState to ensure it's called
      const validateSpy = jest.spyOn(stateManager, 'validateState');
      
      stateManager.getStateData(state);
      
      expect(validateSpy).toHaveBeenCalledWith(state);
    });
  });

  describe('clearState', () => {
    it('should remove specific state', () => {
      const state = 'test_state_123';
      stateManager.storeState(state, mockStateData);
      
      // Verify state exists
      expect(stateManager.validateState(state)).toBe(true);
      
      // Clear the state
      stateManager.clearState(state);
      
      // Verify state is gone
      expect(stateManager.validateState(state)).toBe(false);
    });

    it('should not affect other states', () => {
      const state1 = 'test_state_1';
      const state2 = 'test_state_2';
      
      stateManager.storeState(state1, mockStateData);
      stateManager.storeState(state2, { ...mockStateData, provider: 'facebook' });
      
      // Clear only first state
      stateManager.clearState(state1);
      
      // First should be gone, second should remain
      expect(stateManager.validateState(state1)).toBe(false);
      expect(stateManager.validateState(state2)).toBe(true);
    });

    it('should handle clearing non-existent state gracefully', () => {
      // Should not throw error
      expect(() => stateManager.clearState('non_existent_state')).not.toThrow();
    });
  });

  describe('clearAllStates', () => {
    it('should remove all stored states', () => {
      const state1 = 'test_state_1';
      const state2 = 'test_state_2';
      const state3 = 'test_state_3';
      
      // Store multiple states
      stateManager.storeState(state1, mockStateData);
      stateManager.storeState(state2, { ...mockStateData, provider: 'facebook' });
      stateManager.storeState(state3, { ...mockStateData, provider: 'apple' });
      
      // Verify all states exist
      expect(stateManager.validateState(state1)).toBe(true);
      expect(stateManager.validateState(state2)).toBe(true);
      expect(stateManager.validateState(state3)).toBe(true);
      
      // Clear all states
      stateManager.clearAllStates();
      
      // Verify all states are gone
      expect(stateManager.validateState(state1)).toBe(false);
      expect(stateManager.validateState(state2)).toBe(false);
      expect(stateManager.validateState(state3)).toBe(false);
    });

    it('should handle clearing when no states exist', () => {
      // Should not throw error
      expect(() => stateManager.clearAllStates()).not.toThrow();
    });

    it('should clear internal states map', () => {
      const state = 'test_state_123';
      stateManager.storeState(state, mockStateData);
      
      stateManager.clearAllStates();
      
      // Check internal states map is empty
      const statesMap = (stateManager as any).states;
      expect(statesMap.size).toBe(0);
    });
  });

  describe('Expiration and cleanup', () => {
    it('should automatically clean up expired states during storeState', () => {
      const state1 = 'expired_state';
      const state2 = 'valid_state';
      
      // Store first state
      stateManager.storeState(state1, mockStateData);
      
      // Manually expire the first state
      const statesMap = (stateManager as any).states;
      const expiredData = statesMap.get(state1);
      expiredData.timestamp = Date.now() - 11 * 60 * 1000; // 11 minutes ago
      
      // Store second state (should trigger cleanup)
      stateManager.storeState(state2, mockStateData);
      
      // Expired state should be cleaned up
      expect(statesMap.has(state1)).toBe(false);
      expect(statesMap.has(state2)).toBe(true);
    });

    it('should handle multiple expired states during cleanup', () => {
      const state1 = 'expired_state_1';
      const state2 = 'expired_state_2';
      const state3 = 'valid_state';
      
      // Store multiple states
      stateManager.storeState(state1, mockStateData);
      stateManager.storeState(state2, mockStateData);
      
      // Manually expire the first two states
      const statesMap = (stateManager as any).states;
      const expiredData1 = statesMap.get(state1);
      const expiredData2 = statesMap.get(state2);
      expiredData1.timestamp = Date.now() - 11 * 60 * 1000;
      expiredData2.timestamp = Date.now() - 12 * 60 * 1000;
      
      // Store valid state (should trigger cleanup)
      stateManager.storeState(state3, mockStateData);
      
      // Only valid state should remain
      expect(statesMap.has(state1)).toBe(false);
      expect(statesMap.has(state2)).toBe(false);
      expect(statesMap.has(state3)).toBe(true);
      expect(statesMap.size).toBe(1);
    });
  });

  describe('Edge cases', () => {
    it('should handle empty state string', () => {
      const emptyState = '';
      stateManager.storeState(emptyState, mockStateData);
      
      const isValid = stateManager.validateState(emptyState);
      expect(isValid).toBe(true);
    });

    it('should handle state with special characters', () => {
      const specialState = 'state_with-special.chars_123';
      stateManager.storeState(specialState, mockStateData);
      
      const isValid = stateManager.validateState(specialState);
      expect(isValid).toBe(true);
    });

    it('should handle very long state strings', () => {
      const longState = 'a'.repeat(1000);
      stateManager.storeState(longState, mockStateData);
      
      const isValid = stateManager.validateState(longState);
      expect(isValid).toBe(true);
    });

    it('should handle state data with undefined values', () => {
      const state = 'test_state_123';
      const dataWithUndefined = {
        provider: 'google',
        returnUrl: 'https://example.com/callback',
        customParams: undefined,
        scopes: undefined,
      };
      
      stateManager.storeState(state, dataWithUndefined);
      
      const retrievedData = stateManager.getStateData(state);
      expect(retrievedData).toBeDefined();
      expect(retrievedData!.provider).toBe('google');
      expect(retrievedData!.customParams).toBeUndefined();
      expect(retrievedData!.scopes).toBeUndefined();
    });
  });

  describe('Memory management', () => {
    it('should not leak memory with many states', () => {
      // Store a large number of states
      for (let i = 0; i < 100; i++) {
        stateManager.storeState(`state_${i}`, mockStateData);
      }
      
      const statesMap = (stateManager as any).states;
      expect(statesMap.size).toBe(100);
      
      // Clear all and verify cleanup
      stateManager.clearAllStates();
      expect(statesMap.size).toBe(0);
    });

    it('should properly clean up when validating many expired states', () => {
      // Store multiple states
      for (let i = 0; i < 10; i++) {
        stateManager.storeState(`state_${i}`, mockStateData);
      }
      
      // Expire all states
      const statesMap = (stateManager as any).states;
      const expiredTime = Date.now() - 11 * 60 * 1000;
      for (const data of statesMap.values()) {
        data.timestamp = expiredTime;
      }
      
      // Store one new state to trigger cleanup
      stateManager.storeState('new_state', mockStateData);
      
      // Only the new state should remain
      expect(statesMap.size).toBe(1);
      expect(statesMap.has('new_state')).toBe(true);
    });
  });
});