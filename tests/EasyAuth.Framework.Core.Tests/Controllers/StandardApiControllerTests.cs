using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EasyAuth.Framework.Core.Controllers;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EasyAuth.Framework.Core.Tests.Controllers;

public class StandardApiControllerTests
{
    private readonly Mock<IEAuthService> _mockEAuthService;
    private readonly Mock<ILogger<StandardApiController>> _mockLogger;
    private readonly StandardApiController _controller;

    public StandardApiControllerTests()
    {
        _mockEAuthService = new Mock<IEAuthService>();
        _mockLogger = new Mock<ILogger<StandardApiController>>();
        _controller = new StandardApiController(_mockEAuthService.Object, _mockLogger.Object);
        
        // Setup controller context
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task CheckAuthStatus_WhenNotAuthenticated_ReturnsUnauthenticatedResponse()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _controller.CheckAuthStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AuthApiResponse.AuthStatus>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.False(response.Data.IsAuthenticated);
    }

    [Fact]
    public async Task CheckAuthStatus_WhenAuthenticated_ReturnsAuthenticatedResponse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        var mockUserProfile = new UserProfile
        {
            Id = "user123",
            Name = "Test User",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        _mockEAuthService
            .Setup(x => x.GetUserProfileAsync(It.IsAny<string>()))
            .ReturnsAsync(mockUserProfile);

        // Act
        var result = await _controller.CheckAuthStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AuthApiResponse.AuthStatus>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.IsAuthenticated);
        Assert.NotNull(response.Data.User);
        Assert.Equal("user123", response.Data.User.Id);
        Assert.Equal("Test User", response.Data.User.Name);
    }

    [Fact]
    public async Task Login_WithMissingProvider_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest { Provider = "" };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("MISSING_PARAMETER", response.Error);
    }

    [Fact]
    public async Task Login_WithValidProvider_ReturnsLoginResult()
    {
        // Arrange
        var request = new LoginRequest { Provider = "Google", ReturnUrl = "/dashboard" };
        var mockAuthResult = new AuthenticationResult
        {
            Success = true,
            AuthUrl = "https://accounts.google.com/oauth/authorize?...",
            State = "csrf-state-123"
        };

        _mockEAuthService
            .Setup(x => x.InitiateAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mockAuthResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AuthApiResponse.LoginResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("https://accounts.google.com/oauth/authorize?...", response.Data.AuthUrl);
        Assert.True(response.Data.RedirectRequired);
    }

    [Fact]
    public async Task Logout_WhenAuthenticated_ReturnsLogoutResult()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        _mockEAuthService
            .Setup(x => x.SignOutUserAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AuthApiResponse.LogoutResult>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.LoggedOut);
    }

    [Fact]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "" };

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("MISSING_PARAMETER", response.Error);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsTokenRefreshResult()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
        var mockRefreshResult = new RefreshTokenResult
        {
            Success = true,
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        _mockEAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(mockRefreshResult);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AuthApiResponse.TokenRefresh>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("new-access-token", response.Data.AccessToken);
        Assert.Equal(3600, response.Data.ExpiresIn);
    }

    [Fact]
    public async Task GetUserProfile_WhenAuthenticated_ReturnsUserProfile()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        var mockUserProfile = new UserProfile
        {
            Id = "user123",
            Name = "Test User",
            Email = "test@example.com",
            Roles = new List<string> { "User" }
        };

        _mockEAuthService
            .Setup(x => x.GetUserProfileAsync(It.IsAny<string>()))
            .ReturnsAsync(mockUserProfile);

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApiUserInfo>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("user123", response.Data.Id);
        Assert.Equal("Test User", response.Data.Name);
    }

    [Fact]
    public void HealthCheck_Always_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert - Just verify it returns OK, content verification via integration testing
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}