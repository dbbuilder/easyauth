using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Configuration;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Services
{
    /// <summary>
    /// Comprehensive tests for ConfigurationService following TDD methodology
    /// Tests cover the unified configuration fallback pattern: Key Vault → Environment → App Settings → Default
    /// </summary>
    public class ConfigurationServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
        private readonly KeyVaultOptions _keyVaultOptions;

        public ConfigurationServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<ConfigurationService>>();
            _keyVaultOptions = new KeyVaultOptions
            {
                BaseUrl = "https://test-keyvault.vault.azure.net/",
                SecretNames = new Dictionary<string, string>
                {
                    ["Apple:JwtSecret"] = "EAuthAppleJwtSecret",
                    ["Google:ClientSecret"] = "EAuthGoogleClientSecret",
                    ["Facebook:AppSecret"] = "EAuthFacebookAppSecret",
                    ["AzureB2C:ClientSecret"] = "EAuthAzureB2CClientSecret"
                }
            };
        }

        #region GetSecretValue Tests

        [Fact]
        public void GetSecretValue_ShouldReturnKeyVaultValue_WhenAvailable()
        {
            // Arrange
            var key = "Apple:JwtSecret";
            var expectedSecret = "secure-jwt-secret-from-keyvault";
            var keyVaultSecretName = _keyVaultOptions.SecretNames[key];

            _mockConfiguration.Setup(x => x[keyVaultSecretName]).Returns(expectedSecret);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var result = service.GetSecretValue(key);

            // Assert
            result.Should().Be(expectedSecret);
            _mockConfiguration.Verify(x => x[keyVaultSecretName], Times.Once);
        }

        [Fact]
        public void GetSecretValue_ShouldReturnEnvironmentVariable_WhenKeyVaultEmpty()
        {
            // Arrange
            var key = "Google:ClientSecret";
            var envVar = "GOOGLE_CLIENT_SECRET";
            var expectedSecret = "secure-google-secret-from-env";
            var keyVaultSecretName = _keyVaultOptions.SecretNames[key];

            // Key Vault returns null/empty
            _mockConfiguration.Setup(x => x[keyVaultSecretName]).Returns((string?)null);

            // Mock environment variable
            Environment.SetEnvironmentVariable(envVar, expectedSecret);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            try
            {
                // Act
                var result = service.GetSecretValue(key, envVar);

                // Assert
                result.Should().Be(expectedSecret);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Fact]
        public void GetSecretValue_ShouldReturnAppSettings_WhenKeyVaultAndEnvEmpty()
        {
            // Arrange
            var key = "Facebook:AppSecret";
            var envVar = "FACEBOOK_APP_SECRET";
            var expectedSecret = "secure-facebook-secret-from-config";
            var keyVaultSecretName = _keyVaultOptions.SecretNames[key];

            // Key Vault returns null/empty
            _mockConfiguration.Setup(x => x[keyVaultSecretName]).Returns((string?)null);
            // App settings returns value
            _mockConfiguration.Setup(x => x[key]).Returns(expectedSecret);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var result = service.GetSecretValue(key, envVar);

            // Assert
            result.Should().Be(expectedSecret);
            _mockConfiguration.Verify(x => x[key], Times.Once);
        }

        [Fact]
        public void GetSecretValue_ShouldReturnDefaultValue_WhenAllSourcesEmpty()
        {
            // Arrange
            var key = "Unknown:Secret";
            var envVar = "UNKNOWN_SECRET";
            var defaultValue = "fallback-default-value";

            // All sources return null/empty
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var result = service.GetSecretValue(key, envVar, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetSecretValue_ShouldReturnNull_WhenNoSourcesAndNoDefault()
        {
            // Arrange
            var key = "Unknown:Secret";

            // All sources return null/empty
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var result = service.GetSecretValue(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetSecretValue_ShouldWorkWithoutKeyVault_WhenOptionsNull()
        {
            // Arrange
            var key = "Test:Secret";
            var expectedSecret = "direct-config-value";

            _mockConfiguration.Setup(x => x[key]).Returns(expectedSecret);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            var result = service.GetSecretValue(key);

            // Assert
            result.Should().Be(expectedSecret);
        }

        #endregion

        #region GetRequiredSecretValue Tests

        [Fact]
        public void GetRequiredSecretValue_ShouldReturnValue_WhenFound()
        {
            // Arrange
            var key = "Apple:JwtSecret";
            var expectedSecret = "required-jwt-secret";
            var keyVaultSecretName = _keyVaultOptions.SecretNames[key];

            _mockConfiguration.Setup(x => x[keyVaultSecretName]).Returns(expectedSecret);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var result = service.GetRequiredSecretValue(key);

            // Assert
            result.Should().Be(expectedSecret);
        }

        [Fact]
        public void GetRequiredSecretValue_ShouldThrowException_WhenNotFound()
        {
            // Arrange
            var key = "Missing:Secret";
            var envVar = "MISSING_SECRET";

            // All sources return null/empty
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetRequiredSecretValue(key, envVar));
            exception.Message.Should().Contain("Required secret 'Missing:Secret' not found");
            exception.Message.Should().Contain("Environment Variable (MISSING_SECRET)");
            exception.Message.Should().Contain("App Settings (Missing:Secret)");
        }

        [Fact]
        public void GetRequiredSecretValue_ShouldThrowException_WhenEmpty()
        {
            // Arrange
            var key = "Empty:Secret";

            _mockConfiguration.Setup(x => x[key]).Returns("");

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetRequiredSecretValue(key));
            exception.Message.Should().Contain("Required secret 'Empty:Secret' not found");
        }

        #endregion

        #region GetConfigValue Tests

        [Fact]
        public void GetConfigValue_ShouldReturnEnvironmentVariable_First()
        {
            // Arrange
            var key = "Test:Config";
            var envVar = "TEST_CONFIG";
            var envValue = "env-config-value";
            var appSettingsValue = "app-settings-value";

            Environment.SetEnvironmentVariable(envVar, envValue);
            _mockConfiguration.Setup(x => x[key]).Returns(appSettingsValue);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            try
            {
                // Act
                var result = service.GetConfigValue(key, envVar);

                // Assert
                result.Should().Be(envValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Fact]
        public void GetConfigValue_ShouldReturnAppSettings_WhenEnvEmpty()
        {
            // Arrange
            var key = "Test:Config";
            var envVar = "TEST_CONFIG";
            var appSettingsValue = "app-settings-value";

            _mockConfiguration.Setup(x => x[key]).Returns(appSettingsValue);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            var result = service.GetConfigValue(key, envVar);

            // Assert
            result.Should().Be(appSettingsValue);
        }

        [Fact]
        public void GetConfigValue_ShouldReturnDefault_WhenAllEmpty()
        {
            // Arrange
            var key = "Test:Config";
            var defaultValue = "default-config-value";

            _mockConfiguration.Setup(x => x[key]).Returns((string?)null);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            var result = service.GetConfigValue(key, null, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        #endregion

        #region ValidateRequiredSecrets Tests

        [Fact]
        public void ValidateRequiredSecrets_ShouldReturnEmpty_WhenAllSecretsValid()
        {
            // Arrange
            var requiredSecrets = new Dictionary<string, string>
            {
                ["Apple:JwtSecret"] = "Apple JWT signing secret",
                ["Google:ClientSecret"] = "Google OAuth client secret"
            };

            _mockConfiguration.Setup(x => x[_keyVaultOptions.SecretNames["Apple:JwtSecret"]]).Returns("valid-jwt-secret");
            _mockConfiguration.Setup(x => x[_keyVaultOptions.SecretNames["Google:ClientSecret"]]).Returns("valid-google-secret");

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var errors = service.ValidateRequiredSecrets(requiredSecrets);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRequiredSecrets_ShouldReturnErrors_WhenSecretsMissing()
        {
            // Arrange
            var requiredSecrets = new Dictionary<string, string>
            {
                ["Apple:JwtSecret"] = "Apple JWT signing secret",
                ["Missing:Secret"] = "Missing secret for testing"
            };

            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);
            _mockConfiguration.Setup(x => x[_keyVaultOptions.SecretNames["Apple:JwtSecret"]]).Returns("valid-jwt-secret");

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, _keyVaultOptions);

            // Act
            var errors = service.ValidateRequiredSecrets(requiredSecrets);

            // Assert
            errors.Should().HaveCount(1);
            errors[0].Should().Contain("Missing required secret: Missing:Secret");
            errors[0].Should().Contain("Missing secret for testing");
        }

        [Fact]
        public void ValidateRequiredSecrets_ShouldDetectPlaceholderValues()
        {
            // Arrange
            var requiredSecrets = new Dictionary<string, string>
            {
                ["Test:Secret"] = "Test secret with placeholder"
            };

            _mockConfiguration.Setup(x => x["Test:Secret"]).Returns("dummy_secret_for_testing");

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            var errors = service.ValidateRequiredSecrets(requiredSecrets);

            // Assert
            errors.Should().HaveCount(1);
            errors[0].Should().Contain("Invalid secret value for Test:Secret");
            errors[0].Should().Contain("appears to be a placeholder or test value");
        }

        [Fact]
        public void ValidateRequiredSecrets_ShouldHandleExceptions()
        {
            // Arrange
            var requiredSecrets = new Dictionary<string, string>
            {
                ["Problematic:Secret"] = "Secret that causes exception"
            };

            _mockConfiguration.Setup(x => x[It.IsAny<string>()])
                .Throws(new InvalidOperationException("Configuration error"));

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            var errors = service.ValidateRequiredSecrets(requiredSecrets);

            // Assert
            errors.Should().HaveCount(1);
            errors[0].Should().Contain("Error validating secret Problematic:Secret");
            errors[0].Should().Contain("Configuration error");
        }

        #endregion

        #region Security and Edge Cases

        [Fact]
        public void Constructor_ShouldThrowArgumentNull_WhenConfigurationIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigurationService(null!, _mockLogger.Object, _keyVaultOptions));

            exception.ParamName.Should().Be("configuration");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNull_WhenLoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigurationService(_mockConfiguration.Object, null!, _keyVaultOptions));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_ShouldAcceptNullKeyVaultOptions()
        {
            // Act & Assert (should not throw)
            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);
            service.Should().NotBeNull();
        }

        [Fact]
        public void GetSecretValue_ShouldLogDebugMessages()
        {
            // Arrange
            var key = "Test:Secret";
            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            service.GetSecretValue(key);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Looking up secret value for key: {key}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void GetSecretValue_ShouldLogWarning_WhenNotFound()
        {
            // Arrange
            var key = "Missing:Secret";
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

            var service = new ConfigurationService(_mockConfiguration.Object, _mockLogger.Object, null);

            // Act
            service.GetSecretValue(key);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Secret {key} not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}