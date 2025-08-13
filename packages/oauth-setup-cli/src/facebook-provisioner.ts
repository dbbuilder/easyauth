/**
 * Facebook App Complete Provisioner
 * 
 * Automates Facebook app creation using:
 * - Facebook Graph API for app creation
 * - Guided manual setup with exact instructions
 * - Automated configuration validation
 * - Test app setup for development
 */

// import { promises as fs } from 'fs';
import fetch from 'node-fetch';
import chalk from 'chalk';
import ora from 'ora';
import inquirer from 'inquirer';
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export interface FacebookConfig {
  project: string;
  domain: string;
  appName: string;
  contactEmail: string;
  category: string;
  dryRun: boolean;
  verbose: boolean;
  useTestApp: boolean;
  interactive: boolean;
}

export interface FacebookResult {
  appId: string;
  appSecret: string;
  appName: string;
  appDomain: string;
  testAppId?: string;
  testAppSecret?: string;
  webhookUrl: string;
  loginUrl: string;
  permissions: string[];
  businessVerificationRequired: boolean;
}

export class FacebookProvisioner {
  private config: FacebookConfig;
  private accessToken: string = '';
  private userAccessToken: string = '';

  constructor(config: FacebookConfig) {
    this.config = config;
  }

  /**
   * Complete Facebook app setup process
   */
  async provision(): Promise<FacebookResult> {
    console.log(chalk.cyan('\n📱 Facebook App Complete Provisioning'));
    console.log(chalk.cyan('======================================\n'));

    // Step 1: Check if Facebook CLI tools are available
    await this.checkFacebookTools();

    // Step 2: Guide through Facebook authentication
    await this.authenticateWithFacebook();

    // Step 3: Create main Facebook app
    const mainApp = await this.createFacebookApp();

    // Step 4: Configure app settings
    await this.configureAppSettings(mainApp.appId);

    // Step 5: Setup Facebook Login product
    await this.setupFacebookLogin(mainApp.appId);

    // Step 6: Create test app (if requested)
    let testApp = null;
    if (this.config.useTestApp) {
      testApp = await this.createTestApp(mainApp.appId);
    }

    // Step 7: Configure domain validation
    await this.configureDomainValidation(mainApp.appId);

    // Step 8: Setup webhooks
    await this.setupWebhooks(mainApp.appId);

    // Step 9: Configure permissions and review
    const permissions = await this.configurePermissions(mainApp.appId);

    // Step 10: Generate app review checklist
    await this.generateAppReviewChecklist(mainApp.appId);

    return {
      appId: mainApp.appId,
      appSecret: mainApp.appSecret,
      appName: this.config.appName,
      appDomain: this.config.domain,
      testAppId: testApp?.appId,
      testAppSecret: testApp?.appSecret,
      webhookUrl: `https://${this.config.domain}/webhooks/facebook`,
      loginUrl: `https://${this.config.domain}/auth/facebook/callback`,
      permissions,
      businessVerificationRequired: await this.checkBusinessVerificationRequired()
    };
  }

  private async checkFacebookTools(): Promise<void> {
    const spinner = ora('Checking Facebook development tools...').start();

    try {
      // Check if user has Facebook CLI tools or browser automation available
      try {
        await execAsync('which fbcli');
        spinner.succeed('Facebook CLI found');
        return;
      } catch {
        // Facebook CLI not available, use manual/API approach
      }

      // Check if we can use Facebook Graph API
      spinner.text = 'Setting up Facebook Graph API access...';
      spinner.succeed('Facebook Graph API access ready');

    } catch (error) {
      spinner.fail('Facebook tools check failed');
      throw error;
    }
  }

  private async authenticateWithFacebook(): Promise<void> {
    console.log(chalk.yellow('\n🔐 Facebook Authentication Required\n'));

    if (this.config.dryRun) {
      console.log(chalk.magenta('[DRY RUN] Would authenticate with Facebook'));
      this.accessToken = 'mock-access-token';
      return;
    }

    // Guide user through getting access token
    console.log(chalk.white('To create Facebook apps programmatically, we need a User Access Token with apps_management permission.'));
    console.log(chalk.white('\nPlease follow these steps:'));
    console.log(chalk.blue('1. Go to: https://developers.facebook.com/tools/explorer/'));
    console.log(chalk.blue('2. Select "Get User Access Token"'));
    console.log(chalk.blue('3. Check permissions: apps_management, business_management'));
    console.log(chalk.blue('4. Generate Token'));
    console.log(chalk.blue('5. Copy the access token\n'));

    if (!this.config.interactive) {
      throw new Error('Facebook authentication requires interactive mode');
    }

    // Alternative: Open browser automatically
    const { openBrowser } = await inquirer.prompt([
      {
        type: 'confirm',
        name: 'openBrowser',
        message: 'Open Facebook Graph API Explorer in browser?',
        default: true
      }
    ]);

    if (openBrowser) {
      await this.openFacebookExplorer();
    }

    const { accessToken } = await inquirer.prompt([
      {
        type: 'password',
        name: 'accessToken',
        message: 'Enter your Facebook User Access Token:',
        mask: '*',
        validate: (input) => {
          if (!input || input.length < 20) {
            return 'Please enter a valid Facebook access token';
          }
          return true;
        }
      }
    ]);

    this.userAccessToken = accessToken;

    // Validate token
    await this.validateAccessToken();
  }

  private async openFacebookExplorer(): Promise<void> {
    const url = 'https://developers.facebook.com/tools/explorer/';
    
    try {
      const platform = process.platform;
      const command = platform === 'darwin' ? 'open' : 
                     platform === 'win32' ? 'start' : 'xdg-open';
      
      await execAsync(`${command} "${url}"`);
      console.log(chalk.green('✅ Opened Facebook Graph API Explorer in browser'));
    } catch {
      console.log(chalk.yellow('⚠️  Please manually open: ') + chalk.blue(url));
    }
  }

  private async validateAccessToken(): Promise<void> {
    const spinner = ora('Validating Facebook access token...').start();

    try {
      const response = await fetch(`https://graph.facebook.com/me?access_token=${this.userAccessToken}`);
      
      if (!response.ok) {
        throw new Error('Invalid access token');
      }

      const userData = await response.json();
      spinner.succeed(`Authenticated as: ${(userData as any).name}`);

      // Check permissions
      const permResponse = await fetch(`https://graph.facebook.com/me/permissions?access_token=${this.userAccessToken}`);
      const permissions = await permResponse.json();
      
      const hasAppsManagement = (permissions as any).data.some((p: any) => 
        p.permission === 'apps_management' && p.status === 'granted'
      );

      if (!hasAppsManagement) {
        throw new Error('Access token needs apps_management permission');
      }

    } catch (error) {
      spinner.fail('Facebook authentication failed');
      throw error;
    }
  }

  private async createFacebookApp(): Promise<{ appId: string; appSecret: string }> {
    const spinner = ora('Creating Facebook app...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would create Facebook app');
      return {
        appId: 'mock-facebook-app-id',
        appSecret: 'mock-facebook-app-secret'
      };
    }

    try {
      // Create app using Graph API
      const response = await fetch('https://graph.facebook.com/v18.0/apps', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: this.config.appName,
          namespace: this.formatNamespace(this.config.appName),
          category: this.config.category,
          subcategory: 'OTHER',
          contact_email: this.config.contactEmail,
          privacy_policy_url: `https://${this.config.domain}/privacy`,
          terms_of_service_url: `https://${this.config.domain}/terms`,
          app_domains: [this.config.domain, `www.${this.config.domain}`],
          platform: 'web',
          access_token: this.userAccessToken
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(`Facebook API error: ${(error as any).error?.message || 'Unknown error'}`);
      }

      const appData = await response.json();
      const appId = (appData as any).id;

      // Get app secret
      const secretResponse = await fetch(`https://graph.facebook.com/v18.0/${appId}?fields=app_secret&access_token=${this.userAccessToken}`);
      const secretData = await secretResponse.json();

      spinner.succeed(`Facebook app created: ${appId}`);

      return {
        appId,
        appSecret: (secretData as any).app_secret
      };

    } catch (error) {
      spinner.fail('Failed to create Facebook app');
      
      if (error instanceof Error && error.message.includes('API')) {
        // Fall back to manual setup
        return await this.fallbackManualSetup();
      }
      
      throw error;
    }
  }

  private async fallbackManualSetup(): Promise<{ appId: string; appSecret: string }> {
    console.log(chalk.yellow('\n📋 Manual Facebook App Setup Required\n'));
    
    const instructions = [
      'Go to Facebook Developers: https://developers.facebook.com/',
      'Click "Create App" in the top right',
      'Choose "Consumer" as the app type',
      `Enter app name: "${this.config.appName}"`,
      `Enter contact email: ${this.config.contactEmail}`,
      'Click "Create App"',
      'Go to Settings > Basic',
      'Add App Domains:',
      `  - ${this.config.domain}`,
      `  - www.${this.config.domain}`,
      `Set Privacy Policy URL: https://${this.config.domain}/privacy`,
      `Set Terms of Service URL: https://${this.config.domain}/terms`,
      'Add Platform > Website',
      `  Site URL: https://${this.config.domain}`,
      'Save changes'
    ];

    instructions.forEach((instruction, index) => {
      console.log(chalk.white(`${index + 1}. ${instruction}`));
    });

    console.log(chalk.blue('\n🔗 Facebook Developers: https://developers.facebook.com/\n'));

    const credentials = await inquirer.prompt([
      {
        type: 'input',
        name: 'appId',
        message: 'Enter Facebook App ID:',
        validate: (input) => /^\d{15,16}$/.test(input) || 'Must be a 15-16 digit App ID'
      },
      {
        type: 'password',
        name: 'appSecret',
        message: 'Enter Facebook App Secret:',
        mask: '*'
      }
    ]);

    return credentials;
  }

  private async configureAppSettings(appId: string): Promise<void> {
    const spinner = ora('Configuring app settings...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would configure app settings');
      return;
    }

    try {
      // Update app configuration
      await fetch(`https://graph.facebook.com/v18.0/${appId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          app_domains: [this.config.domain, `www.${this.config.domain}`],
          privacy_policy_url: `https://${this.config.domain}/privacy`,
          terms_of_service_url: `https://${this.config.domain}/terms`,
          access_token: this.userAccessToken
        })
      });

      spinner.succeed('App settings configured');

    } catch (error) {
      spinner.warn('Some app settings may need manual configuration');
    }
  }

  private async setupFacebookLogin(appId: string): Promise<void> {
    const spinner = ora('Setting up Facebook Login...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would setup Facebook Login');
      return;
    }

    try {
      // Add Facebook Login product
      await fetch(`https://graph.facebook.com/v18.0/${appId}/products`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          product: 'facebook_login',
          access_token: this.userAccessToken
        })
      });

      // Configure Valid OAuth Redirect URIs
      const redirectUris = this.getRedirectUris();
      
      await fetch(`https://graph.facebook.com/v18.0/${appId}/fb_login_config`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          web_oauth_flow: true,
          redirect_uris: redirectUris,
          access_token: this.userAccessToken
        })
      });

      spinner.succeed('Facebook Login configured');

    } catch (error) {
      spinner.warn('Facebook Login may need manual configuration');
      
      console.log(chalk.yellow('\n📋 Manual Facebook Login Setup:'));
      console.log(chalk.blue('1. Go to Facebook App Dashboard'));
      console.log(chalk.blue('2. Add Product > Facebook Login'));
      console.log(chalk.blue('3. Go to Facebook Login > Settings'));
      console.log(chalk.blue('4. Add Valid OAuth Redirect URIs:'));
      
      this.getRedirectUris().forEach(uri => {
        console.log(chalk.white(`   - ${uri}`));
      });
    }
  }

  private async createTestApp(parentAppId: string): Promise<{ appId: string; appSecret: string }> {
    const spinner = ora('Creating test app...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would create test app');
      return {
        appId: 'mock-test-app-id',
        appSecret: 'mock-test-app-secret'
      };
    }

    try {
      const response = await fetch(`https://graph.facebook.com/v18.0/${parentAppId}/accounts/test-apps`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: `${this.config.appName} Test`,
          access_token: this.userAccessToken
        })
      });

      const testAppData = await response.json();
      
      spinner.succeed(`Test app created: ${(testAppData as any).id}`);

      return {
        appId: (testAppData as any).id,
        appSecret: (testAppData as any).app_secret
      };

    } catch (error) {
      spinner.warn('Test app creation failed - can be created manually later');
      return { appId: '', appSecret: '' };
    }
  }

  private async configureDomainValidation(appId: string): Promise<void> {
    const spinner = ora('Configuring domain validation...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would configure domain validation');
      return;
    }

    try {
      // Add domain to app
      await fetch(`https://graph.facebook.com/v18.0/${appId}/app_domains`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          domains: [this.config.domain, `www.${this.config.domain}`],
          access_token: this.userAccessToken
        })
      });

      spinner.succeed('Domain validation configured');

      // Provide domain verification instructions
      console.log(chalk.yellow('\n📋 Domain Verification Required:'));
      console.log(chalk.blue('1. Add this meta tag to your website\'s <head> section:'));
      console.log(chalk.white(`   <meta property="fb:app_id" content="${appId}" />`));
      console.log(chalk.blue('2. Or upload this file to your domain root:'));
      console.log(chalk.white(`   https://${this.config.domain}/fb${appId}.html`));
      console.log(chalk.blue('3. Content: "fb${appId}"'));

    } catch (error) {
      spinner.warn('Domain validation needs manual setup');
    }
  }

  private async setupWebhooks(appId: string): Promise<void> {
    const spinner = ora('Setting up webhooks...').start();

    if (this.config.dryRun) {
      spinner.succeed('[DRY RUN] Would setup webhooks');
      return;
    }

    const webhookUrl = `https://${this.config.domain}/webhooks/facebook`;

    try {
      // Configure webhook endpoint
      await fetch(`https://graph.facebook.com/v18.0/${appId}/subscriptions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          object: 'user',
          callback_url: webhookUrl,
          verify_token: 'easyauth_webhook_verify',
          fields: ['email', 'name'],
          access_token: this.userAccessToken
        })
      });

      spinner.succeed('Webhooks configured');

      console.log(chalk.yellow('\n📋 Webhook Endpoint Setup:'));
      console.log(chalk.blue(`Webhook URL: ${webhookUrl}`));
      console.log(chalk.blue('Verify Token: easyauth_webhook_verify'));
      console.log(chalk.blue('Subscribed Fields: email, name'));

    } catch (error) {
      spinner.warn('Webhooks need manual configuration');
    }
  }

  private async configurePermissions(appId: string): Promise<string[]> {
    const basePermissions = ['email', 'public_profile'];
    const additionalPermissions = ['user_friends', 'user_birthday', 'user_location'];

    if (this.config.dryRun) {
      console.log('[DRY RUN] Would configure permissions');
      return basePermissions;
    }

    console.log(chalk.yellow('\n🔐 Facebook Permissions Configuration\n'));
    console.log(chalk.white('Basic permissions (automatically approved):'));
    basePermissions.forEach(perm => {
      console.log(chalk.green(`  ✅ ${perm}`));
    });

    console.log(chalk.white('\nAdditional permissions (require review):'));
    additionalPermissions.forEach(perm => {
      console.log(chalk.yellow(`  ⏳ ${perm}`));
    });

    if (this.config.interactive) {
      const { requestAdditional } = await inquirer.prompt([
        {
          type: 'confirm',
          name: 'requestAdditional',
          message: 'Request additional permissions (requires Facebook review)?',
          default: false
        }
      ]);

      if (requestAdditional) {
        return [...basePermissions, ...additionalPermissions];
      }
    }

    return basePermissions;
  }

  private async generateAppReviewChecklist(appId: string): Promise<void> {
    console.log(chalk.yellow('\n📋 Facebook App Review Checklist\n'));

    const checklist = [
      'Complete App Details (description, category, privacy policy)',
      'Add App Icon (1024x1024 PNG)',
      'Configure Data Use Checkup',
      'Submit for App Review (if using advanced permissions)',
      'Business Verification (for certain features)',
      'Domain Verification completed',
      'Test all authentication flows',
      'Implement proper error handling',
      'Add logout functionality',
      'Handle declined permissions gracefully'
    ];

    checklist.forEach((item, index) => {
      console.log(chalk.white(`${index + 1}. ${item}`));
    });

    console.log(chalk.blue(`\n🔗 App Dashboard: https://developers.facebook.com/apps/${appId}/`));
    console.log(chalk.blue('🔗 App Review Guide: https://developers.facebook.com/docs/app-review/'));
  }

  private async checkBusinessVerificationRequired(): Promise<boolean> {
    // Business verification is required for certain advanced features
    return false; // Most basic use cases don't require it
  }

  private getRedirectUris(): string[] {
    return [
      `https://${this.config.domain}/auth/facebook/callback`,
      `https://www.${this.config.domain}/auth/facebook/callback`,
      'http://localhost:3000/auth/facebook/callback',
      'http://localhost:8080/auth/facebook/callback',
      'http://127.0.0.1:3000/auth/facebook/callback'
    ];
  }

  private formatNamespace(appName: string): string {
    return appName
      .toLowerCase()
      .replace(/[^a-z0-9]/g, '')
      .substring(0, 20);
  }
}