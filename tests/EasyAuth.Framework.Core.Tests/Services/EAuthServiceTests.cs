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
    /// Test-driven development tests for IEAuthService implementation
    /// These tests define the contract and expected behavior BEFORE implementation
    /// </summary>
    public class EAuthServiceTests
    {
        private readonly Mock<IEAuthDatabaseService> _mockDatabaseService;
        private readonly Mock<ILogger<EAuthService>> _mockLogger;
        private readonly Fixture _fixture;

        public EAuthServiceTests()
        {
            _mockDatabaseService = new Mock<IEAuthDatabaseService>();
            _mockLogger = new Mock<ILogger<EAuthService>>();
            _fixture = new Fixture();
        }

        #region TDD RED Phase - Tests that should fail initially

        [Fact]
        public async Task GetProvidersAsync_ShouldReturnEnabledProviders_WhenCalled()
        {
            // Arrange
            var expectedProviders = _fixture.CreateMany<ProviderInfo>(3).ToArray();
            var service = CreateEAuthService();

            // Act
            var result = await service.GetProvidersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task InitiateLoginAsync_ShouldReturnLoginUrl_ForValidProvider()
        {
            // Arrange
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, "Google")
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data.Should().StartWith("https://");
        }

        [Fact]
        public async Task InitiateLoginAsync_ShouldReturnError_ForInvalidProvider()
        {
            // Arrange
            var request = _fixture.Build<LoginRequest>()
                .With(x => x.Provider, "InvalidProvider")
                .Create();
            var service = CreateEAuthService();

            // Act
            var result = await service.InitiateLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().NotBeNullOrEmpty();
            result.Message.Should().Contain("provider");
        }

        [Fact]
        public async Task HandleAuthCallbackAsync_ShouldCreateSession_ForValidCallback()
        {
            // Arrange
            const string provider = "Google";
            const string code = "valid_auth_code";
            const string state = "valid_state";
            var expectedUserInfo = _fixture.Create<UserInfo>();
            
            var service = CreateEAuthService();

            // Act
            var result = await service.HandleAuthCallbackAsync(provider, code, state);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.IsAuthenticated.Should().BeTrue();
            result.Data.UserId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ValidateSessionAsync_ShouldReturnValid_ForActiveSession()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            var expectedSession = _fixture.Build<SessionInfo>()
                .With(x => x.IsValid, true)
                .With(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddHours(1))
                .Create();

            _mockDatabaseService
                .Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(expectedSession);

            var service = CreateEAuthService();

            // Act
            var result = await service.ValidateSessionAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task SignOutAsync_ShouldInvalidateSession_WhenCalled()
        {
            // Arrange
            var sessionId = _fixture.Create<string>();
            
            _mockDatabaseService
                .Setup(x => x.InvalidateSessionAsync(sessionId))
                .ReturnsAsync(true);

            var service = CreateEAuthService();

            // Act
            var result = await service.SignOutAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private IEAuthService CreateEAuthService()
        {
            // Using the stub implementation from Core project
            // Following TDD: test first, then implement
            return new EAuthService(_mockDatabaseService.Object, _mockLogger.Object);
        }

        #endregion
    }
}