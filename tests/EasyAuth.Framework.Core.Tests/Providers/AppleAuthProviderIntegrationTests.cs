using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Providers
{
    /// <summary>
    /// Integration tests for Apple Sign-In provider
    /// Tests real-world scenarios and end-to-end flows
    /// </summary>
    public class AppleAuthProviderIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<AppleAuthProvider>> _mockLogger;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly AppleOptions _appleConfig;

        public AppleAuthProviderIntegrationTests()
        {
            _httpClient = new HttpClient();
            _mockLogger = new Mock<ILogger<AppleAuthProvider>>();
            _mockConfigurationService = new Mock<IConfigurationService>();

            _appleConfig = new AppleOptions
            {
                Enabled = true,
                ClientId = "com.example.test",
                TeamId = "TEST123456",
                KeyId = "TEST123456",
                JwtSecret = "test-integration-secret-for-unit-tests-only",
                UseMockTokensOnFailure = true, // Allow mock tokens in integration tests
                TokenValidation = new AppleTokenValidationOptions
                {
                    ValidateSignature = false, // Disable for integration tests
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
                    IncludeNonce = true
                }
            };

            _mockConfigurationService
                .Setup(x => x.GetRequiredSecretValue("Apple:JwtSecret", "APPLE_JWT_SECRET"))
                .Returns("test-integration-secret-for-unit-tests-only");
        }

        [Fact]
        public async Task EndToEndFlow_ShouldWork_WithWebFlow()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act 1: Get authorization URL
            var authUrl = await provider.GetAuthorizationUrlAsync("https://example.com/dashboard");

            // Assert 1: Authorization URL is valid
            authUrl.Should().NotBeNullOrEmpty();
            authUrl.Should().StartWith("https://appleid.apple.com/auth/authorize");
            authUrl.Should().Contain("client_id=com.example.test");
            authUrl.Should().Contain("response_mode=form_post");
            authUrl.Should().Contain("nonce="); // Should include nonce

            // Act 2: Simulate token exchange (will use mock token due to configuration)
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("mock_auth_code", "test_state");

            // Assert 2: Token response is valid
            tokenResponse.Should().NotBeNull();
            tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
            tokenResponse.IdToken.Should().NotBeNullOrEmpty();
            tokenResponse.TokenType.Should().Be("Bearer");

            // Act 3: Extract user info from token
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert 3: User info is extracted correctly
            userInfo.Should().NotBeNull();
            userInfo.UserId.Should().NotBeNullOrEmpty();
            userInfo.IsAuthenticated.Should().BeTrue();
            userInfo.AuthProvider.Should().Be("Apple");
            userInfo.Claims.Should().NotBeEmpty();
            userInfo.Claims.Should().ContainKey("sub");
            userInfo.Claims.Should().ContainKey("iss");
            userInfo.Claims["iss"].Should().Be("https://appleid.apple.com");
        }

        [Fact]
        public async Task EndToEndFlow_ShouldWork_WithNativeFlow()
        {
            // Arrange
            _appleConfig.Flow.DefaultFlow = AppleFlowType.Native;
            _appleConfig.Flow.NativeApp = new AppleNativeAppOptions
            {
                BundleId = "com.example.test",
                CustomUrlScheme = "exampletest",
                SupportUniversalLinks = true
            };
            _appleConfig.RedirectUri = string.Empty; // Use flow-specific logic

            var provider = CreateAppleAuthProvider();

            // Act: Get authorization URL for native flow
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert: Native flow specific parameters
            authUrl.Should().Contain("redirect_uri=exampletest%3A%2F%2Fauth%2Fapple-callback");
            authUrl.Should().Contain("app_id=com.example.test");
        }

        [Fact]
        public async Task PrivateEmailRelay_ShouldBeHandled_Correctly()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();
            
            // Create a token with private email relay
            var tokenResponse = new TokenResponse
            {
                AccessToken = "test_access_token",
                IdToken = CreatePrivateEmailToken(),
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Email.Should().Contain("@privaterelay.appleid.com");
            userInfo.DisplayName.Should().Contain("(Private)");
            userInfo.Claims.Should().ContainKey("email");
            userInfo.Claims["email"].Should().Contain("privaterelay.appleid.com");
        }

        [Fact]
        public async Task PrivateEmailRelay_ShouldNotStore_WhenDisabled()
        {
            // Arrange
            _appleConfig.PrivateEmail.StorePrivateRelayEmails = false;
            var provider = CreateAppleAuthProvider();
            
            var tokenResponse = new TokenResponse
            {
                AccessToken = "test_access_token",
                IdToken = CreatePrivateEmailToken(),
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Email.Should().BeEmpty(); // Email not stored
            userInfo.DisplayName.Should().Contain("(Private)"); // But still marked as private
        }

        [Fact]
        public async Task ConfigurationValidation_ShouldPass_WithValidConfig()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ConfigurationValidation_ShouldFail_WithInvalidConfig()
        {
            // Arrange
            _appleConfig.ClientId = string.Empty; // Invalid
            var provider = CreateAppleAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task TokenValidation_ShouldRejectExpiredTokens()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();
            var expiredToken = CreateExpiredToken();
            var tokenResponse = new TokenResponse { IdToken = expiredToken };

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task TokenValidation_ShouldRejectInvalidAudience()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();
            var invalidToken = CreateTokenWithInvalidAudience();
            var tokenResponse = new TokenResponse { IdToken = invalidToken };

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task TokenValidation_ShouldRejectInvalidIssuer()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();
            var invalidToken = CreateTokenWithInvalidIssuer();
            var tokenResponse = new TokenResponse { IdToken = invalidToken };

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.IdentityModel.Tokens.SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task FlowConfiguration_ShouldAffectAuthorizationUrl()
        {
            // Arrange - Test different flow configurations
            var testConfigs = new[]
            {
                new { Flow = AppleFlowType.Web, ResponseMode = "form_post", ExpectedPattern = "response_mode=form_post" },
                new { Flow = AppleFlowType.Web, ResponseMode = "fragment", ExpectedPattern = "response_mode=fragment" },
                new { Flow = AppleFlowType.Hybrid, ResponseMode = "form_post", ExpectedPattern = "display=popup" }
            };

            foreach (var config in testConfigs)
            {
                // Arrange
                _appleConfig.Flow.DefaultFlow = config.Flow;
                _appleConfig.Flow.ResponseMode = config.ResponseMode;
                var provider = CreateAppleAuthProvider();

                // Act
                var authUrl = await provider.GetAuthorizationUrlAsync();

                // Assert
                authUrl.Should().Contain(config.ExpectedPattern, 
                    $"Flow {config.Flow} with response mode {config.ResponseMode} should include {config.ExpectedPattern}");
            }
        }

        [Fact]
        public async Task ErrorHandling_ShouldProvideMeaningfulMessages()
        {
            // Arrange
            var provider = CreateAppleAuthProvider();

            // Test various error scenarios
            var errorTests = new[]
            {
                new { Code = (string?)null, ExpectedExceptionType = typeof(ArgumentException) },
                new { Code = "", ExpectedExceptionType = typeof(ArgumentException) },
                new { Code = "   ", ExpectedExceptionType = typeof(ArgumentException) }
            };

            foreach (var test in errorTests)
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync(test.ExpectedExceptionType,
                    () => provider.ExchangeCodeForTokenAsync(test.Code!, "state"));

                exception.Message.Should().NotBeNullOrEmpty();
                exception.Message.ToLower().Should().Contain("code");
            }
        }

        #region Helper Methods

        private AppleAuthProvider CreateAppleAuthProvider()
        {
            var mockOptions = new Mock<IOptions<AppleOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_appleConfig);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            return new AppleAuthProvider(
                mockOptions.Object,
                mockHttpClientFactory.Object,
                _mockLogger.Object,
                _mockConfigurationService.Object);
        }

        private string CreatePrivateEmailToken()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "abc123@privaterelay.appleid.com",
                ["email_verified"] = "true",
                ["aud"] = _appleConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateExpiredToken()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = _appleConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds() // Expired
            });
        }

        private string CreateTokenWithInvalidAudience()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = "wrong.client.id", // Invalid audience
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithInvalidIssuer()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = _appleConfig.ClientId,
                ["iss"] = "https://evil.com", // Invalid issuer
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private static string CreateTestToken(Dictionary<string, object> claims)
        {
            const string TEST_SECRET = "test-integration-secret-for-unit-tests-only";
            
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var claimList = claims.Select(kv => 
                new System.Security.Claims.Claim(kv.Key, kv.Value.ToString()!)).ToArray();

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claimList),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TEST_SECRET)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}