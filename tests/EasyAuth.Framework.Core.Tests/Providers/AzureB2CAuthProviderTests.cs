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
    /// TDD tests for Azure B2C authentication provider
    /// These tests define the expected behavior before implementation (RED phase)
    /// </summary>
    public class AzureB2CAuthProviderTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<AzureB2CAuthProvider>> _mockLogger;
        private readonly Mock<IOptions<AzureB2COptions>> _mockOptions;
        private readonly Mock<IConfigurationService> _mockConfigurationService;
        private readonly Fixture _fixture;
        private readonly AzureB2COptions _azureB2CConfig;

        public AzureB2CAuthProviderTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<AzureB2CAuthProvider>>();
            _mockOptions = new Mock<IOptions<AzureB2COptions>>();
            _mockConfigurationService = new Mock<IConfigurationService>();
            _fixture = new Fixture();

            _azureB2CConfig = new AzureB2COptions
            {
                Enabled = true,
                Instance = "https://contoso.b2clogin.com",
                Domain = "contoso.onmicrosoft.com",
                TenantId = "contoso.onmicrosoft.com",
                ClientId = "12345678-1234-1234-1234-123456789012",
                ClientSecret = "dummy_client_secret",
                CallbackPath = "/auth/azureb2c-signin",
                SignUpSignInPolicyId = "B2C_1_SignInUp",
                ResetPasswordPolicyId = "B2C_1_PasswordReset",
                EditProfilePolicyId = "B2C_1_EditProfile",
                Scopes = new[] { "openid", "profile", "email" }
            };

            _mockOptions.Setup(x => x.Value).Returns(_azureB2CConfig);
            
            // Setup configuration service to return valid client secret
            _mockConfigurationService.Setup(x => x.GetRequiredSecretValue("AzureB2C:ClientSecret", "AZUREB2C_CLIENT_SECRET"))
                .Returns("dummy_client_secret_from_config");
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldReturnValidUrl_WithB2CPolicyAndParameters()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://contoso.b2clogin.com/contoso.onmicrosoft.com/oauth2/v2.0/authorize");
            result.Should().Contain("p=B2C_1_SignInUp"); // B2C policy parameter
            result.Should().Contain("client_id=12345678-1234-1234-1234-123456789012");
            result.Should().Contain("response_type=code");
            result.Should().Contain("scope=openid%20profile%20email");
            result.Should().Contain("redirect_uri=");
            result.Should().Contain("state=");
            result.Should().Contain("nonce=");
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldIncludeNonce_ForOpenIDConnectSecurity()
        {
            // Arrange - Azure B2C requires nonce for OIDC security
            var returnUrl = "https://myapp.com/dashboard";
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync(returnUrl);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("nonce=");

            // Extract nonce parameter
            var uri = new Uri(result);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var nonce = query["nonce"];

            nonce.Should().NotBeNullOrEmpty();
            nonce!.Length.Should().BeGreaterThan(16); // Should be a secure random value
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldReturnTokens_ForValidB2CCode()
        {
            // Arrange - Azure B2C returns JWT tokens with id_token
            var authCode = "valid_b2c_auth_code";
            var state = "valid_state_parameter";
            var provider = CreateAzureB2CAuthProvider();

            // Mock HTTP response for B2C token exchange
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
            result.IdToken.Should().NotBeNullOrEmpty(); // B2C provides id_token
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldExtractClaimsFromIdToken_NotUserInfoEndpoint()
        {
            // Arrange - Azure B2C uses id_token claims instead of userinfo endpoint
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_b2c_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                IdToken = CreateMockB2CIdToken() // B2C returns user claims in id_token
            };

            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().NotBeNullOrEmpty();
            result.Email.Should().NotBeNullOrEmpty();
            result.DisplayName.Should().NotBeNullOrEmpty();
            result.IsAuthenticated.Should().BeTrue();
            result.AuthProvider.Should().Be("AzureB2C");
            result.Claims.Should().ContainKey("oid"); // Azure object ID
            result.Claims.Should().ContainKey("tenant"); // B2C tenant info
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleB2CCustomAttributes_Correctly()
        {
            // Arrange - Azure B2C supports custom user attributes
            var tokenResponse = new TokenResponse
            {
                AccessToken = "valid_access_token",
                TokenType = "Bearer",
                IdToken = CreateMockB2CIdTokenWithCustomAttributes()
            };

            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetUserInfoAsync(tokenResponse);

            // Assert
            result.Should().NotBeNull();
            result.Claims.Should().ContainKey("extension_CompanyName");
            result.Claims.Should().ContainKey("extension_Department");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task ExchangeCodeForTokenAsync_ShouldThrowException_ForInvalidCode(string? code)
        {
            // Arrange
            var provider = CreateAzureB2CAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.ExchangeCodeForTokenAsync(code!, "valid_state"));
        }

        [Fact]
        public async Task GetPasswordResetUrlAsync_ShouldReturnB2CPasswordResetUrl()
        {
            // Arrange - Azure B2C has specific password reset flow
            var email = "user@example.com";
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetPasswordResetUrlAsync(email);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result!.Should().StartWith("https://contoso.b2clogin.com/contoso.onmicrosoft.com/oauth2/v2.0/authorize");
            result.Should().Contain("p=B2C_1_PasswordReset");
            result.Should().Contain("login_hint=" + Uri.EscapeDataString(email));
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldSupportCustomPolicies_ForDifferentFlows()
        {
            // Arrange - Azure B2C supports multiple custom policies
            var provider = CreateAzureB2CAuthProvider();
            var parameters = new Dictionary<string, string>
            {
                ["p"] = "B2C_1_EditProfile" // Override default sign-in policy
            };

            // Act
            var result = await provider.GetLoginUrlAsync(null, parameters);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("p=B2C_1_EditProfile");
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldValidateB2CSpecificFields()
        {
            // Arrange
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnFalse_WhenTenantIdMissing()
        {
            // Arrange
            var invalidConfig = new AzureB2COptions
            {
                Enabled = true,
                TenantId = "", // Missing
                ClientId = "valid_client_id",
                ClientSecret = "valid_secret",
                SignUpSignInPolicyId = "B2C_1_SignInUp"
            };

            var mockInvalidOptions = new Mock<IOptions<AzureB2COptions>>();
            mockInvalidOptions.Setup(x => x.Value).Returns(invalidConfig);

            var provider = new AzureB2CAuthProvider(mockInvalidOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_ShouldReturnFalse_WhenSignInPolicyMissing()
        {
            // Arrange - B2C requires at least one policy
            var invalidConfig = new AzureB2COptions
            {
                Enabled = true,
                TenantId = "contoso.onmicrosoft.com",
                ClientId = "valid_client_id",
                ClientSecret = "valid_secret",
                SignUpSignInPolicyId = "" // Missing required policy
            };

            var mockInvalidOptions = new Mock<IOptions<AzureB2COptions>>();
            mockInvalidOptions.Setup(x => x.Value).Returns(invalidConfig);

            var provider = new AzureB2CAuthProvider(mockInvalidOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);

            // Act
            var result = await provider.ValidateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleCallbackAsync_ShouldProcessB2CCallback_AndReturnUserInfo()
        {
            // Arrange
            var authCode = "valid_b2c_code";
            var state = "valid_state";
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.HandleCallbackAsync(authCode, state);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AuthProvider.Should().Be("AzureB2C");
            result.Message.Should().Contain("Azure B2C authentication successful");
        }

        [Fact]
        public async Task GetUserInfoAsync_ShouldHandleMissingIdToken_Gracefully()
        {
            // Arrange - Handle case where id_token is missing
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access_token_only",
                TokenType = "Bearer",
                IdToken = "" // Missing id_token
            };

            var provider = CreateAzureB2CAuthProvider();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => provider.GetUserInfoAsync(tokenResponse));
        }

        [Fact]
        public async Task GetAuthorizationUrlAsync_ShouldUseCorrectB2CEndpoint_BasedOnTenant()
        {
            // Arrange - Azure B2C uses tenant-specific endpoints
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.GetAuthorizationUrlAsync();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("https://contoso.b2clogin.com/contoso.onmicrosoft.com/oauth2/v2.0/authorize");
        }

        [Fact]
        public async Task ExchangeCodeForTokenAsync_ShouldUseCorrectB2CTokenEndpoint()
        {
            // Arrange - B2C has tenant-specific token endpoints
            var provider = CreateAzureB2CAuthProvider();

            // Act
            var result = await provider.ExchangeCodeForTokenAsync("valid_code", "valid_state");

            // Assert
            // Should make request to correct B2C token endpoint
            result.Should().NotBeNull();
        }

        #endregion

        #region Helper Methods

        private IEAuthProvider CreateAzureB2CAuthProvider()
        {
            // This will fail until we implement AzureB2CAuthProvider
            // Following TDD: test first, then implement
            return new AzureB2CAuthProvider(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object, _mockConfigurationService.Object);
        }

        private string CreateMockB2CIdToken()
        {
            // Mock JWT id_token that would come from Azure B2C
            // In real implementation, this would be a proper JWT with B2C claims
            var claims = new
            {
                oid = "12345678-1234-1234-1234-123456789012", // Azure object ID
                email = "user@contoso.com",
                given_name = "John",
                family_name = "Doe",
                name = "John Doe",
                tenant = "contoso.onmicrosoft.com",
                iss = "https://contoso.b2clogin.com/12345678-1234-1234-1234-123456789012/v2.0/",
                aud = "12345678-1234-1234-1234-123456789012",
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nbf = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                sub = "12345678-1234-1234-1234-123456789012"
            };

            // Return base64 encoded mock token (simplified for testing)
            var json = JsonSerializer.Serialize(claims);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private string CreateMockB2CIdTokenWithCustomAttributes()
        {
            // Mock B2C id_token with custom attributes
            var claims = new
            {
                oid = "12345678-1234-1234-1234-123456789012",
                email = "user@contoso.com",
                given_name = "John",
                family_name = "Doe",
                name = "John Doe",
                tenant = "contoso.onmicrosoft.com",
                extension_CompanyName = "Contoso Corp",
                extension_Department = "Engineering",
                iss = "https://contoso.b2clogin.com/12345678-1234-1234-1234-123456789012/v2.0/",
                aud = "12345678-1234-1234-1234-123456789012",
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            };

            var json = JsonSerializer.Serialize(claims);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        #endregion
    }
}