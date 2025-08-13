using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Providers
{
    /// <summary>
    /// Security-focused tests for Apple Sign-In provider
    /// Tests edge cases, security vulnerabilities, and attack scenarios
    /// </summary>
    public class AppleAuthProviderSecurityTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<AppleAuthProvider>> _mockLogger;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly AppleOptions _secureConfig;

        public AppleAuthProviderSecurityTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<AppleAuthProvider>>();
            _mockConfigurationService = new Mock<IConfigurationService>();

            _secureConfig = new AppleOptions
            {
                Enabled = true,
                ClientId = "com.secure.test",
                TeamId = "SEC123456",
                KeyId = "SEC123456",
                JwtSecret = "secure-test-secret-for-security-tests-only",
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
                .Returns("secure-test-secret-for-security-tests-only");
        }

        #region JWT Security Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectTokenWithoutSignature()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var unsignedToken = CreateUnsignedToken();
            var tokenResponse = new TokenResponse { IdToken = unsignedToken };

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectTokenWithMaliciousIssuer()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var maliciousToken = CreateTokenWithMaliciousIssuer();
            var tokenResponse = new TokenResponse { IdToken = maliciousToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
            
            exception.Message.Should().Contain("Invalid issuer");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectTokenWithWrongAudience()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var wrongAudienceToken = CreateTokenWithWrongAudience();
            var tokenResponse = new TokenResponse { IdToken = wrongAudienceToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SecurityTokenValidationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
            
            exception.Message.Should().Contain("Invalid audience");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectExpiredToken()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var expiredToken = CreateExpiredToken();
            var tokenResponse = new TokenResponse { IdToken = expiredToken };

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenExpiredException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectFutureToken()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var futureToken = CreateFutureToken();
            var tokenResponse = new TokenResponse { IdToken = futureToken };

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenNotYetValidException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        #endregion

        #region Input Validation Security Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n\r")]
        public async Task ExchangeCodeForTokenAsync_ShouldRejectInvalidCodes(string? invalidCode)
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(invalidCode!, "valid_state"));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectNullTokenResponse()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(null!));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectEmptyIdToken()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var tokenResponse = new TokenResponse { IdToken = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectMalformedJWT()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var tokenResponse = new TokenResponse { IdToken = "not.a.jwt" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        #endregion

        #region Private Email Security Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandlePrivateEmailInjection()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var maliciousEmailToken = CreateTokenWithMaliciousEmail();
            var tokenResponse = new TokenResponse { IdToken = maliciousEmailToken };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert - Should not execute any injected content
            userInfo.Email.Should().NotContain("<script");
            userInfo.Email.Should().NotContain("javascript:");
            userInfo.DisplayName.Should().NotContain("<script");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldSanitizePrivateEmailDisplayName()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var maliciousToken = CreateTokenWithMaliciousPrivateEmail();
            var tokenResponse = new TokenResponse { IdToken = maliciousToken };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.DisplayName.Should().NotContain("<script");
            userInfo.DisplayName.Should().NotContain("javascript:");
            userInfo.DisplayName.Should().Contain("(Private)");
        }

        #endregion

        #region Claims Security Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateRequiredClaims()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var tokenWithoutSub = CreateTokenWithoutRequiredClaims();
            var tokenResponse = new TokenResponse { IdToken = tokenWithoutSub };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldSanitizeClaimValues()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();
            var maliciousClaimsToken = CreateTokenWithMaliciousClaims();
            var tokenResponse = new TokenResponse { IdToken = maliciousClaimsToken };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert - Claims should be safely extracted without executing malicious content
            userInfo.Claims.Should().NotBeEmpty();
            foreach (var claim in userInfo.Claims)
            {
                claim.Value.Should().NotContain("<script", 
                    $"Claim {claim.Key} contains potentially malicious content");
                claim.Value.Should().NotContain("javascript:", 
                    $"Claim {claim.Key} contains potentially malicious content");
            }
        }

        #endregion

        #region Configuration Security Tests

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectWeakClockSkew()
        {
            // Arrange
            _secureConfig.TokenValidation.ClockSkewSeconds = 3601; // Too large
            var provider = CreateSecureAppleAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectNegativeClockSkew()
        {
            // Arrange
            _secureConfig.TokenValidation.ClockSkewSeconds = -1; // Invalid
            var provider = CreateSecureAppleAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        #endregion

        #region State Parameter Security Tests

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldGenerateSecureState()
        {
            // Arrange
            var provider = CreateSecureAppleAuthProvider();

            // Act
            var authUrl1 = await provider.GetAuthorizationUrlAsync("https://example.com");
            var authUrl2 = await provider.GetAuthorizationUrlAsync("https://example.com");

            // Assert - State should be different each time (non-predictable)
            var state1 = ExtractStateFromUrl(authUrl1);
            var state2 = ExtractStateFromUrl(authUrl2);
            
            state1.Should().NotBeNullOrEmpty();
            state2.Should().NotBeNullOrEmpty();
            state1.Should().NotBe(state2);
            state1.Length.Should().BeGreaterThan(10); // Meaningful state
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeSecureNonce()
        {
            // Arrange
            _secureConfig.Flow.IncludeNonce = true;
            var provider = CreateSecureAppleAuthProvider();

            // Act
            var authUrl1 = await provider.GetAuthorizationUrlAsync();
            var authUrl2 = await provider.GetAuthorizationUrlAsync();

            // Assert - Nonce should be different each time
            var nonce1 = ExtractNonceFromUrl(authUrl1);
            var nonce2 = ExtractNonceFromUrl(authUrl2);
            
            nonce1.Should().NotBeNullOrEmpty();
            nonce2.Should().NotBeNullOrEmpty();
            nonce1.Should().NotBe(nonce2);
        }

        #endregion

        #region Helper Methods

        private AppleAuthProvider CreateSecureAppleAuthProvider()
        {
            var mockOptions = new Mock<IOptions<AppleOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_secureConfig);

            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

            return new AppleAuthProvider(
                mockOptions.Object,
                _mockHttpClientFactory.Object,
                _mockLogger.Object,
                _mockConfigurationService.Object);
        }

        private string CreateUnsignedToken()
        {
            // Create an unsigned JWT (security vulnerability)
            var payload = new
            {
                sub = "001234.567890abcdef.1234",
                email = "user@example.com",
                aud = _secureConfig.ClientId,
                iss = "https://appleid.apple.com",
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            };

            var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson));
            
            // Return unsigned token (header.payload.signature where signature is empty)
            return $"eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.{payloadBase64}.";
        }

        private string CreateTokenWithMaliciousIssuer()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://evil-attacker.com", // Malicious issuer
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithWrongAudience()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = "com.malicious.app", // Wrong audience
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
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds() // Expired
            });
        }

        private string CreateFutureToken()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(), // Future issued time
                ["exp"] = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithMaliciousEmail()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "<script>alert('XSS')</script>@example.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithMaliciousPrivateEmail()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "<script>alert('XSS')</script>@privaterelay.appleid.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithoutRequiredClaims()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                // Missing required 'sub' claim
                ["email"] = "user@example.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            });
        }

        private string CreateTokenWithMaliciousClaims()
        {
            return CreateTestToken(new Dictionary<string, object>
            {
                ["sub"] = "001234.567890abcdef.1234",
                ["email"] = "user@example.com",
                ["aud"] = _secureConfig.ClientId,
                ["iss"] = "https://appleid.apple.com",
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                ["malicious_claim"] = "<script>alert('XSS')</script>",
                ["javascript_claim"] = "javascript:alert('XSS')"
            });
        }

        private static string CreateTestToken(Dictionary<string, object> claims)
        {
            const string TEST_SECRET = "secure-test-secret-for-security-tests-only";
            
            var handler = new JwtSecurityTokenHandler();
            var claimList = claims.Select(kv => 
                new Claim(kv.Key, kv.Value.ToString()!)).ToArray();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimList),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TEST_SECRET)),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private static string ExtractStateFromUrl(string authUrl)
        {
            var uri = new Uri(authUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["state"] ?? string.Empty;
        }

        private static string ExtractNonceFromUrl(string authUrl)
        {
            var uri = new Uri(authUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["nonce"] ?? string.Empty;
        }

        #endregion
    }
}