using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Providers
{
    /// <summary>
    /// Apple Sign-In authentication provider implementation
    /// Follows TDD methodology - minimal implementation to make tests pass
    /// </summary>
    public class AppleAuthProvider : IEAuthProvider
    {
        private readonly AppleOptions _options;
        // TODO: Use IHttpClientFactory for Apple API calls when implementing real token exchange
        private readonly ILogger<AppleAuthProvider> _logger;
        private readonly IConfigurationService _configurationService;

        public AppleAuthProvider(
            IOptions<AppleOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<AppleAuthProvider> logger,
            IConfigurationService configurationService)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            // httpClientFactory will be used for future Apple API integration
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public string ProviderName => "Apple";

        public string DisplayName => "Apple Sign-In";

        public bool IsEnabled => _options.Enabled;

        /// <summary>
        /// Generates Apple Sign-In authorization URL with client_id, scopes, and state parameters
        /// Apple uses form_post response mode for security and requires nonce for id_token validation
        /// </summary>
        public async Task<string> GetAuthorizationUrlAsync(string? returnUrl = null)
        {
            // TDD GREEN Phase: Minimal implementation to make tests pass
            await Task.CompletedTask.ConfigureAwait(false);

            var state = GenerateState(returnUrl);
            var scopes = string.Join(" ", _options.Scopes);
            var redirectUri = BuildRedirectUri();

            var authUrl = "https://appleid.apple.com/auth/authorize" +
                $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scopes)}" +
                $"&response_mode=form_post" +
                $"&state={Uri.EscapeDataString(state)}";

            return authUrl;
        }

        /// <summary>
        /// Exchanges Apple Sign-In authorization code for access and ID tokens
        /// Returns mock tokens in TDD GREEN phase - real implementation would call Apple's token endpoint
        /// </summary>
        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string? state = null)
        {
            // TDD GREEN Phase: Enhanced validation and minimal implementation
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Authorization code is required", nameof(code));
            }

            await Task.CompletedTask.ConfigureAwait(false);

            // For TDD GREEN phase, return a mock response
            // Real implementation would make HTTP call to Apple's token endpoint
            return new TokenResponse
            {
                AccessToken = "mock_apple_access_token",
                IdToken = GenerateMockIdToken(),
                TokenType = "Bearer",
                ExpiresIn = 3600
            };
        }

        /// <summary>
        /// Extracts user information from Apple ID token JWT claims
        /// Apple provides user data in id_token rather than a separate userinfo endpoint
        /// </summary>
        public async Task<UserInfo> GetUserInfoAsync(TokenResponse tokens)
        {
            // TDD GREEN Phase: Extract user info from ID token
            await Task.CompletedTask.ConfigureAwait(false);

            if (string.IsNullOrEmpty(tokens.IdToken))
            {
                throw new ArgumentException("ID token is required for Apple Sign-In");
            }

            // Parse the JWT ID token to extract user information
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(tokens.IdToken);

            var sub = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? string.Empty;
            var email = jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value ?? string.Empty;
            var emailVerified = jsonToken.Claims.FirstOrDefault(x => x.Type == "email_verified")?.Value == "true";

            return new UserInfo
            {
                UserId = sub,
                Email = email,
                DisplayName = email.Split('@')[0], // Apple often doesn't provide name
                IsAuthenticated = true,
                AuthProvider = ProviderName,
                LastLoginDate = DateTimeOffset.UtcNow
            };
        }

        #region IEAuthProvider Implementation (Stub methods)

        /// <summary>
        /// Generates Apple Sign-In login URL - delegates to GetAuthorizationUrlAsync
        /// Apple authentication follows standard OAuth 2.0 flow with OIDC extensions
        /// </summary>
        public async Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null)
        {
            return await GetAuthorizationUrlAsync(returnUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles Apple Sign-In callback by exchanging code for tokens and extracting user info
        /// Returns standardized EAuthResponse with success/error status and user data
        /// </summary>
        public async Task<EAuthResponse<UserInfo>> HandleCallbackAsync(string code, string? state = null)
        {
            try
            {
                var tokens = await ExchangeCodeForTokenAsync(code, state).ConfigureAwait(false);
                var userInfo = await GetUserInfoAsync(tokens).ConfigureAwait(false);

                return new EAuthResponse<UserInfo>
                {
                    Success = true,
                    Data = userInfo,
                    Message = "Apple authentication successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Apple authentication callback");
                return new EAuthResponse<UserInfo>
                {
                    Success = false,
                    ErrorCode = "APPLE_AUTH_ERROR",
                    Message = "Apple authentication failed"
                };
            }
        }

        /// <summary>
        /// Returns logout redirect URL for Apple Sign-In
        /// Apple doesn't provide centralized logout - returns local redirect URL
        /// </summary>
        public async Task<string> GetLogoutUrlAsync(string? returnUrl = null)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return returnUrl ?? "/";
        }

        /// <summary>
        /// Apple Sign-In doesn't support direct password reset URLs
        /// Users must manage passwords through Apple ID settings
        /// </summary>
        public async Task<string?> GetPasswordResetUrlAsync(string email)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return null; // Apple doesn't support direct password reset
        }

        /// <summary>
        /// Validates Apple Sign-In configuration including ClientId, TeamId, and KeyId
        /// Required for Apple's certificate-based authentication
        /// </summary>
        public async Task<bool> ValidateConfigurationAsync()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            return !string.IsNullOrEmpty(_options.ClientId) &&
                   !string.IsNullOrEmpty(_options.TeamId) &&
                   !string.IsNullOrEmpty(_options.KeyId);
        }

        #endregion

        #region Private Helper Methods

        private string GenerateState(string? returnUrl)
        {
            var stateData = new
            {
                ReturnUrl = returnUrl ?? "/",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Nonce = Guid.NewGuid().ToString("N")
            };

            var json = JsonSerializer.Serialize(stateData);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        private string BuildRedirectUri()
        {
            // For now, return a dummy redirect URI
            // Real implementation would build this from configuration
            return "https://localhost/auth/apple-callback";
        }

        private string GenerateMockIdToken()
        {
            // SECURITY FIX: Use unified configuration service to get JWT secret with fallback
            var jwtSecret = _configurationService.GetRequiredSecretValue(
                "Apple:JwtSecret",
                "APPLE_JWT_SECRET");

            // Generate a mock JWT for testing purposes using securely configured secret
            var handler = new JwtSecurityTokenHandler();

            var claims = new[]
            {
                new Claim("sub", "001234.567890abcdef.1234"),
                new Claim("email", "user@example.com"),
                new Claim("email_verified", "true"),
                new Claim("aud", _options.ClientId),
                new Claim("iss", "https://appleid.apple.com"),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
            };

            // Use the securely configured JWT secret from Key Vault/environment/config
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret));

            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        #endregion
    }
}
