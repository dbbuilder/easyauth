import { EasyAuthClient } from './client';

// Mock the AuthApi
jest.mock('./api');

describe('EasyAuthClient', () => {
  let client: EasyAuthClient;

  beforeEach(() => {
    client = new EasyAuthClient({
      baseUrl: 'http://localhost:5000',
      debug: false
    });
  });

  afterEach(() => {
    client.destroy();
  });

  it('should initialize with default state', () => {
    const state = client.currentState;
    
    expect(state.isLoading).toBe(true);
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.error).toBeNull();
  });

  it('should allow state change listeners', () => {
    const mockCallback = jest.fn();
    
    const unsubscribe = client.onStateChange(mockCallback);
    
    // Trigger a state change
    client.clearError();
    
    expect(mockCallback).toHaveBeenCalled();
    
    // Clean up
    unsubscribe();
  });

  it('should dispatch custom events on state changes', (done) => {
    client.addEventListener('statechange', (event: any) => {
      expect(event.detail).toBeDefined();
      expect(event.detail.isLoading).toBeDefined();
      done();
    });

    // Trigger a state change
    client.clearError();
  });

  it('should handle role checking correctly', () => {
    // Mock authenticated state with roles
    (client as any).state = {
      isAuthenticated: true,
      user: {
        id: '1',
        roles: ['admin', 'user'],
        isVerified: true
      }
    };

    expect(client.hasRole('admin')).toBe(true);
    expect(client.hasRole('moderator')).toBe(false);
    expect(client.hasAnyRole(['admin', 'moderator'])).toBe(true);
    expect(client.hasAnyRole(['moderator', 'guest'])).toBe(false);
  });

  it('should handle permission checking correctly', () => {
    // Mock authenticated state with permissions
    (client as any).state = {
      isAuthenticated: true,
      user: {
        id: '1',
        roles: ['user'],
        permissions: ['read', 'write'],
        isVerified: true
      }
    };

    expect(client.hasPermission('read')).toBe(true);
    expect(client.hasPermission('delete')).toBe(false);
    expect(client.hasAnyPermission(['read', 'delete'])).toBe(true);
    expect(client.hasAnyPermission(['delete', 'admin'])).toBe(false);
  });

  it('should return false for auth checks when not authenticated', () => {
    expect(client.hasRole('admin')).toBe(false);
    expect(client.hasPermission('read')).toBe(false);
    expect(client.hasAnyRole(['admin', 'user'])).toBe(false);
    expect(client.hasAnyPermission(['read', 'write'])).toBe(false);
  });
});