/**
 * OAuth Setup Manager Tests
 * 
 * Basic tests to validate OAuth setup functionality
 */

import { OAuthSetupManager } from '../oauth-setup-manager';
import { SetupConfig } from '../types';

describe('OAuthSetupManager', () => {
  const mockConfig: SetupConfig = {
    project: 'TestApp',
    domain: 'test.com',
    providers: ['google'],
    outputFormat: 'env',
    interactive: false,
    dryRun: true,
    verbose: false,
    force: false
  };

  let setupManager: OAuthSetupManager;

  beforeEach(() => {
    setupManager = new OAuthSetupManager(mockConfig);
  });

  test('should initialize with valid configuration', () => {
    expect(setupManager).toBeInstanceOf(OAuthSetupManager);
  });

  test('should handle dry run mode for Google setup', async () => {
    const result = await setupManager.setupProvider('google');
    
    expect(result).toBeDefined();
    expect(result?.provider).toBe('google');
    expect(result?.credentials).toBeDefined();
    expect(result?.credentials.clientId).toContain('mock-google-client-id');
  });

  test('should generate default output filename', () => {
    const manager = new OAuthSetupManager({
      ...mockConfig,
      outputFormat: 'json'
    });
    
    // Access private method for testing
    const getDefaultOutputFile = (manager as any).getDefaultOutputFile();
    expect(getDefaultOutputFile).toBe('oauth-credentials.json');
  });

  test('should format project names correctly', () => {
    const manager = new OAuthSetupManager(mockConfig);
    
    // Access private method for testing
    const formatProjectName = (manager as any).formatProjectName('My-App Name!');
    expect(formatProjectName).toBe('myappname');
  });

  test('should generate redirect URIs correctly', () => {
    const manager = new OAuthSetupManager(mockConfig);
    
    // Access private method for testing  
    const redirectUris = (manager as any).getRedirectUris('google', 'example.com');
    
    expect(redirectUris).toContain('https://example.com/auth/google/callback');
    expect(redirectUris).toContain('https://www.example.com/auth/google/callback');
    expect(redirectUris).toContain('http://localhost:3000/auth/google/callback');
  });
});

describe('Configuration Validation', () => {
  test('should validate required configuration fields', () => {
    const validConfig: SetupConfig = {
      project: 'TestApp',
      domain: 'test.com',
      providers: ['google'],
      outputFormat: 'env',
      interactive: true,
      dryRun: false,
      verbose: false,
      force: false
    };

    expect(() => new OAuthSetupManager(validConfig)).not.toThrow();
  });

  test('should handle empty providers array', () => {
    const configWithNoProviders: SetupConfig = {
      project: 'TestApp', 
      domain: 'test.com',
      providers: [],
      outputFormat: 'env',
      interactive: false,
      dryRun: true,
      verbose: false,
      force: false
    };

    const manager = new OAuthSetupManager(configWithNoProviders);
    expect(manager).toBeInstanceOf(OAuthSetupManager);
  });
});

describe('TypeScript Configuration Generation', () => {
  test('should generate valid TypeScript configuration', async () => {
    const setupManager = new OAuthSetupManager({
      project: 'TestApp',
      domain: 'test.com', 
      providers: ['google', 'facebook'],
      outputFormat: 'env',
      interactive: false,
      dryRun: true,
      verbose: false,
      force: false
    });

    const googleResult = await setupManager.setupProvider('google');
    const facebookResult = await setupManager.setupProvider('facebook');
    
    expect(googleResult).toBeDefined();
    expect(facebookResult).toBeDefined();

    const results = [googleResult!, facebookResult!];
    
    // Access private method for testing
    const tsConfig = (setupManager as any).generateTypeScriptConfig(results);
    
    expect(tsConfig).toContain('EnhancedEasyAuthClient');
    expect(tsConfig).toContain('GOOGLE_CLIENT_ID');
    expect(tsConfig).toContain('FACEBOOK_APP_ID');
    expect(tsConfig).toContain('test.com');
  });
});