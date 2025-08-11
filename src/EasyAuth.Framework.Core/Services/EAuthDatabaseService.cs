using System.Reflection;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace EasyAuth.Framework.Core.Services
{
    /// <summary>
    /// Implementation of database setup service that auto-configures required database objects
    /// </summary>
    public class EAuthDatabaseService : IEAuthDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<EAuthDatabaseService> _logger;

        public EAuthDatabaseService(string connectionString, ILogger<EAuthDatabaseService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates EasyAuth database schema, tables, and stored procedures from embedded SQL scripts
        /// Executes initialization scripts in sequence and seeds framework metadata
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting EasyAuth database initialization");

                // Execute all setup scripts in order
                var scripts = new[]
                {
                    "create_schema_and_metadata.sql",
                    "create_users_table.sql",
                    "create_user_accounts_table.sql",
                    "create_user_sessions_table.sql",
                    "create_audit_log_table.sql",
                    "create_user_roles_table.sql",
                    "EAuth_UpsertUser.sql",
                    "EAuth_CreateSession.sql",
                    "EAuth_ValidateSession.sql",
                    "EAuth_InvalidateSession.sql",
                    "EAuth_GetUserProfile.sql"
                };

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                foreach (var scriptName in scripts)
                {
                    var script = GetEmbeddedScript(scriptName);
                    if (!string.IsNullOrEmpty(script))
                    {
                        _logger.LogInformation("Executing script: {ScriptName}", scriptName);

                        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - script is from embedded resources, not user input
                        command.CommandText = script;
#pragma warning restore CA2100
                        command.CommandTimeout = 300; // 5 minutes
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }

                // Insert initial metadata
                await SeedInitialDataAsync(connection).ConfigureAwait(false);

                _logger.LogInformation("EasyAuth database initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EasyAuth database initialization");
                return false;
            }
        }

        /// <summary>
        /// Checks if EasyAuth database is properly initialized by verifying framework_metadata table exists
        /// Returns true if core schema and metadata tables are present in the database
        /// </summary>
        public async Task<bool> IsDatabaseInitializedAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'eauth'
                    AND TABLE_NAME = 'framework_metadata'";

                var count = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking EasyAuth database initialization status");
                return false;
            }
        }

        /// <summary>
        /// Applies pending database migrations to upgrade schema to current version
        /// Checks current version and executes version-specific upgrade scripts
        /// </summary>
        public async Task<bool> ApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Applying EasyAuth database migrations");

                var currentVersion = await GetDatabaseVersionAsync().ConfigureAwait(false);
                _logger.LogInformation("Current database version: {Version}", currentVersion);

                // Add future migration logic here

                _logger.LogInformation("EasyAuth database migrations completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying EasyAuth database migrations");
                return false;
            }
        }

        /// <summary>
        /// Retrieves current database schema version from framework_metadata table
        /// Returns '0.0.0' if metadata table doesn't exist or version is not recorded
        /// </summary>
        public async Task<string> GetDatabaseVersionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TOP 1 [value]
                    FROM [eauth].[framework_metadata]
                    WHERE [key] = 'database_version'
                    ORDER BY [created_date] DESC";

                var version = await command.ExecuteScalarAsync().ConfigureAwait(false) as string;
                return version ?? "0.0.0";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting EasyAuth database version, assuming initial state");
                return "0.0.0";
            }
        }

        /// <summary>
        /// Removes expired sessions and old audit log entries to maintain database performance
        /// Deactivates sessions past expiry time and deletes audit logs older than 90 days
        /// </summary>
        public async Task<int> CleanupExpiredDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting EasyAuth data cleanup");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                var totalCleaned = 0;

                // Clean up expired sessions
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE [eauth].[user_sessions]
                        SET is_active = 0, invalidated_date = GETUTCDATE(), invalidated_reason = 'EXPIRED'
                        WHERE is_active = 1 AND expires_at < GETUTCDATE()";

                    var expiredSessions = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    totalCleaned += expiredSessions;
                    _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions);
                }

                // Clean up old audit logs (older than 90 days)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        DELETE FROM [eauth].[audit_log]
                        WHERE created_date < DATEADD(day, -90, GETUTCDATE())";

                    var oldAuditLogs = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    totalCleaned += oldAuditLogs;
                    _logger.LogInformation("Cleaned up {Count} old audit log entries", oldAuditLogs);
                }

                _logger.LogInformation("EasyAuth data cleanup completed. Total records cleaned: {Count}", totalCleaned);
                return totalCleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EasyAuth data cleanup");
                return 0;
            }
        }

        private string GetEmbeddedScript(string scriptName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"EasyAuth.Framework.Core.Database.Scripts.{scriptName}";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning("Embedded script not found: {ScriptName}", scriptName);
                    return string.Empty;
                }

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading embedded script: {ScriptName}", scriptName);
                return string.Empty;
            }
        }

        private async Task SeedInitialDataAsync(SqlConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM [eauth].[framework_metadata] WHERE [key] = 'database_version')
                    BEGIN
                        INSERT INTO [eauth].[framework_metadata] ([key], [value], [version], [created_date])
                        VALUES
                            ('database_version', '1.0.0', '1.0.0', GETUTCDATE()),
                            ('initialization_date', CONVERT(NVARCHAR, GETUTCDATE(), 127), '1.0.0', GETUTCDATE()),
                            ('framework_name', 'EasyAuth.Framework', '1.0.0', GETUTCDATE())
                    END";

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                _logger.LogInformation("Initial metadata seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding initial data");
                throw;
            }
        }

        /// <summary>
        /// Validates session token against database and returns session information if active
        /// TDD RED phase stub - will query user_sessions table and verify expiry in GREEN phase
        /// </summary>
        public async Task<SessionInfo> ValidateSessionAsync(string sessionId)
        {
            // TDD RED Phase - Stub implementation
            // Will be properly implemented in GREEN phase after tests define behavior
            await Task.CompletedTask.ConfigureAwait(false);
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }

        /// <summary>
        /// Marks user session as inactive in database for logout functionality
        /// TDD RED phase stub - will update user_sessions table with invalidation timestamp in GREEN phase
        /// </summary>
        public async Task<bool> InvalidateSessionAsync(string sessionId)
        {
            // TDD RED Phase - Stub implementation
            // Will be properly implemented in GREEN phase after tests define behavior
            await Task.CompletedTask.ConfigureAwait(false);
            throw new NotImplementedException("TDD RED Phase - Test first, implement later");
        }
    }
}
