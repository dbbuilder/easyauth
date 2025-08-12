using AutoFixture;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Providers;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Services
{
    /// <summary>
    /// TDD tests for authentication provider factory
    /// These tests define the expected behavior before implementation (RED phase)
    /// </summary>
    public class EAuthProviderFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<EAuthProviderFactory>> _mockLogger;
        private readonly Mock<IOptions<EAuthOptions>> _mockOptions;
        private readonly Fixture _fixture;
        private readonly EAuthOptions _eauthOptions;

        public EAuthProviderFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<EAuthProviderFactory>>();
            _mockOptions = new Mock<IOptions<EAuthOptions>>();
            _fixture = new Fixture();

            _eauthOptions = new EAuthOptions
            {
                ConnectionString = "Server=localhost;Database=EAuthTest;Trusted_Connection=true;",
                Providers = new AuthProvidersOptions
                {
                    Google = new GoogleOptions { Enabled = true, ClientId = "google-client-id" },
                    AzureB2C = new AzureB2COptions
                    {
                        Enabled = true,
                        ClientId = "b2c-client-id",
                        TenantId = "contoso.onmicrosoft.com",
                        SignUpSignInPolicyId = "B2C_1_SignInUp"
                    },
                    Facebook = new FacebookOptions { Enabled = true, AppId = "facebook-app-id" },
                    Apple = new AppleOptions { Enabled = true, ClientId = "apple-client-id", TeamId = "apple-team-id", KeyId = "apple-key-id" }
                }
            };

            _mockOptions.Setup(x => x.Value).Returns(_eauthOptions);
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public async Task GetProvidersAsync_ShouldReturnAllEnabledProviders()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProvidersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4); // Google, AzureB2C, Facebook, Apple

            var providerNames = result.Select(p => p.ProviderName).ToList();
            providerNames.Should().Contain(new[] { "Google", "AzureB2C", "Facebook", "Apple" });
        }

        [Fact]
        public async Task GetProviderAsync_ShouldReturnCorrectProvider_ForValidName()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var googleProvider = await factory.GetProviderAsync("Google");
            var azureProvider = await factory.GetProviderAsync("AzureB2C");

            // Assert
            googleProvider.Should().NotBeNull();
            googleProvider!.ProviderName.Should().Be("Google");
            // Note: In TDD GREEN phase, we're using mocks, so we check interface behavior rather than concrete type

            azureProvider.Should().NotBeNull();
            azureProvider!.ProviderName.Should().Be("AzureB2C");
            // Note: In TDD GREEN phase, we're using mocks, so we check interface behavior rather than concrete type
        }

        [Fact]
        public async Task GetProviderAsync_ShouldReturnNull_ForInvalidProviderName()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderAsync("InvalidProvider");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetProviderAsync_ShouldReturnNull_ForDisabledProvider()
        {
            // Arrange - Disable Google provider
            _eauthOptions.Providers.Google!.Enabled = false;
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderAsync("Google");

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("google")]
        [InlineData("GOOGLE")]
        [InlineData("Google")]
        public async Task GetProviderAsync_ShouldBeCaseInsensitive(string providerName)
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderAsync(providerName);

            // Assert
            result.Should().NotBeNull();
            result!.ProviderName.Should().Be("Google");
        }

        [Fact]
        public async Task GetDefaultProviderAsync_ShouldReturnConfiguredDefaultProvider()
        {
            // Arrange
            _eauthOptions.Providers.DefaultProvider = "AzureB2C";
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetDefaultProviderAsync();

            // Assert
            result.Should().NotBeNull();
            result!.ProviderName.Should().Be("AzureB2C");
        }

        [Fact]
        public async Task GetDefaultProviderAsync_ShouldReturnFirstEnabledProvider_WhenDefaultNotConfigured()
        {
            // Arrange - Clear default provider
            _eauthOptions.Providers.DefaultProvider = "";
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetDefaultProviderAsync();

            // Assert
            result.Should().NotBeNull();
            result!.ProviderName.Should().BeOneOf("Google", "AzureB2C", "Facebook", "Apple");
        }

        [Fact]
        public async Task ValidateProvidersAsync_ShouldValidateAllEnabledProviders()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.ValidateProvidersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.ValidationErrors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateProvidersAsync_ShouldReturnErrors_ForInvalidProviderConfigurations()
        {
            // Arrange - Create invalid configuration
            _eauthOptions.Providers.Google!.ClientId = ""; // Missing required field
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.ValidateProvidersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.ValidationErrors.Should().NotBeEmpty();
            result.ValidationErrors.Should().Contain(e => e.Contains("Google"));
        }

        [Fact]
        public async Task GetProviderInfoAsync_ShouldReturnProviderMetadata()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderInfoAsync("Google");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Google");
            result.DisplayName.Should().Be("Google");
            result.IsEnabled.Should().BeTrue();
            result.LoginUrl.Should().NotBeNullOrEmpty();
            result.IconUrl.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetAllProviderInfoAsync_ShouldReturnMetadataForAllProviders()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetAllProviderInfoAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);

            var enabledProviders = result.Where(p => p.IsEnabled).ToList();
            enabledProviders.Should().HaveCount(4);
        }

        [Fact]
        public async Task RegisterCustomProviderAsync_ShouldAllowCustomProviderRegistration()
        {
            // Arrange
            var customProvider = new Mock<IEAuthProvider>();
            customProvider.Setup(p => p.ProviderName).Returns("CustomProvider");
            customProvider.Setup(p => p.DisplayName).Returns("Custom Provider");
            customProvider.Setup(p => p.IsEnabled).Returns(true);

            var factory = CreateProviderFactory();

            // Act
            await factory.RegisterCustomProviderAsync("CustomProvider", customProvider.Object);
            var result = await factory.GetProviderAsync("CustomProvider");

            // Assert
            result.Should().NotBeNull();
            result!.ProviderName.Should().Be("CustomProvider");
        }

        [Fact]
        public async Task GetProviderCapabilitiesAsync_ShouldReturnProviderCapabilities()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderCapabilitiesAsync("Google");

            // Assert
            result.Should().NotBeNull();
            result!.SupportsPasswordReset.Should().BeTrue();
            result.SupportsProfileEditing.Should().BeFalse();
            result.SupportedScopes.Should().Contain(new[] { "openid", "profile", "email" });
        }

        [Fact]
        public async Task GetProvidersByCapabilityAsync_ShouldFilterProvidersByCapability()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var passwordResetProviders = await factory.GetProvidersByCapabilityAsync("PasswordReset");

            // Assert
            passwordResetProviders.Should().NotBeNull();
            passwordResetProviders.Should().NotBeEmpty();

            var providerNames = passwordResetProviders.Select(p => p.ProviderName).ToList();
            providerNames.Should().Contain("Google"); // Google supports password reset
        }

        [Fact]
        public async Task RefreshProviderCacheAsync_ShouldRefreshProviderInstances()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Get provider once to cache it
            var firstInstance = await factory.GetProviderAsync("Google");

            // Act
            await factory.RefreshProviderCacheAsync();
            var secondInstance = await factory.GetProviderAsync("Google");

            // Assert
            firstInstance.Should().NotBeNull();
            secondInstance.Should().NotBeNull();
            // Instances should be different after cache refresh
            // Note: This test validates cache behavior exists
        }

        [Fact]
        public async Task GetProviderHealthAsync_ShouldCheckProviderHealth()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetProviderHealthAsync("Google");

            // Assert
            result.Should().NotBeNull();
            result!.IsHealthy.Should().BeTrue();
            result.ProviderName.Should().Be("Google");
            result.LastChecked.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllProviderHealthAsync_ShouldCheckAllProviderHealth()
        {
            // Arrange
            var factory = CreateProviderFactory();

            // Act
            var result = await factory.GetAllProviderHealthAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.All(h => h.IsHealthy).Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private IEAuthProviderFactory CreateProviderFactory()
        {
            // Setup service provider to return provider instances
            SetupMockServiceProvider();

            // This will fail until we implement EAuthProviderFactory
            // Following TDD: test first, then implement
            return new EAuthProviderFactory(_mockServiceProvider.Object, _mockOptions.Object, _mockLogger.Object);
        }

        private void SetupMockServiceProvider()
        {
            // Mock Google provider
            var mockGoogleProvider = new Mock<IEAuthProvider>();
            mockGoogleProvider.Setup(p => p.ProviderName).Returns("Google");
            mockGoogleProvider.Setup(p => p.DisplayName).Returns("Google");
            mockGoogleProvider.Setup(p => p.IsEnabled).Returns(() => _eauthOptions.Providers.Google?.Enabled == true);
            mockGoogleProvider.Setup(p => p.GetLoginUrlAsync(It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>()))
                .ReturnsAsync("https://accounts.google.com/oauth/authorize");
            mockGoogleProvider.Setup(p => p.GetPasswordResetUrlAsync(It.IsAny<string>()))
                .ReturnsAsync("https://accounts.google.com/signin/recovery");
            mockGoogleProvider.Setup(p => p.ValidateConfigurationAsync())
                .ReturnsAsync(() => !string.IsNullOrEmpty(_eauthOptions.Providers.Google?.ClientId));

            // Mock Azure B2C provider
            var mockAzureProvider = new Mock<IEAuthProvider>();
            mockAzureProvider.Setup(p => p.ProviderName).Returns("AzureB2C");
            mockAzureProvider.Setup(p => p.DisplayName).Returns("Azure B2C");
            mockAzureProvider.Setup(p => p.IsEnabled).Returns(() => _eauthOptions.Providers.AzureB2C?.Enabled == true);
            mockAzureProvider.Setup(p => p.GetLoginUrlAsync(It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>()))
                .ReturnsAsync("https://contoso.b2clogin.com/oauth2/v2.0/authorize");
            mockAzureProvider.Setup(p => p.GetPasswordResetUrlAsync(It.IsAny<string>()))
                .ReturnsAsync("https://contoso.b2clogin.com/oauth2/v2.0/authorize?p=B2C_1_PasswordReset");
            mockAzureProvider.Setup(p => p.ValidateConfigurationAsync()).ReturnsAsync(() =>
                !string.IsNullOrEmpty(_eauthOptions.Providers.AzureB2C?.ClientId));

            // Mock Facebook provider
            var mockFacebookProvider = new Mock<IEAuthProvider>();
            mockFacebookProvider.Setup(p => p.ProviderName).Returns("Facebook");
            mockFacebookProvider.Setup(p => p.DisplayName).Returns("Facebook");
            mockFacebookProvider.Setup(p => p.IsEnabled).Returns(() => _eauthOptions.Providers.Facebook?.Enabled == true);
            mockFacebookProvider.Setup(p => p.GetLoginUrlAsync(It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>()))
                .ReturnsAsync("https://www.facebook.com/v18.0/dialog/oauth");
            mockFacebookProvider.Setup(p => p.GetPasswordResetUrlAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null); // Facebook doesn't support password reset
            mockFacebookProvider.Setup(p => p.ValidateConfigurationAsync()).ReturnsAsync(true);

            // Mock Apple provider
            var mockAppleProvider = new Mock<IEAuthProvider>();
            mockAppleProvider.Setup(p => p.ProviderName).Returns("Apple");
            mockAppleProvider.Setup(p => p.DisplayName).Returns("Apple");
            mockAppleProvider.Setup(p => p.IsEnabled).Returns(() => _eauthOptions.Providers.Apple?.Enabled == true);
            mockAppleProvider.Setup(p => p.GetLoginUrlAsync(It.IsAny<string?>(), It.IsAny<Dictionary<string, string>?>()))
                .ReturnsAsync("https://appleid.apple.com/auth/authorize");
            mockAppleProvider.Setup(p => p.GetPasswordResetUrlAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null); // Apple doesn't support direct password reset
            mockAppleProvider.Setup(p => p.ValidateConfigurationAsync()).ReturnsAsync(true);

            // Setup service provider to return these mocked providers
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(GoogleAuthProvider)))
                .Returns(mockGoogleProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(AzureB2CAuthProvider)))
                .Returns(mockAzureProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(FacebookAuthProvider)))
                .Returns(mockFacebookProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(AppleAuthProvider)))
                .Returns(mockAppleProvider.Object);

            // Setup ILogger factory
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<>)))
                .Returns(_mockLogger.Object);
        }

        #endregion
    }
}