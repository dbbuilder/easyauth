using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using EasyAuth.Framework.Core.Controllers;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Models;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Controllers
{
    /// <summary>
    /// Comprehensive tests for EAuthController following TDD methodology
    /// Tests cover API endpoints, authentication flows, and error handling
    /// </summary>
    public class EAuthControllerTests
    {
        private readonly Mock<IEAuthService> _mockEAuthService;
        private readonly Mock<ILogger<EAuthController>> _mockLogger;
        private readonly EAuthController _controller;

        public EAuthControllerTests()
        {
            _mockEAuthService = new Mock<IEAuthService>();
            _mockLogger = new Mock<ILogger<EAuthController>>();
            _controller = new EAuthController(_mockEAuthService.Object, _mockLogger.Object);

            // Setup basic HTTP context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowArgumentNull_WhenEAuthServiceIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new EAuthController(null!, _mockLogger.Object));

            exception.ParamName.Should().Be("eauthService");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNull_WhenLoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new EAuthController(_mockEAuthService.Object, null!));

            exception.ParamName.Should().Be("logger");
        }

        #endregion

        #region GetProviders Tests

        [Fact]
        public async Task GetProviders_ShouldReturnOk_WithProviderList()
        {
            // Arrange
            var expectedProviders = new List<ProviderInfo>
            {
                new ProviderInfo { Name = "Google", DisplayName = "Google", IsEnabled = true },
                new ProviderInfo { Name = "Apple", DisplayName = "Apple", IsEnabled = true }
            };

            var expectedResponse = new EAuthResponse<IEnumerable<ProviderInfo>>
            {
                Success = true,
                Data = expectedProviders,
                Message = "Providers retrieved successfully"
            };

            _mockEAuthService.Setup(x => x.GetProvidersAsync())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetProviders().ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<IEnumerable<ProviderInfo>>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetProviders_ShouldReturnInternalServerError_WhenServiceThrows()
        {
            // Arrange
            _mockEAuthService.Setup(x => x.GetProvidersAsync())
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act
            var result = await _controller.GetProviders().ConfigureAwait(false);

            // Assert
            var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
            serverErrorResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ShouldReturnOk_WithLoginUrl()
        {
            // Arrange
            var request = new LoginRequest { Provider = "Google", ReturnUrl = "https://example.com" };
            var expectedResponse = new EAuthResponse<string>
            {
                Success = true,
                Data = "https://accounts.google.com/oauth2/authorize",
                Message = "Login URL generated successfully"
            };

            _mockEAuthService.Setup(x => x.InitiateLoginAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request).ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<string>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().StartWith("https://accounts.google.com");
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenServiceReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest { Provider = "Invalid" };
            var serviceResponse = new EAuthResponse<string>
            {
                Success = false,
                ErrorCode = "INVALID_PROVIDER",
                Message = "Provider not supported"
            };

            _mockEAuthService.Setup(x => x.InitiateLoginAsync(request))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.Login(request).ConfigureAwait(false);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeOfType<EAuthResponse<string>>().Subject;
            
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be("INVALID_PROVIDER");
        }

        #endregion

        #region AuthCallback Tests

        [Fact]
        public async Task AuthCallback_ShouldReturnRedirect_WhenSuccessful()
        {
            // Arrange
            var expectedResponse = new EAuthResponse<UserInfo>
            {
                Success = true,
                Data = new UserInfo { UserId = "user123", Email = "user@example.com" },
                Message = "Authentication successful"
            };

            _mockEAuthService.Setup(x => x.HandleAuthCallbackAsync("Google", "code123", "state"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AuthCallback("Google", "code123", "state").ConfigureAwait(false);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectResult>().Subject;
            redirectResult.Url.Should().Be("/");
        }

        [Fact]
        public async Task AuthCallback_ShouldReturnBadRequest_WhenServiceReturnsFailure()
        {
            // Arrange
            var serviceResponse = new EAuthResponse<UserInfo>
            {
                Success = false,
                ErrorCode = "AUTHENTICATION_FAILED",
                Message = "Invalid authorization code"
            };

            _mockEAuthService.Setup(x => x.HandleAuthCallbackAsync("Google", "invalid_code", "state"))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.AuthCallback("Google", "invalid_code", "state").ConfigureAwait(false);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeOfType<EAuthResponse<UserInfo>>().Subject;
            
            response.Success.Should().BeFalse();
            response.ErrorCode.Should().Be("AUTHENTICATION_FAILED");
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_ShouldReturnOk_WithUserInfo()
        {
            // Arrange
            var expectedResponse = new EAuthResponse<UserInfo>
            {
                Success = true,
                Data = new UserInfo { UserId = "user123", Email = "user@example.com" },
                Message = "User information retrieved"
            };

            _mockEAuthService.Setup(x => x.GetCurrentUserAsync())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetCurrentUser().ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<UserInfo>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        #endregion

        #region ValidateSession Tests

        [Fact]
        public async Task ValidateSession_ShouldReturnOk_WithSessionInfo()
        {
            // Arrange
            var sessionId = "session123";
            var expectedResponse = new EAuthResponse<SessionInfo>
            {
                Success = true,
                Data = new SessionInfo { SessionId = sessionId, IsValid = true },
                Message = "Session is valid"
            };

            _mockEAuthService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ValidateSession(sessionId).ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<SessionInfo>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data!.IsValid.Should().BeTrue();
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var expectedResponse = new EAuthResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Logout successful"
            };
            
            _mockEAuthService.Setup(x => x.SignOutAsync(null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Logout().ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<bool>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        #endregion

        #region LinkAccount Tests

        [Fact]
        public async Task LinkAccount_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var provider = "Apple";
            var request = new LinkAccountRequest { Code = "code123", State = "state" };
            var expectedResponse = new EAuthResponse<UserInfo>
            {
                Success = true,
                Data = new UserInfo { UserId = "user123" },
                Message = "Account linked successfully"
            };

            _mockEAuthService.Setup(x => x.LinkAccountAsync(provider, request.Code, request.State))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.LinkAccount(provider, request).ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<UserInfo>>().Subject;
            
            response.Success.Should().BeTrue();
        }

        #endregion

        #region UnlinkAccount Tests

        [Fact]
        public async Task UnlinkAccount_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var provider = "Apple";
            var expectedResponse = new EAuthResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Account unlinked successfully"
            };

            _mockEAuthService.Setup(x => x.UnlinkAccountAsync(provider))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UnlinkAccount(provider).ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<bool>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        #endregion

        #region ResetPassword Tests

        [Fact]
        public async Task ResetPassword_ShouldReturnOk_WithResetUrl()
        {
            // Arrange
            var request = new PasswordResetRequest { Email = "user@example.com", Provider = "AzureB2C" };
            var expectedResponse = new EAuthResponse<string>
            {
                Success = true,
                Data = "https://tenant.b2clogin.com/reset-password",
                Message = "Password reset initiated"
            };
            
            _mockEAuthService.Setup(x => x.InitiatePasswordResetAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ResetPassword(request).ConfigureAwait(false);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<EAuthResponse<string>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().Contain("reset-password");
        }

        #endregion
    }
}