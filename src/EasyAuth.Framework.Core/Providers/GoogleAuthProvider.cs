using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

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

        public string ProviderName => "Google";
        public string DisplayName => "Google";
        public bool IsEnabled => _options?.Enabled == true;

        public GoogleAuthProvider(
            IOptions<EAuthOptions> eauthOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<GoogleAuthProvider> logger)
        {
            _options = eauthOptions.Value?.Providers?.Google ?? new GoogleOptions { Enabled = false };
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null)
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
                return authUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Google OAuth login URL");
                throw;
            }
        }

        public async Task<EAuthResponse<UserInfo>> HandleCallbackAsync(string code, string? state = null)
        {
            try
            {
                _logger.LogInformation("Processing Google OAuth callback");

                // Exchange code for tokens
                var tokenResponse = await ExchangeCodeForTokensAsync(code);
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
                var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken);
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

        public async Task<string> GetLogoutUrlAsync(string? returnUrl = null)
        {
            // Google doesn't have a centralized logout URL like some other providers
            // The application should handle session cleanup
            await Task.CompletedTask;
            return returnUrl ?? "/";
        }

        public async Task<string?> GetPasswordResetUrlAsync(string email)
        {
            // Google handles password reset through their own account recovery
            await Task.CompletedTask;
            return "https://accounts.google.com/signin/recovery";
        }

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

                if (string.IsNullOrEmpty(_options.ClientSecret))
                {
                    _logger.LogError("Google ClientSecret is not configured");
                    return false;
                }

                _logger.LogInformation("Google provider configuration is valid");
                return await Task.FromResult(true);
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
                var parameters = new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = GetRedirectUri()
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(tokenEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
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

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
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
