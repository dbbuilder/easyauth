# NuGet Package Deployment Guide

This guide explains how to deploy the EasyAuth Framework NuGet packages both publicly and privately.

## Overview

The EasyAuth Framework consists of multiple NuGet packages:
- `EasyAuth.Framework.Core` - Core authentication services and providers
- `EasyAuth.Framework.Extensions` - ASP.NET Core integration extensions
- `EasyAuth.SDK.JavaScript` - JavaScript/TypeScript SDK for frontend applications

## Public Deployment Options

### 1. NuGet.org (Recommended for Open Source)

NuGet.org is the primary public package repository for .NET packages.

**Prerequisites:**
- NuGet.org account
- API key from nuget.org

**Deployment Steps:**

```bash
# 1. Build packages in Release mode
dotnet pack src/EasyAuth.Framework.Core/EasyAuth.Framework.Core.csproj -c Release
dotnet pack src/EasyAuth.Framework.Extensions/EasyAuth.Framework.Extensions.csproj -c Release

# 2. Push to NuGet.org
dotnet nuget push "src/EasyAuth.Framework.Core/bin/Release/*.nupkg" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json
dotnet nuget push "src/EasyAuth.Framework.Extensions/bin/Release/*.nupkg" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json
```

**GitHub Actions Integration:**

```yaml
name: Publish to NuGet.org

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Build and Pack
      run: |
        dotnet build -c Release
        dotnet pack src/EasyAuth.Framework.Core/EasyAuth.Framework.Core.csproj -c Release --no-build
        dotnet pack src/EasyAuth.Framework.Extensions/EasyAuth.Framework.Extensions.csproj -c Release --no-build
    
    - name: Publish to NuGet.org
      run: |
        dotnet nuget push "src/*/bin/Release/*.nupkg" -k ${{ secrets.NUGET_API_KEY }} -s https://api.nuget.org/v3/index.json --skip-duplicate
```

### 2. GitHub Packages

GitHub Packages provides integrated package hosting with your GitHub repository.

**Configuration:**

```xml
<!-- Add to Directory.Build.props -->
<PropertyGroup>
  <RepositoryUrl>https://github.com/yourusername/easyauth</RepositoryUrl>
  <PackageProjectUrl>$(RepositoryUrl)</PackageProjectUrl>
</PropertyGroup>
```

**Deployment:**

```bash
# Authenticate with GitHub
dotnet nuget add source --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/YOUR_GITHUB_USERNAME/index.json"

# Build and push
dotnet pack -c Release
dotnet nuget push "src/*/bin/Release/*.nupkg" --source github
```

### 3. MyGet (Alternative Public Feed)

MyGet offers both public and private feeds with advanced features.

```bash
# Push to MyGet
dotnet nuget push "src/*/bin/Release/*.nupkg" -k YOUR_MYGET_KEY -s https://www.myget.org/F/your-feed-name/api/v3/index.json
```

## Private Deployment Options

### 1. Azure Artifacts (Recommended for Enterprise)

Azure DevOps Artifacts provides enterprise-grade private package hosting.

**Setup:**
1. Create Azure DevOps organization
2. Create a new feed in Artifacts
3. Configure authentication

**Deployment:**

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - master
    - release/*

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Build projects'
  inputs:
    command: 'build'
    configuration: $(buildConfiguration)

- task: DotNetCoreCLI@2
  displayName: 'Pack projects'
  inputs:
    command: 'pack'
    configuration: $(buildConfiguration)
    outputDir: '$(Build.ArtifactStagingDirectory)'

- task: NuGetAuthenticate@1
  displayName: 'Authenticate with Azure Artifacts'

- task: DotNetCoreCLI@2
  displayName: 'Push to Azure Artifacts'
  inputs:
    command: 'push'
    publishVstsFeed: 'your-organization/your-feed-name'
    publishFeedCredentials: 'Azure Artifacts'
```

**Consumption:**

```xml
<!-- nuget.config -->
<configuration>
  <packageSources>
    <add key="YourPrivateFeed" value="https://pkgs.dev.azure.com/yourorg/_packaging/yourfeed/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

### 2. GitHub Packages (Private)

Use GitHub Packages with private repositories for internal packages.

```bash
# Authenticate
echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u ${{ github.actor }} --password-stdin

# Configure package source
dotnet nuget add source --username YOUR_USERNAME --password YOUR_TOKEN --store-password-in-clear-text --name github-private "https://nuget.pkg.github.com/YOUR_ORG/index.json"

# Push package
dotnet nuget push "src/*/bin/Release/*.nupkg" --source github-private
```

### 3. Self-Hosted NuGet Server

For complete control, host your own NuGet server.

**Using Baget (Lightweight):**

```yaml
# docker-compose.yml
version: '3'
services:
  baget:
    image: loicsharma/baget:latest
    ports:
      - "5000:80"
    environment:
      - ApiKey=YOUR_API_KEY
      - Storage__Type=FileSystem
      - Storage__Path=/var/baget-packages
      - Database__Type=Sqlite
      - Database__ConnectionString=Data Source=/var/baget.db
      - Search__Type=Database
    volumes:
      - baget-data:/var/baget-packages
      - baget-db:/var
volumes:
  baget-data:
  baget-db:
```

**Push to self-hosted:**

```bash
dotnet nuget push "src/*/bin/Release/*.nupkg" -k YOUR_API_KEY -s http://your-server:5000/v3/index.json
```

### 4. Network Folder/File Share

Simple approach for small teams with shared network access.

```bash
# Copy packages to network share
dotnet pack -c Release -o "\\server\packages"

# Configure client
dotnet nuget add source "\\server\packages" --name "CompanyPackages"
```

## Package Versioning Strategy

### Semantic Versioning

Follow semantic versioning (SemVer) for public packages:

```xml
<PropertyGroup>
  <Version>1.2.3</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.2.3.0</FileVersion>
</PropertyGroup>
```

### Automated Versioning

Use GitVersion for automatic versioning based on Git history:

```yaml
# .github/workflows/build.yml
- name: Install GitVersion
  uses: gittools/actions/gitversion/setup@v0.9.7
  with:
    versionSpec: '5.x'

- name: Determine Version
  id: gitversion
  uses: gittools/actions/gitversion/execute@v0.9.7

- name: Build with Version
  run: dotnet pack -c Release -p:Version=${{ steps.gitversion.outputs.nuGetVersionV2 }}
```

## Best Practices

### 1. Package Metadata

Ensure proper metadata in .csproj files:

```xml
<PropertyGroup>
  <PackageId>EasyAuth.Framework.Core</PackageId>
  <Title>EasyAuth Framework - Core Authentication Services</Title>
  <Description>Core authentication services and providers for the EasyAuth Framework</Description>
  <Authors>Your Name</Authors>
  <Company>Your Company</Company>
  <Product>EasyAuth Framework</Product>
  <Copyright>Copyright © Your Company 2024</Copyright>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/yourusername/easyauth</PackageProjectUrl>
  <RepositoryUrl>https://github.com/yourusername/easyauth</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageIcon>icon.png</PackageIcon>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageTags>authentication;oauth;identity;aspnetcore</PackageTags>
  <PackageReleaseNotes>See CHANGELOG.md for details</PackageReleaseNotes>
</PropertyGroup>

<ItemGroup>
  <None Include="icon.png" Pack="true" PackagePath="" />
  <None Include="README.md" Pack="true" PackagePath="" />
</ItemGroup>
```

### 2. Symbol Packages

Include debug symbols for better debugging:

```xml
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### 3. Strong Naming (Optional)

For enterprise scenarios:

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>key.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

### 4. Multi-Targeting

Support multiple .NET versions:

```xml
<PropertyGroup>
  <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>
</PropertyGroup>
```

### 5. Continuous Integration

Implement automated quality gates:

```yaml
- name: Run Tests
  run: dotnet test --configuration Release --logger trx --collect:"XPlat Code Coverage"

- name: Run Security Scan
  run: dotnet list package --vulnerable --include-transitive

- name: Pack only if tests pass
  if: success()
  run: dotnet pack -c Release
```

## Security Considerations

### 1. API Key Management

- Use GitHub Secrets or Azure Key Vault for API keys
- Rotate keys regularly
- Use least privilege access

### 2. Package Signing

Consider signing packages for integrity verification:

```bash
# Sign with certificate
nuget sign MyPackage.1.0.0.nupkg -CertificateSubjectName "CN=My Company" -Timestamper http://timestamp.digicert.com
```

### 3. Vulnerability Scanning

Regularly scan dependencies:

```bash
# Check for vulnerabilities
dotnet list package --vulnerable
dotnet list package --outdated
```

## Consumption Examples

### Public Package Installation

```bash
# Install from NuGet.org
dotnet add package EasyAuth.Framework.Core
dotnet add package EasyAuth.Framework.Extensions
```

### Private Package Installation

```bash
# Configure private source
dotnet nuget add source https://your-private-feed/v3/index.json --name "PrivateFeed" --username your-user --password your-token

# Install from private feed
dotnet add package EasyAuth.Framework.Core --source PrivateFeed
```

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Verify API keys and permissions
   - Check network connectivity
   - Validate feed URLs

2. **Version Conflicts**
   - Use explicit version ranges
   - Check for pre-release packages
   - Clear NuGet cache: `dotnet nuget locals all --clear`

3. **Package Not Found**
   - Verify package source configuration
   - Check package ID and version
   - Ensure proper authentication

### Useful Commands

```bash
# List configured sources
dotnet nuget list source

# Clear local cache
dotnet nuget locals all --clear

# Verify package integrity
dotnet nuget verify MyPackage.1.0.0.nupkg

# List installed packages
dotnet list package

# Update all packages
dotnet list package --outdated
```

This guide provides comprehensive deployment options for both public and private NuGet package distribution, allowing you to choose the best approach for your specific needs and security requirements.