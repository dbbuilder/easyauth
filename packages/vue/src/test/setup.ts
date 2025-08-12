/**
 * Test setup file for EasyAuth Vue package
 * Configures global test environment and mocks
 */

import { vi } from 'vitest'
import { config } from '@vue/test-utils'

// Mock window.open for popup tests
Object.defineProperty(window, 'open', {
  writable: true,
  value: vi.fn(() => ({
    close: vi.fn(),
    closed: false,
    postMessage: vi.fn()
  }))
})

// Mock localStorage
Object.defineProperty(window, 'localStorage', {
  value: {
    getItem: vi.fn(() => null),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn()
  },
  writable: true
})

// Configure Vue Test Utils global properties
config.global.mocks = {
  // Add any global mocks here
}

// Console setup for cleaner test output
const originalError = console.error
console.error = (...args: any[]) => {
  // Suppress Vue warning messages in tests
  if (
    typeof args[0] === 'string' && 
    (args[0].includes('[Vue warn]') || args[0].includes('Warning'))
  ) {
    return
  }
  originalError.call(console, ...args)
}