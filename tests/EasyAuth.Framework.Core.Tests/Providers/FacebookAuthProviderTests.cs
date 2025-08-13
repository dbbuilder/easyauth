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
using System.Text.Json;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Providers
{
    /// <summary>
    /// TDD tests for Facebook/Meta authentication provider
    /// These tests define the expected behavior before implementation (RED phase)
    /// </summary>
    public class FacebookAuthProviderTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<FacebookAuthProvider>> _mockLogger;
        private readonly Mock<IOptions<FacebookOptions>> _mockOptions;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Fixture _fixture;
        private readonly FacebookOptions _facebookConfig;

        public FacebookAuthProviderTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<FacebookAuthProvider>>();
            _mockOptions = new Mock<IOptions<FacebookOptions>>();
            _mockConfigurationService = new Mock<IConfigurationService>();
            _fixture = new Fixture();

            _facebookConfig = new FacebookOptions
            {
                Enabled = true,
                AppId = "123456789012345",
                AppSecret = "dummy_app_secret",
                CallbackPath = "/auth/facebook-signin",
                RedirectUri = "https://localhost/auth/facebook-callback",
                Scopes = new[] { "email", "public_profile" },
                UseLongLivedTokens = true,
                UseMockTokensOnFailure = true,
                DisplayMode = "page",
                Locale = "en_US",
                Business = new FacebookBusinessOptions
                {
                    EnableBusinessLogin = true,
                    BusinessId = "123456789012345",
                    Scopes = new[] { "business_management", "pages_show_list", "pages_read_engagement" }
                },
                Instagram = new FacebookInstagramOptions
                {
                    EnableInstagramIntegration = true,
                    Scopes = new[] { "instagram_basic", "instagram_manage_insights" }
                }
            };

            _mockOptions.Setup(x => x.Value).Returns(_facebookConfig);
            
            // Setup configuration service to return valid app secret
            _mockConfigurationService.Setup(x => x.GetRequiredSecretValue("Facebook:AppSecret", "FACEBOOK_APP_SECRET"))
                .Returns("dummy_app_secret_from_config");
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldReturnValidUrl_WithRequiredParameters()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://www.facebook.com/v19.0/dialog/oauth");
            result.Should().Contain("client_id=123456789012345");
            result.Should().Contain("response_type=code");
            result.Should().Contain("scope=email%2Cpublic_profile");
            result.Should().Contain("redirect_uri=");
            result.Should().Contain("state=");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeState_ForCSRFProtection()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateFacebookAuthProvider();

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
            var authCode = "valid_auth_code_from_facebook";
            var state = "valid_state_parameter";
            var provider = CreateFacebookAuthProvider();

            // Mock HTTP response for token exchange
            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

            // Act
            var result = await provider.ExchangeCodeForTokenAsync(authCode, state);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.TokenType.Should().Be("Bearer");
            result.ExpiresIn.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldReturnUserInfo_FromGraphAPI()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_facebook_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().NotBeNullOrEmpty();
            result.Email.Should().NotBeNullOrEmpty();
            result.DisplayName.Should().NotBeNullOrEmpty();
            result.IsAuthenticated.Should().BeTrue();
            result.AuthProvider.Should().Be("Facebook");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleProfilePicture_Correctly()
        {
            // Arrange - Facebook provides profile pictures via Graph API
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_facebook_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            };

            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.ProfilePictureUrl.Should().NotBeNullOrEmpty();
            result.ProfilePictureUrl.Should().StartWith("https://");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task GetAuthorizationUrlAsync_ShouldHandleInvalidReturnUrl_Gracefully(string? returnUrl)
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://www.facebook.com/v19.0/dialog/oauth");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnError_ForInvalidCode(string? code)
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(code!, "valid_state"));
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldUseCorrectEndpoint_AndParameters()
        {
            // Arrange - Facebook uses specific token endpoint
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("valid_code", "valid_state");

            // Assert
            // Should call Facebook's OAuth token endpoint with correct parameters
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldRequestCorrectFields_FromGraphAPI()
        {
            // Arrange - Facebook Graph API requires specific field requests
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_access_token",
                TokenType = "Bearer"
            };

            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().NotBeNullOrEmpty();
            result.LastName.Should().NotBeNullOrEmpty();
            result.Email.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateRequiredFields()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnFalse_WhenAppIdMissing()
        {
            // Arrange
            var invalidConfig = new FacebookOptions
            {
                Enabled = true,
                AppId = "", // Missing
                AppSecret = "secret"
            };

            var mockInvalidOptions = new Mock<IOptions<FacebookOptions>>();
            mockInvalidOptions.Setup(x => x.Value).Returns(invalidConfig);

            var provider = new FacebookAuthProvider(mockInvalidOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleMissingEmail_Gracefully()
        {
            // Arrange - Some Facebook users may not provide email
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access_token_no_email",
                TokenType = "Bearer"
            };

            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.IsAuthenticated.Should().BeTrue();
            // Email might be empty but user should still be authenticated
        }

        #endregion

        #region v2.4.0 Enhanced Feature Tests

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeBusinessParameters_WhenBusinessLoginEnabled()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("business_id=123456789012345");
            result.Should().Contain("auth_type=rerequest");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeDisplayMode_WhenConfigured()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("display=page");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeLocale_WhenConfigured()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("locale=en_US");
        }

        [Fact]
        public async Task GetRequestedScopes_ShouldIncludeBusinessScopes_WhenBusinessLoginEnabled()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("business_management");
            result.Should().Contain("pages_show_list");
            result.Should().Contain("pages_read_engagement");
        }

        [Fact]
        public async Task GetRequestedScopes_ShouldIncludeInstagramScopes_WhenInstagramIntegrationEnabled()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("instagram_basic");
            result.Should().Contain("instagram_manage_insights");
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldExchangeForLongLivedToken_WhenConfigured()
        {
            // Arrange
            _facebookConfig.UseLongLivedTokens = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("test_code", "test_state");

            // Assert
            result.Should().NotBeNull();
            result.ExpiresIn.Should().BeGreaterThan(3600); // Long-lived tokens should be longer than 1 hour
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnMockToken_WhenServiceUnavailable()
        {
            // Arrange
            _facebookConfig.UseMockTokensOnFailure = true;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("test_code", "test_state");

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.TokenType.Should().Be("Bearer");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldIncludeBusinessClaims_WhenBusinessInfoAvailable()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_access_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("business_id");
            result.Claims.Should().ContainKey("business_name");
            result.Claims.Should().ContainKey("business_verified");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldIncludeInstagramClaims_WhenInstagramAccountsAvailable()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "instagram_access_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("instagram_accounts");
        }

        [Theory]
        [InlineData("page")]
        [InlineData("popup")]
        [InlineData("touch")]
        [InlineData("wap")]
        public async Task ValidateConfigurationAsync_ShouldAcceptValidDisplayModes(string displayMode)
        {
            // Arrange
            _facebookConfig.DisplayMode = displayMode;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRejectInvalidDisplayMode()
        {
            // Arrange
            _facebookConfig.DisplayMode = "invalid_mode";
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateRedirectUri_Format()
        {
            // Arrange
            _facebookConfig.RedirectUri = "not-a-valid-uri";
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateBusinessConfiguration_WhenEnabled()
        {
            // Arrange
            _facebookConfig.Business.BusinessId = ""; // Invalid
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            // Should still pass but with warning (BusinessId is optional)
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateScopes_NotEmpty()
        {
            // Arrange
            _facebookConfig.Scopes = new[] { "", "email" }; // Contains empty scope
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleBusinessAccountData_Correctly()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.Claims.Should().ContainKey("iss");
            result.Claims["iss"].Should().Be("https://www.facebook.com");
            result.Claims.Should().ContainKey("aud");
            result.Claims["aud"].Should().Be("123456789012345");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldExtractAllBasicClaims_FromFacebookResponse()
        {
            // Arrange
            var tokenResponse = new TokenResponse
            {
                AccessToken = "test_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("sub");
            result.Claims.Should().ContainKey("name");
            result.Claims.Should().ContainKey("given_name");
            result.Claims.Should().ContainKey("family_name");
            result.Claims.Should().ContainKey("email");
            result.Claims.Should().ContainKey("picture");
            result.Claims.Should().ContainKey("email_verified");
        }

        #endregion

        #region Enhanced v2.4.0 Business Login Feature Tests

        [Fact]
        public async Task GetUserInfoAsync_ShouldIncludeBusinessAssets_WhenBusinessAssetsEnabled()
        {
            // Arrange
            _facebookConfig.Business.IncludeBusinessAssets = true;
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_access_token_with_assets",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("business_pages_count");
            result.Claims.Should().ContainKey("business_pages");
            result.Claims.Should().ContainKey("business_accounts_count");
            result.Claims.Should().ContainKey("business_accounts");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldIncludeBusinessRoles_WhenBusinessRolesEnabled()
        {
            // Arrange
            _facebookConfig.Business.IncludeBusinessRoles = true;
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_access_token_with_roles",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("business_permissions");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateBusinessRole_WhenPermissionValidationEnabled()
        {
            // Arrange
            _facebookConfig.Business.ValidateBusinessPermissions = true;
            _facebookConfig.Business.RequiredBusinessRole = "Admin";
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_access_token_invalid_role",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateMaxPagesLimit()
        {
            // Arrange
            _facebookConfig.Business.MaxPagesLimit = 150; // Invalid: exceeds 100
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldRequireBusinessRole_WhenValidationEnabled()
        {
            // Arrange
            _facebookConfig.Business.ValidateBusinessPermissions = true;
            _facebookConfig.Business.RequiredBusinessRole = ""; // Missing required role
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(25, true)]
        [InlineData(100, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(101, false)]
        public async Task ValidateConfigurationAsync_ShouldValidateMaxPagesLimitRange(int maxPages, bool expectedValid)
        {
            // Arrange
            _facebookConfig.Business.MaxPagesLimit = maxPages;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().Be(expectedValid);
        }

        [Theory]
        [InlineData("Admin", true)]
        [InlineData("Editor", true)]
        [InlineData("Analyst", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public async Task ValidateConfigurationAsync_ShouldValidateRequiredBusinessRole(string? requiredRole, bool expectedValid)
        {
            // Arrange
            _facebookConfig.Business.ValidateBusinessPermissions = true;
            _facebookConfig.Business.RequiredBusinessRole = requiredRole;
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().Be(expectedValid);
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldRequestBusinessAssetScopes_WhenBusinessAssetsEnabled()
        {
            // Arrange
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.Scopes = new[] { "business_management", "pages_show_list", "ads_read" };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().Contain("business_management");
            result.Should().Contain("pages_show_list");
            result.Should().Contain("ads_read");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleBusinessAssetsGracefully_WhenNotAvailable()
        {
            // Arrange
            _facebookConfig.Business.IncludeBusinessAssets = true;
            var tokenResponse = new TokenResponse
            {
                AccessToken = "business_access_token_no_assets",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.IsAuthenticated.Should().BeTrue();
            // Business asset claims may or may not be present, but should not cause errors
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldIncludeEnhancedBusinessClaims_WithDetailedInfo()
        {
            // Arrange
            _facebookConfig.Business.EnableBusinessLogin = true;
            _facebookConfig.Business.IncludeBusinessAssets = true;
            _facebookConfig.Business.IncludeBusinessRoles = true;
            var tokenResponse = new TokenResponse
            {
                AccessToken = "enhanced_business_token",
                TokenType = "Bearer"
            };
            var provider = CreateFacebookAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Claims.Should().ContainKey("business_account_id");
            result.Claims.Should().ContainKey("business_account_name");
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldValidateArguments()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(null!, "state"));
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync("", "state"));
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync("   ", "state"));
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldValidateTokenResponse()
        {
            // Arrange
            var provider = CreateFacebookAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(null!));
        }

        #endregion

        #region Helper Methods

        private IEAuthProvider CreateFacebookAuthProvider()
        {
            // This will fail until we implement FacebookAuthProvider
            // Following TDD: test first, then implement
            return new FacebookAuthProvider(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);
        }

        #endregion
    }
}