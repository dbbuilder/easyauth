using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Service interface for automatic database setup and migration
    /// </summary>
    public interface IEAuthDatabaseService
    {
        /// <summary>
        /// Initialize the database with required tables and stored procedures
        /// </summary>
        Task<bool> InitializeDatabaseAsync();

        /// <summary>
        /// Check if the database is properly initialized
        /// </summary>
        Task<bool> IsDatabaseInitializedAsync();

        /// <summary>
        /// Apply any pending migrations
        /// </summary>
        Task<bool> ApplyMigrationsAsync();

        /// <summary>
        /// Get the current database version
        /// </summary>
        Task<string> GetDatabaseVersionAsync();

        /// <summary>
        /// Clean up expired sessions and audit logs
        /// </summary>
        Task<int> CleanupExpiredDataAsync();

        /// <summary>
        /// Validate a session token and return session information
        /// </summary>
        Task<SessionInfo> ValidateSessionAsync(string sessionId);

        /// <summary>
        /// Invalidate a session token (logout)
        /// </summary>
        Task<bool> InvalidateSessionAsync(string sessionId);
    }
}
