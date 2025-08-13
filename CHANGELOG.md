# EasyAuth Framework Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.1] - 2025-08-13

### Fixed
- **Async Method Warnings**: Removed unnecessary async/await patterns in provider methods that don't perform asynchronous operations
  - Fixed GoogleAuthProvider.GetLoginUrlAsync to use Task.FromResult instead of async/await
  - Fixed AppleAuthProvider async method warnings in GenerateClientSecretAsync and ValidateIdTokenAsync
  - Fixed FacebookAuthProvider.TryParseErrorResponse async method warnings
  - Fixed CsrfProtectionMiddleware.SetCsrfTokenAsync to use Task.CompletedTask
  - Fixed RateLimitingMiddleware.CheckSlidingWindowLimit to use Task.FromResult

- **Code Quality Improvements**:
  - Enhanced nullable reference type handling in InputValidationMiddleware
  - Fixed HttpClient mock configuration in provider unit tests
  - Improved test setup for Apple and Facebook authentication providers

- **Docker Integration Tests**:
  - Added DockerRequiredFactAttribute for proper test skipping when Docker is unavailable
  - Enhanced BaseIntegrationTest to conditionally initialize database containers
  - Added proper guards for database operations when Docker is not available
  - Fixed integration test configuration to handle missing connection strings gracefully

### Improved
- **Test Infrastructure**: Stabilized provider implementation testing with proper mock configurations
- **Error Handling**: Better handling of Docker unavailable scenarios in integration tests
- **Code Analysis**: Reduced StyleCop and nullable reference warnings across the codebase

### Technical Debt
- Reduced async method warnings from 8 to 0
- Improved test reliability and developer experience when Docker is not installed
- Enhanced SonarCloud integration readiness

## [2.4.0] - 2025-01-15

### Added
- **Universal Integration System**: Complete transformation from .NET-only to universal authentication framework
- **Frontend Packages**: React (@easyauth/react) and Vue (@easyauth/vue) packages with zero-config setup
- **StandardApiController**: Universal backend API for any frontend framework
- **Provider Completion**: Apple (100%), Facebook (100%), Azure B2C (100%)

### Changed
- **Developer Experience**: Reduced frontend integration time from 30+ hours to under 2 hours
- **Architecture**: Universal approach supporting any frontend technology

### Security
- Comprehensive security enhancements and audit logging
- Enhanced input validation and CSRF protection

## [2.3.0] - 2024-12-20

### Added
- Zero-Configuration Release with automatic provider detection
- Enhanced CORS configuration system
- Comprehensive security hardening

### Fixed
- Critical CorrelationIdMiddleware registration issue
- Multiple provider configuration edge cases

### Improved
- Developer documentation and integration guides
- CI/CD pipeline enhancements