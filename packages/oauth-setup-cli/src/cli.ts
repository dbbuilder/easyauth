#!/usr/bin/env node

/**
 * EasyAuth OAuth Setup CLI
 * 
 * Production-ready CLI tool for automated OAuth application setup
 * across Google, Facebook, Apple, and Azure B2C providers.
 */

import { Command } from 'commander';
import chalk from 'chalk';
import ora from 'ora';
import inquirer from 'inquirer';
import { OAuthSetupManager } from './oauth-setup-manager';
import { ProviderType, SetupConfig, SetupResult } from './types';

const program = new Command();

program
  .name('easyauth-setup')
  .description('Automated OAuth application setup for EasyAuth Framework')
  .version('1.0.0');

program
  .requiredOption('-p, --project <name>', 'Project name (used for app naming)')
  .requiredOption('-d, --domain <domain>', 'Your domain name (e.g., myapp.com)')
  .option('--providers <providers>', 'Comma-separated list of providers', 'google,facebook,apple,azure-b2c')
  .option('--google-only', 'Setup Google OAuth only')
  .option('--facebook-only', 'Setup Facebook Login only')
  .option('--apple-only', 'Setup Apple Sign-In only')
  .option('--azure-only', 'Setup Azure B2C only')
  .option('--output-format <format>', 'Output format: env, json, yaml', 'env')
  .option('-o, --output-file <file>', 'Specify output file path')
  .option('--non-interactive', 'Run without prompts (CI mode)')
  .option('--dry-run', 'Show what would be done without executing')
  .option('--force', 'Overwrite existing configurations')
  .option('--verbose', 'Enable verbose logging')
  .action(async (options) => {
    try {
      await runSetup(options);
    } catch (error) {
      console.error(chalk.red('\n❌ Setup failed:'), error instanceof Error ? error.message : error);
      process.exit(1);
    }
  });

async function runSetup(options: any) {
  // Print header
  console.log(chalk.cyan('🚀 EasyAuth OAuth App Setup CLI'));
  console.log(chalk.cyan('====================================\n'));

  // Parse configuration
  const config = await parseConfiguration(options);
  
  // Show configuration
  displayConfiguration(config);
  
  // Confirm setup in interactive mode
  if (config.interactive && !config.dryRun) {
    const { confirmed } = await inquirer.prompt([
      {
        type: 'confirm',
        name: 'confirmed',
        message: 'Proceed with OAuth app setup?',
        default: true
      }
    ]);
    
    if (!confirmed) {
      console.log(chalk.yellow('Setup cancelled.'));
      return;
    }
  }
  
  // Initialize setup manager
  const setupManager = new OAuthSetupManager(config);
  
  // Check and install prerequisites
  const prereqSpinner = ora('Checking and installing prerequisites...').start();
  try {
    await setupManager.checkPrerequisites();
    prereqSpinner.succeed('Prerequisites ready');
  } catch (error) {
    prereqSpinner.fail('Prerequisites setup failed');
    console.log(chalk.yellow('\n💡 Note: Some CLI tools may require manual installation.'));
    console.log(chalk.yellow('   The script will guide you through any required manual steps.\n'));
    throw error;
  }
  
  // Setup providers
  const results: SetupResult[] = [];
  
  for (const provider of config.providers) {
    const providerSpinner = ora(`Setting up ${provider}...`).start();
    
    try {
      const result = await setupManager.setupProvider(provider);
      if (result) {
        results.push(result);
        providerSpinner.succeed(`${provider} setup complete`);
      } else {
        providerSpinner.warn(`${provider} setup skipped`);
      }
    } catch (error) {
      providerSpinner.fail(`${provider} setup failed: ${error instanceof Error ? error.message : error}`);
      
      if (config.interactive && !config.force) {
        const { shouldContinue } = await inquirer.prompt([
          {
            type: 'confirm',
            name: 'shouldContinue',
            message: 'Continue with other providers?',
            default: true
          }
        ]);
        
        if (!shouldContinue) {
          throw error;
        }
      }
    }
  }
  
  if (results.length === 0) {
    throw new Error('No providers were successfully configured');
  }
  
  // Save credentials
  const saveSpinner = ora('Saving credentials...').start();
  try {
    const outputFile = await setupManager.saveCredentials(results);
    saveSpinner.succeed(`Credentials saved to ${outputFile}`);
  } catch (error) {
    saveSpinner.fail('Failed to save credentials');
    throw error;
  }
  
  // Generate integration code
  const codeSpinner = ora('Generating integration code...').start();
  try {
    const codeFile = await setupManager.generateIntegrationCode(results);
    codeSpinner.succeed(`Integration code saved to ${codeFile}`);
  } catch (error) {
    codeSpinner.fail('Failed to generate integration code');
    throw error;
  }
  
  // Display success message
  displaySuccessMessage(results, config);
}

async function parseConfiguration(options: any): Promise<SetupConfig> {
  let providers: ProviderType[];
  
  // Determine providers
  if (options.googleOnly) {
    providers = ['google'];
  } else if (options.facebookOnly) {
    providers = ['facebook'];
  } else if (options.appleOnly) {
    providers = ['apple'];
  } else if (options.azureOnly) {
    providers = ['azure-b2c'];
  } else {
    providers = options.providers.split(',').map((p: string) => p.trim() as ProviderType);
  }
  
  // Validate providers
  const validProviders: ProviderType[] = ['google', 'facebook', 'apple', 'azure-b2c'];
  providers = providers.filter(p => validProviders.includes(p));
  
  if (providers.length === 0) {
    throw new Error('No valid providers specified');
  }
  
  return {
    project: options.project,
    domain: options.domain,
    providers,
    outputFormat: options.outputFormat as 'env' | 'json' | 'yaml',
    outputFile: options.outputFile,
    interactive: !options.nonInteractive,
    dryRun: !!options.dryRun,
    force: !!options.force,
    verbose: !!options.verbose
  };
}

function displayConfiguration(config: SetupConfig) {
  console.log(chalk.yellow('📋 Configuration:'));
  console.log(`   Project: ${chalk.white(config.project)}`);
  console.log(`   Domain: ${chalk.white(config.domain)}`);
  console.log(`   Providers: ${chalk.white(config.providers.join(', '))}`);
  console.log(`   Output: ${chalk.white(config.outputFormat)}`);
  if (config.outputFile) {
    console.log(`   Output file: ${chalk.white(config.outputFile)}`);
  }
  if (config.dryRun) {
    console.log(`   Mode: ${chalk.magenta('DRY RUN')}`);
  }
  console.log();
}

function displaySuccessMessage(results: SetupResult[], config: SetupConfig) {
  console.log(chalk.green('\n🎉 OAuth setup complete!\n'));
  
  console.log(chalk.yellow('📊 Summary:'));
  results.forEach(result => {
    const setupType = result.metadata?.setupType ? ` (${result.metadata.setupType})` : '';
    console.log(chalk.green(`   ✅ ${result.provider}${setupType} - ${Object.keys(result.credentials).length} credentials`));
    
    // Show additional info for comprehensive setups
    if (result.metadata?.setupType === 'comprehensive') {
      if (result.provider === 'azure-b2c' && result.metadata.userFlows) {
        console.log(chalk.blue(`      └─ User flows: ${result.metadata.userFlows.length} created`));
        console.log(chalk.blue(`      └─ Applications: ${Object.keys(result.metadata.applications || {}).length} registered`));
      }
      if (result.provider === 'facebook' && result.metadata.testAppId) {
        console.log(chalk.blue(`      └─ Test app created for development`));
      }
      if (result.provider === 'apple' && result.metadata.testConfiguration) {
        console.log(chalk.blue(`      └─ Test environment configured`));
      }
    }
  });
  
  console.log(chalk.yellow('\n📖 Next steps:'));
  console.log('   1. Review generated credentials and integration code');
  console.log('   2. Add environment variables to your project');
  console.log('   3. Test authentication flows in development');
  console.log('   4. Update redirect URLs for production deployment');
  console.log(chalk.red('\n⚠️  CRITICAL - API Path Warning:'));
  console.log(chalk.white('   EasyAuth uses EXCLUSIVE /api/EAuth/ paths'));
  console.log(chalk.white('   ❌ Do NOT use: /api/auth/, /auth/, or other variants'));
  console.log(chalk.white('   ✅ Correct: /api/EAuth/{provider}/callback'));
  
  // Provider-specific next steps
  const azureB2CResults = results.filter(r => r.provider === 'azure-b2c' && r.metadata?.setupType === 'comprehensive');
  if (azureB2CResults.length > 0) {
    console.log(chalk.yellow('\n📋 Azure B2C Additional Steps:'));
    console.log('   • Visit Azure Portal to customize user flow branding');
    console.log('   • Configure additional identity providers if needed');
    console.log('   • Set up custom policies for advanced scenarios');
    console.log('   • Test user journeys with sample accounts');
  }
  
  const appleResults = results.filter(r => r.provider === 'apple');
  if (appleResults.length > 0) {
    console.log(chalk.yellow('\n🍎 Apple Sign-In Additional Steps:'));
    console.log('   • Upload domain verification file to your web server');
    console.log('   • Complete Sign In with Apple configuration in Apple Developer Portal');
    console.log('   • Test with Apple ID sandbox accounts');
  }
  
  if (!config.dryRun) {
    console.log(chalk.yellow('\n🔗 Useful links:'));
    console.log('   • EasyAuth Documentation: https://docs.easyauth.dev');
    console.log('   • OAuth Provider Setup Guide: https://docs.easyauth.dev/oauth-setup');
    console.log('   • Integration Examples: https://github.com/dbbuilder/easyauth/tree/main/examples');
  }
  
  if (config.dryRun) {
    console.log(chalk.magenta('\n🔍 This was a dry run - no actual changes were made.'));
    console.log(chalk.magenta('Remove --dry-run to execute the setup.'));
  }
}

// Handle uncaught errors
process.on('uncaughtException', (error) => {
  console.error(chalk.red('\n❌ Unexpected error:'), error.message);
  if (process.env.NODE_ENV === 'development') {
    console.error(error.stack);
  }
  process.exit(1);
});

process.on('unhandledRejection', (error) => {
  console.error(chalk.red('\n❌ Unhandled promise rejection:'), error);
  process.exit(1);
});

// Parse command line arguments
program.parse(process.argv);

// Show help if no arguments provided
if (process.argv.length === 2) {
  program.help();
}