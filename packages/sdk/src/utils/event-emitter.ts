import { AuthEvent, AuthEventCallback } from '../types';

/**
 * Simple event emitter for authentication events
 */
export class EventEmitter {
  private listeners = new Map<AuthEvent, Set<AuthEventCallback>>();
  
  /**
   * Subscribe to an event
   */
  on(event: AuthEvent, callback: AuthEventCallback): void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    
    this.listeners.get(event)!.add(callback);
  }
  
  /**
   * Unsubscribe from an event
   */
  off(event: AuthEvent, callback: AuthEventCallback): void {
    const eventListeners = this.listeners.get(event);
    if (eventListeners) {
      eventListeners.delete(callback);
      
      // Clean up empty listener sets
      if (eventListeners.size === 0) {
        this.listeners.delete(event);
      }
    }
  }
  
  /**
   * Emit an event to all subscribers
   */
  emit(event: AuthEvent, data?: unknown): void {
    const eventListeners = this.listeners.get(event);
    if (eventListeners) {
      eventListeners.forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event listener for ${event}:`, error);
        }
      });
    }
  }
  
  /**
   * Remove all listeners for a specific event
   */
  removeAllListeners(event?: AuthEvent): void {
    if (event) {
      this.listeners.delete(event);
    } else {
      this.listeners.clear();
    }
  }
  
  /**
   * Get count of listeners for an event
   */
  listenerCount(event: AuthEvent): number {
    return this.listeners.get(event)?.size ?? 0;
  }
}