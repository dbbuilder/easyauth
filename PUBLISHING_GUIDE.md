# EasyAuth Framework - Publishing Guide

## 🔑 Manual Publishing (One-time setup)

### Get Your NuGet API Key
1. Sign in to [nuget.org](https://www.nuget.org) with **info@servicevision.io**
2. Go to your account → **API Keys**
3. Create new key:
   - **Name**: `EasyAuth-Publishing`
   - **Scopes**: `Push new packages and package versions`
   - **Package Pattern**: `EasyAuth.*`
4. Copy the API key (shown only once!)

### Publish Packages
```bash
# Navigate to project root
cd /mnt/d/dev2/easyauth

# Publish Core package first
dotnet nuget push ./nuget-packages/EasyAuth.Framework.Core.1.0.0-alpha.1.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate

# Publish main package (depends on Core)
dotnet nuget push ./nuget-packages/EasyAuth.Framework.1.0.0-alpha.1.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

## 🤖 Automated Publishing Setup

### Set up GitHub Secrets
1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions
3. Click **New repository secret**
4. Add:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Your NuGet API key from above

### Trigger Automated Publishing
```bash
# Create and push a version tag
git tag v1.0.0-alpha.1
git push origin v1.0.0-alpha.1

# GitHub Actions will automatically:
# ✅ Build and test packages
# ✅ Publish to NuGet.org
# ✅ Create GitHub release
```

## 📦 After Publishing

### Verify Publication
1. Check [nuget.org/packages/EasyAuth.Framework](https://www.nuget.org/packages/EasyAuth.Framework)
2. Check [nuget.org/packages/EasyAuth.Framework.Core](https://www.nuget.org/packages/EasyAuth.Framework.Core)

### Test Installation
```bash
# Create test project
dotnet new console -n TestEasyAuth
cd TestEasyAuth

# Install published package
dotnet add package EasyAuth.Framework --version 1.0.0-alpha.1

# Should work after publication!
```

## 🔄 Future Releases

### Version Bump Process
1. Update version in project files:
   ```xml
   <PackageVersion>1.0.0-alpha.2</PackageVersion>
   ```

2. Commit changes:
   ```bash
   git add -A
   git commit -m "chore: bump version to 1.0.0-alpha.2"
   ```

3. Create and push tag:
   ```bash
   git tag v1.0.0-alpha.2
   git push origin v1.0.0-alpha.2
   ```

4. GitHub Actions automatically publishes new version

### Manual Version Publishing
```bash
# Build new packages
dotnet pack --configuration Release --output ./nuget-packages

# Publish new versions
dotnet nuget push ./nuget-packages/EasyAuth.Framework.Core.NEW_VERSION.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./nuget-packages/EasyAuth.Framework.NEW_VERSION.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## 🏷️ Package Ownership

**Published under**: info@servicevision.io  
**Organization**: ServiceVision  
**License**: MIT  
**Repository**: https://github.com/dbbuilder/easyauth

## 📊 Package Statistics

After publishing, monitor:
- **Download counts**: NuGet.org package pages
- **Usage analytics**: NuGet insights
- **Issues/feedback**: GitHub repository
- **Dependencies**: Dependency graphs

## 🔒 Security

### Package Signing (Optional)
```bash
# Sign packages with certificate
nuget sign ./nuget-packages/*.nupkg \
  -CertificateSubjectName "ServiceVision" \
  -Timestamper http://timestamp.digicert.com
```

### Vulnerability Scanning
- NuGet.org automatically scans for vulnerabilities
- GitHub Dependabot monitors dependencies
- SonarCloud provides security analysis

---

**Ready to publish EasyAuth Framework to the world! 🚀**