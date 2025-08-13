using System.ComponentModel.DataAnnotations;

namespace EasyAuth.Framework.Core.Configuration
{
    /// <summary>
    /// Main configuration options for the EasyAuth Framework
    /// </summary>
    public class EAuthOptions
    {
        public const string ConfigurationSection = "EasyAuth";

        /// <summary>
        /// Database connection string - can be direct value or Key Vault reference
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Azure Key Vault configuration (optional)
        /// </summary>
        public KeyVaultOptions? KeyVault { get; set; }

        /// <summary>
        /// Authentication providers configuration
        /// </summary>
        [Required]
        public AuthProvidersOptions Providers { get; set; } = new();

        /// <summary>
        /// Framework behavior settings
        /// </summary>
        public FrameworkSettings Framework { get; set; } = new();

        /// <summary>
        /// Session configuration
        /// </summary>
        public SessionOptions Session { get; set; } = new();

        /// <summary>
        /// CORS configuration for frontend applications
        /// </summary>
        public CorsOptions Cors { get; set; } = new();
    }

    /// <summary>
    /// Azure Key Vault configuration for secure secret management
    /// </summary>
    public class KeyVaultOptions
    {
        /// <summary>
        /// Base URL of the Azure Key Vault (e.g., https://myvault.vault.azure.net/)
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;
        /// <summary>
        /// Whether to retrieve database connection string from Key Vault
        /// </summary>
        public bool UseConnectionStringFromKeyVault { get; set; } = true;

        /// <summary>
        /// Name of the secret containing the database connection string in Key Vault
        /// </summary>
        public string ConnectionStringSecretName { get; set; } = "EAuthDatabaseConnectionString";

        /// <summary>
        /// Mapping of configuration keys to Key Vault secret names
        /// </summary>
        public Dictionary<string, string> SecretNames { get; set; } = new()
        {
            ["AzureB2C:ClientSecret"] = "EAuthAzureB2CClientSecret",
            ["Google:ClientSecret"] = "EAuthGoogleClientSecret",
            ["Facebook:AppSecret"] = "EAuthFacebookAppSecret",
            ["Apple:ClientSecret"] = "EAuthAppleClientSecret",
            ["Apple:JwtSecret"] = "EAuthAppleJwtSecret"
        };
    }

    public class AuthProvidersOptions
    {
        /// <summary>
        /// Azure AD B2C provider configuration
        /// </summary>
        public AzureB2COptions? AzureB2C { get; set; }

        /// <summary>
        /// Google OAuth provider configuration
        /// </summary>
        public GoogleOptions? Google { get; set; }

        /// <summary>
        /// Facebook OAuth provider configuration
        /// </summary>
        public FacebookOptions? Facebook { get; set; }

        /// <summary>
        /// Apple Sign-In provider configuration
        /// </summary>
        public AppleOptions? Apple { get; set; }

        /// <summary>
        /// Default provider to use for new registrations
        /// </summary>
        public string DefaultProvider { get; set; } = "AzureB2C";

        /// <summary>
        /// Allow account linking across providers
        /// </summary>
        public bool AllowAccountLinking { get; set; } = true;
    }

    /// <summary>
    /// Azure Active Directory B2C authentication provider configuration
    /// </summary>
    public class AzureB2COptions
    {
        /// <summary>
        /// Whether Azure B2C authentication is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Azure B2C instance URL (e.g., https://login.microsoftonline.com/)
        /// </summary>
        [Required]
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// Azure B2C domain (e.g., mycompany.b2clogin.com)
        /// </summary>
        [Required]
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Azure B2C tenant identifier
        /// </summary>
        [Required]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Application (client) ID registered in Azure B2C
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret - will be loaded from Key Vault if configured
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Sign-up and sign-in policy ID (user flow)
        /// </summary>
        [Required]
        public string SignUpSignInPolicyId { get; set; } = string.Empty;

        /// <summary>
        /// Password reset policy ID (user flow)
        /// </summary>
        [Required]
        public string ResetPasswordPolicyId { get; set; } = string.Empty;

        /// <summary>
        /// Profile editing policy ID (user flow)
        /// </summary>
        public string EditProfilePolicyId { get; set; } = string.Empty;

        /// <summary>
        /// OAuth callback path for Azure B2C sign-in
        /// </summary>
        public string CallbackPath { get; set; } = "/auth/azureb2c-signin";

        /// <summary>
        /// Callback path after sign-out
        /// </summary>
        public string SignedOutCallbackPath { get; set; } = "/auth/azureb2c-signout";

        /// <summary>
        /// OAuth scopes to request from Azure B2C
        /// </summary>
        public string[] Scopes { get; set; } = { "openid", "profile", "offline_access" };

        // Additional properties for enhanced B2C support
        public string? CustomDomain { get; set; }
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = true;
        public int ClockSkewSeconds { get; set; } = 300;
        public string[] AdditionalClaims { get; set; } = Array.Empty<string>();

        // Convenience properties for backward compatibility
        public string SignInPolicy => SignUpSignInPolicyId;

        public string? ResetPasswordPolicy => ResetPasswordPolicyId;

        public string? EditProfilePolicy => EditProfilePolicyId;

        /// <summary>
        /// Gets the B2C authority URL based on tenant and custom domain settings
        /// </summary>
        public string GetAuthorityUrl()
        {
            var domain = !string.IsNullOrEmpty(CustomDomain)
                ? CustomDomain
                : $"{GetTenantName()}.b2clogin.com";

            return $"https://{domain}/{TenantId}";
        }

        /// <summary>
        /// Gets the B2C authorization endpoint for the specified policy
        /// </summary>
        public string GetAuthorizationEndpoint(string? policy = null)
        {
            var policyName = policy ?? SignUpSignInPolicyId;
            return $"{GetAuthorityUrl()}/oauth2/v2.0/authorize?p={policyName}";
        }

        /// <summary>
        /// Gets the B2C token endpoint for the specified policy
        /// </summary>
        public string GetTokenEndpoint(string? policy = null)
        {
            var policyName = policy ?? SignUpSignInPolicyId;
            return $"{GetAuthorityUrl()}/oauth2/v2.0/token?p={policyName}";
        }

        /// <summary>
        /// Gets the tenant name from TenantId (removes .onmicrosoft.com if present)
        /// </summary>
        public string GetTenantName()
        {
            return TenantId.Replace(".onmicrosoft.com", "");
        }
    }

    /// <summary>
    /// Google OAuth 2.0 authentication provider configuration
    /// </summary>
    public class GoogleOptions
    {
        /// <summary>
        /// Whether Google authentication is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Google OAuth client ID from Google Cloud Console
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Google OAuth client secret - will be loaded from Key Vault if configured
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth callback path for Google sign-in
        /// </summary>
        public string CallbackPath { get; set; } = "/auth/google-signin";

        /// <summary>
        /// OAuth scopes to request from Google
        /// </summary>
        public string[] Scopes { get; set; } = { "openid", "profile", "email" };
    }

    /// <summary>
    /// Facebook OAuth authentication provider configuration
    /// Enhanced v2.4.0 with API v19+, business features, and Instagram integration
    /// </summary>
    public class FacebookOptions
    {
        /// <summary>
        /// Whether Facebook authentication is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Facebook App ID from Facebook Developers Console
        /// </summary>
        [Required]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Facebook App Secret - will be loaded from Key Vault if configured
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth callback path for Facebook sign-in
        /// </summary>
        public string CallbackPath { get; set; } = "/auth/facebook-signin";

        /// <summary>
        /// Custom redirect URI for Facebook authentication
        /// If not specified, will use default based on host
        /// </summary>
        public string RedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// OAuth scopes to request from Facebook
        /// </summary>
        public string[] Scopes { get; set; } = { "email", "public_profile" };

        /// <summary>
        /// Whether to use long-lived access tokens (60 days vs 2 hours)
        /// </summary>
        public bool UseLongLivedTokens { get; set; } = true;

        /// <summary>
        /// Whether to use mock tokens when Facebook service is unavailable (development only)
        /// </summary>
        public bool UseMockTokensOnFailure { get; set; } = false;

        /// <summary>
        /// Display mode for Facebook login dialog
        /// Valid values: page, popup, touch, wap
        /// </summary>
        public string DisplayMode { get; set; } = "page";

        /// <summary>
        /// Locale for Facebook login dialog
        /// </summary>
        public string Locale { get; set; } = "en_US";

        /// <summary>
        /// Facebook Business Login configuration
        /// </summary>
        public FacebookBusinessOptions? Business { get; set; }

        /// <summary>
        /// Instagram integration configuration
        /// </summary>
        public FacebookInstagramOptions? Instagram { get; set; }
    }

    /// <summary>
    /// Facebook Business Login configuration options
    /// Enhanced v2.4.0 with comprehensive business asset management
    /// </summary>
    public class FacebookBusinessOptions
    {
        /// <summary>
        /// Whether to enable Facebook Business Login features
        /// </summary>
        public bool EnableBusinessLogin { get; set; } = false;

        /// <summary>
        /// Facebook Business ID for business login
        /// </summary>
        public string BusinessId { get; set; } = string.Empty;

        /// <summary>
        /// Business-specific scopes to request
        /// </summary>
        public string[] Scopes { get; set; } = { "business_management", "pages_show_list", "pages_read_engagement" };

        /// <summary>
        /// Whether to retrieve detailed business asset information (Pages, Ad Accounts)
        /// </summary>
        public bool IncludeBusinessAssets { get; set; } = true;

        /// <summary>
        /// Whether to include business roles and permissions in claims
        /// </summary>
        public bool IncludeBusinessRoles { get; set; } = true;

        /// <summary>
        /// Maximum number of business pages to retrieve per request
        /// </summary>
        public int MaxPagesLimit { get; set; } = 25;

        /// <summary>
        /// Whether to validate business permissions on each request
        /// </summary>
        public bool ValidateBusinessPermissions { get; set; } = false;

        /// <summary>
        /// Required business role level (Admin, Editor, Analyst, etc.)
        /// </summary>
        public string? RequiredBusinessRole { get; set; }
    }

    /// <summary>
    /// Facebook Instagram integration configuration options
    /// </summary>
    public class FacebookInstagramOptions
    {
        /// <summary>
        /// Whether to enable Instagram integration
        /// </summary>
        public bool EnableInstagramIntegration { get; set; } = false;

        /// <summary>
        /// Instagram-specific scopes to request
        /// </summary>
        public string[] Scopes { get; set; } = { "instagram_basic", "instagram_manage_insights" };
    }

    /// <summary>
    /// Apple Sign-In authentication provider configuration
    /// Enhanced v2.4.0 with comprehensive token validation and private email relay support
    /// </summary>
    public class AppleOptions
    {
        /// <summary>
        /// Whether Apple Sign-In authentication is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Apple Services ID (client identifier)
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Apple Developer Team ID
        /// </summary>
        [Required]
        public string TeamId { get; set; } = string.Empty;

        /// <summary>
        /// Apple Key ID for JWT signing
        /// </summary>
        [Required]
        public string KeyId { get; set; } = string.Empty;

        /// <summary>
        /// Apple private key in PKCS#8 format (Base64 encoded)
        /// Required for production ES256 JWT signing
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Client secret - will be loaded from Key Vault if configured
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// JWT signing secret for Apple Sign-In token generation and validation
        /// SECURITY: This must come from environment variables or Key Vault - NEVER hardcode in production
        /// </summary>
        public string JwtSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth callback path for Apple Sign-In
        /// </summary>
        public string CallbackPath { get; set; } = "/auth/apple-signin";

        /// <summary>
        /// Custom redirect URI for Apple authentication
        /// If not specified, will use default based on host
        /// </summary>
        public string RedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// OAuth scopes to request from Apple
        /// </summary>
        public string[] Scopes { get; set; } = { "name", "email" };

        /// <summary>
        /// Whether to use mock tokens when Apple service is unavailable (development only)
        /// </summary>
        public bool UseMockTokensOnFailure { get; set; } = false;

        /// <summary>
        /// Token validation settings
        /// </summary>
        public AppleTokenValidationOptions TokenValidation { get; set; } = new();

        /// <summary>
        /// Private email relay handling settings
        /// </summary>
        public ApplePrivateEmailOptions PrivateEmail { get; set; } = new();

        /// <summary>
        /// Web vs native app flow settings
        /// </summary>
        public AppleFlowOptions Flow { get; set; } = new();
    }

    /// <summary>
    /// Apple token validation configuration
    /// </summary>
    public class AppleTokenValidationOptions
    {
        /// <summary>
        /// Whether to validate token signatures against Apple's public keys
        /// </summary>
        public bool ValidateSignature { get; set; } = true;

        /// <summary>
        /// Whether to validate token issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate token audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token expiration
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Clock skew tolerance in seconds
        /// </summary>
        public int ClockSkewSeconds { get; set; } = 300;

        /// <summary>
        /// Cache duration for Apple public keys in minutes
        /// </summary>
        public int KeyCacheDurationMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Apple private email relay configuration
    /// </summary>
    public class ApplePrivateEmailOptions
    {
        /// <summary>
        /// Whether to handle private email relay addresses
        /// </summary>
        public bool HandlePrivateRelay { get; set; } = true;

        /// <summary>
        /// Whether to log when private relay emails are detected
        /// </summary>
        public bool LogPrivateRelayDetection { get; set; } = true;

        /// <summary>
        /// Custom prefix for private relay user display names
        /// </summary>
        public string PrivateUserDisplayPrefix { get; set; } = "Apple User";

        /// <summary>
        /// Whether to store private relay emails in database
        /// </summary>
        public bool StorePrivateRelayEmails { get; set; } = true;
    }

    /// <summary>
    /// Apple Sign-In flow configuration for web vs native apps
    /// </summary>
    public class AppleFlowOptions
    {
        /// <summary>
        /// Default flow type for Apple Sign-In
        /// </summary>
        public AppleFlowType DefaultFlow { get; set; } = AppleFlowType.Web;

        /// <summary>
        /// Response mode for Apple authentication
        /// </summary>
        public string ResponseMode { get; set; } = "form_post";

        /// <summary>
        /// Whether to include nonce in authentication requests
        /// </summary>
        public bool IncludeNonce { get; set; } = true;

        /// <summary>
        /// Native app configuration (for iOS/macOS apps)
        /// </summary>
        public AppleNativeAppOptions? NativeApp { get; set; }
    }

    /// <summary>
    /// Apple Sign-In flow types
    /// </summary>
    public enum AppleFlowType
    {
        /// <summary>
        /// Web browser-based flow
        /// </summary>
        Web,

        /// <summary>
        /// Native iOS/macOS app flow
        /// </summary>
        Native,

        /// <summary>
        /// Hybrid flow supporting both web and native
        /// </summary>
        Hybrid
    }

    /// <summary>
    /// Native app configuration for Apple Sign-In
    /// </summary>
    public class AppleNativeAppOptions
    {
        /// <summary>
        /// iOS bundle identifier
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// macOS bundle identifier
        /// </summary>
        public string? MacOsBundleId { get; set; }

        /// <summary>
        /// Custom URL scheme for native app callbacks
        /// </summary>
        public string? CustomUrlScheme { get; set; }

        /// <summary>
        /// Whether to support universal links
        /// </summary>
        public bool SupportUniversalLinks { get; set; } = true;
    }

    /// <summary>
    /// EasyAuth Framework behavior and feature settings
    /// </summary>
    public class FrameworkSettings
    {
        /// <summary>
        /// Whether to automatically set up database schema on startup
        /// </summary>
        public bool AutoDatabaseSetup { get; set; } = true;

        /// <summary>
        /// Whether to enable Swagger API documentation
        /// </summary>
        public bool EnableSwagger { get; set; } = true;

        /// <summary>
        /// Whether to enable ASP.NET Core health checks
        /// </summary>
        public bool EnableHealthChecks { get; set; } = true;

        /// <summary>
        /// API route prefix for EasyAuth endpoints
        /// </summary>
        public string ApiPrefix { get; set; } = "api/eauth";

        /// <summary>
        /// Default token expiration time in minutes
        /// </summary>
        public int TokenExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to include detailed error information in responses (disable in production)
        /// </summary>
        public bool EnableDetailedErrors { get; set; } = false;

        /// <summary>
        /// Whether to enable audit logging of authentication events
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;

        /// <summary>
        /// Interval in minutes for automatic session cleanup
        /// </summary>
        public int SessionCleanupIntervalMinutes { get; set; } = 60;
    }

    /// <summary>
    /// User session and cookie configuration options
    /// </summary>
    public class SessionOptions
    {
        /// <summary>
        /// Session idle timeout in hours before automatic expiration
        /// </summary>
        public int IdleTimeoutHours { get; set; } = 24;

        /// <summary>
        /// Whether session cookies should be HTTP-only (recommended for security)
        /// </summary>
        public bool HttpOnly { get; set; } = true;

        /// <summary>
        /// Whether session cookies require HTTPS (recommended for production)
        /// </summary>
        public bool Secure { get; set; } = true;

        /// <summary>
        /// SameSite cookie attribute for CSRF protection
        /// </summary>
        public string SameSite { get; set; } = "Lax";

        /// <summary>
        /// Name of the session cookie
        /// </summary>
        public string CookieName { get; set; } = "EAuth.Session";

        /// <summary>
        /// Whether to use sliding expiration (session timeout resets on activity)
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;
    }

    /// <summary>
    /// Cross-Origin Resource Sharing (CORS) configuration for frontend applications
    /// </summary>
    public class CorsOptions
    {
        /// <summary>
        /// List of allowed origin URLs for CORS requests
        /// </summary>
        public string[] AllowedOrigins { get; set; } = { "http://localhost:3000", "http://localhost:5173" };

        /// <summary>
        /// Whether to allow credentials (cookies, authorization headers) in CORS requests
        /// </summary>
        public bool AllowCredentials { get; set; } = true;

        /// <summary>
        /// List of allowed HTTP methods for CORS requests
        /// </summary>
        public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

        /// <summary>
        /// List of allowed headers for CORS requests
        /// </summary>
        public string[] AllowedHeaders { get; set; } = { "*" };
    }
}
