# SonarCloud Integration Setup

This document describes the SonarCloud integration for the EasyAuth Framework project, which includes both .NET and JavaScript/TypeScript components.

## Overview

The SonarCloud analysis workflow automatically runs on:
- Push to `master`, `main`, or `develop` branches
- Pull requests (opened, synchronized, reopened)

## Configuration

### Required Secrets

Configure these secrets in your GitHub repository settings:

1. **SONAR_TOKEN**: Your SonarCloud token
   - Go to https://sonarcloud.io/account/security
   - Generate a new token
   - Add it as a repository secret

2. **GITHUB_TOKEN**: Automatically provided by GitHub Actions
   - Used for PR decoration and comments

### Project Structure

The analysis covers:
- **.NET Projects**: `src/` directory with C# code
- **JavaScript SDK**: `packages/easyauth-js-sdk/` directory with TypeScript code
- **Tests**: Both .NET tests in `tests/` and JS tests in `packages/easyauth-js-sdk/tests/`

## Analysis Features

### Code Coverage
- **.NET**: Uses OpenCover format (`coverage.opencover.xml`)
- **JavaScript/TypeScript**: Uses LCOV format (`coverage/lcov.info`)
- **Test Results**: .NET uses `.trx` files, JS uses Jest coverage

### Security Analysis
Enhanced security rules for authentication frameworks:
- HTTPS enforcement
- Cryptographic security
- Hardcoded credentials detection
- JWT security validation
- Cross-site scripting prevention
- Session security

### Quality Gates
- Automatic quality gate evaluation
- PR decoration with results
- Fail-fast on critical issues

## Local Development

### Running Analysis Locally

For .NET projects:
```bash
# Install SonarScanner
dotnet tool install --global dotnet-sonarscanner

# Start analysis
dotnet sonarscanner begin /k:"dbbuilder_easyauth" /o:"dbbuilder" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.token="YOUR_TOKEN"

# Build and test
dotnet build
dotnet test --collect:"XPlat Code Coverage" --settings coverage.settings

# End analysis
dotnet sonarscanner end /d:sonar.token="YOUR_TOKEN"
```

For JavaScript SDK:
```bash
cd packages/easyauth-js-sdk
npm run test:coverage
```

### Pre-commit Checks

Before pushing changes, run:
```bash
# .NET
dotnet build
dotnet test

# JavaScript SDK
cd packages/easyauth-js-sdk
npm run validate
```

## Troubleshooting

### Common Issues

1. **Missing Coverage Reports**
   - Ensure tests run successfully
   - Check coverage file paths in `sonar-project.properties`
   - Verify file permissions and paths

2. **Authentication Errors**
   - Verify `SONAR_TOKEN` secret is set correctly
   - Check token permissions in SonarCloud

3. **Build Failures**
   - Ensure all dependencies are restored
   - Check .NET version compatibility
   - Verify Node.js version for JS SDK

### Debug Mode

Enable debug logging by adding to workflow:
```yaml
env:
  SONAR_SCANNER_OPTS: "-Dsonar.verbose=true"
```

## SonarCloud Dashboard

Access your project analysis at:
https://sonarcloud.io/project/overview?id=dbbuilder_easyauth

## Quality Profiles

The project uses custom quality profiles optimized for authentication frameworks with enhanced security rules.