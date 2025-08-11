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
        private readonly Fixture _fixture;
        private readonly FacebookOptions _facebookConfig;

        public FacebookAuthProviderTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<FacebookAuthProvider>>();
            _mockOptions = new Mock<IOptions<FacebookOptions>>();
            _fixture = new Fixture();

            _facebookConfig = new FacebookOptions
            {
                Enabled = true,
                AppId = "123456789012345",
                AppSecret = "dummy_app_secret",
                CallbackPath = "/auth/facebook-signin",
                Scopes = new[] { "email", "public_profile" }
            };

            _mockOptions.Setup(x => x.Value).Returns(_facebookConfig);
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
            result.Should().StartWith("https://www.facebook.com/v18.0/dialog/oauth");
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
            result.Should().StartWith("https://www.facebook.com/v18.0/dialog/oauth");
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
            
            var provider = new FacebookAuthProvider(mockInvalidOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object);

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

        #region Helper Methods

        private IEAuthProvider CreateFacebookAuthProvider()
        {
            // This will fail until we implement FacebookAuthProvider
            // Following TDD: test first, then implement
            return new FacebookAuthProvider(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object);
        }

        #endregion
    }
}