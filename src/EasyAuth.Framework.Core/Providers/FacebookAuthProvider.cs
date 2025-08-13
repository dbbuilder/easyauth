using System.Text;
using System.Text.Json;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Web;

namespace EasyAuth.Framework.Core.Providers
{
    /// <summary>
    /// Facebook/Meta authentication provider implementation
    /// Enhanced v2.4.0 with API v18+ compatibility, business features, and advanced permissions
    /// </summary>
    public class FacebookAuthProvider : IEAuthProvider
    {
        private readonly FacebookOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FacebookAuthProvider> _logger;
        private readonly IConfigurationService _configurationService;
        
        // Facebook API v18+ constants
        private const string FacebookApiVersion = "v19.0";
        private const string FacebookOAuthBaseUrl = "https://www.facebook.com";
        private const string FacebookGraphBaseUrl = "https://graph.facebook.com";
        private const string FacebookBusinessGraphUrl = "https://graph.facebook.com";

        public FacebookAuthProvider(
            IOptions<FacebookOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<FacebookAuthProvider> logger,
            IConfigurationService configurationService)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClientFactory?.CreateClient(nameof(FacebookAuthProvider)) ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            
            // Configure HttpClient for Facebook API
            _httpClient.BaseAddress = new Uri(FacebookGraphBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EasyAuth-Framework/2.4.0");
        }

        public string ProviderName => "Facebook";

        public string DisplayName => "Facebook";

        public bool IsEnabled => _options.Enabled;

        /// <summary>
        /// Generates Facebook OAuth authorization URL with enhanced v2.4.0 features
        /// Supports business login, dynamic scopes, and advanced permissions management
        /// </summary>
        public async Task<string> GetAuthorizationUrlAsync(string? returnUrl = null)
        {
            await Task.CompletedTask.ConfigureAwait(false);

            var state = GenerateState(returnUrl);
            var scopes = GetRequestedScopes();
            var redirectUri = BuildRedirectUri();
            
            var authUrlBuilder = new StringBuilder($"{FacebookOAuthBaseUrl}/{FacebookApiVersion}/dialog/oauth");
            authUrlBuilder.Append($"?client_id={Uri.EscapeDataString(_options.AppId)}");
            authUrlBuilder.Append($"&redirect_uri={Uri.EscapeDataString(redirectUri)}");
            authUrlBuilder.Append($"&response_type=code");
            authUrlBuilder.Append($"&scope={Uri.EscapeDataString(scopes)}");
            authUrlBuilder.Append($"&state={Uri.EscapeDataString(state)}");
            
            // Add business-specific parameters if configured
            if (_options.Business?.EnableBusinessLogin == true)
            {
                if (!string.IsNullOrEmpty(_options.Business.BusinessId))
                {
                    authUrlBuilder.Append($"&business_id={Uri.EscapeDataString(_options.Business.BusinessId)}");
                }
                
                authUrlBuilder.Append($"&auth_type=rerequest"); // Request business permissions
            }
            
            // Add display mode for enhanced UX
            if (!string.IsNullOrEmpty(_options.DisplayMode))
            {
                authUrlBuilder.Append($"&display={Uri.EscapeDataString(_options.DisplayMode)}");
            }
            
            // Add locale if configured
            if (!string.IsNullOrEmpty(_options.Locale))
            {
                authUrlBuilder.Append($"&locale={Uri.EscapeDataString(_options.Locale)}");
            }

            var authUrl = authUrlBuilder.ToString();
            _logger.LogDebug("Generated Facebook authorization URL with scopes: {Scopes}", scopes);
            
            return authUrl;
        }

        /// <summary>
        /// Exchanges Facebook authorization code for access token with enhanced v2.4.0 features
        /// Supports long-lived tokens and comprehensive error handling
        /// </summary>
        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string? state = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Authorization code is required", nameof(code));
            }

            try
            {
                _logger.LogDebug("Exchanging Facebook authorization code for access token");
                
                var appSecret = _configurationService.GetRequiredSecretValue(
                    "Facebook:AppSecret", "FACEBOOK_APP_SECRET");
                
                var tokenEndpoint = $"/{FacebookApiVersion}/oauth/access_token";
                var parameters = new Dictionary<string, string>
                {
                    ["client_id"] = _options.AppId,
                    ["client_secret"] = appSecret,
                    ["code"] = code,
                    ["redirect_uri"] = BuildRedirectUri()
                };

                var response = await _httpClient.PostAsync(tokenEndpoint, 
                    new FormUrlEncodedContent(parameters));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Facebook token exchange failed: {StatusCode} {Error}", 
                        response.StatusCode, errorContent);
                    
                    // Parse Facebook error response
                    var errorResponse = await TryParseErrorResponse(errorContent);
                    throw new InvalidOperationException($"Facebook token exchange failed: {errorResponse?.Error?.Message ?? response.StatusCode.ToString()}");
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<FacebookTokenResponse>();
                
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    throw new InvalidOperationException("Invalid token response from Facebook");
                }

                // Exchange for long-lived token if configured
                var finalTokenResponse = tokenResponse;
                if (_options.UseLongLivedTokens)
                {
                    finalTokenResponse = await ExchangeForLongLivedToken(tokenResponse.access_token) ?? tokenResponse;
                }

                _logger.LogDebug("Facebook token exchange successful, expires in {ExpiresIn} seconds", 
                    finalTokenResponse.expires_in);

                return new TokenResponse
                {
                    AccessToken = finalTokenResponse.access_token,
                    TokenType = "Bearer",
                    ExpiresIn = finalTokenResponse.expires_in,
                    RefreshToken = string.Empty // Facebook doesn't use refresh tokens
                };
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                _logger.LogError(ex, "Error during Facebook token exchange");
                
                // Fallback to mock token for development if configured
                if (_options.UseMockTokensOnFailure)
                {
                    _logger.LogWarning("Facebook service unavailable, returning mock token for development");
                    return new TokenResponse
                    {
                        AccessToken = "mock_facebook_access_token",
                        TokenType = "Bearer",
                        ExpiresIn = 5183944
                    };
                }
                
                throw;
            }
        }

        /// <summary>
        /// Retrieves user information from Facebook Graph API with enhanced v2.4.0 features
        /// Supports business accounts, Instagram integration, and comprehensive user data
        /// </summary>
        public async Task<UserInfo> GetUserInfoAsync(TokenResponse tokens)
        {
            if (string.IsNullOrEmpty(tokens.AccessToken))
            {
                throw new ArgumentException("Access token is required for Facebook Graph API");
            }

            try
            {
                _logger.LogDebug("Retrieving user information from Facebook Graph API");
                
                // Get basic user info
                var userInfo = await GetBasicUserInfo(tokens.AccessToken);
                
                // Get additional business info if business login is enabled
                var businessInfo = await GetBusinessInfo(tokens.AccessToken);
                
                // Validate business permissions if configured
                if (businessInfo != null && _options.Business?.ValidateBusinessPermissions == true)
                {
                    if (!ValidateBusinessRole(businessInfo))
                    {
                        throw new UnauthorizedAccessException($"User does not have required business role: {_options.Business.RequiredBusinessRole}");
                    }
                }
                
                // Get Instagram accounts if permission granted
                var instagramAccounts = await GetInstagramAccounts(tokens.AccessToken);
                
                // Build comprehensive claims dictionary
                var claims = BuildUserClaims(userInfo, businessInfo, instagramAccounts);
                
                var result = new UserInfo
                {
                    UserId = userInfo?.id ?? string.Empty,
                    Email = userInfo?.email ?? string.Empty,
                    DisplayName = userInfo?.name ?? $"{userInfo?.first_name} {userInfo?.last_name}".Trim(),
                    FirstName = userInfo?.first_name ?? string.Empty,
                    LastName = userInfo?.last_name ?? string.Empty,
                    ProfilePictureUrl = userInfo?.picture?.data?.url ?? string.Empty,
                    IsAuthenticated = true,
                    AuthProvider = ProviderName,
                    LastLoginDate = DateTimeOffset.UtcNow,
                    Claims = claims,
                    LinkedAccounts = Array.Empty<UserAccount>()
                };
                
                _logger.LogDebug("Successfully retrieved Facebook user info for user {UserId}", result.UserId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user info from Facebook Graph API");
                throw;
            }
        }

        #region IEAuthProvider Implementation

        /// <summary>
        /// Generates Facebook login URL - delegates to GetAuthorizationUrlAsync
        /// Facebook OAuth follows standard flow with app-specific scopes and permissions
        /// </summary>
        public async Task<string> GetLoginUrlAsync(string? returnUrl = null, Dictionary<string, string>? parameters = null)
        {
            return await GetAuthorizationUrlAsync(returnUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles Facebook OAuth callback with token exchange and user data retrieval
        /// Returns standardized response with Facebook-specific error codes and messages
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
                    Message = "Facebook authentication successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Facebook authentication callback");
                return new EAuthResponse<UserInfo>
                {
                    Success = false,
                    ErrorCode = "FACEBOOK_AUTH_ERROR",
                    Message = "Facebook authentication failed"
                };
            }
        }

        /// <summary>
        /// Returns local logout redirect URL for Facebook authentication
        /// Facebook doesn't provide centralized logout URL - applications manage sessions locally
        /// </summary>
        public async Task<string> GetLogoutUrlAsync(string? returnUrl = null)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return returnUrl ?? "/";
        }

        /// <summary>
        /// Facebook doesn't support direct password reset URLs
        /// Users must reset passwords through Facebook's own account recovery process
        /// </summary>
        public async Task<string?> GetPasswordResetUrlAsync(string email)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return null; // Facebook doesn't support direct password reset
        }

        /// <summary>
        /// Validates Facebook OAuth configuration including AppId and AppSecret
        /// Enhanced v2.4.0 validation with business features and security checks
        /// </summary>
        public async Task<bool> ValidateConfigurationAsync()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            if (!IsEnabled) return true; // Skip validation if disabled

            // Validate required AppId
            if (string.IsNullOrEmpty(_options.AppId))
            {
                _logger.LogError("Facebook AppId is not configured");
                return false;
            }

            // SECURITY: Use unified configuration service to validate app secret
            try
            {
                var appSecret = _configurationService.GetRequiredSecretValue(
                    "Facebook:AppSecret",
                    "FACEBOOK_APP_SECRET");

                if (string.IsNullOrEmpty(appSecret))
                {
                    _logger.LogError("Facebook AppSecret is not configured");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Facebook AppSecret validation failed");
                return false;
            }

            // Validate scopes configuration
            if (_options.Scopes?.Any() == true)
            {
                var invalidScopes = _options.Scopes.Where(s => string.IsNullOrWhiteSpace(s)).ToArray();
                if (invalidScopes.Any())
                {
                    _logger.LogError("Facebook configuration contains empty scopes");
                    return false;
                }
            }

            // Validate business configuration if enabled
            if (_options.Business?.EnableBusinessLogin == true)
            {
                if (string.IsNullOrEmpty(_options.Business.BusinessId))
                {
                    _logger.LogWarning("Business login enabled but BusinessId not configured");
                }

                if (_options.Business.Scopes?.Any() == true)
                {
                    var invalidBusinessScopes = _options.Business.Scopes.Where(s => string.IsNullOrWhiteSpace(s)).ToArray();
                    if (invalidBusinessScopes.Any())
                    {
                        _logger.LogError("Facebook business configuration contains empty scopes");
                        return false;
                    }
                }

                // Validate business asset limits
                if (_options.Business.MaxPagesLimit <= 0 || _options.Business.MaxPagesLimit > 100)
                {
                    _logger.LogError("Facebook business MaxPagesLimit must be between 1 and 100");
                    return false;
                }

                // Validate business role if permissions validation is enabled
                if (_options.Business.ValidateBusinessPermissions && 
                    string.IsNullOrEmpty(_options.Business.RequiredBusinessRole))
                {
                    _logger.LogError("Business permission validation enabled but RequiredBusinessRole not specified");
                    return false;
                }

                // Log configuration summary
                _logger.LogInformation("Facebook Business Login configured: BusinessId={BusinessId}, Assets={IncludeAssets}, Roles={IncludeRoles}, Validation={Validate}", 
                    !string.IsNullOrEmpty(_options.Business.BusinessId) ? "***" : "None",
                    _options.Business.IncludeBusinessAssets,
                    _options.Business.IncludeBusinessRoles,
                    _options.Business.ValidateBusinessPermissions);
            }

            // Validate Instagram configuration if enabled
            if (_options.Instagram?.EnableInstagramIntegration == true)
            {
                if (_options.Instagram.Scopes?.Any() == true)
                {
                    var invalidInstagramScopes = _options.Instagram.Scopes.Where(s => string.IsNullOrWhiteSpace(s)).ToArray();
                    if (invalidInstagramScopes.Any())
                    {
                        _logger.LogError("Facebook Instagram configuration contains empty scopes");
                        return false;
                    }
                }
            }

            // Validate redirect URI format if configured
            if (!string.IsNullOrEmpty(_options.RedirectUri))
            {
                if (!Uri.TryCreate(_options.RedirectUri, UriKind.Absolute, out var redirectUri))
                {
                    _logger.LogError("Facebook RedirectUri is not a valid absolute URI: {RedirectUri}", 
                        _options.RedirectUri);
                    return false;
                }

                if (redirectUri.Scheme != "https" && !redirectUri.IsLoopback)
                {
                    _logger.LogWarning("Facebook RedirectUri should use HTTPS in production: {RedirectUri}", 
                        _options.RedirectUri);
                }
            }

            // Validate display mode if configured
            if (!string.IsNullOrEmpty(_options.DisplayMode))
            {
                var validDisplayModes = new[] { "page", "popup", "touch", "wap" };
                if (!validDisplayModes.Contains(_options.DisplayMode.ToLower()))
                {
                    _logger.LogError("Invalid Facebook DisplayMode: {DisplayMode}. Valid options: {ValidModes}", 
                        _options.DisplayMode, string.Join(", ", validDisplayModes));
                    return false;
                }
            }

            _logger.LogDebug("Facebook configuration validation successful");
            return true;
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
            // Use configured redirect URI or build from options
            if (!string.IsNullOrEmpty(_options.RedirectUri))
            {
                return _options.RedirectUri;
            }
            
            return "https://localhost/auth/facebook-callback";
        }

        /// <summary>
        /// Gets the requested scopes based on configuration and business features
        /// </summary>
        private string GetRequestedScopes()
        {
            var scopes = new List<string>(_options.Scopes ?? new[] { "email", "public_profile" });
            
            // Add business-specific scopes if business login is enabled
            if (_options.Business?.EnableBusinessLogin == true)
            {
                var businessScopes = _options.Business.Scopes ?? new[]
                {
                    "business_management",
                    "pages_show_list",
                    "pages_read_engagement"
                };
                scopes.AddRange(businessScopes);
            }
            
            // Add Instagram scopes if enabled
            if (_options.Instagram?.EnableInstagramIntegration == true)
            {
                var instagramScopes = _options.Instagram.Scopes ?? new[]
                {
                    "instagram_basic",
                    "instagram_manage_insights"
                };
                scopes.AddRange(instagramScopes);
            }
            
            return string.Join(" ", scopes.Distinct());
        }

        /// <summary>
        /// Retrieves basic user information from Facebook Graph API
        /// </summary>
        private async Task<FacebookUserInfo?> GetBasicUserInfo(string accessToken)
        {
            try
            {
                var fields = "id,email,first_name,last_name,name,picture.type(large),verified";
                var endpoint = $"/{FacebookApiVersion}/me?fields={fields}&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get basic user info: {StatusCode}", response.StatusCode);
                    return null;
                }
                
                return await response.Content.ReadFromJsonAsync<FacebookUserInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic user info from Facebook");
                return null;
            }
        }

        /// <summary>
        /// Retrieves comprehensive business account information if business login is enabled
        /// Enhanced v2.4.0 with business assets, roles, and detailed permissions
        /// </summary>
        private async Task<FacebookBusinessInfo?> GetBusinessInfo(string accessToken)
        {
            if (_options.Business?.EnableBusinessLogin != true)
            {
                return null;
            }
            
            try
            {
                var fields = "id,name,email,verification_status,business";
                
                // Include business assets if configured
                if (_options.Business.IncludeBusinessAssets)
                {
                    fields += ",accounts{id,name,account_status,business_country_code}";
                }
                
                // Include business roles if configured
                if (_options.Business.IncludeBusinessRoles)
                {
                    fields += ",permissions{permission,status}";
                }
                
                var endpoint = $"/{FacebookApiVersion}/me?fields={fields}&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("No business info available for user");
                    return null;
                }
                
                var businessInfo = await response.Content.ReadFromJsonAsync<FacebookBusinessInfo>();
                
                // Get additional business data if available
                if (businessInfo != null && _options.Business.IncludeBusinessAssets)
                {
                    await EnhanceBusinessInfoWithAssets(businessInfo, accessToken);
                }
                
                return businessInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving business info from Facebook");
                return null;
            }
        }

        /// <summary>
        /// Retrieves Instagram accounts if Instagram integration is enabled
        /// </summary>
        private async Task<FacebookInstagramAccount[]> GetInstagramAccounts(string accessToken)
        {
            if (_options.Instagram?.EnableInstagramIntegration != true)
            {
                return Array.Empty<FacebookInstagramAccount>();
            }
            
            try
            {
                var fields = "instagram_business_account{id,name,username,profile_picture_url}";
                var endpoint = $"/{FacebookApiVersion}/me/accounts?fields={fields}&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("No Instagram accounts available for user");
                    return Array.Empty<FacebookInstagramAccount>();
                }
                
                var data = await response.Content.ReadFromJsonAsync<FacebookInstagramResponse>();
                return data?.Data ?? Array.Empty<FacebookInstagramAccount>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Instagram accounts from Facebook");
                return Array.Empty<FacebookInstagramAccount>();
            }
        }

        /// <summary>
        /// Enhances business info with additional asset information
        /// </summary>
        private async Task EnhanceBusinessInfoWithAssets(FacebookBusinessInfo businessInfo, string accessToken)
        {
            try
            {
                // Get business pages if business ID is available
                if (!string.IsNullOrEmpty(businessInfo.business?.id))
                {
                    await GetBusinessPages(businessInfo, accessToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not enhance business info with assets");
            }
        }

        /// <summary>
        /// Retrieves business pages associated with the business account
        /// </summary>
        private async Task GetBusinessPages(FacebookBusinessInfo businessInfo, string accessToken)
        {
            try
            {
                var limit = _options.Business?.MaxPagesLimit ?? 25;
                var fields = "id,name,access_token,category,verification_status,about,fan_count";
                var endpoint = $"/{FacebookApiVersion}/me/accounts?fields={fields}&limit={limit}&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var pagesResponse = await response.Content.ReadFromJsonAsync<FacebookPagesResponse>();
                    businessInfo.pages = pagesResponse?.data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not retrieve business pages");
            }
        }

        /// <summary>
        /// Validates business permissions and roles
        /// </summary>
        private bool ValidateBusinessRole(FacebookBusinessInfo businessInfo)
        {
            if (string.IsNullOrEmpty(_options.Business?.RequiredBusinessRole))
            {
                return true; // No validation required
            }
            
            // Check if user has required business role
            var hasRequiredRole = businessInfo.permissions?.Any(p => 
                p.permission?.Contains(_options.Business.RequiredBusinessRole, StringComparison.OrdinalIgnoreCase) == true &&
                p.status == "granted") ?? false;
                
            if (!hasRequiredRole)
            {
                _logger.LogWarning("User does not have required business role: {RequiredRole}", 
                    _options.Business.RequiredBusinessRole);
            }
            
            return hasRequiredRole;
        }

        /// <summary>
        /// Builds comprehensive claims dictionary from all available user data
        /// Enhanced v2.4.0 with business assets and detailed permissions
        /// </summary>
        private Dictionary<string, string> BuildUserClaims(
            FacebookUserInfo? userInfo,
            FacebookBusinessInfo? businessInfo,
            FacebookInstagramAccount[] instagramAccounts)
        {
            var claims = new Dictionary<string, string>();
            
            if (userInfo != null)
            {
                claims["sub"] = userInfo.id;
                claims["name"] = userInfo.name ?? string.Empty;
                claims["given_name"] = userInfo.first_name ?? string.Empty;
                claims["family_name"] = userInfo.last_name ?? string.Empty;
                claims["email"] = userInfo.email ?? string.Empty;
                claims["picture"] = userInfo.picture?.data?.url ?? string.Empty;
                claims["email_verified"] = userInfo.verified?.ToString().ToLower() ?? "false";
                claims["iss"] = "https://www.facebook.com";
                claims["aud"] = _options.AppId;
                claims["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
            
            if (businessInfo != null)
            {
                claims["business_id"] = businessInfo.id ?? string.Empty;
                claims["business_name"] = businessInfo.name ?? string.Empty;
                claims["business_verified"] = businessInfo.verification_status ?? "false";
                
                // Add business-specific claims
                if (businessInfo.business != null)
                {
                    claims["business_account_id"] = businessInfo.business.id ?? string.Empty;
                    claims["business_account_name"] = businessInfo.business.name ?? string.Empty;
                }
                
                // Add business pages information
                if (businessInfo.pages?.Any() == true)
                {
                    claims["business_pages_count"] = businessInfo.pages.Length.ToString();
                    claims["business_pages"] = JsonSerializer.Serialize(businessInfo.pages.Select(p => new
                    {
                        id = p.id,
                        name = p.name,
                        category = p.category,
                        verified = p.verification_status,
                        fan_count = p.fan_count
                    }));
                }
                
                // Add business permissions
                if (businessInfo.permissions?.Any() == true)
                {
                    claims["business_permissions"] = JsonSerializer.Serialize(businessInfo.permissions.Select(p => new
                    {
                        permission = p.permission,
                        status = p.status
                    }));
                }
                
                // Add business accounts
                if (businessInfo.accounts?.Any() == true)
                {
                    claims["business_accounts_count"] = businessInfo.accounts.Length.ToString();
                    claims["business_accounts"] = JsonSerializer.Serialize(businessInfo.accounts.Select(a => new
                    {
                        id = a.id,
                        name = a.name,
                        status = a.account_status,
                        country = a.business_country_code
                    }));
                }
            }
            
            if (instagramAccounts.Any())
            {
                claims["instagram_accounts"] = JsonSerializer.Serialize(instagramAccounts.Select(a => new
                {
                    id = a.instagram_business_account?.id,
                    name = a.instagram_business_account?.name,
                    username = a.instagram_business_account?.username
                }));
            }
            
            return claims;
        }

        /// <summary>
        /// Exchanges short-lived token for long-lived token
        /// </summary>
        private async Task<FacebookTokenResponse?> ExchangeForLongLivedToken(string shortLivedToken)
        {
            try
            {
                var appSecret = _configurationService.GetRequiredSecretValue(
                    "Facebook:AppSecret", "FACEBOOK_APP_SECRET");
                
                var endpoint = $"/{FacebookApiVersion}/oauth/access_token";
                var parameters = new Dictionary<string, string>
                {
                    ["grant_type"] = "fb_exchange_token",
                    ["client_id"] = _options.AppId,
                    ["client_secret"] = appSecret,
                    ["fb_exchange_token"] = shortLivedToken
                };
                
                var response = await _httpClient.PostAsync(endpoint, 
                    new FormUrlEncodedContent(parameters));
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to exchange for long-lived token: {StatusCode}", 
                        response.StatusCode);
                    return null;
                }
                
                return await response.Content.ReadFromJsonAsync<FacebookTokenResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging for long-lived token");
                return null;
            }
        }

        /// <summary>
        /// Attempts to parse Facebook error response
        /// </summary>
        private Task<FacebookErrorResponse?> TryParseErrorResponse(string content)
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<FacebookErrorResponse>(content));
            }
            catch
            {
                return Task.FromResult<FacebookErrorResponse?>(null);
            }
        }

        #endregion


        #region Facebook API Response Models

        private class FacebookTokenResponse
        {
            public string access_token { get; set; } = string.Empty;

            public string token_type { get; set; } = "Bearer";

            public int expires_in { get; set; }
        }

        private class FacebookUserInfo
        {
            public string id { get; set; } = string.Empty;

            public string? email { get; set; }

            public string? first_name { get; set; }

            public string? last_name { get; set; }

            public string? name { get; set; }

            public FacebookPicture? picture { get; set; }

            public bool? verified { get; set; }
        }

        private class FacebookPicture
        {
            public FacebookPictureData? data { get; set; }
        }

        private class FacebookPictureData
        {
            public string? url { get; set; }
        }

        private class FacebookBusinessInfo
        {
            public string? id { get; set; }
            
            public string? name { get; set; }
            
            public string? email { get; set; }
            
            public string? verification_status { get; set; }
            
            public FacebookBusiness? business { get; set; }
            
            public FacebookBusinessAccount[]? accounts { get; set; }
            
            public FacebookBusinessPermission[]? permissions { get; set; }
            
            public FacebookBusinessPage[]? pages { get; set; }
        }

        private class FacebookBusiness
        {
            public string? id { get; set; }
            
            public string? name { get; set; }
        }

        private class FacebookInstagramResponse
        {
            public FacebookInstagramAccount[]? Data { get; set; }
        }

        private class FacebookInstagramAccount
        {
            public FacebookInstagramBusinessAccount? instagram_business_account { get; set; }
        }

        private class FacebookInstagramBusinessAccount
        {
            public string? id { get; set; }
            
            public string? name { get; set; }
            
            public string? username { get; set; }
            
            public string? profile_picture_url { get; set; }
        }

        private class FacebookErrorResponse
        {
            public FacebookError? Error { get; set; }
        }

        private class FacebookError
        {
            public string? Message { get; set; }
            
            public string? Type { get; set; }
            
            public int? Code { get; set; }
        }

        private class FacebookBusinessAccount
        {
            public string? id { get; set; }
            
            public string? name { get; set; }
            
            public string? account_status { get; set; }
            
            public string? business_country_code { get; set; }
        }

        private class FacebookBusinessPermission
        {
            public string? permission { get; set; }
            
            public string? status { get; set; }
        }

        private class FacebookBusinessPage
        {
            public string? id { get; set; }
            
            public string? name { get; set; }
            
            public string? access_token { get; set; }
            
            public string? category { get; set; }
            
            public string? verification_status { get; set; }
            
            public string? about { get; set; }
            
            public int? fan_count { get; set; }
        }

        private class FacebookPagesResponse
        {
            public FacebookBusinessPage[]? data { get; set; }
        }

        #endregion
    }
}
