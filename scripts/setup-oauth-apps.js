#!/usr/bin/env node

/**
 * EasyAuth OAuth App Setup CLI
 * 
 * Automatically creates OAuth applications across multiple providers
 * and saves credentials securely for use with EasyAuth Framework.
 * 
 * Usage:
 *   npx easyauth-setup --project "MyApp" --domain "myapp.com"
 *   npx easyauth-setup --project "MyApp" --domain "myapp.com" --providers google,facebook
 *   npx easyauth-setup --project "MyApp" --domain "myapp.com" --google-only
 *   npx easyauth-setup --project "MyApp" --domain "myapp.com" --output-env
 */

const fs = require('fs');
const path = require('path');
const https = require('https');
const { spawn, execSync } = require('child_process');
const readline = require('readline');

// CLI argument parsing
const args = process.argv.slice(2);
const config = parseArgs(args);

// Main execution
async function main() {
  console.log('🚀 EasyAuth OAuth App Setup CLI');
  console.log('=====================================\n');
  
  // Validate configuration
  if (!config.project || !config.domain) {
    showUsage();
    process.exit(1);
  }
  
  // Show configuration
  console.log('📋 Configuration:');
  console.log(`   Project: ${config.project}`);
  console.log(`   Domain: ${config.domain}`);
  console.log(`   Providers: ${config.providers.join(', ')}`);
  console.log(`   Output: ${config.outputFormat}\n`);
  
  // Check prerequisites
  await checkPrerequisites();
  
  // Setup each provider
  const credentials = {};
  
  for (const provider of config.providers) {
    console.log(`\n🔧 Setting up ${provider.charAt(0).toUpperCase() + provider.slice(1)}...`);
    
    try {
      const creds = await setupProvider(provider, config);
      if (creds) {
        credentials[provider] = creds;
        console.log(`✅ ${provider} setup complete`);
      }
    } catch (error) {
      console.error(`❌ ${provider} setup failed:`, error.message);
      
      if (config.interactive) {
        const shouldContinue = await askQuestion(`Continue with other providers? (y/n): `);
        if (shouldContinue.toLowerCase() !== 'y') {
          process.exit(1);
        }
      }
    }
  }
  
  // Save credentials
  await saveCredentials(credentials, config);
  
  // Generate integration code
  generateIntegrationCode(credentials, config);
  
  console.log('\n🎉 OAuth setup complete!');
  console.log('\n📖 Next steps:');
  console.log('   1. Review generated credentials');
  console.log('   2. Add environment variables to your project');
  console.log('   3. Test authentication flows');
  console.log('   4. Deploy and update redirect URLs for production');
}

function parseArgs(args) {
  const config = {
    project: null,
    domain: null,
    providers: ['google', 'facebook', 'apple', 'azure-b2c'],
    outputFormat: 'env', // 'env' | 'json' | 'yaml'
    outputFile: null,
    interactive: true,
    dryRun: false,
    force: false
  };
  
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    const next = args[i + 1];
    
    switch (arg) {
      case '--project':
      case '-p':
        config.project = next;
        i++;
        break;
        
      case '--domain':
      case '-d':
        config.domain = next;
        i++;
        break;
        
      case '--providers':
        config.providers = next.split(',').map(p => p.trim());
        i++;
        break;
        
      case '--google-only':
        config.providers = ['google'];
        break;
        
      case '--facebook-only':
        config.providers = ['facebook'];
        break;
        
      case '--apple-only':
        config.providers = ['apple'];
        break;
        
      case '--azure-only':
        config.providers = ['azure-b2c'];
        break;
        
      case '--output-env':
        config.outputFormat = 'env';
        break;
        
      case '--output-json':
        config.outputFormat = 'json';
        break;
        
      case '--output-yaml':
        config.outputFormat = 'yaml';
        break;
        
      case '--output-file':
      case '-o':
        config.outputFile = next;
        i++;
        break;
        
      case '--non-interactive':
      case '--ci':
        config.interactive = false;
        break;
        
      case '--dry-run':
        config.dryRun = true;
        break;
        
      case '--force':
        config.force = true;
        break;
        
      case '--help':
      case '-h':
        showUsage();
        process.exit(0);
        break;
    }
  }
  
  return config;
}

function showUsage() {
  console.log(`
🚀 EasyAuth OAuth App Setup CLI

USAGE:
  npx easyauth-setup --project PROJECT_NAME --domain DOMAIN [OPTIONS]

REQUIRED:
  --project, -p    Project name (used for app naming)
  --domain, -d     Your domain name (e.g., myapp.com)

OPTIONS:
  --providers      Comma-separated list of providers (default: all)
  --google-only    Setup Google OAuth only
  --facebook-only  Setup Facebook Login only  
  --apple-only     Setup Apple Sign-In only
  --azure-only     Setup Azure B2C only
  
  --output-env     Output as .env file (default)
  --output-json    Output as JSON file
  --output-yaml    Output as YAML file
  --output-file    Specify output file path
  
  --non-interactive  Run without prompts (CI mode)
  --dry-run        Show what would be done without executing
  --force          Overwrite existing configurations
  --help, -h       Show this help

EXAMPLES:
  # Setup all providers for MyApp
  npx easyauth-setup --project "MyApp" --domain "myapp.com"
  
  # Setup only Google and Facebook
  npx easyauth-setup --project "MyApp" --domain "myapp.com" --providers google,facebook
  
  # Setup Google only with JSON output
  npx easyauth-setup --project "MyApp" --domain "myapp.com" --google-only --output-json
  
  # CI mode with custom output file
  npx easyauth-setup --project "MyApp" --domain "myapp.com" --non-interactive --output-file oauth-config.env

PROVIDERS SUPPORTED:
  ✅ google      - Google OAuth 2.0
  ✅ facebook    - Facebook Login
  ✅ apple       - Apple Sign-In
  ✅ azure-b2c   - Azure B2C

AUTHENTICATION:
  The script will prompt for authentication with each provider's CLI tool.
  Make sure you have the necessary permissions to create OAuth applications.
`);
}

async function checkPrerequisites() {
  console.log('🔍 Checking prerequisites...');
  
  const requirements = [
    { name: 'Node.js', command: 'node --version', required: true },
    { name: 'Google Cloud CLI', command: 'gcloud version', providers: ['google'] },
    { name: 'Facebook CLI', command: 'fb --version', providers: ['facebook'] },
    { name: 'Azure CLI', command: 'az version', providers: ['azure-b2c'] }
  ];
  
  for (const req of requirements) {
    if (req.required || req.providers?.some(p => config.providers.includes(p))) {
      try {
        execSync(req.command, { stdio: 'ignore' });
        console.log(`✅ ${req.name} is available`);
      } catch (error) {
        console.log(`❌ ${req.name} is not available`);
        
        if (req.required) {
          console.error(`Error: ${req.name} is required but not installed.`);
          process.exit(1);
        } else if (req.providers) {
          const affectedProviders = req.providers.filter(p => config.providers.includes(p));
          console.log(`   Skipping providers: ${affectedProviders.join(', ')}`);
          config.providers = config.providers.filter(p => !affectedProviders.includes(p));
        }
      }
    }
  }
  
  if (config.providers.length === 0) {
    console.error('Error: No providers available for setup.');
    process.exit(1);
  }
  
  console.log('');
}

async function setupProvider(provider, config) {
  switch (provider) {
    case 'google':
      return await setupGoogle(config);
    case 'facebook':
      return await setupFacebook(config);
    case 'apple':
      return await setupApple(config);
    case 'azure-b2c':
      return await setupAzureB2C(config);
    default:
      throw new Error(`Unknown provider: ${provider}`);
  }
}

async function setupGoogle(config) {
  console.log('   Setting up Google Cloud project and OAuth app...');
  
  const projectId = formatProjectName(config.project).toLowerCase();
  const appName = `${config.project} EasyAuth`;
  
  if (config.dryRun) {
    console.log(`   [DRY RUN] Would create Google Cloud project: ${projectId}`);
    console.log(`   [DRY RUN] Would create OAuth 2.0 client: ${appName}`);
    return {
      clientId: 'mock-google-client-id.apps.googleusercontent.com',
      clientSecret: 'mock-google-client-secret'
    };
  }
  
  try {
    // Check if user is authenticated
    execSync('gcloud auth list --filter=status:ACTIVE --format="value(account)"', { stdio: 'ignore' });
  } catch (error) {
    console.log('   🔐 Please authenticate with Google Cloud...');
    execSync('gcloud auth login', { stdio: 'inherit' });
  }
  
  // Create or select project
  let currentProject;
  try {
    currentProject = execSync('gcloud config get-value project', { encoding: 'utf8' }).trim();
  } catch (error) {
    // No project set
  }
  
  if (!currentProject || currentProject === '(unset)') {
    if (config.interactive) {
      const createNew = await askQuestion(`   Create new Google Cloud project '${projectId}'? (y/n): `);
      if (createNew.toLowerCase() === 'y') {
        execSync(`gcloud projects create ${projectId} --name="${config.project}"`, { stdio: 'inherit' });
        execSync(`gcloud config set project ${projectId}`, { stdio: 'inherit' });
      } else {
        const existingProject = await askQuestion('   Enter existing project ID: ');
        execSync(`gcloud config set project ${existingProject}`, { stdio: 'inherit' });
      }
    } else {
      throw new Error('No Google Cloud project configured. Use --interactive or set up project manually.');
    }
  }
  
  // Enable required APIs
  console.log('   📡 Enabling required APIs...');
  execSync('gcloud services enable oauth2.googleapis.com', { stdio: 'inherit' });
  execSync('gcloud services enable plus.googleapis.com', { stdio: 'inherit' });
  
  // Create OAuth consent screen
  console.log('   🖥️  Configuring OAuth consent screen...');
  const consentConfig = {
    application_name: appName,
    authorized_domains: [config.domain],
    developer_contact_information: [`admin@${config.domain}`]
  };
  
  // Create OAuth 2.0 client
  console.log('   🔑 Creating OAuth 2.0 client...');
  const redirectUris = [
    `https://${config.domain}/auth/google/callback`,
    `https://www.${config.domain}/auth/google/callback`,
    'http://localhost:3000/auth/google/callback' // For development
  ];
  
  const clientResult = execSync(`gcloud alpha oauth-clients create --display-name="${appName}" --allowed-redirect-uris="${redirectUris.join(',')}" --format="value(name)"`, { encoding: 'utf8' }).trim();
  
  // Extract client ID and secret
  const clientDetails = execSync(`gcloud alpha oauth-clients describe ${clientResult} --format="json"`, { encoding: 'utf8' });
  const clientData = JSON.parse(clientDetails);
  
  return {
    clientId: clientData.clientId,
    clientSecret: clientData.clientSecret,
    projectId: execSync('gcloud config get-value project', { encoding: 'utf8' }).trim()
  };
}

async function setupFacebook(config) {
  console.log('   Setting up Facebook app...');
  
  const appName = `${config.project} EasyAuth`;
  
  if (config.dryRun) {
    console.log(`   [DRY RUN] Would create Facebook app: ${appName}`);
    return {
      appId: 'mock-facebook-app-id',
      appSecret: 'mock-facebook-app-secret'
    };
  }
  
  // Enhanced Facebook setup with API automation
  console.log('   🚀 Enhanced Facebook app setup with API automation:');
  
  if (config.interactive) {
    console.log('\n   📋 Facebook App Setup Options:');
    console.log('   1. 🤖 Automated setup using Facebook Graph API (recommended)');
    console.log('   2. 📋 Manual setup with guided instructions');
    console.log('   3. ⏭️  Skip Facebook setup');
    
    const choice = await askQuestion('   Choose setup method (1-3): ');
    
    switch (choice) {
      case '1':
        return await setupFacebookAPI(config, appName);
      case '2':
        return await setupFacebookManual(config, appName);
      case '3':
        console.log('   ⏭️  Skipping Facebook setup');
        return null;
      default:
        console.log('   🤖 Using automated setup (default)');
        return await setupFacebookAPI(config, appName);
    }
  } else {
    console.log('   📋 Manual setup required in non-interactive mode');
    return await setupFacebookManual(config, appName);
  }
}

async function setupFacebookAPI(config, appName) {
  console.log('   🤖 Setting up Facebook app using Graph API...');
  
  // Guide user to get access token
  console.log('\n   🔐 Facebook API Access Required:');
  console.log('   1. Go to: https://developers.facebook.com/tools/explorer/');
  console.log('   2. Select "Get User Access Token"');
  console.log('   3. Check permissions: apps_management, business_management');
  console.log('   4. Generate Token and copy it');
  
  const openBrowser = await askQuestion('   Open Facebook Graph API Explorer? (y/n): ');
  if (openBrowser.toLowerCase() === 'y') {
    try {
      const { spawn } = require('child_process');
      const platform = process.platform;
      const command = platform === 'darwin' ? 'open' : 
                     platform === 'win32' ? 'start' : 'xdg-open';
      spawn(command, ['https://developers.facebook.com/tools/explorer/'], { detached: true });
      console.log('   ✅ Opened Facebook Graph API Explorer');
    } catch {
      console.log('   ⚠️  Please manually open: https://developers.facebook.com/tools/explorer/');
    }
  }
  
  const accessToken = await askQuestion('   Enter Facebook User Access Token: ');
  
  try {
    // Validate token
    const https = require('https');
    const validateResponse = await makeRequest(`https://graph.facebook.com/me?access_token=${accessToken}`);
    console.log(`   ✅ Authenticated as: ${validateResponse.name}`);
    
    // Create Facebook app via API
    const appData = {
      name: appName,
      namespace: appName.toLowerCase().replace(/[^a-z0-9]/g, '').substring(0, 20),
      category: 'BUSINESS',
      subcategory: 'OTHER',
      contact_email: `admin@${config.domain}`,
      privacy_policy_url: `https://${config.domain}/privacy`,
      terms_of_service_url: `https://${config.domain}/terms`,
      app_domains: [config.domain, `www.${config.domain}`],
      platform: 'web'
    };
    
    const createResponse = await makeRequest('https://graph.facebook.com/v18.0/apps', 'POST', {
      ...appData,
      access_token: accessToken
    });
    
    const appId = createResponse.id;
    
    // Get app secret
    const secretResponse = await makeRequest(`https://graph.facebook.com/v18.0/${appId}?fields=app_secret&access_token=${accessToken}`);
    
    console.log(`   ✅ Facebook app created: ${appId}`);
    
    // Configure Facebook Login
    await configureFacebookLogin(appId, accessToken, config);
    
    return {
      appId,
      appSecret: secretResponse.app_secret
    };
    
  } catch (error) {
    console.log(`   ❌ API setup failed: ${error.message}`);
    console.log('   📋 Falling back to manual setup...');
    return await setupFacebookManual(config, appName);
  }
}

async function configureFacebookLogin(appId, accessToken, config) {
  try {
    // Add Facebook Login product
    await makeRequest(`https://graph.facebook.com/v18.0/${appId}/products`, 'POST', {
      product: 'facebook_login',
      access_token: accessToken
    });
    
    // Configure OAuth redirect URIs
    const redirectUris = [
      `https://${config.domain}/auth/facebook/callback`,
      `https://www.${config.domain}/auth/facebook/callback`,
      'http://localhost:3000/auth/facebook/callback'
    ];
    
    await makeRequest(`https://graph.facebook.com/v18.0/${appId}/fb_login_config`, 'POST', {
      web_oauth_flow: true,
      redirect_uris: redirectUris,
      access_token: accessToken
    });
    
    console.log('   ✅ Facebook Login configured with redirect URIs');
    
  } catch (error) {
    console.log('   ⚠️  Facebook Login needs manual configuration');
  }
}

async function setupFacebookManual(config, appName) {
  console.log('   📋 Manual Facebook app setup:');
  console.log('   1. Go to https://developers.facebook.com/');
  console.log('   2. Click "Create App"');
  console.log('   3. Choose "Consumer" app type');
  console.log(`   4. App name: ${appName}`);
  console.log(`   5. App contact email: admin@${config.domain}`);
  console.log('   6. Add "Facebook Login" product');
  console.log('   7. Configure Valid OAuth Redirect URIs:');
  console.log(`      - https://${config.domain}/auth/facebook/callback`);
  console.log(`      - https://www.${config.domain}/auth/facebook/callback`);
  console.log('      - http://localhost:3000/auth/facebook/callback');
  
  if (config.interactive) {
    console.log('\n   Please complete the setup and then provide the credentials:');
    const appId = await askQuestion('   Facebook App ID: ');
    const appSecret = await askQuestion('   Facebook App Secret: ');
    
    return {
      appId,
      appSecret
    };
  } else {
    console.log('\n   ⚠️  Manual setup required - skipping Facebook in non-interactive mode');
    return null;
  }
}

// Helper function for HTTP requests
function makeRequest(url, method = 'GET', data = null) {
  return new Promise((resolve, reject) => {
    const https = require('https');
    const urlObj = new URL(url);
    
    const options = {
      hostname: urlObj.hostname,
      path: urlObj.pathname + urlObj.search,
      method: method,
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': 'EasyAuth-Setup/1.0'
      }
    };
    
    if (method === 'POST' && data) {
      const postData = typeof data === 'string' ? data : JSON.stringify(data);
      options.headers['Content-Length'] = Buffer.byteLength(postData);
      
      const req = https.request(options, (res) => {
        let responseData = '';
        res.on('data', (chunk) => responseData += chunk);
        res.on('end', () => {
          try {
            const parsed = JSON.parse(responseData);
            if (res.statusCode >= 200 && res.statusCode < 300) {
              resolve(parsed);
            } else {
              reject(new Error(parsed.error?.message || `HTTP ${res.statusCode}`));
            }
          } catch (e) {
            reject(new Error('Invalid JSON response'));
          }
        });
      });
      
      req.on('error', reject);
      req.write(postData);
      req.end();
    } else {
      const req = https.request(options, (res) => {
        let responseData = '';
        res.on('data', (chunk) => responseData += chunk);
        res.on('end', () => {
          try {
            const parsed = JSON.parse(responseData);
            resolve(parsed);
          } catch (e) {
            reject(new Error('Invalid JSON response'));
          }
        });
      });
      
      req.on('error', reject);
      req.end();
    }
  });
}

async function setupApple(config) {
  console.log('   Setting up Apple Sign-In...');
  
  const serviceId = `com.${config.domain.replace(/\./g, '-')}.easyauth`;
  
  if (config.dryRun) {
    console.log(`   [DRY RUN] Would create Apple Service ID: ${serviceId}`);
    return {
      serviceId,
      teamId: 'mock-team-id',
      keyId: 'mock-key-id'
    };
  }
  
  console.log('   🍎 Apple Sign-In setup requires manual configuration:');
  console.log('   1. Go to https://developer.apple.com/account/');
  console.log('   2. Navigate to "Certificates, Identifiers & Profiles"');
  console.log('   3. Create a new Service ID:');
  console.log(`      - Identifier: ${serviceId}`);
  console.log(`      - Description: ${config.project} EasyAuth Service`);
  console.log('   4. Configure Sign In with Apple:');
  console.log(`      - Primary App ID: (your app's bundle ID)`);
  console.log(`      - Domains: ${config.domain}, www.${config.domain}`);
  console.log('      - Return URLs:');
  console.log(`        - https://${config.domain}/auth/apple/callback`);
  console.log(`        - https://www.${config.domain}/auth/apple/callback`);
  console.log('   5. Create a private key for Sign In with Apple');
  
  if (config.interactive) {
    console.log('\n   Please complete the setup and then provide the credentials:');
    const serviceIdInput = await askQuestion(`   Service ID (${serviceId}): `) || serviceId;
    const teamId = await askQuestion('   Team ID: ');
    const keyId = await askQuestion('   Key ID: ');
    const privateKeyPath = await askQuestion('   Private key file path: ');
    
    let privateKey = '';
    if (privateKeyPath && fs.existsSync(privateKeyPath)) {
      privateKey = fs.readFileSync(privateKeyPath, 'utf8');
    }
    
    return {
      serviceId: serviceIdInput,
      teamId,
      keyId,
      privateKey
    };
  } else {
    console.log('\n   ⚠️  Manual setup required - skipping Apple in non-interactive mode');
    return null;
  }
}

async function setupAzureB2C(config) {
  console.log('   Setting up Azure B2C tenant and app...');
  
  const tenantName = formatProjectName(config.project).toLowerCase();
  const appName = `${config.project}-easyauth`;
  
  if (config.dryRun) {
    console.log(`   [DRY RUN] Would create Azure B2C tenant: ${tenantName}`);
    console.log(`   [DRY RUN] Would create app registration: ${appName}`);
    return {
      clientId: 'mock-azure-client-id',
      tenantId: `${tenantName}.onmicrosoft.com`
    };
  }
  
  try {
    // Check if user is authenticated
    execSync('az account show', { stdio: 'ignore' });
  } catch (error) {
    console.log('   🔐 Please authenticate with Azure...');
    execSync('az login', { stdio: 'inherit' });
  }
  
  // Create B2C tenant (requires manual approval)
  console.log('   🏢 Creating Azure B2C tenant...');
  console.log('   Note: B2C tenant creation may require manual approval.');
  
  const resourceGroup = `${tenantName}-rg`;
  const tenantDomain = `${tenantName}.onmicrosoft.com`;
  
  // Create resource group
  execSync(`az group create --name ${resourceGroup} --location "West US 2"`, { stdio: 'inherit' });
  
  // Create B2C tenant
  try {
    execSync(`az b2c tenant create --tenant-name ${tenantName} --resource-group ${resourceGroup} --location "United States"`, { stdio: 'inherit' });
  } catch (error) {
    console.log('   ⚠️  B2C tenant creation requires manual approval. Please check Azure portal.');
  }
  
  // Create app registration
  console.log('   📱 Creating app registration...');
  const redirectUris = [
    `https://${config.domain}/auth/azure-b2c/callback`,
    `https://www.${config.domain}/auth/azure-b2c/callback`,
    'http://localhost:3000/auth/azure-b2c/callback'
  ];
  
  const appResult = execSync(`az ad app create --display-name "${appName}" --web-redirect-uris "${redirectUris.join(' ')}" --query "appId" -o tsv`, { encoding: 'utf8' }).trim();
  
  return {
    clientId: appResult,
    tenantId: tenantDomain,
    resourceGroup
  };
}

async function saveCredentials(credentials, config) {
  console.log('\n💾 Saving credentials...');
  
  const outputFile = config.outputFile || getDefaultOutputFile(config.outputFormat);
  
  switch (config.outputFormat) {
    case 'env':
      await saveAsEnv(credentials, outputFile, config);
      break;
    case 'json':
      await saveAsJson(credentials, outputFile, config);
      break;
    case 'yaml':
      await saveAsYaml(credentials, outputFile, config);
      break;
  }
  
  console.log(`   ✅ Credentials saved to ${outputFile}`);
}

function getDefaultOutputFile(format) {
  switch (format) {
    case 'env': return '.env.oauth';
    case 'json': return 'oauth-credentials.json';
    case 'yaml': return 'oauth-credentials.yaml';
    default: return '.env.oauth';
  }
}

async function saveAsEnv(credentials, filename, config) {
  const lines = [
    '# EasyAuth OAuth Credentials',
    `# Generated on ${new Date().toISOString()}`,
    `# Project: ${config.project}`,
    `# Domain: ${config.domain}`,
    ''
  ];
  
  Object.entries(credentials).forEach(([provider, creds]) => {
    lines.push(`# ${provider.charAt(0).toUpperCase() + provider.slice(1)} OAuth`);
    
    switch (provider) {
      case 'google':
        lines.push(`GOOGLE_CLIENT_ID=${creds.clientId}`);
        lines.push(`GOOGLE_CLIENT_SECRET=${creds.clientSecret}`);
        if (creds.projectId) lines.push(`GOOGLE_PROJECT_ID=${creds.projectId}`);
        break;
        
      case 'facebook':
        lines.push(`FACEBOOK_APP_ID=${creds.appId}`);
        lines.push(`FACEBOOK_APP_SECRET=${creds.appSecret}`);
        break;
        
      case 'apple':
        lines.push(`APPLE_SERVICE_ID=${creds.serviceId}`);
        lines.push(`APPLE_TEAM_ID=${creds.teamId}`);
        lines.push(`APPLE_KEY_ID=${creds.keyId}`);
        if (creds.privateKey) {
          lines.push(`APPLE_PRIVATE_KEY="${creds.privateKey.replace(/\n/g, '\\n')}"`);
        }
        break;
        
      case 'azure-b2c':
        lines.push(`AZURE_B2C_CLIENT_ID=${creds.clientId}`);
        lines.push(`AZURE_B2C_TENANT_ID=${creds.tenantId}`);
        if (creds.resourceGroup) lines.push(`AZURE_B2C_RESOURCE_GROUP=${creds.resourceGroup}`);
        break;
    }
    
    lines.push('');
  });
  
  // Add EasyAuth configuration
  lines.push('# EasyAuth Configuration');
  lines.push(`EASYAUTH_BASE_URL=https://${config.domain}`);
  lines.push('EASYAUTH_ENVIRONMENT=production');
  lines.push('');
  
  fs.writeFileSync(filename, lines.join('\n'));
}

async function saveAsJson(credentials, filename, config) {
  const data = {
    metadata: {
      generated: new Date().toISOString(),
      project: config.project,
      domain: config.domain,
      providers: Object.keys(credentials)
    },
    credentials,
    easyauth: {
      baseUrl: `https://${config.domain}`,
      environment: 'production'
    }
  };
  
  fs.writeFileSync(filename, JSON.stringify(data, null, 2));
}

async function saveAsYaml(credentials, filename, config) {
  const yamlContent = `
# EasyAuth OAuth Credentials
metadata:
  generated: ${new Date().toISOString()}
  project: ${config.project}
  domain: ${config.domain}
  providers: [${Object.keys(credentials).join(', ')}]

credentials:
${Object.entries(credentials).map(([provider, creds]) => 
  `  ${provider}:\n${Object.entries(creds).map(([key, value]) => 
    `    ${key}: ${typeof value === 'string' && value.includes('\n') ? `|\n      ${value.replace(/\n/g, '\n      ')}` : value}`
  ).join('\n')}`
).join('\n')}

easyauth:
  baseUrl: https://${config.domain}
  environment: production
`.trim();
  
  fs.writeFileSync(filename, yamlContent);
}

function generateIntegrationCode(credentials, config) {
  console.log('\n🔧 Generating integration code...');
  
  const codeFile = 'easyauth-config.ts';
  const code = `
// EasyAuth Configuration
// Generated on ${new Date().toISOString()}

import { EnhancedEasyAuthClient } from '@easyauth/sdk';

export const easyAuthConfig = {
  baseUrl: process.env.EASYAUTH_BASE_URL || 'https://${config.domain}',
  environment: process.env.EASYAUTH_ENVIRONMENT as 'development' | 'staging' | 'production' || 'production',
  
  // Provider credentials from environment variables
  providers: {
    ${Object.keys(credentials).map(provider => {
      switch (provider) {
        case 'google':
          return `google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!
    }`;
        case 'facebook':
          return `facebook: {
      appId: process.env.FACEBOOK_APP_ID!,
      appSecret: process.env.FACEBOOK_APP_SECRET!
    }`;
        case 'apple':
          return `apple: {
      serviceId: process.env.APPLE_SERVICE_ID!,
      teamId: process.env.APPLE_TEAM_ID!,
      keyId: process.env.APPLE_KEY_ID!,
      privateKey: process.env.APPLE_PRIVATE_KEY!
    }`;
        case 'azure-b2c':
          return `'azure-b2c': {
      clientId: process.env.AZURE_B2C_CLIENT_ID!,
      tenantId: process.env.AZURE_B2C_TENANT_ID!
    }`;
        default:
          return '';
      }
    }).filter(Boolean).join(',\n    ')}
  }
};

// Initialize EasyAuth client
export const easyAuthClient = new EnhancedEasyAuthClient(easyAuthConfig);

// Export for convenience
export default easyAuthClient;
`;
  
  fs.writeFileSync(codeFile, code.trim());
  console.log(`   ✅ Integration code saved to ${codeFile}`);
}

function formatProjectName(name) {
  return name.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
}

function askQuestion(question) {
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
  });
  
  return new Promise((resolve) => {
    rl.question(question, (answer) => {
      rl.close();
      resolve(answer);
    });
  });
}

// Error handling
process.on('uncaughtException', (error) => {
  console.error('\n❌ Unexpected error:', error.message);
  process.exit(1);
});

process.on('unhandledRejection', (error) => {
  console.error('\n❌ Unhandled promise rejection:', error);
  process.exit(1);
});

// Run main function
if (require.main === module) {
  main().catch(error => {
    console.error('\n❌ Setup failed:', error.message);
    process.exit(1);
  });
}

module.exports = { main, parseArgs, setupProvider };