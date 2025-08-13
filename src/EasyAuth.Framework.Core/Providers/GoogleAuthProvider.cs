using System.Text;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Core.Providers
{
    /// <summary>
    /// Google OAuth 2.0 authentication provider
    /// </summary>
    public class GoogleAuthProvider : IEAuthProvider
    {
        private readonly GoogleOptions _options;
        private readonly ILogger<GoogleAuthProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

        public string ProviderName => "Google";

        public string DisplayName => "Google";

        public bool IsEnabled => _options?.Enabled == true;

        public GoogleAuthProvider(
            IOptions<EAuthOptions> eauthOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<GoogleAuthProvider> logger,
            IConfigurationService configurationService)
        {
            _options = eauthOptions.Value?.Providers?.Google ?? new GoogleOptions { Enabled = false };
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        /// <summary>
        /// Generates Google OAuth 2.0 authorization URL with consent prompt and offline access
        /// Delegates to GetLoginUrlAsync for consistency in Google's OAuth implementation
        /// </summary>
        public async Task<string> GetAuthorizationUrlAsync(string? returnUrl = null)
        {
            return await GetLoginUrlAsync(returnUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Exchanges Google OAuth authorization code for access and refresh tokens
        /// Makes HTTP POST request to Google's token endpoint with client credentials
        /// </summary>
        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string? state = null)
        {
            var googleTokens = await ExchangeCodeForTokensAsync(code).ConfigureAwait(false);

            if (googleTokens == null)
            {
                throw new InvalidOperationException("Failed to exchange code for tokens");
            }

            return new TokenResponse
            {
                AccessToken = googleTokens.AccessToken,
                TokenType = googleTokens.token_type,
                ExpiresIn = googleTokens.expires_in,
                RefreshToken = googleTokens.refresh_token ?? string.Empty
            };
        }

        /// <summary>
        /// Retrieves Google user profile information using access token
        /// Calls Google's userinfo endpoint and maps to standardized UserInfo model
        /// </summary>
        public async Task<UserInfo> GetUserInfoAsync(TokenResponse tokens)
        {
            var userInfo = await GetUserInfoAsync(tokens.AccessToken).ConfigureAwait(false);

            if (userInfo == null)
            {
                throw new InvalidOperationException("Failed to retrieve user information");
            }

            return userInfo;
        }

        /// <summary>
        /// Builds Google OAuth login URL with scopes, state, and custom parameters
        /// Uses consent prompt and offline access for refresh token capability
        /// </summary>
        public Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null)
        {
            try
            {
                if (!IsEnabled)
                    throw new InvalidOperationException("Google provider is not enabled");

                _logger.LogInformation("Generating Google OAuth login URL");

                var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"{returnUrl ?? "/"};{DateTimeOffset.UtcNow.Ticks}"));

                var queryParams = new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["response_type"] = "code",
                    ["scope"] = string.Join(" ", _options.Scopes),
                    ["redirect_uri"] = GetRedirectUri(),
                    ["state"] = state,
                    ["access_type"] = "offline",
                    ["prompt"] = "consent"
                };

                // Add any additional parameters
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        queryParams[param.Key] = param.Value;
                    }
                }

                var queryString = string.Join("&",
                    queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";

                _logger.LogInformation("Generated Google OAuth URL successfully");
                return Task.FromResult(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Google OAuth login URL");
                throw;
            }
        }

        /// <summary>
        /// Processes Google OAuth callback by exchanging code for tokens and retrieving user info
        /// Handles errors gracefully with detailed error codes and logging
        /// </summary>
        public async Task<EAuthResponse<UserInfo>> HandleCallbackAsync(string code, string? state = null)
        {
            try
            {
                _logger.LogInformation("Processing Google OAuth callback");

                // Exchange code for tokens
                var tokenResponse = await ExchangeCodeForTokensAsync(code).ConfigureAwait(false);
                if (tokenResponse == null)
                {
                    return new EAuthResponse<UserInfo>
                    {
                        Success = false,
                        Message = "Failed to exchange authorization code for tokens",
                        ErrorCode = "TOKEN_EXCHANGE_FAILED"
                    };
                }

                // Get user info from Google
                var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken).ConfigureAwait(false);
                if (userInfo == null)
                {
                    return new EAuthResponse<UserInfo>
                    {
                        Success = false,
                        Message = "Failed to retrieve user information from Google",
                        ErrorCode = "USER_INFO_FAILED"
                    };
                }

                _logger.LogInformation("Successfully processed Google OAuth callback for user: {Email}", userInfo.Email);

                return new EAuthResponse<UserInfo>
                {
                    Success = true,
                    Data = userInfo,
                    Message = "Google authentication successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google OAuth callback");
                return new EAuthResponse<UserInfo>
                {
                    Success = false,
                    Message = "Error processing Google authentication callback",
                    ErrorCode = "CALLBACK_ERROR"
                };
            }
        }

        /// <summary>
        /// Returns local logout redirect URL for Google OAuth
        /// Google doesn't provide centralized logout - applications handle session cleanup
        /// </summary>
        public async Task<string> GetLogoutUrlAsync(string? returnUrl = null)
        {
            // Google doesn't have a centralized logout URL like some other providers
            // The application should handle session cleanup
            await Task.CompletedTask.ConfigureAwait(false);
            return returnUrl ?? "/";
        }

        /// <summary>
        /// Returns Google's account recovery URL for password reset
        /// Google handles password reset through their account recovery system
        /// </summary>
        public async Task<string?> GetPasswordResetUrlAsync(string email)
        {
            // Google handles password reset through their own account recovery
            await Task.CompletedTask.ConfigureAwait(false);
            return "https://accounts.google.com/signin/recovery";
        }

        /// <summary>
        /// Validates Google OAuth configuration including ClientId and ClientSecret
        /// Uses secure configuration service to verify required credentials
        /// </summary>
        public async Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                if (!IsEnabled) return true; // Skip validation if disabled

                if (string.IsNullOrEmpty(_options.ClientId))
                {
                    _logger.LogError("Google ClientId is not configured");
                    return false;
                }

                // SECURITY: Use unified configuration service to validate client secret
                try
                {
                    var clientSecret = _configurationService.GetRequiredSecretValue(
                        "Google:ClientSecret",
                        "GOOGLE_CLIENT_SECRET");

                    if (string.IsNullOrEmpty(clientSecret))
                    {
                        _logger.LogError("Google ClientSecret is not configured");
                        return false;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Google ClientSecret validation failed");
                    return false;
                }

                _logger.LogInformation("Google provider configuration is valid");
                return await Task.FromResult(true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google provider configuration");
                return false;
            }
        }

        private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code)
        {
            try
            {
                var tokenEndpoint = "https://oauth2.googleapis.com/token";
                // SECURITY: Use unified configuration service for client secret
                var clientSecret = _configurationService.GetRequiredSecretValue(
                    "Google:ClientSecret",
                    "GOOGLE_CLIENT_SECRET");

                var parameters = new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = GetRedirectUri()
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(tokenEndpoint, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(json);
                }

                _logger.LogWarning("Token exchange failed with status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens");
                return null;
            }
        }

        private async Task<UserInfo?> GetUserInfoAsync(string accessToken)
        {
            try
            {
                var userInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

                using var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var googleUser = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(json);

                    if (googleUser != null)
                    {
                        return new UserInfo
                        {
                            UserId = googleUser.Id,
                            Email = googleUser.Email ?? string.Empty,
                            DisplayName = googleUser.Name ?? string.Empty,
                            FirstName = googleUser.GivenName ?? string.Empty,
                            LastName = googleUser.FamilyName ?? string.Empty,
                            ProfilePictureUrl = googleUser.Picture ?? string.Empty,
                            AuthProvider = ProviderName,
                            IsAuthenticated = true,
                            Claims = new Dictionary<string, string>
                            {
                                ["provider_id"] = googleUser.Id,
                                ["email_verified"] = googleUser.VerifiedEmail?.ToString() ?? "false",
                                ["locale"] = googleUser.Locale ?? string.Empty
                            }
                        };
                    }
                }

                _logger.LogWarning("User info request failed with status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info from Google");
                return null;
            }
        }

        private string GetRedirectUri()
        {
            // This would need to be constructed based on the current request
            // For now, return the configured callback path
            return _options.CallbackPath;
        }

        private class GoogleTokenResponse
        {
            public string access_token { get; set; } = string.Empty;

            public string? refresh_token { get; set; }

            public int expires_in { get; set; }

            public string token_type { get; set; } = string.Empty;

            public string AccessToken => access_token;
        }

        private class GoogleUserInfo
        {
            public string id { get; set; } = string.Empty;

            public string? email { get; set; }

            public bool? verified_email { get; set; }

            public string? name { get; set; }

            public string? given_name { get; set; }

            public string? family_name { get; set; }

            public string? picture { get; set; }

            public string? locale { get; set; }

            public string Id => id;

            public string? Email => email;

            public bool? VerifiedEmail => verified_email;

            public string? Name => name;

            public string? GivenName => given_name;

            public string? FamilyName => family_name;

            public string? Picture => picture;

            public string? Locale => locale;
        }
    }
}

