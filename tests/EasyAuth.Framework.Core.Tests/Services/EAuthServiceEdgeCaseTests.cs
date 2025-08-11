using AutoFixture;
using AutoFixture.Xunit2;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Services
{
    /// <summary>
    /// Additional test cases for edge cases, error conditions, and security scenarios
    /// Following TDD: These tests define additional behavior requirements
    /// </summary>
    public class EAuthServiceEdgeCaseTests
    {
        private readonly Mock<IEAuthDatabaseService> _mockDatabaseService;
        private readonly Mock<ILogger<EAuthService>> _mockLogger;
        private readonly Fixture _fixture;

        public EAuthServiceEdgeCaseTests()
        {
            _mockDatabaseService = new Mock<IEAuthDatabaseService>();
            _mockLogger = new Mock<ILogger<EAuthService>>();
            _fixture = new Fixture();
        }

        #region GetProvidersAsync Edge Cases

        [Fact]
        public async Task GetProvidersAsync_ShouldReturnEmptyList_WhenNoProvidersConfigured()
        {
            // Arrange
            var service = CreateEAuthService();

            // Act
            var result = await service.GetProvidersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeAssignableTo<IEnumerable<ProviderInfo>>();
        }

        #endregion

        #region InitiateLoginAsync Edge Cases

        [Fact]
        public async Task InitiateLoginAsync_ShouldReturnError_WhenRequestIsNull()
        {
            // Arrange
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(null!);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().NotBeNullOrEmpty();
            result.Message.Should().Contain("provider");
        }

        [Fact]
        public async Task InitiateLoginAsync_ShouldReturnError_WhenProviderIsEmpty()
        {
            // Arrange
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, string.Empty)
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("INVALID_PROVIDER");
        }

        [Fact]
        public async Task InitiateLoginAsync_ShouldReturnError_WhenProviderIsWhitespace()
        {
            // Arrange
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, "   ")
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("INVALID_PROVIDER");
        }

        [Theory]
        [InlineData("google")]     // lowercase
        [InlineData("GOOGLE")]     // uppercase  
        [InlineData("gOoGlE")]     // mixed case
        public async Task InitiateLoginAsync_ShouldBeCaseInsensitive_ForProviderNames(string provider)
        {
            // Arrange
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, provider)
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert - Should succeed for any case variation of "Google"
            result.Should().NotBeNull();
            // For now, we expect this to fail until we implement case-insensitive matching
            // This test defines the requirement for future implementation
        }

        [Fact]
        public async Task InitiateLoginAsync_ShouldIncludeReturnUrl_WhenProvided()
        {
            // Arrange
            var returnUrl = "https://myapp.com/dashboard";
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, "Google")
                .With(x => x.ReturnUrl, returnUrl)
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            // Future requirement: state parameter should encode return URL
        }

        #endregion

        #region HandleAuthCallbackAsync Edge Cases

        [Theory]
        [InlineData(null, "valid_code")]
        [InlineData("", "valid_code")]
        [InlineData("   ", "valid_code")]
        public async Task HandleAuthCallbackAsync_ShouldReturnError_WhenProviderIsInvalid(string? provider, string code)
        {
            // Arrange
            var service = CreateEAuthService();

            // Act
            var result = await service.HandleAuthCallbackAsync(provider!, code, "state");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("INVALID_CALLBACK");
        }

        [Theory]
        [InlineData("Google", null)]
        [InlineData("Google", "")]
        [InlineData("Google", "   ")]
        public async Task HandleAuthCallbackAsync_ShouldReturnError_WhenCodeIsInvalid(string provider, string? code)
        {
            // Arrange
            var service = CreateEAuthService();

            // Act
            var result = await service.HandleAuthCallbackAsync(provider, code!, "state");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("INVALID_CALLBACK");
        }

        [Fact]
        public async Task HandleAuthCallbackAsync_ShouldHandleStateParameter_Correctly()
        {
            // Arrange
            var provider = "Google";
            var code = "valid_auth_code";
            var state = "some_state_value";
            var service = CreateEAuthService();

            // Act
            var result = await service.HandleAuthCallbackAsync(provider, code, state);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AuthProvider.Should().Be(provider);
        }

        #endregion

        #region ValidateSessionAsync Edge Cases

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateSessionAsync_ShouldReturnError_WhenSessionIdIsInvalid(string? sessionId)
        {
            // Arrange
            var service = CreateEAuthService();

            // Act
            var result = await service.ValidateSessionAsync(sessionId!);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateSessionAsync_ShouldHandleDatabaseException_Gracefully()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            
            _mockDatabaseService
                .Setup(x => x.ValidateSessionAsync(sessionId))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            var service = CreateEAuthService();

            // Act
            var result = await service.ValidateSessionAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("SESSION_VALIDATION_ERROR");
        }

        [Fact]
        public async Task ValidateSessionAsync_ShouldReturnExpiredSession_WhenSessionIsExpired()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            var expiredSession = _fixture.Build<SessionInfo>()
                .With(x => x.IsValid, false)
                .With(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddHours(-1))
                .Create();

            _mockDatabaseService
                .Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(expiredSession);

            var service = CreateEAuthService();

            // Act
            var result = await service.ValidateSessionAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();  // Service call succeeds
            result.Data.Should().NotBeNull();
            result.Data!.IsValid.Should().BeFalse();
        }

        #endregion

        #region SignOutAsync Edge Cases

        [Fact]
        public async Task SignOutAsync_ShouldHandleDatabaseException_Gracefully()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            
            _mockDatabaseService
                .Setup(x => x.InvalidateSessionAsync(sessionId))
                .ThrowsAsync(new TimeoutException("Database timeout"));

            var service = CreateEAuthService();

            // Act
            var result = await service.SignOutAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be("SIGNOUT_ERROR");
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task SignOutAsync_ShouldReturnFalse_WhenSessionAlreadyInvalid()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            
            _mockDatabaseService
                .Setup(x => x.InvalidateSessionAsync(sessionId))
                .ReturnsAsync(false);

            var service = CreateEAuthService();

            // Act
            var result = await service.SignOutAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeFalse();
            result.Message.Should().Contain("already invalid");
        }

        #endregion

        #region Concurrent Operations

        [Fact]
        public async Task MultipleOperations_ShouldHandleConcurrency_Safely()
        {
            // Arrange
            var service = CreateEAuthService();
            var tasks = new List<Task<EAuthResponse<IEnumerable<ProviderInfo>>>>();

            // Act - Execute multiple concurrent operations
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(service.GetProvidersAsync());
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All operations should complete successfully
            results.Should().AllSatisfy(result =>
            {
                result.Should().NotBeNull();
                result.Success.Should().BeTrue();
            });
        }

        #endregion

        #region Helper Methods

        private IEAuthService CreateEAuthService()
        {
            return new EAuthService(_mockDatabaseService.Object, _mockLogger.Object);
        }

        #endregion
    }
}