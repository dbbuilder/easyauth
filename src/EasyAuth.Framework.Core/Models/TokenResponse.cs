namespace EasyAuth.Framework.Core.Models
{
    /// <summary>
    /// OAuth token response model for authentication providers
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
    }
}