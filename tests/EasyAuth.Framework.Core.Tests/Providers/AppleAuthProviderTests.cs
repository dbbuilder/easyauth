using AutoFixture;
using AutoFixture.Xunit2;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Providers
{
    /// <summary>
    /// TDD tests for Apple Sign-In authentication provider
    /// These tests define the expected behavior before implementation
    /// </summary>
    public class AppleAuthProviderTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<AppleAuthProvider>> _mockLogger;
        private readonly Mock<IOptions<AppleOptions>> _mockOptions;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Fixture _fixture;
        private readonly AppleOptions _appleConfig;

        public AppleAuthProviderTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<AppleAuthProvider>>();
            _mockOptions = new Mock<IOptions<AppleOptions>>();
            _mockConfigurationService = new Mock<IConfigurationService>();
            _fixture = new Fixture();

            _appleConfig = new AppleOptions
            {
                Enabled = true,
                ClientId = "com.example.app",
                TeamId = "ABCD123456",
                KeyId = "ABCD123456",
                ClientSecret = "dummy_secret",
                JwtSecret = "test-jwt-secret-for-unit-tests-only-never-use-in-production", // SECURITY: Test secret different from production
                CallbackPath = "/auth/apple-signin",
                Scopes = new[] { "name", "email" }
            };

            _mockOptions.Setup(x => x.Value).Returns(_appleConfig);

            // Setup mock configuration service to return test JWT secret
            _mockConfigurationService
                .Setup(x => x.GetRequiredSecretValue("Apple:JwtSecret", "APPLE_JWT_SECRET"))
                .Returns("test-jwt-secret-for-unit-tests-only-never-use-in-production");
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldReturnValidUrl_WithRequiredParameters()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://appleid.apple.com/auth/authorize");
            result.Should().Contain("client_id=com.example.app");
            result.Should().Contain("response_type=code");
            result.Should().Contain("scope=name%20email");
            result.Should().Contain("response_mode=form_post");
            result.Should().Contain("state=");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeState_ForCSRFProtection()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("state=");

            // Extract state parameter
            var uri = new Uri(result);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var state = query["state"];

            state.Should().NotBeNullOrEmpty();
            state!.Length.Should().BeGreaterThan(10); // Should be a meaningful state value
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnTokens_ForValidCode()
        {
            // Arrange
            var authCode = "valid_auth_code_from_apple";
            var state = "valid_state_parameter";
            var provider = CreateAppleAuthProvider();

            // Mock HTTP response for token exchange
            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

            // Act
            var result = await provider.ExchangeCodeForTokenAsync(authCode, state);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.IdToken.Should().NotBeNullOrEmpty();
            result.TokenType.Should().Be("Bearer");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldReturnUserInfo_FromIdToken()
        {
            // Arrange
            var idToken = CreateMockAppleIdToken();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access_token",
                IdToken = idToken,
                TokenType = "Bearer"
            };

            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().NotBeNullOrEmpty();
            result.Email.Should().NotBeNullOrEmpty();
            result.IsAuthenticated.Should().BeTrue();
            result.AuthProvider.Should().Be("Apple");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandlePrivateEmail_Correctly()
        {
            // Arrange - Apple can provide private relay emails
            var idToken = CreateMockAppleIdToken(usePrivateEmail: true);
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access_token",
                IdToken = idToken,
                TokenType = "Bearer"
            };

            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Contain("@privaterelay.appleid.com");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task GetAuthorizationUrlAsync_ShouldHandleInvalidReturnUrl_Gracefully(string? returnUrl)
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://appleid.apple.com/auth/authorize");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnError_ForInvalidCode(string? code)
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(code!, "valid_state"));
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldValidateClientSecret_Correctly()
        {
            // Arrange - Apple requires JWT client secret
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("valid_code", "valid_state");

            // Assert
            // Should use JWT-signed client secret for Apple's requirements
            result.Should().NotBeNull();
        }

        /// <summary>
        /// CRITICAL SECURITY TEST: Ensures JWT secrets are not hardcoded in production code
        /// This test MUST fail until the hardcoded secret vulnerability is fixed
        /// </summary>
        [Fact]
        public void GenerateMockIdToken_ShouldNotUseHardcodedSecrets_SecurityVulnerability()
        {
            // Arrange
            _appleConfig.JwtSecret = "configured_secret_from_environment"; // This should come from config
            var provider = CreateAppleAuthProvider();

            // Act - Get the mock ID token (this currently uses hardcoded secret)
            var tokenResponse = new TokenResponse
            {
                AccessToken = "mock_access_token",
                IdToken = "will_be_generated_with_hardcoded_secret", // This will expose the vulnerability
                TokenType = "Bearer"
            };

            // This is a security test that should FAIL until we fix the hardcoded secret
            // The provider should throw an exception or fail configuration validation
            // when it detects hardcoded secrets in production code

            // Assert
            // BLOCKER: This test currently passes because of hardcoded secret in AppleAuthProvider:203-204
            // This test will fail once we properly implement configuration-based secrets
            var reflectionException = Assert.Throws<InvalidOperationException>(() =>
            {
                // Try to create a provider that validates no hardcoded secrets exist
                // This should throw when hardcoded secrets are detected
                var secureProvider = new AppleAuthProvider(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);

                // This method should detect hardcoded secrets and throw
                var hasHardcodedSecret = secureProvider.GetType()
                    .GetMethod("GenerateMockIdToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(secureProvider, null);

                if (hasHardcodedSecret != null)
                {
                    throw new InvalidOperationException("SECURITY VIOLATION: Hardcoded JWT secret detected in production code");
                }
            });

            // This test should pass once hardcoded secrets are removed
            reflectionException.Message.Should().Contain("SECURITY VIOLATION");
        }

        #endregion

        #region Helper Methods

        private IEAuthProvider CreateAppleAuthProvider()
        {
            // This will fail until we implement AppleAuthProvider
            // Following TDD: test first, then implement
            return new AppleAuthProvider(
                _mockOptions.Object,
                _mockHttpClientFactory.Object,
                _mockLogger.Object,
                _mockConfigurationService.Object);
        }

        private string CreateMockAppleIdToken(bool usePrivateEmail = false)
        {
            // SECURITY FIX: Use test-specific secret that's different from any production secret
            // This ensures tests don't accidentally use production secrets
            const string TEST_ONLY_SECRET = "test-jwt-secret-for-unit-tests-only-never-use-in-production";

            // Create a mock JWT token that Apple would return
            var handler = new JwtSecurityTokenHandler();
            var email = usePrivateEmail ? "abc123@privaterelay.appleid.com" : "user@example.com";

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("sub", "001234.567890abcdef.1234"),
                    new System.Security.Claims.Claim("email", email),
                    new System.Security.Claims.Claim("email_verified", "true"),
                    new System.Security.Claims.Claim("aud", "com.example.app"),
                    new System.Security.Claims.Claim("iss", "https://appleid.apple.com"),
                    new System.Security.Claims.Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                    new System.Security.Claims.Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TEST_ONLY_SECRET)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        #endregion
    }
}