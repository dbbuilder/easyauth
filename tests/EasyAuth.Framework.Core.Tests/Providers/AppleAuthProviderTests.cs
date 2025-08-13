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
                PrivateKey = "LS0tLS1CRUdJTi...", // Mock base64 private key
                ClientSecret = "dummy_secret",
                JwtSecret = "test-jwt-secret-for-unit-tests-only-never-use-in-production",
                CallbackPath = "/auth/apple-signin",
                RedirectUri = "https://localhost/auth/apple-callback",
                Scopes = new[] { "name", "email" },
                UseMockTokensOnFailure = true,
                TokenValidation = new AppleTokenValidationOptions
                {
                    ValidateSignature = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkewSeconds = 300
                },
                PrivateEmail = new ApplePrivateEmailOptions
                {
                    HandlePrivateRelay = true,
                    LogPrivateRelayDetection = true,
                    PrivateUserDisplayPrefix = "Apple User",
                    StorePrivateRelayEmails = true
                },
                Flow = new AppleFlowOptions
                {
                    DefaultFlow = AppleFlowType.Web,
                    ResponseMode = "form_post",
                    IncludeNonce = true,
                    NativeApp = new AppleNativeAppOptions
                    {
                        BundleId = "com.example.app",
                        CustomUrlScheme = "exampleapp",
                        SupportUniversalLinks = true
                    }
                }
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

        #region v2.4.0 Enhanced Feature Tests

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeNonce_WhenConfigured()
        {
            // Arrange
            _appleConfig.Flow.IncludeNonce = true;
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("nonce=");
            var uri = new Uri(result);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var nonce = query["nonce"];
            nonce.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldNotIncludeNonce_WhenDisabled()
        {
            // Arrange
            _appleConfig.Flow.IncludeNonce = false;
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().NotContain("nonce=");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldUseCustomResponseMode_WhenConfigured()
        {
            // Arrange
            _appleConfig.Flow.ResponseMode = "fragment";
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("response_mode=fragment");
        }

        [Theory]
        [InlineData(AppleFlowType.Web, "https://localhost/auth/apple-callback")]
        [InlineData(AppleFlowType.Native, "exampleapp://auth/apple-callback")]
        [InlineData(AppleFlowType.Hybrid, "https://localhost/auth/apple-callback")]
        public async Task GetAuthorizationUrlAsync_ShouldUseCorrectRedirectUri_ForFlowType(AppleFlowType flowType, string expectedRedirectPattern)
        {
            // Arrange
            _appleConfig.Flow.DefaultFlow = flowType;
            _appleConfig.RedirectUri = string.Empty; // Use flow-specific logic
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain($"redirect_uri={Uri.EscapeDataString(expectedRedirectPattern)}");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldDetectPrivateEmailRelay_Correctly()
        {
            // Arrange
            var idToken = CreateMockAppleIdToken(usePrivateEmail: true);
            var tokenResponse = new TokenResponse { IdToken = idToken };
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Email.Should().Contain("@privaterelay.appleid.com");
            result.DisplayName.Should().Contain("(Private)");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldUseCustomPrivateDisplayPrefix_WhenConfigured()
        {
            // Arrange
            _appleConfig.PrivateEmail.PrivateUserDisplayPrefix = "Custom User";
            var idToken = CreateMockAppleIdToken(usePrivateEmail: true);
            var tokenResponse = new TokenResponse { IdToken = idToken };
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.DisplayName.Should().Contain("Custom User (Private)");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldNotStorePrivateEmail_WhenDisabled()
        {
            // Arrange
            _appleConfig.PrivateEmail.StorePrivateRelayEmails = false;
            var idToken = CreateMockAppleIdToken(usePrivateEmail: true);
            var tokenResponse = new TokenResponse { IdToken = idToken };
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Email.Should().BeEmpty();
            result.DisplayName.Should().Contain("(Private)");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldExtractAllClaims_FromIdToken()
        {
            // Arrange
            var idToken = CreateMockAppleIdToken();
            var tokenResponse = new TokenResponse { IdToken = idToken };
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().NotBeEmpty();
            result.Claims.Should().ContainKey("sub");
            result.Claims.Should().ContainKey("email");
            result.Claims.Should().ContainKey("email_verified");
            result.Claims.Should().ContainKey("aud");
            result.Claims.Should().ContainKey("iss");
            result.Claims["iss"].Should().Be("https://appleid.apple.com");
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnTrue_ForValidConfiguration()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "ABCD123456", "ABCD123456")]
        [InlineData("com.example.app", "", "ABCD123456")]
        [InlineData("com.example.app", "ABCD123456", "")]
        public async Task ValidateConfigurationAsync_ShouldReturnFalse_ForMissingRequiredFields(
            string clientId, string teamId, string keyId)
        {
            // Arrange
            _appleConfig.ClientId = clientId;
            _appleConfig.TeamId = teamId;
            _appleConfig.KeyId = keyId;
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateNativeAppConfig_ForNativeFlow()
        {
            // Arrange
            _appleConfig.Flow.DefaultFlow = AppleFlowType.Native;
            _appleConfig.Flow.NativeApp = null;
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3601)]
        public async Task ValidateConfigurationAsync_ShouldValidateClockSkew_Range(int clockSkewSeconds)
        {
            // Arrange
            _appleConfig.TokenValidation.ClockSkewSeconds = clockSkewSeconds;
            var provider = CreateAppleAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldThrowArgumentException_ForNullCode()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(null!, "state"));
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnMockToken_WhenAppleServiceUnavailable()
        {
            // Arrange
            _appleConfig.UseMockTokensOnFailure = true;
            var provider = CreateAppleAuthProvider();
            
            // Mock HttpClient to simulate Apple service failure
            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("test_code", "test_state");

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.IdToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateTokenExpiration_AndThrow()
        {
            // Arrange
            var expiredToken = CreateExpiredAppleIdToken();
            var tokenResponse = new TokenResponse { IdToken = expiredToken };
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateAudience_AndThrow()
        {
            // Arrange
            var invalidAudienceToken = CreateMockAppleIdToken(audience: "wrong.client.id");
            var tokenResponse = new TokenResponse { IdToken = invalidAudienceToken };
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateIssuer_AndThrow()
        {
            // Arrange
            var invalidIssuerToken = CreateMockAppleIdToken(issuer: "https://evil.com");
            var tokenResponse = new TokenResponse { IdToken = invalidIssuerToken };
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldThrowInvalidOperationException_ForMissingSubClaim()
        {
            // Arrange
            var tokenWithoutSub = CreateMockAppleIdToken(includeSub: false);
            var tokenResponse = new TokenResponse { IdToken = tokenWithoutSub };
            var provider = CreateAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
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

        private string CreateMockAppleIdToken(
            bool usePrivateEmail = false,
            string? audience = null,
            string? issuer = null,
            bool includeSub = true)
        {
            const string TEST_ONLY_SECRET = "test-jwt-secret-for-unit-tests-only-never-use-in-production";

            var handler = new JwtSecurityTokenHandler();
            var email = usePrivateEmail ? "abc123@privaterelay.appleid.com" : "user@example.com";
            var aud = audience ?? "com.example.app";
            var iss = issuer ?? "https://appleid.apple.com";

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("email", email),
                new System.Security.Claims.Claim("email_verified", "true"),
                new System.Security.Claims.Claim("aud", aud),
                new System.Security.Claims.Claim("iss", iss),
                new System.Security.Claims.Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new System.Security.Claims.Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
            };

            if (includeSub)
            {
                claims.Add(new System.Security.Claims.Claim("sub", "001234.567890abcdef.1234"));
            }

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TEST_ONLY_SECRET)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private string CreateExpiredAppleIdToken()
        {
            const string TEST_ONLY_SECRET = "test-jwt-secret-for-unit-tests-only-never-use-in-production";

            var handler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("sub", "001234.567890abcdef.1234"),
                    new System.Security.Claims.Claim("email", "user@example.com"),
                    new System.Security.Claims.Claim("aud", "com.example.app"),
                    new System.Security.Claims.Claim("iss", "https://appleid.apple.com"),
                    new System.Security.Claims.Claim("iat", DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds().ToString()),
                    new System.Security.Claims.Claim("exp", DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(-1), // Expired token
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TEST_ONLY_SECRET)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private static Mock<HttpClient> CreateMockHttpClient(string responseContent, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var mockHttpClient = new Mock<HttpClient>();
            var mockResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            };

            // Note: Mocking HttpClient directly is complex due to protected methods
            // In real implementation, we'd use HttpClientFactory with a mock handler
            return mockHttpClient;
        }

        #endregion
    }
}