#Requires -Version 5.1

<#
.SYNOPSIS
    EasyAuth OAuth App Setup CLI for Windows
    
.DESCRIPTION
    PowerShell version of the OAuth app setup script that automatically creates
    OAuth applications across multiple providers and saves credentials securely.
    
.PARAMETER Project
    Project name (used for app naming)
    
.PARAMETER Domain
    Your domain name (e.g., myapp.com)
    
.PARAMETER Providers
    Comma-separated list of providers (default: all)
    
.PARAMETER GoogleOnly
    Setup Google OAuth only
    
.PARAMETER FacebookOnly
    Setup Facebook Login only
    
.PARAMETER AppleOnly
    Setup Apple Sign-In only
    
.PARAMETER AzureOnly
    Setup Azure B2C only
    
.PARAMETER OutputFormat
    Output format: env, json, or yaml (default: env)
    
.PARAMETER OutputFile
    Specify output file path
    
.PARAMETER NonInteractive
    Run without prompts (CI mode)
    
.PARAMETER DryRun
    Show what would be done without executing
    
.PARAMETER Force
    Overwrite existing configurations
    
.EXAMPLE
    .\setup-oauth-apps.ps1 -Project "MyApp" -Domain "myapp.com"
    
.EXAMPLE
    .\setup-oauth-apps.ps1 -Project "MyApp" -Domain "myapp.com" -GoogleOnly
    
.EXAMPLE
    .\setup-oauth-apps.ps1 -Project "MyApp" -Domain "myapp.com" -Providers "google,facebook" -OutputFormat json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Project,
    
    [Parameter(Mandatory = $true)]
    [string]$Domain,
    
    [string]$Providers = "google,facebook,apple,azure-b2c",
    
    [switch]$GoogleOnly,
    [switch]$FacebookOnly,
    [switch]$AppleOnly,
    [switch]$AzureOnly,
    
    [ValidateSet("env", "json", "yaml")]
    [string]$OutputFormat = "env",
    
    [string]$OutputFile,
    
    [switch]$NonInteractive,
    [switch]$DryRun,
    [switch]$Force
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Global configuration
$script:Config = @{
    Project = $Project
    Domain = $Domain
    Providers = @()
    OutputFormat = $OutputFormat
    OutputFile = $OutputFile
    Interactive = !$NonInteractive
    DryRun = $DryRun
    Force = $Force
}

function Initialize-Configuration {
    Write-Host "🚀 EasyAuth OAuth App Setup CLI (PowerShell)" -ForegroundColor Cyan
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Determine providers
    if ($GoogleOnly) { 
        $script:Config.Providers = @("google") 
    }
    elseif ($FacebookOnly) { 
        $script:Config.Providers = @("facebook") 
    }
    elseif ($AppleOnly) { 
        $script:Config.Providers = @("apple") 
    }
    elseif ($AzureOnly) { 
        $script:Config.Providers = @("azure-b2c") 
    }
    else {
        $script:Config.Providers = $Providers -split "," | ForEach-Object { $_.Trim() }
    }
    
    # Show configuration
    Write-Host "📋 Configuration:" -ForegroundColor Yellow
    Write-Host "   Project: $($script:Config.Project)" -ForegroundColor White
    Write-Host "   Domain: $($script:Config.Domain)" -ForegroundColor White
    Write-Host "   Providers: $($script:Config.Providers -join ', ')" -ForegroundColor White
    Write-Host "   Output: $($script:Config.OutputFormat)" -ForegroundColor White
    Write-Host ""
}

function Test-Prerequisites {
    Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow
    
    $requirements = @(
        @{ Name = "PowerShell"; Command = "Get-Host"; Required = $true },
        @{ Name = "Google Cloud CLI"; Command = "gcloud"; Providers = @("google") },
        @{ Name = "Azure CLI"; Command = "az"; Providers = @("azure-b2c") }
    )
    
    foreach ($req in $requirements) {
        $shouldCheck = $req.Required -or ($req.Providers | Where-Object { $script:Config.Providers -contains $_ })
        
        if ($shouldCheck) {
            try {
                if ($req.Command -eq "Get-Host") {
                    # PowerShell is obviously available
                    Write-Host "✅ $($req.Name) is available" -ForegroundColor Green
                }
                else {
                    Invoke-Expression "$($req.Command) --version" | Out-Null
                    Write-Host "✅ $($req.Name) is available" -ForegroundColor Green
                }
            }
            catch {
                Write-Host "❌ $($req.Name) is not available" -ForegroundColor Red
                
                if ($req.Required) {
                    throw "Error: $($req.Name) is required but not installed."
                }
                else {
                    $affectedProviders = $req.Providers | Where-Object { $script:Config.Providers -contains $_ }
                    Write-Host "   Skipping providers: $($affectedProviders -join ', ')" -ForegroundColor Yellow
                    $script:Config.Providers = $script:Config.Providers | Where-Object { $affectedProviders -notcontains $_ }
                }
            }
        }
    }
    
    if ($script:Config.Providers.Count -eq 0) {
        throw "Error: No providers available for setup."
    }
    
    Write-Host ""
}

function Invoke-ProviderSetup {
    param([string]$Provider)
    
    switch ($Provider) {
        "google" { return Invoke-GoogleSetup }
        "facebook" { return Invoke-FacebookSetup }
        "apple" { return Invoke-AppleSetup }
        "azure-b2c" { return Invoke-AzureB2CSetup }
        default { throw "Unknown provider: $Provider" }
    }
}

function Invoke-GoogleSetup {
    Write-Host "   Setting up Google Cloud project and OAuth app..." -ForegroundColor White
    
    $projectId = Format-ProjectName $script:Config.Project
    $appName = "$($script:Config.Project) EasyAuth"
    
    if ($script:Config.DryRun) {
        Write-Host "   [DRY RUN] Would create Google Cloud project: $projectId" -ForegroundColor Magenta
        Write-Host "   [DRY RUN] Would create OAuth 2.0 client: $appName" -ForegroundColor Magenta
        return @{
            clientId = "mock-google-client-id.apps.googleusercontent.com"
            clientSecret = "mock-google-client-secret"
        }
    }
    
    # Check if user is authenticated
    try {
        gcloud auth list --filter="status:ACTIVE" --format="value(account)" | Out-Null
    }
    catch {
        Write-Host "   🔐 Please authenticate with Google Cloud..." -ForegroundColor Yellow
        gcloud auth login
    }
    
    # Check current project
    $currentProject = ""
    try {
        $currentProject = gcloud config get-value project 2>$null
    }
    catch {
        # No project set
    }
    
    if (!$currentProject -or $currentProject -eq "(unset)") {
        if ($script:Config.Interactive) {
            $createNew = Read-Host "   Create new Google Cloud project '$projectId'? (y/n)"
            if ($createNew.ToLower() -eq "y") {
                gcloud projects create $projectId --name="$($script:Config.Project)"
                gcloud config set project $projectId
            }
            else {
                $existingProject = Read-Host "   Enter existing project ID"
                gcloud config set project $existingProject
            }
        }
        else {
            throw "No Google Cloud project configured. Use interactive mode or set up project manually."
        }
    }
    
    # Enable required APIs
    Write-Host "   📡 Enabling required APIs..." -ForegroundColor White
    gcloud services enable oauth2.googleapis.com
    gcloud services enable plus.googleapis.com
    
    # Create OAuth 2.0 client
    Write-Host "   🔑 Creating OAuth 2.0 client..." -ForegroundColor White
    $redirectUris = @(
        "https://$($script:Config.Domain)/auth/google/callback",
        "https://www.$($script:Config.Domain)/auth/google/callback",
        "http://localhost:3000/auth/google/callback"
    )
    
    # Note: Google Cloud CLI OAuth client creation is limited, may require manual setup
    Write-Host "   ⚠️  Google OAuth client creation may require manual setup in Google Cloud Console" -ForegroundColor Yellow
    Write-Host "      Go to: https://console.cloud.google.com/apis/credentials" -ForegroundColor Yellow
    
    if ($script:Config.Interactive) {
        Write-Host ""
        Write-Host "   Please create OAuth 2.0 Client ID and provide credentials:" -ForegroundColor Yellow
        $clientId = Read-Host "   Google Client ID"
        $clientSecret = Read-Host "   Google Client Secret"
        
        return @{
            clientId = $clientId
            clientSecret = $clientSecret
            projectId = (gcloud config get-value project)
        }
    }
    else {
        Write-Host "   ⚠️  Manual setup required - skipping Google in non-interactive mode" -ForegroundColor Yellow
        return $null
    }
}

function Invoke-FacebookSetup {
    Write-Host "   Setting up Facebook app..." -ForegroundColor White
    
    $appName = "$($script:Config.Project) EasyAuth"
    
    if ($script:Config.DryRun) {
        Write-Host "   [DRY RUN] Would create Facebook app: $appName" -ForegroundColor Magenta
        return @{
            appId = "mock-facebook-app-id"
            appSecret = "mock-facebook-app-secret"
        }
    }
    
    Write-Host "   📱 Facebook app setup requires manual configuration:" -ForegroundColor Yellow
    Write-Host "   1. Go to https://developers.facebook.com/" -ForegroundColor White
    Write-Host "   2. Click 'Create App'" -ForegroundColor White
    Write-Host "   3. Choose 'Consumer' app type" -ForegroundColor White
    Write-Host "   4. App name: $appName" -ForegroundColor White
    Write-Host "   5. App contact email: admin@$($script:Config.Domain)" -ForegroundColor White
    Write-Host "   6. Add 'Facebook Login' product" -ForegroundColor White
    Write-Host "   7. Configure Valid OAuth Redirect URIs:" -ForegroundColor White
    Write-Host "      - https://$($script:Config.Domain)/auth/facebook/callback" -ForegroundColor White
    Write-Host "      - https://www.$($script:Config.Domain)/auth/facebook/callback" -ForegroundColor White
    Write-Host "      - http://localhost:3000/auth/facebook/callback" -ForegroundColor White
    
    if ($script:Config.Interactive) {
        Write-Host ""
        Write-Host "   Please complete the setup and then provide the credentials:" -ForegroundColor Yellow
        $appId = Read-Host "   Facebook App ID"
        $appSecret = Read-Host "   Facebook App Secret"
        
        return @{
            appId = $appId
            appSecret = $appSecret
        }
    }
    else {
        Write-Host "   ⚠️  Manual setup required - skipping Facebook in non-interactive mode" -ForegroundColor Yellow
        return $null
    }
}

function Invoke-AppleSetup {
    Write-Host "   Setting up Apple Sign-In..." -ForegroundColor White
    
    $serviceId = "com.$($script:Config.Domain -replace '\.','-').easyauth"
    
    if ($script:Config.DryRun) {
        Write-Host "   [DRY RUN] Would create Apple Service ID: $serviceId" -ForegroundColor Magenta
        return @{
            serviceId = $serviceId
            teamId = "mock-team-id"
            keyId = "mock-key-id"
        }
    }
    
    Write-Host "   🍎 Apple Sign-In setup requires manual configuration:" -ForegroundColor Yellow
    Write-Host "   1. Go to https://developer.apple.com/account/" -ForegroundColor White
    Write-Host "   2. Navigate to 'Certificates, Identifiers & Profiles'" -ForegroundColor White
    Write-Host "   3. Create a new Service ID:" -ForegroundColor White
    Write-Host "      - Identifier: $serviceId" -ForegroundColor White
    Write-Host "      - Description: $($script:Config.Project) EasyAuth Service" -ForegroundColor White
    Write-Host "   4. Configure Sign In with Apple:" -ForegroundColor White
    Write-Host "      - Domains: $($script:Config.Domain), www.$($script:Config.Domain)" -ForegroundColor White
    Write-Host "      - Return URLs:" -ForegroundColor White
    Write-Host "        - https://$($script:Config.Domain)/auth/apple/callback" -ForegroundColor White
    Write-Host "        - https://www.$($script:Config.Domain)/auth/apple/callback" -ForegroundColor White
    Write-Host "   5. Create a private key for Sign In with Apple" -ForegroundColor White
    
    if ($script:Config.Interactive) {
        Write-Host ""
        Write-Host "   Please complete the setup and then provide the credentials:" -ForegroundColor Yellow
        $serviceIdInput = Read-Host "   Service ID ($serviceId)"
        if (!$serviceIdInput) { $serviceIdInput = $serviceId }
        $teamId = Read-Host "   Team ID"
        $keyId = Read-Host "   Key ID"
        $privateKeyPath = Read-Host "   Private key file path"
        
        $privateKey = ""
        if ($privateKeyPath -and (Test-Path $privateKeyPath)) {
            $privateKey = Get-Content $privateKeyPath -Raw
        }
        
        return @{
            serviceId = $serviceIdInput
            teamId = $teamId
            keyId = $keyId
            privateKey = $privateKey
        }
    }
    else {
        Write-Host "   ⚠️  Manual setup required - skipping Apple in non-interactive mode" -ForegroundColor Yellow
        return $null
    }
}

function Invoke-AzureB2CSetup {
    Write-Host "   Setting up Azure B2C tenant and app..." -ForegroundColor White
    
    $tenantName = Format-ProjectName $script:Config.Project
    $appName = "$($script:Config.Project)-easyauth"
    
    if ($script:Config.DryRun) {
        Write-Host "   [DRY RUN] Would create Azure B2C tenant: $tenantName" -ForegroundColor Magenta
        Write-Host "   [DRY RUN] Would create app registration: $appName" -ForegroundColor Magenta
        return @{
            clientId = "mock-azure-client-id"
            tenantId = "$tenantName.onmicrosoft.com"
        }
    }
    
    # Check if user is authenticated
    try {
        az account show | Out-Null
    }
    catch {
        Write-Host "   🔐 Please authenticate with Azure..." -ForegroundColor Yellow
        az login
    }
    
    Write-Host "   🏢 Creating Azure B2C tenant..." -ForegroundColor White
    Write-Host "   Note: B2C tenant creation may require manual approval." -ForegroundColor Yellow
    
    $resourceGroup = "$tenantName-rg"
    $tenantDomain = "$tenantName.onmicrosoft.com"
    
    # Create resource group
    az group create --name $resourceGroup --location "West US 2"
    
    # Create app registration
    Write-Host "   📱 Creating app registration..." -ForegroundColor White
    $redirectUris = @(
        "https://$($script:Config.Domain)/auth/azure-b2c/callback",
        "https://www.$($script:Config.Domain)/auth/azure-b2c/callback",
        "http://localhost:3000/auth/azure-b2c/callback"
    )
    
    $appResult = az ad app create --display-name $appName --web-redirect-uris $redirectUris --query "appId" -o tsv
    
    return @{
        clientId = $appResult
        tenantId = $tenantDomain
        resourceGroup = $resourceGroup
    }
}

function Save-Credentials {
    param([hashtable]$Credentials)
    
    Write-Host ""
    Write-Host "💾 Saving credentials..." -ForegroundColor Yellow
    
    $outputFile = $script:Config.OutputFile
    if (!$outputFile) {
        $outputFile = switch ($script:Config.OutputFormat) {
            "env" { ".env.oauth" }
            "json" { "oauth-credentials.json" }
            "yaml" { "oauth-credentials.yaml" }
            default { ".env.oauth" }
        }
    }
    
    switch ($script:Config.OutputFormat) {
        "env" { Save-AsEnv $Credentials $outputFile }
        "json" { Save-AsJson $Credentials $outputFile }
        "yaml" { Save-AsYaml $Credentials $outputFile }
    }
    
    Write-Host "   ✅ Credentials saved to $outputFile" -ForegroundColor Green
}

function Save-AsEnv {
    param([hashtable]$Credentials, [string]$Filename)
    
    $lines = @(
        "# EasyAuth OAuth Credentials",
        "# Generated on $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')",
        "# Project: $($script:Config.Project)",
        "# Domain: $($script:Config.Domain)",
        ""
    )
    
    foreach ($provider in $Credentials.Keys) {
        $creds = $Credentials[$provider]
        $lines += "# $((Get-Culture).TextInfo.ToTitleCase($provider)) OAuth"
        
        switch ($provider) {
            "google" {
                $lines += "GOOGLE_CLIENT_ID=$($creds.clientId)"
                $lines += "GOOGLE_CLIENT_SECRET=$($creds.clientSecret)"
                if ($creds.projectId) { $lines += "GOOGLE_PROJECT_ID=$($creds.projectId)" }
            }
            "facebook" {
                $lines += "FACEBOOK_APP_ID=$($creds.appId)"
                $lines += "FACEBOOK_APP_SECRET=$($creds.appSecret)"
            }
            "apple" {
                $lines += "APPLE_SERVICE_ID=$($creds.serviceId)"
                $lines += "APPLE_TEAM_ID=$($creds.teamId)"
                $lines += "APPLE_KEY_ID=$($creds.keyId)"
                if ($creds.privateKey) {
                    $lines += "APPLE_PRIVATE_KEY=`"$($creds.privateKey -replace "`n", "\n")`""
                }
            }
            "azure-b2c" {
                $lines += "AZURE_B2C_CLIENT_ID=$($creds.clientId)"
                $lines += "AZURE_B2C_TENANT_ID=$($creds.tenantId)"
                if ($creds.resourceGroup) { $lines += "AZURE_B2C_RESOURCE_GROUP=$($creds.resourceGroup)" }
            }
        }
        
        $lines += ""
    }
    
    # Add EasyAuth configuration
    $lines += "# EasyAuth Configuration"
    $lines += "EASYAUTH_BASE_URL=https://$($script:Config.Domain)"
    $lines += "EASYAUTH_ENVIRONMENT=production"
    $lines += ""
    
    $lines | Out-File -FilePath $Filename -Encoding UTF8
}

function Save-AsJson {
    param([hashtable]$Credentials, [string]$Filename)
    
    $data = @{
        metadata = @{
            generated = Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ'
            project = $script:Config.Project
            domain = $script:Config.Domain
            providers = @($Credentials.Keys)
        }
        credentials = $Credentials
        easyauth = @{
            baseUrl = "https://$($script:Config.Domain)"
            environment = "production"
        }
    }
    
    $data | ConvertTo-Json -Depth 10 | Out-File -FilePath $Filename -Encoding UTF8
}

function Save-AsYaml {
    param([hashtable]$Credentials, [string]$Filename)
    
    # Simple YAML generation (PowerShell doesn't have built-in YAML support)
    $yaml = @"
# EasyAuth OAuth Credentials
metadata:
  generated: $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')
  project: $($script:Config.Project)
  domain: $($script:Config.Domain)
  providers: [$($Credentials.Keys -join ', ')]

credentials:
"@
    
    foreach ($provider in $Credentials.Keys) {
        $yaml += "`n  $provider:"
        foreach ($key in $Credentials[$provider].Keys) {
            $value = $Credentials[$provider][$key]
            if ($value -match "`n") {
                $yaml += "`n    $key: |`n      $($value -replace "`n", "`n      ")"
            }
            else {
                $yaml += "`n    $key: $value"
            }
        }
    }
    
    $yaml += @"

easyauth:
  baseUrl: https://$($script:Config.Domain)
  environment: production
"@
    
    $yaml | Out-File -FilePath $Filename -Encoding UTF8
}

function New-IntegrationCode {
    param([hashtable]$Credentials)
    
    Write-Host ""
    Write-Host "🔧 Generating integration code..." -ForegroundColor Yellow
    
    $codeFile = "easyauth-config.ts"
    $providersCode = @()
    
    foreach ($provider in $Credentials.Keys) {
        switch ($provider) {
            "google" {
                $providersCode += @"
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!
    }
"@
            }
            "facebook" {
                $providersCode += @"
    facebook: {
      appId: process.env.FACEBOOK_APP_ID!,
      appSecret: process.env.FACEBOOK_APP_SECRET!
    }
"@
            }
            "apple" {
                $providersCode += @"
    apple: {
      serviceId: process.env.APPLE_SERVICE_ID!,
      teamId: process.env.APPLE_TEAM_ID!,
      keyId: process.env.APPLE_KEY_ID!,
      privateKey: process.env.APPLE_PRIVATE_KEY!
    }
"@
            }
            "azure-b2c" {
                $providersCode += @"
    'azure-b2c': {
      clientId: process.env.AZURE_B2C_CLIENT_ID!,
      tenantId: process.env.AZURE_B2C_TENANT_ID!
    }
"@
            }
        }
    }
    
    $code = @"
// EasyAuth Configuration
// Generated on $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')

import { EnhancedEasyAuthClient } from '@easyauth/sdk';

export const easyAuthConfig = {
  baseUrl: process.env.EASYAUTH_BASE_URL || 'https://$($script:Config.Domain)',
  environment: process.env.EASYAUTH_ENVIRONMENT as 'development' | 'staging' | 'production' || 'production',
  
  // Provider credentials from environment variables
  providers: {
$($providersCode -join ",`n")
  }
};

// Initialize EasyAuth client
export const easyAuthClient = new EnhancedEasyAuthClient(easyAuthConfig);

// Export for convenience
export default easyAuthClient;
"@
    
    $code | Out-File -FilePath $codeFile -Encoding UTF8
    Write-Host "   ✅ Integration code saved to $codeFile" -ForegroundColor Green
}

function Format-ProjectName {
    param([string]$Name)
    return $Name -replace '[^a-zA-Z0-9]', '' | ForEach-Object { $_.ToLower() }
}

# Main execution
try {
    Initialize-Configuration
    Test-Prerequisites
    
    $credentials = @{}
    
    foreach ($provider in $script:Config.Providers) {
        Write-Host ""
        Write-Host "🔧 Setting up $((Get-Culture).TextInfo.ToTitleCase($provider))..." -ForegroundColor Cyan
        
        try {
            $creds = Invoke-ProviderSetup $provider
            if ($creds) {
                $credentials[$provider] = $creds
                Write-Host "✅ $provider setup complete" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "❌ $provider setup failed: $($_.Exception.Message)" -ForegroundColor Red
            
            if ($script:Config.Interactive) {
                $shouldContinue = Read-Host "Continue with other providers? (y/n)"
                if ($shouldContinue.ToLower() -ne "y") {
                    exit 1
                }
            }
        }
    }
    
    # Save credentials
    Save-Credentials $credentials
    
    # Generate integration code
    New-IntegrationCode $credentials
    
    Write-Host ""
    Write-Host "🎉 OAuth setup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📖 Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Review generated credentials" -ForegroundColor White
    Write-Host "   2. Add environment variables to your project" -ForegroundColor White
    Write-Host "   3. Test authentication flows" -ForegroundColor White
    Write-Host "   4. Deploy and update redirect URLs for production" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "❌ Setup failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}