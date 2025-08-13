/**
 * CLI Tool Installer & Authentication Manager
 * 
 * Automatically downloads and installs required CLI tools for OAuth setup,
 * then guides users through authentication process.
 */

import { exec, spawn } from 'child_process';
import { promisify } from 'util';
import { promises as fs } from 'fs';
import path from 'path';
import os from 'os';
import chalk from 'chalk';
import ora from 'ora';
import inquirer from 'inquirer';

const execAsync = promisify(exec);

export interface CLITool {
  name: string;
  command: string;
  installUrl: string;
  installInstructions: string[];
  checkCommand: string;
  providers: string[];
  loginCommand?: string;
  loginInstructions?: string[];
}

export class CLIInstaller {
  private tools: CLITool[] = [
    {
      name: 'Google Cloud CLI',
      command: 'gcloud',
      installUrl: 'https://cloud.google.com/sdk/docs/install',
      installInstructions: [
        'Google Cloud CLI installation:',
        '1. Download installer from: https://cloud.google.com/sdk/docs/install',
        '2. Run the installer for your platform',
        '3. Restart your terminal',
        '4. Run: gcloud init'
      ],
      checkCommand: 'gcloud version',
      providers: ['google'],
      loginCommand: 'gcloud auth login',
      loginInstructions: [
        'Google Cloud authentication:',
        '1. Run: gcloud auth login',
        '2. Follow browser authentication flow',
        '3. Grant necessary permissions'
      ]
    },
    {
      name: 'Azure CLI',
      command: 'az',
      installUrl: 'https://docs.microsoft.com/en-us/cli/azure/install-azure-cli',
      installInstructions: [
        'Azure CLI installation:',
        'Windows: winget install -e --id Microsoft.AzureCLI',
        'macOS: brew install azure-cli',
        'Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash',
        'Or download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli'
      ],
      checkCommand: 'az version',
      providers: ['azure-b2c'],
      loginCommand: 'az login',
      loginInstructions: [
        'Azure authentication:',
        '1. Run: az login',
        '2. Complete browser authentication',
        '3. Select appropriate subscription'
      ]
    }
  ];

  constructor(private verbose: boolean = false) {}

  /**
   * Check and install all required CLI tools for specified providers
   */
  async ensureToolsAvailable(providers: string[]): Promise<void> {
    const requiredTools = this.tools.filter(tool => 
      tool.providers.some(p => providers.includes(p))
    );

    if (requiredTools.length === 0) {
      return;
    }

    console.log(chalk.yellow('🔧 Checking required CLI tools...\n'));

    for (const tool of requiredTools) {
      await this.ensureToolAvailable(tool);
    }
  }

  /**
   * Authenticate with all required CLI tools
   */
  async authenticateTools(providers: string[]): Promise<void> {
    const requiredTools = this.tools.filter(tool => 
      tool.providers.some(p => providers.includes(p))
    );

    console.log(chalk.yellow('🔐 Checking authentication status...\n'));

    for (const tool of requiredTools) {
      await this.ensureAuthenticated(tool);
    }
  }

  private async ensureToolAvailable(tool: CLITool): Promise<void> {
    const spinner = ora(`Checking ${tool.name}...`).start();

    try {
      await execAsync(tool.checkCommand);
      spinner.succeed(`${tool.name} is available`);
      return;
    } catch (error) {
      spinner.warn(`${tool.name} is not installed`);
    }

    // Tool not available - offer installation options
    await this.offerInstallation(tool);
  }

  private async offerInstallation(tool: CLITool): Promise<void> {
    console.log(chalk.yellow(`\n📦 ${tool.name} Installation Required`));
    console.log(chalk.gray('━'.repeat(50)));

    const { installChoice } = await inquirer.prompt([
      {
        type: 'list',
        name: 'installChoice',
        message: `How would you like to install ${tool.name}?`,
        choices: [
          { name: '🚀 Auto-install (recommended)', value: 'auto' },
          { name: '📋 Show manual instructions', value: 'manual' },
          { name: '⏭️  Skip (will limit functionality)', value: 'skip' }
        ]
      }
    ]);

    switch (installChoice) {
      case 'auto':
        await this.autoInstall(tool);
        break;
      case 'manual':
        await this.showManualInstructions(tool);
        break;
      case 'skip':
        console.log(chalk.yellow(`⚠️  Skipping ${tool.name} - some providers will not be available`));
        break;
    }
  }

  private async autoInstall(tool: CLITool): Promise<void> {
    const spinner = ora(`Installing ${tool.name}...`).start();

    try {
      await this.executeInstallation(tool);
      
      // Verify installation
      await execAsync(tool.checkCommand);
      spinner.succeed(`${tool.name} installed successfully`);
      
      // Show next steps
      console.log(chalk.green(`✅ ${tool.name} is now available`));
      console.log(chalk.yellow('💡 You may need to restart your terminal for changes to take effect\n'));
      
    } catch (error) {
      spinner.fail(`Failed to install ${tool.name}`);
      console.log(chalk.red(`❌ Auto-installation failed: ${error instanceof Error ? error.message : error}`));
      
      // Fallback to manual instructions
      await this.showManualInstructions(tool);
    }
  }

  private async executeInstallation(tool: CLITool): Promise<void> {
    const platform = os.platform();
    
    switch (tool.command) {
      case 'gcloud':
        await this.installGoogleCloudCLI(platform);
        break;
      case 'az':
        await this.installAzureCLI(platform);
        break;
      default:
        throw new Error(`Auto-installation not supported for ${tool.name}`);
    }
  }

  private async installGoogleCloudCLI(platform: NodeJS.Platform): Promise<void> {
    switch (platform) {
      case 'win32':
        // Download and run Windows installer
        await this.downloadAndInstall(
          'https://dl.google.com/dl/cloudsdk/channels/rapid/GoogleCloudSDKInstaller.exe',
          'GoogleCloudSDKInstaller.exe'
        );
        break;
      
      case 'darwin':
        // Use Homebrew if available, otherwise download
        try {
          await execAsync('brew --version');
          await execAsync('brew install --cask google-cloud-sdk');
        } catch {
          await this.downloadAndInstall(
            'https://dl.google.com/dl/cloudsdk/channels/rapid/downloads/google-cloud-cli-darwin-x86_64.tar.gz',
            'google-cloud-sdk.tar.gz'
          );
        }
        break;
      
      case 'linux':
        // Use package manager or download
        try {
          // Try apt-get first
          await execAsync('curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key add -');
          await execAsync('echo "deb https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee -a /etc/apt/sources.list.d/google-cloud-sdk.list');
          await execAsync('sudo apt-get update && sudo apt-get install google-cloud-cli');
        } catch {
          // Fallback to manual download
          await this.downloadAndInstall(
            'https://dl.google.com/dl/cloudsdk/channels/rapid/downloads/google-cloud-cli-linux-x86_64.tar.gz',
            'google-cloud-sdk.tar.gz'
          );
        }
        break;
      
      default:
        throw new Error(`Unsupported platform: ${platform}`);
    }
  }

  private async installAzureCLI(platform: NodeJS.Platform): Promise<void> {
    switch (platform) {
      case 'win32':
        // Use winget if available, otherwise download
        try {
          await execAsync('winget install -e --id Microsoft.AzureCLI');
        } catch {
          await this.downloadAndInstall(
            'https://aka.ms/installazurecliwindows',
            'AzureCLI.msi'
          );
        }
        break;
      
      case 'darwin':
        // Use Homebrew if available
        try {
          await execAsync('brew --version');
          await execAsync('brew install azure-cli');
        } catch {
          throw new Error('Homebrew required for Azure CLI installation on macOS');
        }
        break;
      
      case 'linux':
        // Use curl installer
        await execAsync('curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash');
        break;
      
      default:
        throw new Error(`Unsupported platform: ${platform}`);
    }
  }

  private async downloadAndInstall(url: string, filename: string): Promise<void> {
    const tempDir = os.tmpdir();
    const filePath = path.join(tempDir, filename);
    
    // Download file
    const { default: fetch } = await import('node-fetch');
    const response = await fetch(url);
    
    if (!response.ok) {
      throw new Error(`Failed to download ${filename}: ${response.statusText}`);
    }
    
    const buffer = await response.buffer();
    await fs.writeFile(filePath, buffer);
    
    // Execute installer
    const platform = os.platform();
    
    if (platform === 'win32') {
      // Windows installer
      await execAsync(`"${filePath}" /S`);
    } else {
      // Unix-like systems
      if (filename.endsWith('.tar.gz')) {
        const extractDir = path.join(tempDir, 'extracted');
        await execAsync(`mkdir -p "${extractDir}" && tar -xzf "${filePath}" -C "${extractDir}"`);
        await execAsync(`cd "${extractDir}" && ./google-cloud-sdk/install.sh --quiet`);
      } else {
        await execAsync(`chmod +x "${filePath}" && "${filePath}"`);
      }
    }
    
    // Cleanup
    await fs.unlink(filePath).catch(() => {});
  }

  private async showManualInstructions(tool: CLITool): Promise<void> {
    console.log(chalk.yellow(`\n📋 Manual Installation Instructions for ${tool.name}:`));
    console.log(chalk.gray('━'.repeat(60)));
    
    tool.installInstructions.forEach(instruction => {
      console.log(chalk.white(instruction));
    });
    
    console.log(chalk.blue(`\n🔗 Installation URL: ${tool.installUrl}`));
    
    const { proceed } = await inquirer.prompt([
      {
        type: 'confirm',
        name: 'proceed',
        message: 'Have you completed the installation? (we\'ll verify)',
        default: false
      }
    ]);
    
    if (proceed) {
      const spinner = ora(`Verifying ${tool.name} installation...`).start();
      try {
        await execAsync(tool.checkCommand);
        spinner.succeed(`${tool.name} verified successfully`);
      } catch {
        spinner.fail(`${tool.name} verification failed`);
        console.log(chalk.red('Please complete the installation and try again.'));
        throw new Error(`${tool.name} installation incomplete`);
      }
    } else {
      throw new Error(`${tool.name} installation cancelled`);
    }
  }

  private async ensureAuthenticated(tool: CLITool): Promise<void> {
    if (!tool.loginCommand) {
      return; // No authentication required
    }

    const spinner = ora(`Checking ${tool.name} authentication...`).start();
    
    try {
      // Check if already authenticated
      await this.checkAuthentication(tool);
      spinner.succeed(`${tool.name} authentication verified`);
      return;
    } catch (error) {
      spinner.warn(`${tool.name} authentication required`);
    }

    // Guide through authentication
    await this.guideAuthentication(tool);
  }

  private async checkAuthentication(tool: CLITool): Promise<void> {
    switch (tool.command) {
      case 'gcloud':
        const gcloudResult = await execAsync('gcloud auth list --filter=status:ACTIVE --format="value(account)"');
        if (!gcloudResult.stdout.trim()) {
          throw new Error('No active Google Cloud authentication');
        }
        break;
      
      case 'az':
        await execAsync('az account show');
        break;
      
      default:
        throw new Error(`Authentication check not implemented for ${tool.command}`);
    }
  }

  private async guideAuthentication(tool: CLITool): Promise<void> {
    console.log(chalk.yellow(`\n🔐 ${tool.name} Authentication Required`));
    console.log(chalk.gray('━'.repeat(50)));
    
    if (tool.loginInstructions) {
      tool.loginInstructions.forEach(instruction => {
        console.log(chalk.white(instruction));
      });
    }
    
    const { startAuth } = await inquirer.prompt([
      {
        type: 'confirm',
        name: 'startAuth',
        message: `Start ${tool.name} authentication?`,
        default: true
      }
    ]);
    
    if (!startAuth) {
      throw new Error(`${tool.name} authentication cancelled`);
    }
    
    // Execute authentication command
    console.log(chalk.blue(`\n⚡ Running: ${tool.loginCommand}`));
    console.log(chalk.gray('Follow the instructions in your browser...\n'));
    
    try {
      await this.executeInteractiveCommand(tool.loginCommand!);
      
      // Verify authentication
      const verifySpinner = ora(`Verifying ${tool.name} authentication...`).start();
      await this.checkAuthentication(tool);
      verifySpinner.succeed(`${tool.name} authentication successful`);
      
    } catch (error) {
      throw new Error(`${tool.name} authentication failed: ${error instanceof Error ? error.message : error}`);
    }
  }

  private async executeInteractiveCommand(command: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const [cmd, ...args] = command.split(' ');
      const child = spawn(cmd, args, {
        stdio: 'inherit',
        shell: true
      });
      
      child.on('close', (code) => {
        if (code === 0) {
          resolve();
        } else {
          reject(new Error(`Command failed with exit code ${code}`));
        }
      });
      
      child.on('error', (error) => {
        reject(error);
      });
    });
  }
}