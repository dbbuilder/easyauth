using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EasyAuth.Framework.Core.Controllers
{
    /// <summary>
    /// EasyAuth authentication controller for handling user authentication flows
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EAuthController : ControllerBase
    {
        private readonly IEAuthService _eauthService;
        private readonly ILogger<EAuthController> _logger;

        public EAuthController(
            IEAuthService eauthService,
            ILogger<EAuthController> logger)
        {
            _eauthService = eauthService ?? throw new ArgumentNullException(nameof(eauthService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all available authentication providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var result = await _eauthService.GetProvidersAsync().ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authentication providers");
                return StatusCode(500, new EAuthResponse<IEnumerable<ProviderInfo>>
                {
                    Success = false,
                    Message = "Internal server error retrieving providers",
                    ErrorCode = "PROVIDERS_ERROR"
                });
            }
        }

        /// <summary>
        /// Initiate login with specified provider
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login request received for provider: {Provider}", request.Provider);

                var result = await _eauthService.InitiateLoginAsync(request).ConfigureAwait(false);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login request for provider: {Provider}", request.Provider);
                return StatusCode(500, new EAuthResponse<string>
                {
                    Success = false,
                    Message = "Internal server error during login",
                    ErrorCode = "LOGIN_ERROR"
                });
            }
        }

        /// <summary>
        /// Handle authentication callback from provider
        /// </summary>
        [HttpGet("callback/{provider}")]
        public async Task<IActionResult> AuthCallback(string provider, [FromQuery] string code, [FromQuery] string? state = null)
        {
            try
            {
                _logger.LogInformation("Authentication callback received for provider: {Provider}", provider);

                var result = await _eauthService.HandleAuthCallbackAsync(provider, code, state).ConfigureAwait(false);

                if (result.Success)
                {
                    // Extract return URL from state if available
                    var returnUrl = ExtractReturnUrlFromState(state) ?? "/";
                    return Redirect(returnUrl);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling authentication callback for provider: {Provider}", provider);
                return StatusCode(500, new EAuthResponse<UserInfo>
                {
                    Success = false,
                    Message = "Internal server error during authentication callback",
                    ErrorCode = "CALLBACK_ERROR"
                });
            }
        }

        /// <summary>
        /// Sign out current user
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                _logger.LogInformation("Logout request received");

                var result = await _eauthService.SignOutAsync().ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing logout request");
                return StatusCode(500, new EAuthResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error during logout",
                    ErrorCode = "LOGOUT_ERROR"
                });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var result = await _eauthService.GetCurrentUserAsync().ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user information");
                return StatusCode(500, new EAuthResponse<UserInfo>
                {
                    Success = false,
                    Message = "Internal server error retrieving user information",
                    ErrorCode = "USER_INFO_ERROR"
                });
            }
        }

        /// <summary>
        /// Validate current session
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateSession([FromQuery] string? sessionId = null)
        {
            try
            {
                // Use session ID from query parameter or extract from request
                sessionId ??= HttpContext.Session.Id;

                var result = await _eauthService.ValidateSessionAsync(sessionId).ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session");
                return StatusCode(500, new EAuthResponse<SessionInfo>
                {
                    Success = false,
                    Message = "Internal server error validating session",
                    ErrorCode = "SESSION_VALIDATION_ERROR"
                });
            }
        }

        /// <summary>
        /// Link account from another provider
        /// </summary>
        [HttpPost("link/{provider}")]
        [Authorize]
        public async Task<IActionResult> LinkAccount(string provider, [FromBody] LinkAccountRequest request)
        {
            try
            {
                _logger.LogInformation("Account linking request for provider: {Provider}", provider);

                var result = await _eauthService.LinkAccountAsync(provider, request.Code, request.State).ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking account for provider: {Provider}", provider);
                return StatusCode(500, new EAuthResponse<UserInfo>
                {
                    Success = false,
                    Message = "Internal server error linking account",
                    ErrorCode = "LINK_ACCOUNT_ERROR"
                });
            }
        }

        /// <summary>
        /// Unlink account from provider
        /// </summary>
        [HttpDelete("unlink/{provider}")]
        [Authorize]
        public async Task<IActionResult> UnlinkAccount(string provider)
        {
            try
            {
                _logger.LogInformation("Account unlinking request for provider: {Provider}", provider);

                var result = await _eauthService.UnlinkAccountAsync(provider).ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking account for provider: {Provider}", provider);
                return StatusCode(500, new EAuthResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error unlinking account",
                    ErrorCode = "UNLINK_ACCOUNT_ERROR"
                });
            }
        }

        /// <summary>
        /// Initiate password reset
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest request)
        {
            try
            {
                _logger.LogInformation("Password reset request for email: {Email} via provider: {Provider}",
                    request.Email, request.Provider);

                var result = await _eauthService.InitiatePasswordResetAsync(request).ConfigureAwait(false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset request for email: {Email}", request.Email);
                return StatusCode(500, new EAuthResponse<string>
                {
                    Success = false,
                    Message = "Internal server error during password reset",
                    ErrorCode = "PASSWORD_RESET_ERROR"
                });
            }
        }

        private string? ExtractReturnUrlFromState(string? state)
        {
            if (string.IsNullOrEmpty(state))
                return null;

            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
                var parts = decoded.Split(';');
                return parts.Length > 0 ? parts[0] : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Request model for linking accounts
    /// </summary>
    public class LinkAccountRequest
    {
        /// <summary>
        /// Authorization code from the OAuth provider
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// State parameter for CSRF protection
        /// </summary>
        public string State { get; set; } = string.Empty;
    }
}
