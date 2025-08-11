/**
 * Simple event emitter for authentication events
 */

export class EventEmitter {
  private readonly events = new Map<string, Set<Function>>();

  /**
   * Add event listener
   */
  public on(event: string, listener: Function): void {
    if (!this.events.has(event)) {
      this.events.set(event, new Set());
    }
    
    this.events.get(event)!.add(listener);
  }

  /**
   * Remove event listener
   */
  public off(event: string, listener: Function): void {
    const listeners = this.events.get(event);
    
    if (listeners) {
      listeners.delete(listener);
      
      // Clean up empty event sets
      if (listeners.size === 0) {
        this.events.delete(event);
      }
    }
  }

  /**
   * Emit event to all listeners
   */
  public emit(event: string, ...args: any[]): void {
    const listeners = this.events.get(event);
    
    if (listeners) {
      // Create array to avoid iterator issues if listeners are modified during emit
      const listenersArray = Array.from(listeners);
      
      for (const listener of listenersArray) {
        try {
          listener(...args);
        } catch (error) {
          // Log error but don't stop other listeners
          console.error('Error in event listener:', error);
        }
      }
    }
  }

  /**
   * Add one-time event listener
   */
  public once(event: string, listener: Function): void {
    const onceListener = (...args: any[]) => {
      this.off(event, onceListener);
      listener(...args);
    };
    
    this.on(event, onceListener);
  }

  /**
   * Remove all listeners for an event or all events
   */
  public removeAllListeners(event?: string): void {
    if (event) {
      this.events.delete(event);
    } else {
      this.events.clear();
    }
  }

  /**
   * Get listener count for an event
   */
  public listenerCount(event: string): number {
    const listeners = this.events.get(event);
    return listeners ? listeners.size : 0;
  }

  /**
   * Get all event names
   */
  public eventNames(): string[] {
    return Array.from(this.events.keys());
  }
}