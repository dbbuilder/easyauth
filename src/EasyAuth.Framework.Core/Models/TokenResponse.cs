namespace EasyAuth.Framework.Core.Models
{
    /// <summary>
    /// OAuth token response model for authentication providers
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// OAuth access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;
        /// <summary>
        /// OpenID Connect ID token (JWT)
        /// </summary>
        public string IdToken { get; set; } = string.Empty;
        /// <summary>
        /// OAuth refresh token for token renewal
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
        /// <summary>
        /// Token type (typically "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";
        /// <summary>
        /// Token lifetime in seconds
        /// </summary>
        public int ExpiresIn { get; set; }
        /// <summary>
        /// OAuth scopes granted for this token
        /// </summary>
        public string Scope { get; set; } = string.Empty;
        /// <summary>
        /// When the token was issued
        /// </summary>
        public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// When the token expires (calculated from IssuedAt + ExpiresIn)
        /// </summary>
        public DateTimeOffset ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
    }
}