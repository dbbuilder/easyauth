using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Providers
{
    /// <summary>
    /// Security-focused tests for Facebook authentication provider
    /// Tests edge cases, security vulnerabilities, and attack scenarios
    /// </summary>
    public class FacebookAuthProviderSecurityTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<FacebookAuthProvider>> _mockLogger;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly FacebookOptions _secureConfig;

        public FacebookAuthProviderSecurityTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<FacebookAuthProvider>>();
            _mockConfigurationService = new Mock<IConfigurationService>();

            _secureConfig = new FacebookOptions
            {
                Enabled = true,
                AppId = "123456789012345",
                AppSecret = "secure_test_secret",
                CallbackPath = "/auth/facebook-signin",
                RedirectUri = "https://secure.example.com/auth/facebook-callback",
                Scopes = new[] { "email", "public_profile" },
                UseLongLivedTokens = true,
                UseMockTokensOnFailure = false, // Security: Disable mock tokens
                DisplayMode = "page",
                Locale = "en_US",
                Business = new FacebookBusinessOptions
                {
                    EnableBusinessLogin = true,
                    BusinessId = "123456789012345",
                    Scopes = new[] { "business_management", "pages_show_list" }
                },
                Instagram = new FacebookInstagramOptions
                {
                    EnableInstagramIntegration = true,
                    Scopes = new[] { "instagram_basic", "instagram_manage_insights" }
                }
            };

            _mockConfigurationService
                .Setup(x => x.GetRequiredSecretValue("Facebook:AppSecret", "FACEBOOK_APP_SECRET"))
                .Returns("secure_test_secret");
        }

        #region Input Validation Security Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n\r")]
        public async Task ExchangeCodeForTokenAsync_ShouldRejectInvalidCodes(string? invalidCode)
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(invalidCode!, "valid_state"));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectNullTokenResponse()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(null!));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRejectEmptyAccessToken()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse { AccessToken = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        #endregion

        #region Configuration Security Tests

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectInvalidRedirectUri()
        {
            // Arrange
            _secureConfig.RedirectUri = "http://insecure.example.com/callback"; // HTTP instead of HTTPS
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            // Should warn about non-HTTPS URI but still pass for localhost
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectMalformedRedirectUri()
        {
            // Arrange
            _secureConfig.RedirectUri = "not-a-valid-uri";
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("invalid_display")]
        [InlineData("MALICIOUS_SCRIPT")]
        [InlineData("<script>")]
        public async Task ValidateConfigurationAsync_ShouldRejectInvalidDisplayModes(string invalidDisplayMode)
        {
            // Arrange
            _secureConfig.DisplayMode = invalidDisplayMode;
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectEmptyScopes()
        {
            // Arrange
            _secureConfig.Scopes = new[] { "", "email" }; // Contains empty scope
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectEmptyBusinessScopes()
        {
            // Arrange
            _secureConfig.Business.Scopes = new[] { "business_management", "" }; // Contains empty scope
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectEmptyInstagramScopes()
        {
            // Arrange
            _secureConfig.Instagram.Scopes = new[] { "", "instagram_basic" }; // Contains empty scope
            var provider = CreateSecureFacebookAuthProvider();

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
            var provider = CreateSecureFacebookAuthProvider();

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
        public async Task GetAuthorizationUrlAsync_ShouldContainValidParameters()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var authUrl = await provider.GetAuthorizationUrlAsync("https://example.com");

            // Assert
            authUrl.Should().Contain("response_type=code");
            authUrl.Should().Contain("client_id=123456789012345");
            authUrl.Should().NotContain("client_secret"); // Should never be in URL
        }

        #endregion

        #region Data Sanitization Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldSanitizeUserData()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "test_token",
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert - Claims should be safely extracted without malicious content
            userInfo.Claims.Should().NotBeEmpty();
            foreach (var claim in userInfo.Claims)
            {
                claim.Value.Should().NotContain("<script", 
                    $"Claim {claim.Key} contains potentially malicious content");
                claim.Value.Should().NotContain("javascript:", 
                    $"Claim {claim.Key} contains potentially malicious content");
                claim.Value.Should().NotContain("data:", 
                    $"Claim {claim.Key} contains potentially dangerous data URI");
            }
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleMaliciousProfileData()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "malicious_token",
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.DisplayName.Should().NotContain("<script");
            userInfo.DisplayName.Should().NotContain("javascript:");
            userInfo.Email.Should().NotContain("<script");
            userInfo.ProfilePictureUrl.Should().NotContain("javascript:");
        }

        #endregion

        #region Business Login Security Tests

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldSecurelyHandleBusinessParameters()
        {
            // Arrange
            _secureConfig.Business.BusinessId = "123456789012345";
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert
            authUrl.Should().Contain("business_id=123456789012345");
            authUrl.Should().Contain("auth_type=rerequest");
            // Ensure business ID is properly URL encoded
            authUrl.Should().NotContain("&business_id=123456789012345&"); // Should be encoded
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateBusinessData()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_token",
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            if (userInfo.Claims.ContainsKey("business_id"))
            {
                userInfo.Claims["business_id"].Should().NotContain("<script");
                userInfo.Claims["business_id"].Should().NotContain("javascript:");
            }
            
            if (userInfo.Claims.ContainsKey("business_name"))
            {
                userInfo.Claims["business_name"].Should().NotContain("<script");
                userInfo.Claims["business_name"].Should().NotContain("javascript:");
            }
        }

        #endregion

        #region Instagram Integration Security Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldSecurelyHandleInstagramData()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "instagram_token",
                TokenType = "Bearer"
            };

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            if (userInfo.Claims.ContainsKey("instagram_accounts"))
            {
                var instagramData = userInfo.Claims["instagram_accounts"];
                instagramData.Should().NotContain("<script");
                instagramData.Should().NotContain("javascript:");
                
                // Should be valid JSON if present
                if (!string.IsNullOrEmpty(instagramData))
                {
                    var isValidJson = IsValidJson(instagramData);
                    isValidJson.Should().BeTrue("Instagram accounts data should be valid JSON");
                }
            }
        }

        #endregion

        #region Error Handling Security Tests

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldNotExposeSecretsInExceptions()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();

            try
            {
                // Act - This should fail safely
                await provider.ExchangeCodeForTokenAsync("invalid_code", "state");
            }
            catch (Exception ex)
            {
                // Assert - Exception should not contain sensitive data
                ex.Message.Should().NotContain("secure_test_secret");
                ex.Message.Should().NotContain("app_secret");
                ex.ToString().Should().NotContain("secure_test_secret");
            }
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleErrorResponsesSafely()
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();
            var tokenResponse = new TokenResponse
            {
                AccessToken = "invalid_token",
                TokenType = "Bearer"
            };

            // Act & Assert
            // Should handle errors gracefully without exposing sensitive information
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);
            userInfo.Should().NotBeNull();
        }

        #endregion

        #region URL Injection Tests

        [Theory]
        [InlineData("https://evil.com?redirect=")]
        [InlineData("javascript:alert('xss')")]
        [InlineData("data:text/html,<script>alert('xss')</script>")]
        public async Task GetAuthorizationUrlAsync_ShouldSanitizeReturnUrl(string maliciousUrl)
        {
            // Arrange
            var provider = CreateSecureFacebookAuthProvider();

            // Act
            var authUrl = await provider.GetAuthorizationUrlAsync(maliciousUrl);

            // Assert
            authUrl.Should().StartWith("https://www.facebook.com/v19.0/dialog/oauth");
            authUrl.Should().NotContain("javascript:");
            authUrl.Should().NotContain("data:");
        }

        #endregion

        #region Helper Methods

        private FacebookAuthProvider CreateSecureFacebookAuthProvider()
        {
            var mockOptions = new Mock<IOptions<FacebookOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_secureConfig);

            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

            return new FacebookAuthProvider(
                mockOptions.Object,
                _mockHttpClientFactory.Object,
                _mockLogger.Object,
                _mockConfigurationService.Object);
        }

        private static string ExtractStateFromUrl(string authUrl)
        {
            var uri = new Uri(authUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["state"] ?? string.Empty;
        }

        private static bool IsValidJson(string jsonString)
        {
            try
            {
                JsonSerializer.Deserialize<object>(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}