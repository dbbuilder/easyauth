/**
 * State management for OAuth flows
 * Handles CSRF protection via state parameters
 */

import { CryptoUtils } from './CryptoUtils';

interface StateData {
  provider: string;
  returnUrl: string;
  customParams?: Record<string, string>;
  scopes?: string[];
  timestamp: number;
}

export class StateManager {
  private readonly states = new Map<string, StateData>();
  private readonly EXPIRATION_TIME = 10 * 60 * 1000; // 10 minutes

  /**
   * Store state data with automatic expiration
   */
  public storeState(state: string, data: Omit<StateData, 'timestamp'>): void {
    this.states.set(state, {
      ...data,
      timestamp: Date.now(),
    });

    // Clean up expired states
    this.cleanupExpiredStates();
  }

  /**
   * Validate and retrieve state data
   */
  public validateState(state: string): boolean {
    const data = this.states.get(state);
    
    if (!data) {
      return false;
    }

    // Check if state has expired
    const isExpired = Date.now() - data.timestamp > this.EXPIRATION_TIME;
    
    if (isExpired) {
      this.states.delete(state);
      return false;
    }

    return true;
  }

  /**
   * Get state data (after validation)
   */
  public getStateData(state: string): StateData | null {
    if (!this.validateState(state)) {
      return null;
    }

    return this.states.get(state) || null;
  }

  /**
   * Clear specific state
   */
  public clearState(state: string): void {
    this.states.delete(state);
  }

  /**
   * Clear all states
   */
  public clearAllStates(): void {
    this.states.clear();
  }

  /**
   * Clean up expired states
   */
  private cleanupExpiredStates(): void {
    const now = Date.now();
    
    for (const [state, data] of this.states.entries()) {
      if (now - data.timestamp > this.EXPIRATION_TIME) {
        this.states.delete(state);
      }
    }
  }
}