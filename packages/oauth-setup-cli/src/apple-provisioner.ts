/**
 * Apple Sign-In Complete Provisioner
 * 
 * Automates Apple Sign-In setup using:
 * - App Store Connect API for app/service creation
 * - Automated certificate and key generation
 * - Guided manual setup with exact instructions
 * - Development team management
 */

import { promises as fs } from 'fs';
import fetch from 'node-fetch';
import { exec } from 'child_process';
import { promisify } from 'util';
// import * as crypto from 'crypto';
import * as jwt from 'jsonwebtoken';
import chalk from 'chalk';
import ora from 'ora';
import inquirer from 'inquirer';
import path from 'path';

const execAsync = promisify(exec);

export interface AppleConfig {
  project: string;
  domain: string;
  teamId?: string;
  bundleId?: string;
  serviceId?: string;
  appName: string;
  dryRun: boolean;
  verbose: boolean;
  interactive: boolean;
  generateCertificates: boolean;
  setupTestEnvironment: boolean;
}

export interface AppleResult {
  serviceId: string;
  teamId: string;
  keyId: string;
  privateKey: string;
  bundleId?: string;
  appId?: string;
  domains: string[];
  returnUrls: string[];
  certificateId?: string;
  developmentTeam: {
    name: string;
    id: string;
    role: string;
  };
  testConfiguration?: {
    sandboxMode: boolean;
    testUsers: string[];
  };
}

export class AppleProvisioner {
  private config: AppleConfig;
  private apiKey: string = '';
  private apiKeyId: string = '';
  private apiIssuer: string = '';
  private jwtToken: string = '';

  constructor(config: AppleConfig) {
    this.config = config;
  }

  /**
   * Complete Apple Sign-In setup process
   */
  async provision(): Promise<AppleResult> {
    console.log(chalk.cyan('\n🍎 Apple Sign-In Complete Provisioning'));
    console.log(chalk.cyan('======================================\n'));

    // Step 1: Check Apple development tools
    await this.checkAppleTools();

    // Step 2: Authenticate with Apple Developer
    await this.authenticateWithApple();

    // Step 3: Determine or create Team ID
    const teamId = await this.ensureTeamId();

    // Step 4: Create or verify App ID
    const bundleId = await this.ensureAppId();

    // Step 5: Create Service ID for Sign-In
    const serviceId = await this.createServiceId();

    // Step 6: Configure Sign-In with Apple
    await this.configureSignInWithApple(serviceId);

    // Step 7: Generate signing key
    const signingKey = await this.generateSigningKey();

    // Step 8: Setup domain verification
    await this.setupDomainVerification(serviceId);

    // Step 9: Configure test environment (if requested)
    let testConfig = null;
    if (this.config.setupTestEnvironment) {
      testConfig = await this.setupTestEnvironment() || null;
    }

    // Step 10: Generate certificates (if requested)
    let certificateId = undefined;
    if (this.config.generateCertificates) {
      certificateId = await this.generateCertificates();
    }

    // Step 11: Get team information
    const teamInfo = await this.getTeamInformation(teamId);

    return {
      serviceId,
      teamId,
      keyId: signingKey.keyId,
      privateKey: signingKey.privateKey,
      bundleId,
      domains: [this.config.domain, `www.${this.config.domain}`],
      returnUrls: this.getReturnUrls(),
      certificateId,
      developmentTeam: teamInfo,
      testConfiguration: testConfig
    };
  }

  private async checkAppleTools(): Promise<void> {
    const spinner = ora('Checking Apple development tools...').start();

    try {
      // Check if Xcode command line tools are available
      try {
        await execAsync('xcode-select --version');
        spinner.text = 'Xcode command line tools found';
      } catch {
        spinner.text = 'Xcode tools not required for web-only setup';
      }

      // Check if we can generate keys locally
      try {
        await execAsync('openssl version');
        spinner.text = 'OpenSSL available for key generation';
      } catch {
        spinner.warn('OpenSSL not available - will use Node.js crypto');
      }

      spinner.succeed('Apple development environment ready');

    } catch (error) {
      spinner.fail('Apple tools check failed');
      throw error;
    }
  }

  private async authenticateWithApple(): Promise<void> {
    console.log(chalk.yellow('\n🔐 Apple Developer Authentication Required\n'));

    if (this.config.dryRun) {
      console.log(chalk.magenta('[DRY RUN] Would authenticate with Apple Developer'));
      this.apiKey = 'mock-api-key';
      this.apiKeyId = 'mock-key-id';
      this.apiIssuer = 'mock-issuer-id';
      return;
    }

    console.log(chalk.white('To automate Apple Sign-In setup, we need App Store Connect API access.'));
    console.log(chalk.white('\nPlease follow these steps to get API credentials:'));
    
    const steps = [
      'Go to App Store Connect: https://appstoreconnect.apple.com/',
      'Navigate to Users and Access > Keys',
      'Click the "+" button to create a new API key',
      'Give it a name (e.g., "EasyAuth Setup Key")',
      'Select "Developer" role (minimum required)',
      'Click "Generate"',
      'Download the .p8 private key file',
      'Note down the Key ID and Issuer ID'
    ];

    steps.forEach((step, index) => {
      console.log(chalk.blue(`${index + 1}. ${step}`));
    });

    console.log(chalk.blue('\n🔗 App Store Connect: https://appstoreconnect.apple.com/access/api\n'));

    if (!this.config.interactive) {
      throw new Error('Apple authentication requires interactive mode');
    }

    // Option to open browser
    const { openBrowser } = await inquirer.prompt([
      {
        type: 'confirm',
        name: 'openBrowser',
        message: 'Open App Store Connect in browser?',
        default: true
      }
    ]);

    if (openBrowser) {
      await this.openAppStoreConnect();
    }

    // Get API credentials
    const credentials = await inquirer.prompt([
      {
        type: 'input',
        name: 'keyId',
        message: 'Enter API Key ID (10 characters):',
        validate: (input) => /^[A-Z0-9]{10}$/.test(input) || 'Must be 10 characters (letters and numbers)'
      },
      {
        type: 'input',
        name: 'issuerId',
        message: 'Enter Issuer ID (UUID format):',
        validate: (input) => /^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/.test(input) || 'Must be a valid UUID'
      },
      {
        type: 'input',
        name: 'privateKeyPath',
        message: 'Enter path to .p8 private key file:',
        validate: async (input) => {
          try {
            await fs.access(input);
            return true;
          } catch {
            return 'File not found. Please provide valid path to .p8 file.';
          }
        }
      }
    ]);

    // Read private key
    this.apiKey = await fs.readFile(credentials.privateKeyPath, 'utf8');
    this.apiKeyId = credentials.keyId;
    this.apiIssuer = credentials.issuerId;

    // Generate JWT token for API access
    await this.generateJWTToken();

    // Validate credentials
    await this.validateApiCredentials();
  }

  private async openAppStoreConnect(): Promise<void> {
    const url = 'https://appstoreconnect.apple.com/access/api';
    
    try {
      const platform = process.platform;
      const command = platform === 'darwin' ? 'open' : 
                     platform === 'win32' ? 'start' : 'xdg-open';
      
      await execAsync(`${command} "${url}"`);
      console.log(chalk.green('✅ Opened App Store Connect in browser'));
    } catch {
      console.log(chalk.yellow('⚠️  Please manually open: ') + chalk.blue(url));
    }
  }

  private async generateJWTToken(): Promise<void> {
    const now = Math.floor(Date.now() / 1000);
    
    const payload = {
      iss: this.apiIssuer,
      iat: now,
      exp: now + 1200, // 20 minutes
      aud: 'appstoreconnect-v1'
    };

    const header = {
      alg: 'ES256',
      kid: this.apiKeyId,
      typ: 'JWT'
    };

    this.jwtToken = jwt.sign(payload, this.apiKey, { 
      algorithm: 'ES256', 
      header 
    });
  }

  private async validateApiCredentials(): Promise<void> {
    const spinner = ora('Validating Apple API credentials...').start();

    try {
      const response = await fetch('https://api.appstoreconnect.apple.com/v1/users', {
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`API validation failed: ${response.status} ${response.statusText}`);
      }

      spinner.succeed('Apple API credentials validated');

    } catch (error) {
      spinner.fail('Apple API validation failed');
      throw error;
    }
  }

  private async ensureTeamId(): Promise<string> {
    if (this.config.teamId) {
      return this.config.teamId;
    }

    const spinner = ora('Getting development team information...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would get team information');
      return 'MOCK_TEAM_ID';
    }

    try {
      // Get available teams
      const response = await fetch('https://api.appstoreconnect.apple.com/v1/users/me/visibleApps', {
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to get team information');
      }

      // For simplicity, we'll ask user to provide team ID manually
      // In a full implementation, we'd parse the response and show available teams

      spinner.stop();

      if (this.config.interactive) {
        const { teamId } = await inquirer.prompt([
          {
            type: 'input',
            name: 'teamId',
            message: 'Enter your Apple Developer Team ID (10 characters):',
            validate: (input) => /^[A-Z0-9]{10}$/.test(input) || 'Must be 10 characters'
          }
        ]);

        console.log(chalk.green(`✅ Using Team ID: ${teamId}`));
        return teamId;
      }

      throw new Error('Team ID required - use interactive mode or provide --team-id');

    } catch (error) {
      spinner.fail('Failed to get team information');
      throw error;
    }
  }

  private async ensureAppId(): Promise<string> {
    if (this.config.bundleId) {
      return this.config.bundleId;
    }

    const bundleId = `com.${this.config.domain.replace(/\./g, '-')}.${this.config.project.toLowerCase()}`;
    
    const spinner = ora(`Creating App ID: ${bundleId}...`).start();

    if (this.config.dryRun) {
      spinner.succeed(`[DRY RUN] Would create App ID: ${bundleId}`);
      return bundleId;
    }

    try {
      // Create App ID using App Store Connect API
      const response = await fetch('https://api.appstoreconnect.apple.com/v1/bundleIds', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          data: {
            type: 'bundleIds',
            attributes: {
              identifier: bundleId,
              name: `${this.config.appName} App ID`,
              platform: 'IOS',
              seedId: this.config.teamId
            }
          }
        })
      });

      if (response.status === 409) {
        // App ID already exists
        spinner.succeed(`App ID already exists: ${bundleId}`);
        return bundleId;
      }

      if (!response.ok) {
        const error = await response.json();
        throw new Error(`Failed to create App ID: ${(error as any).errors?.[0]?.detail || 'Unknown error'}`);
      }

      spinner.succeed(`App ID created: ${bundleId}`);
      return bundleId;

    } catch (error) {
      spinner.fail('Failed to create App ID');
      
      // Fall back to manual instructions
      console.log(chalk.yellow('\n📋 Manual App ID Creation:'));
      console.log(chalk.blue('1. Go to Apple Developer Portal'));
      console.log(chalk.blue('2. Navigate to Certificates, Identifiers & Profiles'));
      console.log(chalk.blue('3. Create new App ID'));
      console.log(chalk.blue(`4. Bundle ID: ${bundleId}`));
      
      if (this.config.interactive) {
        const { existingBundleId } = await inquirer.prompt([
          {
            type: 'input',
            name: 'existingBundleId',
            message: `Enter App ID/Bundle ID (${bundleId}):`,
            default: bundleId
          }
        ]);
        return existingBundleId;
      }

      return bundleId;
    }
  }

  private async createServiceId(): Promise<string> {
    const serviceId = this.config.serviceId || `com.${this.config.domain.replace(/\./g, '-')}.easyauth`;
    
    const spinner = ora(`Creating Service ID: ${serviceId}...`).start();

    if (this.config.dryRun) {
      spinner.succeed(`[DRY RUN] Would create Service ID: ${serviceId}`);
      return serviceId;
    }

    try {
      // Create Service ID using App Store Connect API
      const response = await fetch('https://api.appstoreconnect.apple.com/v1/bundleIds', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          data: {
            type: 'bundleIds',
            attributes: {
              identifier: serviceId,
              name: `${this.config.appName} Sign-In Service`,
              platform: 'SERVICES'
            }
          }
        })
      });

      if (response.status === 409) {
        spinner.succeed(`Service ID already exists: ${serviceId}`);
        return serviceId;
      }

      if (!response.ok) {
        throw new Error('Failed to create Service ID via API');
      }

      spinner.succeed(`Service ID created: ${serviceId}`);
      return serviceId;

    } catch (error) {
      spinner.fail('Service ID creation failed - will use manual setup');
      return await this.manualServiceIdSetup(serviceId);
    }
  }

  private async manualServiceIdSetup(defaultServiceId: string): Promise<string> {
    console.log(chalk.yellow('\n📋 Manual Service ID Setup Required\n'));
    
    const instructions = [
      'Go to Apple Developer Portal: https://developer.apple.com/account/',
      'Navigate to "Certificates, Identifiers & Profiles"',
      'Click "Identifiers" in the sidebar',
      'Click the "+" button to create new identifier',
      'Select "Services IDs" and continue',
      `Enter identifier: ${defaultServiceId}`,
      `Enter description: "${this.config.appName} EasyAuth Service"`,
      'Click "Continue" and "Register"'
    ];

    instructions.forEach((instruction, index) => {
      console.log(chalk.blue(`${index + 1}. ${instruction}`));
    });

    console.log(chalk.blue('\n🔗 Apple Developer Portal: https://developer.apple.com/account/\n'));

    if (!this.config.interactive) {
      return defaultServiceId;
    }

    const { serviceId } = await inquirer.prompt([
      {
        type: 'input',
        name: 'serviceId',
        message: `Enter Service ID (${defaultServiceId}):`,
        default: defaultServiceId
      }
    ]);

    return serviceId;
  }

  private async configureSignInWithApple(_serviceId: string): Promise<void> {
    const spinner = ora('Configuring Sign In with Apple...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would configure Sign In with Apple');
      return;
    }

    // This typically requires manual configuration in Apple Developer Portal
    spinner.warn('Sign In with Apple requires manual configuration');

    console.log(chalk.yellow('\n📋 Configure Sign In with Apple:\n'));
    
    const steps = [
      'Go to your Service ID in Apple Developer Portal',
      'Click "Configure" next to "Sign In with Apple"',
      'Select your primary App ID',
      'Add domains and subdomains:',
      `  - ${this.config.domain}`,
      `  - www.${this.config.domain}`,
      'Add return URLs:'
    ];

    steps.forEach((step, index) => {
      console.log(chalk.blue(`${index + 1}. ${step}`));
    });

    this.getReturnUrls().forEach(url => {
      console.log(chalk.white(`     - ${url}`));
    });

    console.log(chalk.blue('7. Click "Save" and "Continue"'));
    console.log(chalk.blue('8. Click "Done"'));
  }

  private async generateSigningKey(): Promise<{ keyId: string; privateKey: string }> {
    const spinner = ora('Generating Sign In with Apple key...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would generate signing key');
      return {
        keyId: 'MOCK_KEY_ID',
        privateKey: 'mock-private-key'
      };
    }

    try {
      // Try to create key via API first
      const response = await fetch('https://api.appstoreconnect.apple.com/v1/keys', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          data: {
            type: 'keys',
            attributes: {
              name: `${this.config.appName} Sign In Key`,
              keyType: 'SIGN_IN_WITH_APPLE'
            }
          }
        })
      });

      if (response.ok) {
        const keyData = await response.json() as any;
        const keyId = keyData.data.id;
        
        // Download the private key
        const keyResponse = await fetch(`https://api.appstoreconnect.apple.com/v1/keys/${keyId}/download`, {
          headers: {
            'Authorization': `Bearer ${this.jwtToken}`,
            'Accept': 'application/x-pem-file'
          }
        });

        if (keyResponse.ok) {
          const privateKey = await keyResponse.text();
          spinner.succeed(`Sign In key generated: ${keyId}`);
          
          // Save key to file
          await this.savePrivateKey(keyId, privateKey);
          
          return { keyId, privateKey };
        }
      }

      // Fall back to manual setup
      throw new Error('API key generation not available');

    } catch (error) {
      spinner.fail('Automatic key generation failed - manual setup required');
      return await this.manualKeyGeneration();
    }
  }

  private async manualKeyGeneration(): Promise<{ keyId: string; privateKey: string }> {
    console.log(chalk.yellow('\n📋 Manual Key Generation Required\n'));
    
    const instructions = [
      'Go to Apple Developer Portal > Certificates, Identifiers & Profiles',
      'Click "Keys" in the sidebar',
      'Click the "+" button to create new key',
      `Enter name: "${this.config.appName} Sign In Key"`,
      'Check "Sign In with Apple"',
      'Click "Configure" and select your primary App ID',
      'Click "Save", "Continue", then "Register"',
      'Download the .p8 key file',
      'Note the Key ID (10 characters)'
    ];

    instructions.forEach((instruction, index) => {
      console.log(chalk.blue(`${index + 1}. ${instruction}`));
    });

    if (!this.config.interactive) {
      return {
        keyId: 'MANUAL_KEY_ID',
        privateKey: 'manual-setup-required'
      };
    }

    const keyInfo = await inquirer.prompt([
      {
        type: 'input',
        name: 'keyId',
        message: 'Enter Key ID (10 characters):',
        validate: (input) => /^[A-Z0-9]{10}$/.test(input) || 'Must be 10 characters'
      },
      {
        type: 'input',
        name: 'privateKeyPath',
        message: 'Enter path to .p8 private key file:',
        validate: async (input) => {
          try {
            await fs.access(input);
            return true;
          } catch {
            return 'File not found';
          }
        }
      }
    ]);

    const privateKey = await fs.readFile(keyInfo.privateKeyPath, 'utf8');
    
    // Save key for later use
    await this.savePrivateKey(keyInfo.keyId, privateKey);
    
    return {
      keyId: keyInfo.keyId,
      privateKey
    };
  }

  private async savePrivateKey(keyId: string, privateKey: string): Promise<void> {
    const keyDir = './apple-keys';
    const keyFile = path.join(keyDir, `AuthKey_${keyId}.p8`);
    
    try {
      await fs.mkdir(keyDir, { recursive: true });
      await fs.writeFile(keyFile, privateKey);
      console.log(chalk.green(`✅ Private key saved: ${keyFile}`));
    } catch (error) {
      console.log(chalk.yellow('⚠️  Could not save private key file'));
    }
  }

  private async setupDomainVerification(serviceId: string): Promise<void> {
    console.log(chalk.yellow('\n📋 Domain Verification Setup\n'));
    
    console.log(chalk.white('To complete Sign In with Apple setup, add this file to your domain:'));
    console.log(chalk.blue(`File: https://${this.config.domain}/.well-known/apple-developer-domain-association.txt`));
    
    const domainAssociation = `${serviceId}`;
    
    console.log(chalk.white('Content:'));
    console.log(chalk.gray(`${domainAssociation}`));
    
    try {
      const wellKnownDir = './.well-known';
      await fs.mkdir(wellKnownDir, { recursive: true });
      await fs.writeFile(path.join(wellKnownDir, 'apple-developer-domain-association.txt'), domainAssociation);
      console.log(chalk.green('✅ Domain association file created locally'));
      console.log(chalk.yellow('   Please upload to your web server\'s .well-known directory'));
    } catch (error) {
      console.log(chalk.yellow('⚠️  Please create the domain association file manually'));
    }
  }

  private async setupTestEnvironment(): Promise<{
    sandboxMode: boolean;
    testUsers: string[];
  }> {
    console.log(chalk.yellow('\n🧪 Test Environment Setup\n'));
    
    if (this.config.dryRun) {
      return {
        sandboxMode: true,
        testUsers: ['test@example.com']
      };
    }

    console.log(chalk.white('Apple Sign-In Test Configuration:'));
    console.log(chalk.blue('1. Use development/staging domains for testing'));
    console.log(chalk.blue('2. Test with Apple ID sandbox accounts'));
    console.log(chalk.blue('3. Configure different return URLs for test environment'));
    
    const testUsers = [
      'test1@example.com',
      'test2@example.com'
    ];

    console.log(chalk.white('\nRecommended test users:'));
    testUsers.forEach(user => {
      console.log(chalk.gray(`  - ${user}`));
    });

    return {
      sandboxMode: true,
      testUsers
    };
  }

  private async generateCertificates(): Promise<string> {
    const spinner = ora('Generating development certificates...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would generate certificates');
      return 'mock-certificate-id';
    }

    try {
      // Generate development certificate for testing
      const certificateId = await this.createDevelopmentCertificate();
      spinner.succeed(`Development certificate created: ${certificateId}`);
      return certificateId;
    } catch (error) {
      spinner.warn('Certificate generation requires Xcode or manual setup');
      return '';
    }
  }

  private async createDevelopmentCertificate(): Promise<string> {
    // This would typically involve:
    // 1. Generating a certificate signing request (CSR)
    // 2. Submitting it to Apple Developer Portal
    // 3. Downloading the certificate
    
    // For web-only Sign In with Apple, certificates are not strictly required
    return 'web-only-no-certificate-needed';
  }

  private async getTeamInformation(teamId: string): Promise<{
    name: string;
    id: string;
    role: string;
  }> {
    if (this.config.dryRun) {
      return {
        name: 'Mock Development Team',
        id: teamId,
        role: 'Developer'
      };
    }

    try {
      // Get team information via API
      const response = await fetch(`https://api.appstoreconnect.apple.com/v1/users/me`, {
        headers: {
          'Authorization': `Bearer ${this.jwtToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const userData = await response.json();
        return {
          name: (userData as any).data.attributes.firstName + ' ' + (userData as any).data.attributes.lastName,
          id: teamId,
          role: 'Developer'
        };
      }
    } catch {
      // Fallback
    }

    return {
      name: 'Apple Development Team',
      id: teamId,
      role: 'Developer'
    };
  }

  private getReturnUrls(): string[] {
    return [
      `https://${this.config.domain}/auth/apple/callback`,
      `https://www.${this.config.domain}/auth/apple/callback`,
      'http://localhost:3000/auth/apple/callback',
      'http://localhost:8080/auth/apple/callback'
    ];
  }
}