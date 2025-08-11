/**
 * Unit tests for EventEmitter - Event handling system
 * Following TDD methodology: Testing all event operations
 */

import { EventEmitter } from '../../../src/utils/EventEmitter';

describe('EventEmitter', () => {
  let emitter: EventEmitter;
  let mockListener: jest.Mock;
  let mockListener2: jest.Mock;

  beforeEach(() => {
    emitter = new EventEmitter();
    mockListener = jest.fn();
    mockListener2 = jest.fn();
    
    // Spy on console.error to test error handling
    jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('on', () => {
    it('should add event listener', () => {
      emitter.on('test-event', mockListener);
      
      expect(emitter.listenerCount('test-event')).toBe(1);
    });

    it('should add multiple listeners for the same event', () => {
      emitter.on('test-event', mockListener);
      emitter.on('test-event', mockListener2);
      
      expect(emitter.listenerCount('test-event')).toBe(2);
    });

    it('should add the same listener multiple times', () => {
      emitter.on('test-event', mockListener);
      emitter.on('test-event', mockListener);
      
      // Set-based storage prevents duplicates
      expect(emitter.listenerCount('test-event')).toBe(1);
    });

    it('should handle different event types', () => {
      emitter.on('event1', mockListener);
      emitter.on('event2', mockListener2);
      
      expect(emitter.listenerCount('event1')).toBe(1);
      expect(emitter.listenerCount('event2')).toBe(1);
      expect(emitter.eventNames()).toContain('event1');
      expect(emitter.eventNames()).toContain('event2');
    });

    it('should handle empty event names', () => {
      emitter.on('', mockListener);
      
      expect(emitter.listenerCount('')).toBe(1);
      expect(emitter.eventNames()).toContain('');
    });
  });

  describe('off', () => {
    beforeEach(() => {
      emitter.on('test-event', mockListener);
      emitter.on('test-event', mockListener2);
    });

    it('should remove specific listener', () => {
      emitter.off('test-event', mockListener);
      
      expect(emitter.listenerCount('test-event')).toBe(1);
    });

    it('should remove all listeners when last one is removed', () => {
      emitter.off('test-event', mockListener);
      emitter.off('test-event', mockListener2);
      
      expect(emitter.listenerCount('test-event')).toBe(0);
      expect(emitter.eventNames()).not.toContain('test-event');
    });

    it('should handle removal of non-existent listener', () => {
      const nonExistentListener = jest.fn();
      
      expect(() => emitter.off('test-event', nonExistentListener)).not.toThrow();
      expect(emitter.listenerCount('test-event')).toBe(2); // Original listeners still there
    });

    it('should handle removal from non-existent event', () => {
      expect(() => emitter.off('non-existent-event', mockListener)).not.toThrow();
    });

    it('should clean up empty event sets', () => {
      emitter.off('test-event', mockListener);
      emitter.off('test-event', mockListener2);
      
      // Internal events map should not contain the event anymore
      expect(emitter.eventNames()).not.toContain('test-event');
    });
  });

  describe('emit', () => {
    beforeEach(() => {
      emitter.on('test-event', mockListener);
      emitter.on('test-event', mockListener2);
    });

    it('should call all listeners for an event', () => {
      emitter.emit('test-event');
      
      expect(mockListener).toHaveBeenCalledTimes(1);
      expect(mockListener2).toHaveBeenCalledTimes(1);
    });

    it('should pass arguments to listeners', () => {
      const arg1 = 'test-arg';
      const arg2 = { data: 'value' };
      const arg3 = 123;
      
      emitter.emit('test-event', arg1, arg2, arg3);
      
      expect(mockListener).toHaveBeenCalledWith(arg1, arg2, arg3);
      expect(mockListener2).toHaveBeenCalledWith(arg1, arg2, arg3);
    });

    it('should handle events with no listeners', () => {
      expect(() => emitter.emit('non-existent-event')).not.toThrow();
    });

    it('should handle listener errors gracefully', () => {
      const errorListener = jest.fn().mockImplementation(() => {
        throw new Error('Listener error');
      });
      
      emitter.on('test-event', errorListener);
      
      // Should not throw and should continue with other listeners
      expect(() => emitter.emit('test-event')).not.toThrow();
      
      expect(errorListener).toHaveBeenCalled();
      expect(mockListener).toHaveBeenCalled();
      expect(mockListener2).toHaveBeenCalled();
      expect(console.error).toHaveBeenCalledWith('Error in event listener:', expect.any(Error));
    });

    it('should not be affected by listeners being removed during emit', () => {
      const selfRemovingListener = jest.fn(() => {
        emitter.off('test-event', selfRemovingListener);
      });
      
      emitter.on('test-event', selfRemovingListener);
      
      expect(() => emitter.emit('test-event')).not.toThrow();
      
      expect(selfRemovingListener).toHaveBeenCalled();
      expect(mockListener).toHaveBeenCalled();
      expect(mockListener2).toHaveBeenCalled();
    });

    it('should handle listeners being added during emit', () => {
      const addingListener = jest.fn(() => {
        emitter.on('test-event', mockListener2);
      });
      
      emitter.off('test-event', mockListener2); // Remove first
      emitter.on('test-event', addingListener);
      
      emitter.emit('test-event');
      
      expect(addingListener).toHaveBeenCalled();
      expect(mockListener).toHaveBeenCalled();
      // mockListener2 was added during emit, so it should not be called in this emit
      expect(mockListener2).not.toHaveBeenCalled();
    });
  });

  describe('once', () => {
    it('should add one-time listener', () => {
      emitter.once('test-event', mockListener);
      
      expect(emitter.listenerCount('test-event')).toBe(1);
    });

    it('should remove listener after first emit', () => {
      emitter.once('test-event', mockListener);
      
      emitter.emit('test-event', 'arg1');
      
      expect(mockListener).toHaveBeenCalledWith('arg1');
      expect(emitter.listenerCount('test-event')).toBe(0);
    });

    it('should not call listener on subsequent emits', () => {
      emitter.once('test-event', mockListener);
      
      emitter.emit('test-event');
      emitter.emit('test-event');
      emitter.emit('test-event');
      
      expect(mockListener).toHaveBeenCalledTimes(1);
    });

    it('should work with multiple once listeners', () => {
      emitter.once('test-event', mockListener);
      emitter.once('test-event', mockListener2);
      
      expect(emitter.listenerCount('test-event')).toBe(2);
      
      emitter.emit('test-event');
      
      expect(mockListener).toHaveBeenCalledTimes(1);
      expect(mockListener2).toHaveBeenCalledTimes(1);
      expect(emitter.listenerCount('test-event')).toBe(0);
    });

    it('should allow manual removal of once listeners', () => {
      emitter.once('test-event', mockListener);
      
      // We can't directly remove a once listener since it's wrapped,
      // but we can test that removeAllListeners works
      emitter.removeAllListeners('test-event');
      
      expect(emitter.listenerCount('test-event')).toBe(0);
      
      emitter.emit('test-event');
      expect(mockListener).not.toHaveBeenCalled();
    });

    it('should handle errors in once listeners', () => {
      const errorListener = jest.fn().mockImplementation(() => {
        throw new Error('Once listener error');
      });
      
      emitter.once('test-event', errorListener);
      
      expect(() => emitter.emit('test-event')).not.toThrow();
      
      expect(errorListener).toHaveBeenCalled();
      expect(emitter.listenerCount('test-event')).toBe(0);
      expect(console.error).toHaveBeenCalledWith('Error in event listener:', expect.any(Error));
    });
  });

  describe('removeAllListeners', () => {
    beforeEach(() => {
      emitter.on('event1', mockListener);
      emitter.on('event1', mockListener2);
      emitter.on('event2', mockListener);
      emitter.once('event3', mockListener2);
    });

    it('should remove all listeners for specific event', () => {
      emitter.removeAllListeners('event1');
      
      expect(emitter.listenerCount('event1')).toBe(0);
      expect(emitter.listenerCount('event2')).toBe(1);
      expect(emitter.listenerCount('event3')).toBe(1);
      expect(emitter.eventNames()).not.toContain('event1');
    });

    it('should remove all listeners for all events when no event specified', () => {
      emitter.removeAllListeners();
      
      expect(emitter.listenerCount('event1')).toBe(0);
      expect(emitter.listenerCount('event2')).toBe(0);
      expect(emitter.listenerCount('event3')).toBe(0);
      expect(emitter.eventNames()).toHaveLength(0);
    });

    it('should handle removal of non-existent event', () => {
      expect(() => emitter.removeAllListeners('non-existent')).not.toThrow();
      
      // Other events should remain unchanged
      expect(emitter.listenerCount('event1')).toBe(2);
      expect(emitter.listenerCount('event2')).toBe(1);
    });
  });

  describe('listenerCount', () => {
    it('should return correct count for existing event', () => {
      emitter.on('test-event', mockListener);
      emitter.on('test-event', mockListener2);
      
      expect(emitter.listenerCount('test-event')).toBe(2);
    });

    it('should return 0 for non-existent event', () => {
      expect(emitter.listenerCount('non-existent')).toBe(0);
    });

    it('should update count when listeners are added/removed', () => {
      expect(emitter.listenerCount('test-event')).toBe(0);
      
      emitter.on('test-event', mockListener);
      expect(emitter.listenerCount('test-event')).toBe(1);
      
      emitter.on('test-event', mockListener2);
      expect(emitter.listenerCount('test-event')).toBe(2);
      
      emitter.off('test-event', mockListener);
      expect(emitter.listenerCount('test-event')).toBe(1);
      
      emitter.off('test-event', mockListener2);
      expect(emitter.listenerCount('test-event')).toBe(0);
    });

    it('should handle once listeners in count', () => {
      emitter.once('test-event', mockListener);
      expect(emitter.listenerCount('test-event')).toBe(1);
      
      emitter.emit('test-event');
      expect(emitter.listenerCount('test-event')).toBe(0);
    });
  });

  describe('eventNames', () => {
    it('should return empty array when no events', () => {
      expect(emitter.eventNames()).toEqual([]);
    });

    it('should return all event names', () => {
      emitter.on('event1', mockListener);
      emitter.on('event2', mockListener2);
      emitter.once('event3', mockListener);
      
      const eventNames = emitter.eventNames();
      expect(eventNames).toHaveLength(3);
      expect(eventNames).toContain('event1');
      expect(eventNames).toContain('event2');
      expect(eventNames).toContain('event3');
    });

    it('should not include events with no listeners', () => {
      emitter.on('event1', mockListener);
      emitter.on('event2', mockListener2);
      
      emitter.off('event1', mockListener);
      
      const eventNames = emitter.eventNames();
      expect(eventNames).toHaveLength(1);
      expect(eventNames).not.toContain('event1');
      expect(eventNames).toContain('event2');
    });

    it('should handle special event names', () => {
      const specialNames = ['', ' ', '123', 'event-with-dashes', 'event.with.dots', 'event_with_underscores'];
      
      specialNames.forEach(name => {
        emitter.on(name, mockListener);
      });
      
      const eventNames = emitter.eventNames();
      specialNames.forEach(name => {
        expect(eventNames).toContain(name);
      });
    });
  });

  describe('Memory management and performance', () => {
    it('should not leak memory when events are cleaned up', () => {
      // Add many listeners
      for (let i = 0; i < 100; i++) {
        emitter.on(`event-${i}`, mockListener);
      }
      
      expect(emitter.eventNames()).toHaveLength(100);
      
      // Remove all
      emitter.removeAllListeners();
      
      expect(emitter.eventNames()).toHaveLength(0);
    });

    it('should handle rapid add/remove cycles', () => {
      for (let i = 0; i < 50; i++) {
        emitter.on('test-event', mockListener);
        emitter.off('test-event', mockListener);
      }
      
      expect(emitter.listenerCount('test-event')).toBe(0);
      expect(emitter.eventNames()).not.toContain('test-event');
    });

    it('should handle many listeners on single event', () => {
      const listeners = Array.from({ length: 100 }, () => jest.fn());
      
      listeners.forEach(listener => {
        emitter.on('big-event', listener);
      });
      
      expect(emitter.listenerCount('big-event')).toBe(100);
      
      emitter.emit('big-event', 'test-data');
      
      listeners.forEach(listener => {
        expect(listener).toHaveBeenCalledWith('test-data');
      });
    });
  });

  describe('Edge cases', () => {
    it('should handle null/undefined event names', () => {
      // TypeScript would prevent this, but JavaScript might allow it
      const nullEventName = null as any;
      const undefinedEventName = undefined as any;
      
      expect(() => emitter.on(nullEventName, mockListener)).not.toThrow();
      expect(() => emitter.on(undefinedEventName, mockListener)).not.toThrow();
      expect(() => emitter.emit(nullEventName)).not.toThrow();
      expect(() => emitter.emit(undefinedEventName)).not.toThrow();
    });

    it('should handle function objects as listeners', () => {
      class TestClass {
        handleEvent(data: string) {
          return `handled: ${data}`;
        }
      }
      
      const instance = new TestClass();
      const boundMethod = instance.handleEvent.bind(instance);
      
      emitter.on('test-event', boundMethod);
      
      expect(emitter.listenerCount('test-event')).toBe(1);
      
      emitter.emit('test-event', 'test-data');
      
      // Can't easily test return value, but it shouldn't throw
    });

    it('should handle very long event names', () => {
      const longEventName = 'a'.repeat(1000);
      
      emitter.on(longEventName, mockListener);
      expect(emitter.listenerCount(longEventName)).toBe(1);
      
      emitter.emit(longEventName, 'test');
      expect(mockListener).toHaveBeenCalledWith('test');
    });

    it('should handle emitting with many arguments', () => {
      const manyArgs = Array.from({ length: 100 }, (_, i) => `arg${i}`);
      
      emitter.on('test-event', mockListener);
      emitter.emit('test-event', ...manyArgs);
      
      expect(mockListener).toHaveBeenCalledWith(...manyArgs);
    });
  });
});