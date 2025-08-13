/**
 * OAuth Setup Manager
 * 
 * Manages the complete OAuth application setup process across all providers,
 * handling CLI tool installation, authentication, and credential management.
 */

import { promises as fs } from 'fs';
// import path from 'path';
import { execSync } from 'child_process';
import chalk from 'chalk';
import inquirer from 'inquirer';
import { stringify as yamlStringify } from 'yaml';
import { CLIInstaller } from './cli-installer';
import { 
  SetupConfig, 
  SetupResult, 
  ProviderType, 
  ProviderSetupOptions,
  ManualSetupInstructions,
  // OAuthApp
} from './types';

export class OAuthSetupManager {
  private cliInstaller: CLIInstaller;

  constructor(private config: SetupConfig) {
    this.cliInstaller = new CLIInstaller(config.verbose);
  }

  /**
   * Check and install all prerequisites for the setup
   */
  async checkPrerequisites(): Promise<void> {
    // Install required CLI tools
    await this.cliInstaller.ensureToolsAvailable(this.config.providers);
    
    // Authenticate with CLI tools
    await this.cliInstaller.authenticateTools(this.config.providers);
  }

  /**
   * Setup OAuth application for a specific provider
   */
  async setupProvider(provider: ProviderType): Promise<SetupResult | null> {
    const options: ProviderSetupOptions = {
      project: this.config.project,
      domain: this.config.domain,
      dryRun: this.config.dryRun,
      interactive: this.config.interactive,
      verbose: this.config.verbose
    };

    switch (provider) {
      case 'google':
        return await this.setupGoogle(options);
      case 'facebook':
        return await this.setupFacebook(options);
      case 'apple':
        return await this.setupApple(options);
      case 'azure-b2c':
        return await this.setupAzureB2C(options);
      default:
        throw new Error(`Unknown provider: ${provider}`);
    }
  }

  /**
   * Save all credentials to specified output format
   */
  async saveCredentials(results: SetupResult[]): Promise<string> {
    const outputFile = this.config.outputFile || this.getDefaultOutputFile();
    
    switch (this.config.outputFormat) {
      case 'env':
        await this.saveAsEnv(results, outputFile);
        break;
      case 'json':
        await this.saveAsJson(results, outputFile);
        break;
      case 'yaml':
        await this.saveAsYaml(results, outputFile);
        break;
    }
    
    return outputFile;
  }

  /**
   * Generate TypeScript integration code
   */
  async generateIntegrationCode(results: SetupResult[]): Promise<string> {
    const codeFile = 'easyauth-config.ts';
    const code = this.generateTypeScriptConfig(results);
    
    await fs.writeFile(codeFile, code, 'utf8');
    return codeFile;
  }

  // Provider-specific setup methods

  private async setupGoogle(options: ProviderSetupOptions): Promise<SetupResult | null> {
    if (options.verbose) {
      console.log(chalk.gray('Setting up Google OAuth with Cloud SDK...'));
    }

    const projectId = this.formatProjectName(options.project);
    const appName = `${options.project} EasyAuth`;

    if (options.dryRun) {
      console.log(chalk.magenta(`[DRY RUN] Would create Google Cloud project: ${projectId}`));
      console.log(chalk.magenta(`[DRY RUN] Would create OAuth 2.0 client: ${appName}`));
      return {
        provider: 'google',
        credentials: {
          clientId: 'mock-google-client-id.apps.googleusercontent.com',
          clientSecret: 'mock-google-client-secret',
          projectId
        }
      };
    }

    try {
      // Get current project or create new one
      const currentProject = await this.ensureGoogleProject(projectId, options);
      
      // Enable required APIs
      console.log(chalk.blue('   📡 Enabling required APIs...'));
      execSync('gcloud services enable oauth2.googleapis.com --quiet', { stdio: 'pipe' });
      execSync('gcloud services enable plus.googleapis.com --quiet', { stdio: 'pipe' });
      
      // Google Cloud CLI has limited OAuth client creation, so we guide manual setup
      const credentials = await this.guideManualGoogleSetup(appName, options);
      
      return {
        provider: 'google',
        credentials: {
          ...credentials,
          projectId: currentProject
        }
      };
      
    } catch (error) {
      if (options.interactive) {
        console.log(chalk.yellow('   ⚠️  Automated setup failed, falling back to manual setup...'));
        return await this.fallbackToManualSetup('google', options);
      }
      throw error;
    }
  }

  private async setupFacebook(options: ProviderSetupOptions): Promise<SetupResult | null> {
    const { FacebookProvisioner } = await import('./facebook-provisioner');
    
    const facebookConfig = {
      project: options.project,
      domain: options.domain,
      appName: `${options.project} EasyAuth`,
      contactEmail: `admin@${options.domain}`,
      category: 'BUSINESS',
      dryRun: options.dryRun,
      verbose: options.verbose,
      useTestApp: true,
      interactive: options.interactive
    };

    const provisioner = new FacebookProvisioner(facebookConfig);
    
    try {
      const result = await provisioner.provision();
      
      return {
        provider: 'facebook',
        credentials: {
          appId: result.appId,
          appSecret: result.appSecret
        },
        metadata: {
          appName: result.appName,
          testAppId: result.testAppId,
          webhookUrl: result.webhookUrl,
          permissions: result.permissions,
          businessVerificationRequired: result.businessVerificationRequired
        }
      };
    } catch (error) {
      if (options.interactive) {
        console.log(chalk.yellow('   ⚠️  Automated setup failed, falling back to manual setup...'));
        return await this.fallbackToManualSetup('facebook', options);
      }
      throw error;
    }
  }

  private async setupApple(options: ProviderSetupOptions): Promise<SetupResult | null> {
    const { AppleProvisioner } = await import('./apple-provisioner');
    
    const appleConfig = {
      project: options.project,
      domain: options.domain,
      appName: `${options.project} EasyAuth`,
      dryRun: options.dryRun,
      verbose: options.verbose,
      interactive: options.interactive,
      generateCertificates: false, // Web-only setup
      setupTestEnvironment: true
    };

    const provisioner = new AppleProvisioner(appleConfig);
    
    try {
      const result = await provisioner.provision();
      
      return {
        provider: 'apple',
        credentials: {
          serviceId: result.serviceId,
          teamId: result.teamId,
          keyId: result.keyId,
          privateKey: result.privateKey
        },
        metadata: {
          bundleId: result.bundleId,
          appId: result.appId,
          domains: result.domains,
          returnUrls: result.returnUrls,
          certificateId: result.certificateId,
          developmentTeam: result.developmentTeam,
          testConfiguration: result.testConfiguration
        }
      };
    } catch (error) {
      if (options.interactive) {
        console.log(chalk.yellow('   ⚠️  Automated setup failed, falling back to manual setup...'));
        return await this.fallbackToManualSetup('apple', options);
      }
      throw error;
    }
  }

  private async setupAzureB2C(options: ProviderSetupOptions): Promise<SetupResult | null> {
    if (options.verbose) {
      console.log(chalk.gray('Setting up Azure B2C with comprehensive Graph API setup...'));
    }

    // Check if user wants comprehensive B2C setup or basic setup
    let useComprehensiveSetup = false;
    if (options.interactive) {
      const { setupType } = await inquirer.prompt([
        {
          type: 'list',
          name: 'setupType',
          message: 'Choose Azure B2C setup type:',
          choices: [
            {
              name: '🚀 Comprehensive Setup (Graph API) - Full B2C tenant with user flows, policies, and providers',
              value: 'comprehensive'
            },
            {
              name: '⚡ Basic Setup (Azure CLI) - Simple app registration only',
              value: 'basic'
            }
          ],
          default: 'comprehensive'
        }
      ]);
      useComprehensiveSetup = setupType === 'comprehensive';
    }

    if (useComprehensiveSetup) {
      return await this.setupAzureB2CComprehensive(options);
    } else {
      return await this.setupAzureB2CBasic(options);
    }
  }

  private async setupAzureB2CComprehensive(options: ProviderSetupOptions): Promise<SetupResult | null> {
    const { AzureB2CGraphProvisioner } = await import('./azure-b2c-graph-provisioner');
    
    const tenantName = this.formatProjectName(options.project);
    const b2cConfig = {
      project: options.project,
      domain: options.domain,
      tenantName,
      resourceGroup: `${tenantName}-rg`,
      location: 'West US 2',
      subscriptionId: '', // Will be detected from Azure CLI
      
      // B2C Configuration
      enableCustomPolicies: true,
      enableConditionalAccess: false,
      enableMFA: true,
      enableSelfServicePasswordReset: true,
      
      // User Flow Configuration
      userFlows: {
        signUpSignIn: true,
        profileEdit: true,
        passwordReset: true
      },
      
      // Identity Providers
      identityProviders: {
        google: false, // Will be configured separately
        facebook: false,
        apple: false,
        microsoft: true,
        linkedin: false,
        twitter: false
      },
      
      // Branding
      branding: {
        companyName: options.project,
        backgroundColor: '#ffffff',
        primaryColor: '#0078d4'
      },
      
      // Advanced features
      apiConnectors: false,
      customAttributes: ['department', 'jobTitle'],
      localization: ['en', 'es', 'fr'],
      
      dryRun: options.dryRun,
      verbose: options.verbose,
      interactive: options.interactive
    };

    const provisioner = new AzureB2CGraphProvisioner(b2cConfig);
    
    try {
      const result = await provisioner.provision();
      
      return {
        provider: 'azure-b2c',
        credentials: {
          clientId: result.applications.webApp.appId,
          clientSecret: result.applications.webApp.clientSecret,
          tenantId: result.tenant.domain,
          tenantName: result.tenant.displayName,
          authority: result.endpoints.authority
        },
        metadata: {
          tenantId: result.tenant.id,
          applications: result.applications,
          userFlows: result.userFlows,
          customPolicies: result.customPolicies,
          identityProviders: result.identityProviders,
          endpoints: result.endpoints,
          settings: result.settings,
          setupType: 'comprehensive'
        }
      };
    } catch (error) {
      if (options.interactive) {
        console.log(chalk.yellow('   ⚠️  Comprehensive setup failed, falling back to basic setup...'));
        return await this.setupAzureB2CBasic(options);
      }
      throw error;
    }
  }

  private async setupAzureB2CBasic(options: ProviderSetupOptions): Promise<SetupResult | null> {
    const tenantName = this.formatProjectName(options.project);
    const appName = `${options.project}-easyauth`;

    if (options.dryRun) {
      return {
        provider: 'azure-b2c',
        credentials: {
          clientId: 'mock-azure-client-id',
          tenantId: `${tenantName}.onmicrosoft.com`
        }
      };
    }

    try {
      // Create resource group
      const resourceGroup = `${tenantName}-rg`;
      console.log(chalk.blue('   🏢 Creating resource group...'));
      execSync(`az group create --name ${resourceGroup} --location "West US 2" --output none`, { stdio: 'pipe' });
      
      // Create app registration
      console.log(chalk.blue('   📱 Creating app registration...'));
      const redirectUris = this.getRedirectUris('azure-b2c', options.domain);
      
      const appResult = execSync(
        `az ad app create --display-name "${appName}" --web-redirect-uris ${redirectUris.join(' ')} --query "appId" -o tsv`,
        { encoding: 'utf8', stdio: 'pipe' }
      ).trim();

      return {
        provider: 'azure-b2c',
        credentials: {
          clientId: appResult,
          tenantId: `${tenantName}.onmicrosoft.com`,
          resourceGroup
        },
        metadata: {
          setupType: 'basic'
        }
      };

    } catch (error) {
      if (options.interactive) {
        console.log(chalk.yellow('   ⚠️  Automated setup failed, falling back to manual setup...'));
        return await this.fallbackToManualSetup('azure-b2c', options);
      }
      throw error;
    }
  }

  // Helper methods

  private async ensureGoogleProject(projectId: string, options: ProviderSetupOptions): Promise<string> {
    try {
      const currentProject = execSync('gcloud config get-value project', { encoding: 'utf8', stdio: 'pipe' }).trim();
      if (currentProject && currentProject !== '(unset)') {
        return currentProject;
      }
    } catch {
      // No project set
    }

    if (options.interactive) {
      const { createNew } = await inquirer.prompt([
        {
          type: 'confirm',
          name: 'createNew',
          message: `Create new Google Cloud project '${projectId}'?`,
          default: true
        }
      ]);

      if (createNew) {
        console.log(chalk.blue('   🏗️  Creating Google Cloud project...'));
        execSync(`gcloud projects create ${projectId} --name="${options.project}" --quiet`, { stdio: 'pipe' });
        execSync(`gcloud config set project ${projectId} --quiet`, { stdio: 'pipe' });
        return projectId;
      } else {
        const { existingProject } = await inquirer.prompt([
          {
            type: 'input',
            name: 'existingProject',
            message: 'Enter existing project ID:'
          }
        ]);
        execSync(`gcloud config set project ${existingProject} --quiet`, { stdio: 'pipe' });
        return existingProject;
      }
    } else {
      throw new Error('No Google Cloud project configured. Use interactive mode or set up project manually.');
    }
  }

  private async guideManualGoogleSetup(_appName: string, options: ProviderSetupOptions) {
    const instructions = this.getManualInstructions('google', options);
    
    console.log(chalk.yellow('\n   📋 Manual Google OAuth Setup Required:'));
    instructions.steps.forEach((step, index) => {
      console.log(chalk.white(`   ${index + 1}. ${step}`));
    });
    
    instructions.urls.forEach(url => {
      console.log(chalk.blue(`   🔗 ${url}`));
    });

    if (!options.interactive) {
      throw new Error('Manual setup required - use interactive mode');
    }

    const credentials = await inquirer.prompt([
      {
        type: 'input',
        name: 'clientId',
        message: 'Google Client ID:',
        validate: (input) => input.includes('.apps.googleusercontent.com') || 'Must be a valid Google Client ID'
      },
      {
        type: 'password',
        name: 'clientSecret',
        message: 'Google Client Secret:',
        mask: '*'
      }
    ]);

    return credentials;
  }

  private async guideManualFacebookSetup(options: ProviderSetupOptions) {
    const instructions = this.getManualInstructions('facebook', options);
    
    console.log(chalk.yellow('\n   📋 Manual Facebook Setup Required:'));
    instructions.steps.forEach((step, index) => {
      console.log(chalk.white(`   ${index + 1}. ${step}`));
    });

    if (!options.interactive) {
      throw new Error('Manual setup required - use interactive mode');
    }

    const credentials = await inquirer.prompt([
      {
        type: 'input',
        name: 'appId',
        message: 'Facebook App ID:',
        validate: (input) => /^\d{15,16}$/.test(input) || 'Must be a 15-16 digit Facebook App ID'
      },
      {
        type: 'password',
        name: 'appSecret',
        message: 'Facebook App Secret:',
        mask: '*'
      }
    ]);

    return {
      provider: 'facebook' as const,
      credentials: {
        appId: credentials.appId,
        appSecret: credentials.appSecret
      }
    };
  }

  private async guideManualAppleSetup(options: ProviderSetupOptions) {
    const serviceId = `com.${options.domain.replace(/\./g, '-')}.easyauth`;
    const instructions = this.getManualInstructions('apple', options);
    
    console.log(chalk.yellow('\n   📋 Manual Apple Setup Required:'));
    instructions.steps.forEach((step, index) => {
      console.log(chalk.white(`   ${index + 1}. ${step}`));
    });

    if (!options.interactive) {
      throw new Error('Manual setup required - use interactive mode');
    }

    const credentials = await inquirer.prompt([
      {
        type: 'input',
        name: 'serviceId',
        message: `Service ID (${serviceId}):`,
        default: serviceId
      },
      {
        type: 'input',
        name: 'teamId',
        message: 'Team ID:',
        validate: (input) => /^[A-Z0-9]{10}$/.test(input) || 'Must be a 10-character team ID'
      },
      {
        type: 'input',
        name: 'keyId',
        message: 'Key ID:',
        validate: (input) => /^[A-Z0-9]{10}$/.test(input) || 'Must be a 10-character key ID'
      },
      {
        type: 'input',
        name: 'privateKeyPath',
        message: 'Private key file path (optional):'
      }
    ]);

    let privateKey = '';
    if (credentials.privateKeyPath) {
      try {
        privateKey = await fs.readFile(credentials.privateKeyPath, 'utf8');
      } catch {
        console.log(chalk.yellow('   ⚠️  Could not read private key file'));
      }
    }

    return {
      provider: 'apple' as const,
      credentials: {
        serviceId: credentials.serviceId,
        teamId: credentials.teamId,
        keyId: credentials.keyId,
        ...(privateKey && { privateKey })
      }
    };
  }

  private async fallbackToManualSetup(provider: ProviderType, options: ProviderSetupOptions): Promise<SetupResult | null> {
    const instructions = this.getManualInstructions(provider, options);
    
    console.log(chalk.yellow(`\n   📋 Manual ${provider} Setup:`));
    instructions.steps.forEach((step, index) => {
      console.log(chalk.white(`   ${index + 1}. ${step}`));
    });

    if (!options.interactive) {
      return null;
    }

    // Provider-specific credential collection
    // Implementation would depend on provider...
    return null;
  }

  private getManualInstructions(provider: ProviderType, options: ProviderSetupOptions): ManualSetupInstructions {
    const redirectUris = this.getRedirectUris(provider, options.domain);
    
    switch (provider) {
      case 'google':
        return {
          provider,
          steps: [
            'Go to Google Cloud Console',
            'Navigate to APIs & Services > Credentials',
            'Click "Create Credentials" > "OAuth 2.0 Client ID"',
            'Choose "Web application" as application type',
            `Set name: "${options.project} EasyAuth"`,
            `Add authorized redirect URIs: ${redirectUris.join(', ')}`,
            'Click "Create" and copy the credentials'
          ],
          urls: ['https://console.cloud.google.com/apis/credentials']
        };
      
      case 'facebook':
        return {
          provider,
          steps: [
            'Go to Facebook Developers',
            'Click "Create App" > Choose "Consumer"',
            `App name: "${options.project} EasyAuth"`,
            `Contact email: admin@${options.domain}`,
            'Add "Facebook Login" product',
            'Configure Valid OAuth Redirect URIs',
            `Add redirect URIs: ${redirectUris.join(', ')}`,
            `⚠️ CRITICAL: Use /api/EAuth/ paths, NOT /api/auth/!`
          ],
          urls: ['https://developers.facebook.com/']
        };
      
      case 'apple':
        return {
          provider,
          steps: [
            'Go to Apple Developer Account',
            'Navigate to "Certificates, Identifiers & Profiles"',
            'Create new Service ID',
            `Identifier: com.${options.domain.replace(/\./g, '-')}.easyauth`,
            `Description: "${options.project} EasyAuth Service"`,
            'Configure Sign In with Apple',
            `Add domains: ${options.domain}, www.${options.domain}`,
            `Add return URLs: ${redirectUris.join(', ')}`,
            'Create private key for Sign In with Apple'
          ],
          urls: ['https://developer.apple.com/account/']
        };
      
      case 'azure-b2c':
        return {
          provider,
          steps: [
            'Go to Azure Portal',
            'Create Azure B2C tenant',
            'Register new application',
            `Name: "${options.project}-easyauth"`,
            'Configure redirect URIs',
            `Add redirect URIs: ${redirectUris.join(', ')}`,
            `⚠️ CRITICAL: Use /api/EAuth/ paths, NOT /api/auth/!`,
            'Create user flows for sign-up/sign-in'
          ],
          urls: ['https://portal.azure.com/']
        };
      
      default:
        throw new Error(`Unknown provider: ${provider}`);
    }
  }

  private getRedirectUris(provider: ProviderType, domain: string): string[] {
    // CRITICAL: EasyAuth Framework uses EXCLUSIVE /api/EAuth/ paths
    // NOT /api/auth/ or other variants!
    const baseUris = [
      `https://${domain}/api/EAuth/${provider}/callback`,
      `https://www.${domain}/api/EAuth/${provider}/callback`,
      `http://localhost:3000/api/EAuth/${provider}/callback`
    ];
    
    // Add development URIs with correct EasyAuth path structure
    baseUris.push(
      `http://localhost:3000/api/EAuth/${provider}/callback`,
      `http://localhost:8080/api/EAuth/${provider}/callback`,
      `http://127.0.0.1:3000/api/EAuth/${provider}/callback`
    );
    
    return [...new Set(baseUris)]; // Remove duplicates
  }

  private formatProjectName(name: string): string {
    return name.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
  }

  private getDefaultOutputFile(): string {
    switch (this.config.outputFormat) {
      case 'env': return '.env.oauth';
      case 'json': return 'oauth-credentials.json';
      case 'yaml': return 'oauth-credentials.yaml';
      default: return '.env.oauth';
    }
  }

  // File generation methods

  private async saveAsEnv(results: SetupResult[], filename: string): Promise<void> {
    const lines = [
      '# EasyAuth OAuth Credentials',
      `# Generated on ${new Date().toISOString()}`,
      `# Project: ${this.config.project}`,
      `# Domain: ${this.config.domain}`,
      ''
    ];

    results.forEach(result => {
      lines.push(`# ${result.provider.charAt(0).toUpperCase() + result.provider.slice(1)} OAuth`);
      
      Object.entries(result.credentials).forEach(([key, value]) => {
        const envKey = `${result.provider.toUpperCase().replace('-', '_')}_${key.toUpperCase().replace(/([A-Z])/g, '_$1')}`;
        if (typeof value === 'string' && value.includes('\n')) {
          // Handle multi-line values like private keys
          lines.push(`${envKey}="${value.replace(/\n/g, '\\n')}"`);
        } else {
          lines.push(`${envKey}=${value}`);
        }
      });
      
      lines.push('');
    });

    // Add EasyAuth configuration
    lines.push('# EasyAuth Configuration');
    lines.push(`EASYAUTH_BASE_URL=https://${this.config.domain}`);
    lines.push('EASYAUTH_ENVIRONMENT=production');

    await fs.writeFile(filename, lines.join('\n'), 'utf8');
  }

  private async saveAsJson(results: SetupResult[], filename: string): Promise<void> {
    const credentials: Record<string, any> = {};
    results.forEach(result => {
      credentials[result.provider] = result.credentials;
    });

    const data = {
      metadata: {
        generated: new Date().toISOString(),
        project: this.config.project,
        domain: this.config.domain,
        providers: results.map(r => r.provider)
      },
      credentials,
      easyauth: {
        baseUrl: `https://${this.config.domain}`,
        environment: 'production'
      }
    };

    await fs.writeFile(filename, JSON.stringify(data, null, 2), 'utf8');
  }

  private async saveAsYaml(results: SetupResult[], filename: string): Promise<void> {
    const credentials: Record<string, any> = {};
    results.forEach(result => {
      credentials[result.provider] = result.credentials;
    });

    const data = {
      metadata: {
        generated: new Date().toISOString(),
        project: this.config.project,
        domain: this.config.domain,
        providers: results.map(r => r.provider)
      },
      credentials,
      easyauth: {
        baseUrl: `https://${this.config.domain}`,
        environment: 'production'
      }
    };

    await fs.writeFile(filename, yamlStringify(data), 'utf8');
  }

  private generateTypeScriptConfig(results: SetupResult[]): string {
    const providersCode = results.map(result => {
      const envPrefix = result.provider.toUpperCase().replace('-', '_');
      
      switch (result.provider) {
        case 'google':
          return `    google: {
      clientId: process.env.${envPrefix}_CLIENT_ID!,
      clientSecret: process.env.${envPrefix}_CLIENT_SECRET!
    }`;
        case 'facebook':
          return `    facebook: {
      appId: process.env.${envPrefix}_APP_ID!,
      appSecret: process.env.${envPrefix}_APP_SECRET!
    }`;
        case 'apple':
          return `    apple: {
      serviceId: process.env.${envPrefix}_SERVICE_ID!,
      teamId: process.env.${envPrefix}_TEAM_ID!,
      keyId: process.env.${envPrefix}_KEY_ID!,
      privateKey: process.env.${envPrefix}_PRIVATE_KEY!
    }`;
        case 'azure-b2c':
          const isComprehensive = result.metadata?.setupType === 'comprehensive';
          if (isComprehensive) {
            return `    'azure-b2c': {
      clientId: process.env.${envPrefix}_CLIENT_ID!,
      clientSecret: process.env.${envPrefix}_CLIENT_SECRET!,
      tenantId: process.env.${envPrefix}_TENANT_ID!,
      authority: process.env.${envPrefix}_AUTHORITY!,
      userFlows: {
        signUpSignIn: 'B2C_1_SignUpSignIn',
        profileEdit: 'B2C_1_ProfileEdit',
        passwordReset: 'B2C_1_PasswordReset'
      }
    }`;
          } else {
            return `    'azure-b2c': {
      clientId: process.env.${envPrefix}_CLIENT_ID!,
      tenantId: process.env.${envPrefix}_TENANT_ID!
    }`;
          }
        default:
          return '';
      }
    }).filter(Boolean);

    return `// EasyAuth Configuration
// Generated on ${new Date().toISOString()}

import { EnhancedEasyAuthClient } from '@easyauth/sdk';

export const easyAuthConfig = {
  baseUrl: process.env.EASYAUTH_BASE_URL || 'https://${this.config.domain}',
  environment: process.env.EASYAUTH_ENVIRONMENT as 'development' | 'staging' | 'production' || 'production',
  
  // Provider credentials from environment variables
  providers: {
${providersCode.join(',\n')}
  }
};

// Initialize EasyAuth client
export const easyAuthClient = new EnhancedEasyAuthClient(easyAuthConfig);

// Export for convenience
export default easyAuthClient;
`;
  }
}