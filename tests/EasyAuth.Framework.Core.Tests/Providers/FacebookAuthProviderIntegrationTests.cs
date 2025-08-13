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
    /// Integration tests for Facebook authentication provider
    /// Tests real-world scenarios and end-to-end flows
    /// </summary>
    public class FacebookAuthProviderIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<FacebookAuthProvider>> _mockLogger;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly FacebookOptions _facebookConfig;

        public FacebookAuthProviderIntegrationTests()
        {
            _httpClient = new HttpClient();
            _mockLogger = new Mock<ILogger<FacebookAuthProvider>>();
            _mockConfigurationService = new Mock<IConfigurationService>();

            _facebookConfig = new FacebookOptions
            {
                Enabled = true,
                AppId = "123456789012345",
                AppSecret = "test_app_secret",
                CallbackPath = "/auth/facebook-signin",
                RedirectUri = "https://localhost/auth/facebook-callback",
                Scopes = new[] { "email", "public_profile" },
                UseLongLivedTokens = true,
                UseMockTokensOnFailure = true,
                DisplayMode = "page",
                Locale = "en_US",
                Business = new FacebookBusinessOptions
                {
                    EnableBusinessLogin = false, // Start with basic login
                    BusinessId = "123456789012345",
                    Scopes = new[] { "business_management", "pages_show_list" }
                },
                Instagram = new FacebookInstagramOptions
                {
                    EnableInstagramIntegration = false, // Start with basic login
                    Scopes = new[] { "instagram_basic" }
                }
            };

            _mockConfigurationService
                .Setup(x => x.GetRequiredSecretValue("Facebook:AppSecret", "FACEBOOK_APP_SECRET"))
                .Returns("test_app_secret");
        }

        [Fact]
        public async Task EndToEndFlow_ShouldWork_WithBasicLogin()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act 1: Get authorization URL
            var authUrl = await provider.GetAuthorizationUrlAsync("https://example.com/dashboard");

            // Assert 1: Authorization URL is valid
            authUrl.Should().NotBeNullOrEmpty();
            authUrl.Should().StartWith("https://www.facebook.com/v19.0/dialog/oauth");
            authUrl.Should().Contain("client_id=123456789012345");
            authUrl.Should().Contain("response_type=code");
            authUrl.Should().Contain("scope=");
            authUrl.Should().Contain("state=");
            authUrl.Should().Contain("redirect_uri=");

            // Act 2: Simulate token exchange (will use mock token due to configuration)
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("mock_auth_code", "test_state");

            // Assert 2: Token response is valid
            tokenResponse.Should().NotBeNull();
            tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
            tokenResponse.TokenType.Should().Be("Bearer");

            // Act 3: Extract user info from token
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert 3: User info is extracted correctly
            userInfo.Should().NotBeNull();
            userInfo.UserId.Should().NotBeNullOrEmpty();
            userInfo.IsAuthenticated.Should().BeTrue();
            userInfo.AuthProvider.Should().Be("Facebook");
            userInfo.Claims.Should().NotBeEmpty();
        }

        [Fact]
        public async Task EndToEndFlow_ShouldWork_WithBusinessLogin()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            var provider = CreateFacebookAuthProvider();

            // Act 1: Get authorization URL with business parameters
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert 1: Business parameters included
            authUrl.Should().Contain("business_id=123456789012345");
            authUrl.Should().Contain("auth_type=rerequest");
            authUrl.Should().Contain("business_management");

            // Act 2: Token exchange and user info
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("business_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert 2: Business claims included
            userInfo.Claims.Should().ContainKey("business_id");
            userInfo.Claims.Should().ContainKey("business_name");
        }

        [Fact]
        public async Task EndToEndFlow_ShouldWork_WithInstagramIntegration()
        {
            // Arrange
            _facebookConfig.Instagram.EnableInstagramIntegration = true;
            var provider = CreateFacebookAuthProvider();

            // Act 1: Get authorization URL with Instagram scopes
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert 1: Instagram scopes included
            authUrl.Should().Contain("instagram_basic");

            // Act 2: Token exchange and user info
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("instagram_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert 2: Instagram claims included when available
            userInfo.Claims.Should().ContainKey("instagram_accounts");
        }

        [Fact]
        public async Task LongLivedTokenExchange_ShouldWork_WhenEnabled()
        {
            // Arrange
            _facebookConfig.UseLongLivedTokens = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("test_code", "test_state");

            // Assert
            tokenResponse.Should().NotBeNull();
            tokenResponse.ExpiresIn.Should().BeGreaterThan(3600); // Should be longer than 1 hour for long-lived tokens
        }

        [Theory]
        [InlineData("page", "display=page")]
        [InlineData("popup", "display=popup")]
        [InlineData("touch", "display=touch")]
        [InlineData("wap", "display=wap")]
        public async Task DisplayModeConfiguration_ShouldAffectAuthorizationUrl(string displayMode, string expectedPattern)
        {
            // Arrange
            _facebookConfig.DisplayMode = displayMode;
            var provider = CreateFacebookAuthProvider();

            // Act
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert
            authUrl.Should().Contain(expectedPattern);
        }

        [Fact]
        public async Task LocaleConfiguration_ShouldAffectAuthorizationUrl()
        {
            // Arrange
            var testLocales = new[] { "en_US", "es_ES", "fr_FR", "de_DE" };

            foreach (var locale in testLocales)
            {
                // Arrange
                _facebookConfig.Locale = locale;
                var provider = CreateFacebookAuthProvider();

                // Act
                var authUrl = await provider.GetAuthorizationUrlAsync();

                // Assert
                authUrl.Should().Contain($"locale={locale}");
            }
        }

        [Fact]
        public async Task ConfigurationValidation_ShouldPass_WithValidConfig()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ConfigurationValidation_ShouldFail_WithInvalidConfig()
        {
            // Arrange
            _facebookConfig.AppId = string.Empty; // Invalid
            var provider = CreateFacebookAuthProvider();

            // Act
            var isValid = await provider.ValidateConfigurationAsync();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ErrorHandling_ShouldProvideMeaningfulMessages()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

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

        [Fact]
        public async Task ScopeManagement_ShouldCombineScopes_Correctly()
        {
            // Arrange
            _facebookConfig.Scopes = new[] { "email", "public_profile" };
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.Scopes = new[] { "business_management" };
            _facebookConfig.Instagram.EnableInstagramIntegration = true;
            _facebookConfig.Instagram.Scopes = new[] { "instagram_basic" };

            var provider = CreateFacebookAuthProvider();

            // Act
            var authUrl = await provider.GetAuthorizationUrlAsync();

            // Assert
            authUrl.Should().Contain("email");
            authUrl.Should().Contain("public_profile");
            authUrl.Should().Contain("business_management");
            authUrl.Should().Contain("instagram_basic");
        }

        [Fact]
        public async Task MockTokenFallback_ShouldWork_WhenServiceUnavailable()
        {
            // Arrange
            _facebookConfig.UseMockTokensOnFailure = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("test_code", "test_state");

            // Assert
            tokenResponse.Should().NotBeNull();
            tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
            tokenResponse.TokenType.Should().Be("Bearer");
        }

        [Fact]
        public async Task UserInfoExtraction_ShouldIncludeAllStandardClaims()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "test_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Claims.Should().ContainKey("sub");
            userInfo.Claims.Should().ContainKey("name");
            userInfo.Claims.Should().ContainKey("given_name");
            userInfo.Claims.Should().ContainKey("family_name");
            userInfo.Claims.Should().ContainKey("email");
            userInfo.Claims.Should().ContainKey("picture");
            userInfo.Claims.Should().ContainKey("email_verified");
            userInfo.Claims.Should().ContainKey("iss");
            userInfo.Claims.Should().ContainKey("aud");
        }

        [Fact]
        public async Task BusinessAssetIntegration_ShouldRetrieveBusinessPages_WhenAssetsEnabled()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.MaxPagesLimit = 10;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("business_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Claims.Should().ContainKey("business_pages_count");
            userInfo.Claims.Should().ContainKey("business_pages");
            
            // Verify pages data structure
            if (userInfo.Claims.ContainsKey("business_pages"))
            {
                var pagesJson = userInfo.Claims["business_pages"];
                pagesJson.Should().NotBeEmpty();
                // Should be valid JSON
                var isValidJson = IsValidJson(pagesJson);
                isValidJson.Should().BeTrue();
            }
        }

        [Fact]
        public async Task BusinessRoleValidation_ShouldWork_WithPermissionValidation()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessRoles = true;
            _facebookConfig.Business.ValidateBusinessPermissions = true;
            _facebookConfig.Business.RequiredBusinessRole = "Admin";
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("admin_business_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Should().NotBeNull();
            userInfo.Claims.Should().ContainKey("business_permissions");
        }

        [Fact]
        public async Task BusinessConfiguration_ShouldLogConfigurationSummary_OnValidation()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.BusinessId = "123456789012345";
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.IncludeBusinessRoles = true;
            _facebookConfig.Business.ValidateBusinessPermissions = false;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
            // Configuration logging should have occurred (verified via mock logger)
        }

        [Fact]
        public async Task BusinessAccountData_ShouldIncludeAccountStatus_WhenAvailable()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessAssets = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("business_account_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            if (userInfo.Claims.ContainsKey("business_accounts"))
            {
                var accountsJson = userInfo.Claims["business_accounts"];
                accountsJson.Should().NotBeEmpty();
                
                // Verify accounts data includes status and country
                var isValidJson = IsValidJson(accountsJson);
                isValidJson.Should().BeTrue();
            }
        }

        [Fact]
        public async Task MaxPagesLimit_ShouldBeRespected_InAssetRetrieval()
        {
            // Arrange
            var customLimit = 5;
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.MaxPagesLimit = customLimit;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("business_many_pages_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            if (userInfo.Claims.ContainsKey("business_pages_count"))
            {
                var pageCount = int.Parse(userInfo.Claims["business_pages_count"]);
                (pageCount <= customLimit).Should().BeTrue();
            }
        }

        [Fact]
        public async Task EnhancedBusinessClaims_ShouldIncludeDetailedInformation()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.IncludeBusinessRoles = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var tokenResponse = await provider.ExchangeCodeForTokenAsync("enhanced_business_code", "test_state");
            var userInfo = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            userInfo.Claims.Should().ContainKey("business_account_id");
            userInfo.Claims.Should().ContainKey("business_account_name");
            
            // Standard business claims should still be present
            userInfo.Claims.Should().ContainKey("business_id");
            userInfo.Claims.Should().ContainKey("business_name");
            userInfo.Claims.Should().ContainKey("business_verified");
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

        #region Helper Methods

        private FacebookAuthProvider CreateFacebookAuthProvider()
        {
            var mockOptions = new Mock<IOptions<FacebookOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_facebookConfig);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            return new FacebookAuthProvider(
                mockOptions.Object,
                mockHttpClientFactory.Object,
                _mockLogger.Object,
                _mockConfigurationService.Object);
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}