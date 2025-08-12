/**
 * Test setup for React components and hooks testing
 */

import '@testing-library/jest-dom';

// Create mock client that can be shared across tests
export const mockClient = {
  config: { baseUrl: 'https://mock.api.com' },
  session: { isAuthenticated: false, user: null },
  login: jest.fn(),
  logout: jest.fn(),
  refresh: jest.fn(),
  getSession: jest.fn().mockReturnValue({ isAuthenticated: false, user: null }),
  isAuthenticated: jest.fn().mockReturnValue(false),
  getUser: jest.fn().mockReturnValue(null),
  getToken: jest.fn().mockReturnValue(null),
  setToken: jest.fn(),
  clearToken: jest.fn(),
  on: jest.fn(),
  off: jest.fn(),
};

// Mock EasyAuth SDK for testing
jest.mock('@easyauth/sdk', () => {
  return {
    EasyAuthClient: jest.fn(() => mockClient),
    __esModule: true,
  };
});

// Setup global test helpers
beforeEach(() => {
  jest.clearAllMocks();
  
  // Reset mock client to default state
  mockClient.getSession.mockReturnValue({ isAuthenticated: false, user: null });
  mockClient.isAuthenticated.mockReturnValue(false);
  mockClient.getUser.mockReturnValue(null);
  mockClient.getToken.mockReturnValue(null);
});