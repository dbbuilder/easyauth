import { Logger } from '../types';

/**
 * Console-based logger implementation
 */
export class ConsoleLogger implements Logger {
  private readonly isEnabled: boolean;
  
  constructor(enabled = false) {
    this.isEnabled = enabled;
  }
  
  debug(message: string, ...args: unknown[]): void {
    if (this.isEnabled && typeof console !== 'undefined') {
      console.debug(`[EasyAuth Debug] ${message}`, ...args);
    }
  }
  
  info(message: string, ...args: unknown[]): void {
    if (this.isEnabled && typeof console !== 'undefined') {
      console.info(`[EasyAuth Info] ${message}`, ...args);
    }
  }
  
  warn(message: string, ...args: unknown[]): void {
    if (this.isEnabled && typeof console !== 'undefined') {
      console.warn(`[EasyAuth Warning] ${message}`, ...args);
    }
  }
  
  error(message: string, ...args: unknown[]): void {
    if (this.isEnabled && typeof console !== 'undefined') {
      console.error(`[EasyAuth Error] ${message}`, ...args);
    }
  }
}

/**
 * No-op logger implementation (disabled logging)
 */
export class NoOpLogger implements Logger {
  debug(_message: string, ..._args: unknown[]): void {
    // No-op - parameters prefixed with _ to indicate intentionally unused
  }
  
  info(_message: string, ..._args: unknown[]): void {
    // No-op - parameters prefixed with _ to indicate intentionally unused
  }
  
  warn(_message: string, ..._args: unknown[]): void {
    // No-op - parameters prefixed with _ to indicate intentionally unused
  }
  
  error(_message: string, ..._args: unknown[]): void {
    // No-op - parameters prefixed with _ to indicate intentionally unused
  }
}