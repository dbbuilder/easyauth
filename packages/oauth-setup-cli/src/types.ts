/**
 * Type definitions for OAuth Setup CLI
 */

export type ProviderType = 'google' | 'facebook' | 'apple' | 'azure-b2c';

export type OutputFormat = 'env' | 'json' | 'yaml';

export interface SetupConfig {
  project: string;
  domain: string;
  providers: ProviderType[];
  outputFormat: OutputFormat;
  outputFile?: string;
  interactive: boolean;
  dryRun: boolean;
  force: boolean;
  verbose: boolean;
}

export interface SetupResult {
  provider: ProviderType;
  credentials: Record<string, string>;
  metadata?: Record<string, any>;
}

export interface ProviderCredentials {
  google?: {
    clientId: string;
    clientSecret: string;
    projectId?: string;
  };
  facebook?: {
    appId: string;
    appSecret: string;
  };
  apple?: {
    serviceId: string;
    teamId: string;
    keyId: string;
    privateKey?: string;
  };
  'azure-b2c'?: {
    clientId: string;
    tenantId: string;
    resourceGroup?: string;
  };
}

export interface OAuthApp {
  id: string;
  name: string;
  clientId: string;
  clientSecret?: string;
  redirectUris: string[];
  scopes?: string[];
  metadata?: Record<string, any>;
}

export interface ProviderSetupOptions {
  project: string;
  domain: string;
  dryRun: boolean;
  interactive: boolean;
  verbose: boolean;
}

export interface ManualSetupInstructions {
  provider: ProviderType;
  steps: string[];
  urls: string[];
  notes?: string[];
}