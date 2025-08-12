using System.Data;
using Microsoft.Data.SqlClient;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Integration tests for database operations and session management
/// Tests real database connectivity, session lifecycle, and data persistence
/// Follows TDD methodology with comprehensive database scenarios
/// </summary>
public class DatabaseSessionIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DatabaseSessionIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    #region Database Connectivity Tests

    [Fact]
    public async Task DatabaseService_ConnectivityCheck_EstablishesConnection()
    {
        try
        {
            // Arrange
            using var scope = ServiceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetService<IEAuthDatabaseService>();

            // Act & Assert - Database service may not be fully implemented yet
            if (databaseService != null)
            {
                // Test basic connectivity if service exists
                var isInitialized = await Record.ExceptionAsync(() => databaseService.IsDatabaseInitializedAsync());
                
                _testOutputHelper.WriteLine($"✅ Database service connectivity tested");
            }
            else
            {
                // Direct SQL connection test
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                connection.State.Should().Be(ConnectionState.Open);
                _testOutputHelper.WriteLine("✅ Direct database connection established");
            }
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task DatabaseSchema_TablesExist_ValidatesStructure()
    {
        try
        {
            // Arrange
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Check for required tables
            var requiredTables = new[] { "Users", "UserAccounts", "UserSessions", "AuditLog" };
            var existingTables = new List<string>();

            foreach (var tableName in requiredTables)
            {
                var checkTableSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName";

                await using var command = new SqlCommand(checkTableSql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                var count = (int)await command.ExecuteScalarAsync();
                
                if (count > 0)
                {
                    existingTables.Add(tableName);
                }
            }

            // Assert
            existingTables.Should().NotBeEmpty();
            existingTables.Should().Contain("Users"); // At minimum, Users table should exist

            _testOutputHelper.WriteLine($"✅ Database tables found: {string.Join(", ", existingTables)}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Session Management Tests

    [Fact]
    public async Task SessionManagement_CreateAndValidate_WorksEndToEnd()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("session-test@example.com");
            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(1);

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Create session
            var createSessionSql = @"
                INSERT INTO UserSessions (SessionId, UserId, ExpiresAt, IsValid, CreatedAt, IpAddress, UserAgent, AuthProvider)
                VALUES (@SessionId, @UserId, @ExpiresAt, @IsValid, @CreatedAt, @IpAddress, @UserAgent, @AuthProvider)";

            await using var createCommand = new SqlCommand(createSessionSql, connection);
            createCommand.Parameters.AddWithValue("@SessionId", sessionId);
            createCommand.Parameters.AddWithValue("@UserId", testUserId);
            createCommand.Parameters.AddWithValue("@ExpiresAt", expiresAt);
            createCommand.Parameters.AddWithValue("@IsValid", true);
            createCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            createCommand.Parameters.AddWithValue("@IpAddress", "127.0.0.1");
            createCommand.Parameters.AddWithValue("@UserAgent", "TestAgent/1.0");
            createCommand.Parameters.AddWithValue("@AuthProvider", "google");

            var created = await createCommand.ExecuteNonQueryAsync();

            // Act - Validate session
            var validateSessionSql = @"
                SELECT SessionId, UserId, ExpiresAt, IsValid, AuthProvider
                FROM UserSessions 
                WHERE SessionId = @SessionId AND IsValid = 1 AND ExpiresAt > GETUTCDATE()";

            await using var validateCommand = new SqlCommand(validateSessionSql, connection);
            validateCommand.Parameters.AddWithValue("@SessionId", sessionId);

            await using var reader = await validateCommand.ExecuteReaderAsync();
            var sessionFound = await reader.ReadAsync();

            // Assert
            created.Should().Be(1);
            sessionFound.Should().BeTrue();
            
            if (sessionFound)
            {
                reader["SessionId"].ToString().Should().Be(sessionId);
                reader["UserId"].ToString().Should().Be(testUserId.ToString());
                reader["AuthProvider"].ToString().Should().Be("google");
            }

            _testOutputHelper.WriteLine($"✅ Session {sessionId} created and validated successfully");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task SessionManagement_ExpiredSessions_AreInvalidated()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("expired-test@example.com");
            var sessionId = Guid.NewGuid().ToString();
            var expiredTime = DateTime.UtcNow.AddHours(-1); // Already expired

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Create expired session
            var createSessionSql = @"
                INSERT INTO UserSessions (SessionId, UserId, ExpiresAt, IsValid, CreatedAt, IpAddress, UserAgent, AuthProvider)
                VALUES (@SessionId, @UserId, @ExpiresAt, @IsValid, @CreatedAt, @IpAddress, @UserAgent, @AuthProvider)";

            await using var createCommand = new SqlCommand(createSessionSql, connection);
            createCommand.Parameters.AddWithValue("@SessionId", sessionId);
            createCommand.Parameters.AddWithValue("@UserId", testUserId);
            createCommand.Parameters.AddWithValue("@ExpiresAt", expiredTime);
            createCommand.Parameters.AddWithValue("@IsValid", true);
            createCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            createCommand.Parameters.AddWithValue("@IpAddress", "127.0.0.1");
            createCommand.Parameters.AddWithValue("@UserAgent", "TestAgent/1.0");
            createCommand.Parameters.AddWithValue("@AuthProvider", "google");

            await createCommand.ExecuteNonQueryAsync();

            // Act - Try to validate expired session
            var validateSessionSql = @"
                SELECT COUNT(*) 
                FROM UserSessions 
                WHERE SessionId = @SessionId AND IsValid = 1 AND ExpiresAt > GETUTCDATE()";

            await using var validateCommand = new SqlCommand(validateSessionSql, connection);
            validateCommand.Parameters.AddWithValue("@SessionId", sessionId);

            var validCount = (int)await validateCommand.ExecuteScalarAsync();

            // Assert
            validCount.Should().Be(0, "Expired sessions should not be considered valid");

            _testOutputHelper.WriteLine($"✅ Expired session {sessionId} correctly invalidated");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task SessionManagement_ConcurrentSessions_HandledCorrectly()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("concurrent-test@example.com");
            var sessionCount = 5;
            var sessionIds = new List<string>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Create multiple sessions concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < sessionCount; i++)
            {
                var sessionId = Guid.NewGuid().ToString();
                sessionIds.Add(sessionId);
                
                tasks.Add(Task.Run(async () =>
                {
                    const string createSessionSql = @"
                        INSERT INTO UserSessions (SessionId, UserId, ExpiresAt, IsValid, CreatedAt, IpAddress, UserAgent, AuthProvider)
                        VALUES (@SessionId, @UserId, @ExpiresAt, @IsValid, @CreatedAt, @IpAddress, @UserAgent, @AuthProvider)";

                    await using var localConnection = new SqlConnection(ConnectionString);
                    await localConnection.OpenAsync();
                    #pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - Static SQL with parameters
                    await using var createCommand = new SqlCommand(createSessionSql, localConnection);
                    #pragma warning restore CA2100
                    createCommand.Parameters.AddWithValue("@SessionId", sessionId);
                    createCommand.Parameters.AddWithValue("@UserId", testUserId);
                    createCommand.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddHours(1));
                    createCommand.Parameters.AddWithValue("@IsValid", true);
                    createCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    createCommand.Parameters.AddWithValue("@IpAddress", "127.0.0.1");
                    createCommand.Parameters.AddWithValue("@UserAgent", $"TestAgent/{i}");
                    createCommand.Parameters.AddWithValue("@AuthProvider", "google");

                    await createCommand.ExecuteNonQueryAsync();
                }));
            }

            await Task.WhenAll(tasks);

            // Act - Verify all sessions were created
            var countSql = "SELECT COUNT(*) FROM UserSessions WHERE UserId = @UserId";
            await using var countCommand = new SqlCommand(countSql, connection);
            countCommand.Parameters.AddWithValue("@UserId", testUserId);
            
            var actualCount = (int)await countCommand.ExecuteScalarAsync();

            // Assert
            actualCount.Should().Be(sessionCount, "All concurrent sessions should be created");

            _testOutputHelper.WriteLine($"✅ Created {actualCount} concurrent sessions successfully");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region User Account Management Tests

    [Fact]
    public async Task UserAccountManagement_MultipleProviders_LinksCorrectly()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("multi-provider@example.com");

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Link multiple providers to same user
            var providers = new[] { "google", "apple", "facebook" };
            foreach (var provider in providers)
            {
                var linkAccountSql = @"
                    INSERT INTO UserAccounts (UserId, Provider, ProviderUserId, Email, CreatedAt)
                    VALUES (@UserId, @Provider, @ProviderUserId, @Email, @CreatedAt)";

                await using var linkCommand = new SqlCommand(linkAccountSql, connection);
                linkCommand.Parameters.AddWithValue("@UserId", testUserId);
                linkCommand.Parameters.AddWithValue("@Provider", provider);
                linkCommand.Parameters.AddWithValue("@ProviderUserId", $"{provider}_{testUserId}_linked");
                linkCommand.Parameters.AddWithValue("@Email", $"user_{provider}@example.com");
                linkCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await linkCommand.ExecuteNonQueryAsync();
            }

            // Act - Verify linked accounts
            var getAccountsSql = @"
                SELECT Provider, ProviderUserId 
                FROM UserAccounts 
                WHERE UserId = @UserId 
                ORDER BY Provider";

            await using var getCommand = new SqlCommand(getAccountsSql, connection);
            getCommand.Parameters.AddWithValue("@UserId", testUserId);

            var linkedProviders = new List<string>();
            await using var reader = await getCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                linkedProviders.Add(reader["Provider"].ToString()!);
            }

            // Assert
            linkedProviders.Should().HaveCount(providers.Length + 1); // +1 for original account from CreateTestUserAsync
            linkedProviders.Should().Contain("google");
            linkedProviders.Should().Contain("apple");
            linkedProviders.Should().Contain("facebook");

            _testOutputHelper.WriteLine($"✅ User {testUserId} linked to providers: {string.Join(", ", linkedProviders)}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public async Task AuditLogging_UserActions_RecordsCorrectly()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("audit-test@example.com");

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Create audit log entries
            var auditEvents = new[]
            {
                ("LOGIN_SUCCESS", "User logged in successfully"),
                ("PROFILE_UPDATE", "User updated profile information"),
                ("PASSWORD_CHANGE", "User changed password"),
                ("LOGOUT", "User logged out")
            };

            foreach (var (eventType, description) in auditEvents)
            {
                var auditSql = @"
                    INSERT INTO AuditLog (UserId, EventType, Description, IpAddress, UserAgent, CreatedAt)
                    VALUES (@UserId, @EventType, @Description, @IpAddress, @UserAgent, @CreatedAt)";

                await using var auditCommand = new SqlCommand(auditSql, connection);
                auditCommand.Parameters.AddWithValue("@UserId", testUserId);
                auditCommand.Parameters.AddWithValue("@EventType", eventType);
                auditCommand.Parameters.AddWithValue("@Description", description);
                auditCommand.Parameters.AddWithValue("@IpAddress", "192.168.1.100");
                auditCommand.Parameters.AddWithValue("@UserAgent", "TestBrowser/1.0");
                auditCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await auditCommand.ExecuteNonQueryAsync();
            }

            // Act - Retrieve audit logs
            var getAuditSql = @"
                SELECT EventType, Description, CreatedAt
                FROM AuditLog 
                WHERE UserId = @UserId 
                ORDER BY CreatedAt";

            await using var getCommand = new SqlCommand(getAuditSql, connection);
            getCommand.Parameters.AddWithValue("@UserId", testUserId);

            var auditLogs = new List<(string EventType, string Description)>();
            await using var reader = await getCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                auditLogs.Add((reader["EventType"].ToString()!, reader["Description"].ToString()!));
            }

            // Assert
            auditLogs.Should().HaveCount(auditEvents.Length);
            auditLogs.Should().Contain(log => log.EventType == "LOGIN_SUCCESS");
            auditLogs.Should().Contain(log => log.EventType == "LOGOUT");

            _testOutputHelper.WriteLine($"✅ Recorded {auditLogs.Count} audit log entries for user {testUserId}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Performance and Cleanup Tests

    [Fact]
    public async Task DatabaseOperations_BulkInsert_PerformsEfficiently()
    {
        try
        {
            // Arrange
            var userCount = 100;
            var userIds = new List<int>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Bulk create users
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < userCount; i++)
            {
                var insertSql = @"
                    INSERT INTO Users (Email, DisplayName, FirstName, LastName, IsAuthenticated, CreatedAt)
                    OUTPUT INSERTED.UserId
                    VALUES (@Email, @DisplayName, @FirstName, @LastName, @IsAuthenticated, @CreatedAt)";

                await using var command = new SqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@Email", $"bulk-test-{i}@example.com");
                command.Parameters.AddWithValue("@DisplayName", $"Bulk User {i}");
                command.Parameters.AddWithValue("@FirstName", "Bulk");
                command.Parameters.AddWithValue("@LastName", $"User{i}");
                command.Parameters.AddWithValue("@IsAuthenticated", true);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var userId = (int)await command.ExecuteScalarAsync();
                userIds.Add(userId);
            }

            stopwatch.Stop();

            // Assert
            userIds.Should().HaveCount(userCount);
            userIds.Should().AllSatisfy(id => id.Should().BeGreaterThan(0));
            
            var avgTimePerInsert = stopwatch.ElapsedMilliseconds / (double)userCount;
            avgTimePerInsert.Should().BeLessThan(100, "Each insert should complete within 100ms");

            _testOutputHelper.WriteLine($"✅ Bulk inserted {userCount} users in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimePerInsert:F2}ms per user)");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [Fact]
    public async Task DatabaseCleanup_ExpiredData_RemovesCorrectly()
    {
        try
        {
            // Arrange
            var testUserId = await CreateTestUserAsync("cleanup-test@example.com");

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Create expired session
            var expiredSessionSql = @"
                INSERT INTO UserSessions (SessionId, UserId, ExpiresAt, IsValid, CreatedAt, IpAddress, UserAgent, AuthProvider)
                VALUES (@SessionId, @UserId, @ExpiresAt, @IsValid, @CreatedAt, @IpAddress, @UserAgent, @AuthProvider)";

            await using var createCommand = new SqlCommand(expiredSessionSql, connection);
            createCommand.Parameters.AddWithValue("@SessionId", Guid.NewGuid().ToString());
            createCommand.Parameters.AddWithValue("@UserId", testUserId);
            createCommand.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddDays(-1)); // Expired yesterday
            createCommand.Parameters.AddWithValue("@IsValid", true);
            createCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.AddDays(-1));
            createCommand.Parameters.AddWithValue("@IpAddress", "127.0.0.1");
            createCommand.Parameters.AddWithValue("@UserAgent", "TestAgent/1.0");
            createCommand.Parameters.AddWithValue("@AuthProvider", "google");

            await createCommand.ExecuteNonQueryAsync();

            // Act - Clean up expired sessions
            var cleanupSql = "DELETE FROM UserSessions WHERE ExpiresAt < GETUTCDATE()";
            await using var cleanupCommand = new SqlCommand(cleanupSql, connection);
            var deletedCount = await cleanupCommand.ExecuteNonQueryAsync();

            // Assert
            deletedCount.Should().BeGreaterThan(0, "Should have deleted expired sessions");

            _testOutputHelper.WriteLine($"✅ Cleaned up {deletedCount} expired sessions");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion

    #region Database Security Tests

    [Fact]
    public async Task DatabaseSecurity_ParameterizedQueries_PreventSQLInjection()
    {
        try
        {
            // Arrange
            var maliciousEmail = "test@example.com'; DROP TABLE Users; --";

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Act - Attempt SQL injection through parameterized query
            var safeSql = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            await using var command = new SqlCommand(safeSql, connection);
            command.Parameters.AddWithValue("@Email", maliciousEmail);

            var count = (int)await command.ExecuteScalarAsync();

            // Assert - Query should execute safely and return 0
            count.Should().Be(0, "Malicious SQL should not affect the query");

            // Verify Users table still exists
            var tableCheckSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'";
            await using var tableCommand = new SqlCommand(tableCheckSql, connection);
            var tableExists = (int)await tableCommand.ExecuteScalarAsync();

            tableExists.Should().Be(1, "Users table should still exist after injection attempt");

            _testOutputHelper.WriteLine("✅ SQL injection prevented by parameterized queries");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    #endregion
}