# Contributing to EasyAuth Framework

Welcome to the EasyAuth Framework! We're excited that you want to contribute. This document outlines our development practices, quality standards, and contribution guidelines.

## 🚀 Getting Started

### Prerequisites

Before contributing, ensure you have:

- .NET 8.0 SDK installed
- Git configured with your GitHub account
- A code editor (Visual Studio, VS Code, or JetBrains Rider recommended)
- Docker Desktop (optional, for containerized development)

### Development Environment Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/dbbuilder/easyauth.git
   cd easyauth
   ```

2. **Run the setup script**
   ```bash
   # Windows PowerShell
   .\scripts\setup-dev-environment.ps1 -All
   
   # Or step by step
   .\scripts\setup-dev-environment.ps1 -InstallTools -SetupGitHooks
   ```

3. **Verify your setup**
   ```bash
   dotnet build
   dotnet test
   ```

## 📋 Development Workflow

### Branch Strategy

- **`master`**: Production-ready code
- **`develop`**: Integration branch for features
- **`feature/*`**: Individual features
- **`hotfix/*`**: Critical production fixes
- **`release/*`**: Release preparation

### Commit Messages

Follow conventional commits:
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

Example:
```
feat(auth): implement Apple Sign-In provider

Add support for Apple Sign-In authentication with proper token validation
and user profile mapping. Includes comprehensive test coverage.

Closes #123
```

## 🔍 Quality Standards

### Test-Driven Development (TDD)

**MANDATORY**: All features must follow TDD practices.

1. **RED**: Write failing tests that define expected behavior
2. **GREEN**: Write minimal code to make tests pass
3. **REFACTOR**: Improve code while keeping tests green

### Code Coverage

- **Minimum**: 85% code coverage
- **Target**: 90%+ code coverage
- **Critical paths**: 100% coverage

### Code Quality Checks

Our CI/CD pipeline enforces:

#### Static Analysis
- **SonarCloud**: Code quality, bugs, vulnerabilities, code smells
- **CodeQL**: Security vulnerability scanning
- **StyleCop**: C# code style enforcement
- **Microsoft Code Analysis**: .NET best practices

#### Security Scanning
- **Trivy**: Dependency vulnerability scanning
- **CodeQL**: Security hotspots and vulnerabilities
- **Secret detection**: Prevents credential leaks

#### Performance Standards
- Response times < 200ms for authentication endpoints
- Memory usage < 100MB for base framework
- Zero memory leaks in long-running scenarios

## 🛠️ Development Tools

### Required Tools

```bash
# Global .NET tools
dotnet tool install -g dotnet-ef
dotnet tool install -g dotnet-outdated-tool
dotnet tool install -g dotnet-sonarscanner
dotnet tool install -g security-scan
```

### IDE Configuration

#### Visual Studio Code
Install extensions:
- C# for Visual Studio Code
- GitLens
- SonarLint
- EditorConfig
- Docker

#### Visual Studio
Enable:
- Code Analysis on Build
- EditorConfig support
- Live Unit Testing (Premium)

### Pre-commit Hooks

Pre-commit hooks automatically run:
- Code formatting (`dotnet format`)
- Compilation check
- Unit tests
- Security scans
- Linting

Install with:
```bash
pip install pre-commit
pre-commit install
```

## 🧪 Testing Guidelines

### Test Structure

```csharp
[Fact]
public async Task AuthenticateAsync_WithValidGoogleToken_ShouldReturnUser()
{
    // Arrange
    var provider = new GoogleAuthProvider(_mockHttpClient.Object, _logger.Object);
    var validToken = "valid.google.token";
    
    // Act
    var result = await provider.AuthenticateAsync(validToken);
    
    // Assert
    result.Should().NotBeNull();
    result.Provider.Should().Be("Google");
    result.IsAuthenticated.Should().BeTrue();
}
```

### Test Categories

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **Security Tests**: Test authentication flows and security measures
4. **Performance Tests**: Validate response times and resource usage

### Test Naming

```csharp
// Pattern: MethodName_StateUnderTest_ExpectedBehavior
[Fact]
public void ValidateToken_WithExpiredToken_ShouldThrowSecurityException()

// For async methods
[Fact] 
public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnUnauthorized()
```

## 🔒 Security Guidelines

### Authentication & Authorization
- Always validate tokens server-side
- Use secure token storage (HTTP-only cookies)
- Implement proper CSRF protection
- Follow OWASP security guidelines

### Secret Management
- Never commit secrets to version control
- Use Azure Key Vault for production secrets
- Use user secrets for local development
- Rotate secrets regularly

### Data Protection
- Encrypt sensitive data at rest
- Use HTTPS for all communication
- Implement proper input validation
- Log security events (without sensitive data)

## 📦 Package Development

### NuGet Package Standards

```xml
<PropertyGroup>
  <PackageId>EasyAuth.Framework.Core</PackageId>
  <PackageVersion>1.0.0</PackageVersion>
  <Authors>EasyAuth Development Team</Authors>
  <Description>Enterprise-grade authentication framework</Description>
  <PackageTags>authentication;oauth;security</PackageTags>
  <RepositoryUrl>https://github.com/dbbuilder/easyauth</RepositoryUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### Versioning

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## 🚀 CI/CD Pipeline

### Automated Workflows

1. **Pull Request Checks**
   - Build verification
   - Test execution
   - Code coverage analysis
   - Security scanning
   - SonarCloud analysis

2. **Release Process**
   - Automated versioning
   - GitHub release creation
   - NuGet package publishing
   - Documentation updates

### Quality Gates

Pull requests must pass:
- ✅ All tests passing
- ✅ 85%+ code coverage
- ✅ No security vulnerabilities
- ✅ SonarCloud quality gate
- ✅ Code review approval

## 📝 Documentation Standards

### Code Documentation

```csharp
/// <summary>
/// Authenticates a user using the specified authentication provider.
/// </summary>
/// <param name="provider">The authentication provider to use.</param>
/// <param name="credentials">The user credentials.</param>
/// <returns>An authentication result containing user information.</returns>
/// <exception cref="SecurityException">Thrown when authentication fails.</exception>
public async Task<AuthenticationResult> AuthenticateAsync(
    IAuthProvider provider, 
    UserCredentials credentials)
{
    // Implementation
}
```

### API Documentation

- Use XML documentation comments
- Provide examples for complex scenarios
- Document error conditions and exceptions
- Include performance considerations

## 🤝 Pull Request Process

### Before Submitting

1. **Ensure code quality**
   ```bash
   # Run all quality checks
   pre-commit run --all-files
   
   # Verify tests pass
   dotnet test --configuration Release
   
   # Check code coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

2. **Update documentation**
   - Update README if needed
   - Add/update API documentation
   - Update CHANGELOG.md

3. **Self-review**
   - Review your own changes
   - Ensure commit messages are clear
   - Verify no sensitive data is included

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Tests pass locally
- [ ] Documentation updated
```

### Review Process

1. **Automated checks** must pass
2. **Code review** by at least one maintainer
3. **Security review** for security-related changes
4. **Performance review** for performance-critical changes

## 🐛 Issue Reporting

### Bug Reports

Use the issue template and include:
- Environment details (.NET version, OS, etc.)
- Steps to reproduce
- Expected vs actual behavior
- Code samples (if applicable)
- Error messages and stack traces

### Feature Requests

Include:
- Use case description
- Proposed solution
- Alternative solutions considered
- Breaking change impact

## 📞 Getting Help

- 💬 [GitHub Discussions](https://github.com/dbbuilder/easyauth/discussions)
- 🐛 [Issue Tracker](https://github.com/dbbuilder/easyauth/issues)
- 📧 Email: support@easyauth.dev
- 📖 [Documentation](https://docs.easyauth.dev)

## 🏆 Recognition

Contributors are recognized in:
- Release notes
- Contributors section in README
- Special recognition for significant contributions

Thank you for contributing to EasyAuth Framework! 🚀