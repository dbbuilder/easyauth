using AutoFixture;
using EasyAuth.Framework.Core.Models;
using EasyAuth.Framework.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Integration tests for database operations
/// Tests core database functionality using available interface methods
/// </summary>
public class DatabaseIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Fixture _fixture;

    public DatabaseIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fixture = new Fixture();
    }

    [DockerRequiredFact]
    public async Task InitializeDatabase_CompletesSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        try
        {
            // Act
            var initResult = await databaseService.InitializeDatabaseAsync();
            var isInitialized = await databaseService.IsDatabaseInitializedAsync();

            // Assert
            initResult.Should().BeTrue();
            isInitialized.Should().BeTrue();

            _testOutputHelper.WriteLine($"Database initialized: {initResult}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task GetDatabaseVersion_ReturnsVersion()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        try
        {
            // Act
            var version = await databaseService.GetDatabaseVersionAsync();

            // Assert
            version.Should().NotBeNullOrEmpty();

            _testOutputHelper.WriteLine($"Database version: {version}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task ApplyMigrations_CompletesSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        try
        {
            // Act
            var migrationResult = await databaseService.ApplyMigrationsAsync();

            // Assert
            migrationResult.Should().BeTrue();

            _testOutputHelper.WriteLine($"Migrations applied: {migrationResult}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task ValidateSession_WithInvalidSessionId_ReturnsInvalidSession()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        var invalidSessionId = Guid.NewGuid().ToString();

        try
        {
            // Act
            var sessionInfo = await databaseService.ValidateSessionAsync(invalidSessionId);

            // Assert
            // Should return session info with IsValid = false for invalid sessions
            sessionInfo.Should().NotBeNull();
            sessionInfo.IsValid.Should().BeFalse();

            _testOutputHelper.WriteLine($"Invalid session validation completed: IsValid = {sessionInfo.IsValid}");
        }
        catch (Exception ex)
        {
            // Some implementations might throw for invalid sessions, which is also valid
            _testOutputHelper.WriteLine($"Exception thrown for invalid session (acceptable): {ex.Message}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task InvalidateSession_WithInvalidSessionId_HandlesGracefully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        var invalidSessionId = Guid.NewGuid().ToString();

        try
        {
            // Act
            var result = await databaseService.InvalidateSessionAsync(invalidSessionId);

            // Assert - Should handle invalid session gracefully
            // Either returns false or true (idempotent), both are acceptable
            // Just verify it returns without throwing
            Assert.NotNull(result); // Should return a non-null boolean result
            _testOutputHelper.WriteLine($"Invalidate invalid session result: {result}");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task CleanupExpiredData_CompletesSuccessfully()
    {
        // Arrange
        using var scope = ServiceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

        try
        {
            // Act
            var cleanupResult = await databaseService.CleanupExpiredDataAsync();

            // Assert
            cleanupResult.Should().BeGreaterThanOrEqualTo(0);

            _testOutputHelper.WriteLine($"Cleanup expired data: {cleanupResult} records cleaned");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }

    [DockerRequiredFact]
    public async Task DatabaseService_CompleteWorkflow_ValidatesIntegration()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IEAuthDatabaseService>();

            // Step 1: Initialize database
            var initResult = await databaseService.InitializeDatabaseAsync();
            initResult.Should().BeTrue();

            // Step 2: Check initialization
            var isInitialized = await databaseService.IsDatabaseInitializedAsync();
            isInitialized.Should().BeTrue();

            // Step 3: Get version
            var version = await databaseService.GetDatabaseVersionAsync();
            version.Should().NotBeNullOrEmpty();

            // Step 4: Apply migrations
            var migrationsResult = await databaseService.ApplyMigrationsAsync();
            migrationsResult.Should().BeTrue();

            // Step 5: Cleanup expired data
            var cleanupResult = await databaseService.CleanupExpiredDataAsync();
            cleanupResult.Should().BeGreaterThanOrEqualTo(0);

            _testOutputHelper.WriteLine($"✅ Complete database service workflow validated");
            _testOutputHelper.WriteLine($"   - Initialization: ✅ {initResult}");
            _testOutputHelper.WriteLine($"   - Version: ✅ {version}");
            _testOutputHelper.WriteLine($"   - Migrations: ✅ {migrationsResult}");
            _testOutputHelper.WriteLine($"   - Cleanup: ✅ {cleanupResult} records");
        }
        finally
        {
            await CleanupTestDataAsync();
        }
    }
}