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

    public class KeyVaultOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public bool UseConnectionStringFromKeyVault { get; set; } = true;
        public string ConnectionStringSecretName { get; set; } = "EAuthDatabaseConnectionString";
        public Dictionary<string, string> SecretNames { get; set; } = new()
        {
            ["AzureB2C:ClientSecret"] = "EAuthAzureB2CClientSecret",
            ["Google:ClientSecret"] = "EAuthGoogleClientSecret",
            ["Facebook:AppSecret"] = "EAuthFacebookAppSecret",
            ["Apple:ClientSecret"] = "EAuthAppleClientSecret"
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

    public class AzureB2COptions
    {
        public bool Enabled { get; set; } = true;
        [Required]
        public string Instance { get; set; } = string.Empty;
        [Required]
        public string Domain { get; set; } = string.Empty;
        [Required]
        public string TenantId { get; set; } = string.Empty;
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty; // Will be loaded from Key Vault if configured
        [Required]
        public string SignUpSignInPolicyId { get; set; } = string.Empty;
        [Required]
        public string ResetPasswordPolicyId { get; set; } = string.Empty;
        public string EditProfilePolicyId { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/auth/azureb2c-signin";
        public string SignedOutCallbackPath { get; set; } = "/auth/azureb2c-signout";
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

    public class GoogleOptions
    {
        public bool Enabled { get; set; } = true;
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty; // Will be loaded from Key Vault if configured
        public string CallbackPath { get; set; } = "/auth/google-signin";
        public string[] Scopes { get; set; } = { "openid", "profile", "email" };
    }
    public class FacebookOptions
    {
        public bool Enabled { get; set; } = true;
        [Required]
        public string AppId { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty; // Will be loaded from Key Vault if configured
        public string CallbackPath { get; set; } = "/auth/facebook-signin";
        public string[] Scopes { get; set; } = { "email", "public_profile" };
    }

    public class AppleOptions
    {
        public bool Enabled { get; set; } = true;
        [Required]
        public string ClientId { get; set; } = string.Empty;
        [Required]
        public string TeamId { get; set; } = string.Empty;
        [Required]
        public string KeyId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty; // Will be loaded from Key Vault if configured
        public string CallbackPath { get; set; } = "/auth/apple-signin";
        public string[] Scopes { get; set; } = { "name", "email" };
    }

    public class FrameworkSettings
    {
        public bool AutoDatabaseSetup { get; set; } = true;
        public bool EnableSwagger { get; set; } = true;
        public bool EnableHealthChecks { get; set; } = true;
        public string ApiPrefix { get; set; } = "api/eauth";
        public int TokenExpirationMinutes { get; set; } = 60;
        public bool EnableDetailedErrors { get; set; } = false;
        public bool EnableAuditLogging { get; set; } = true;
        public int SessionCleanupIntervalMinutes { get; set; } = 60;
    }

    public class SessionOptions
    {
        public int IdleTimeoutHours { get; set; } = 24;
        public bool HttpOnly { get; set; } = true;
        public bool Secure { get; set; } = true;
        public string SameSite { get; set; } = "Lax";
        public string CookieName { get; set; } = "EAuth.Session";
        public bool SlidingExpiration { get; set; } = true;
    }

    public class CorsOptions
    {
        public string[] AllowedOrigins { get; set; } = { "http://localhost:3000", "http://localhost:5173" };
        public bool AllowCredentials { get; set; } = true;
        public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        public string[] AllowedHeaders { get; set; } = { "*" };
    }
}
