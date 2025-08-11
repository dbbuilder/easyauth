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
        /// OAuth scopes to request from Facebook
        /// </summary>
        public string[] Scopes { get; set; } = { "email", "public_profile" };
    }

    /// <summary>
    /// Apple Sign-In authentication provider configuration
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
        /// OAuth scopes to request from Apple
        /// </summary>
        public string[] Scopes { get; set; } = { "name", "email" };
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
