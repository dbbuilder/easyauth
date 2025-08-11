using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EasyAuth.Framework.Core.Configuration;
using EasyAuth.Framework.Core.Services;
using EasyAuth.Framework.Core.Extensions;
using Testcontainers.MsSql;
using Xunit;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Base class for integration tests providing database setup
/// Follows TDD methodology with proper setup and cleanup
/// </summary>
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly MsSqlContainer DatabaseContainer;
    protected readonly ServiceProvider ServiceProvider;
    protected string ConnectionString { get; private set; } = string.Empty;
    
    protected BaseIntegrationTest()
    {
        // Setup SQL Server test container
        DatabaseContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPortBinding(0, 1433)
            .Build();

        // Build service provider with EasyAuth configuration
        var services = new ServiceCollection();
        var configuration = BuildTestConfiguration();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add EasyAuth services
        services.AddEasyAuth(configuration);
        
        ServiceProvider = services.BuildServiceProvider();
    }

    private IConfiguration BuildTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EasyAuth:ConnectionString"] = ConnectionString,
            ["EasyAuth:Framework:EnableHealthChecks"] = "true",
            ["EasyAuth:Providers:Google:Enabled"] = "true",
            ["EasyAuth:Providers:Google:ClientId"] = "test-client-id",
            ["EasyAuth:Providers:Google:ClientSecret"] = "test-client-secret",
            ["EasyAuth:Providers:Apple:Enabled"] = "true",
            ["EasyAuth:Providers:Apple:ClientId"] = "test-apple-client",
            ["EasyAuth:Providers:Apple:TeamId"] = "test-team-id",
            ["EasyAuth:Providers:Apple:KeyId"] = "test-key-id",
            ["EasyAuth:Providers:Facebook:Enabled"] = "true",
            ["EasyAuth:Providers:Facebook:AppId"] = "test-app-id",
            ["EasyAuth:Providers:Facebook:AppSecret"] = "test-app-secret",
            ["EasyAuth:Providers:AzureB2C:Enabled"] = "true",
            ["EasyAuth:Providers:AzureB2C:ClientId"] = "test-b2c-client",
            ["EasyAuth:Providers:AzureB2C:TenantId"] = "test-tenant.onmicrosoft.com",
            ["EasyAuth:Providers:AzureB2C:SignUpSignInPolicyId"] = "B2C_1_signupsignin"
        });
        return configBuilder.Build();
    }

    public async Task InitializeAsync()
    {
        // Start database container
        await DatabaseContainer.StartAsync();
        
        // Get connection string
        ConnectionString = DatabaseContainer.GetConnectionString();
        
        // Initialize database schema
        await InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        ServiceProvider?.Dispose();
        await DatabaseContainer.DisposeAsync();
    }
    
    /// <summary>
    /// Initialize database with EasyAuth schema and test data
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        // Create EasyAuth tables
        var createTablesScript = await File.ReadAllTextAsync(
            Path.Combine(GetProjectRoot(), "src", "EasyAuth.Framework.Core", "Database", "Scripts", "create_schema_and_metadata.sql"));
        
        await ExecuteSqlScriptAsync(connection, createTablesScript);
        
        // Create other required tables
        var scriptFiles = new[]
        {
            "create_users_table.sql",
            "create_user_accounts_table.sql", 
            "create_user_sessions_table.sql",
            "create_user_roles_table.sql",
            "create_audit_log_table.sql"
        };
        
        foreach (var scriptFile in scriptFiles)
        {
            var scriptPath = Path.Combine(GetProjectRoot(), "src", "EasyAuth.Framework.Core", "Database", "Scripts", scriptFile);
            if (File.Exists(scriptPath))
            {
                var script = await File.ReadAllTextAsync(scriptPath);
                await ExecuteSqlScriptAsync(connection, script);
            }
        }
    }
    
    /// <summary>
    /// Execute SQL script with proper error handling
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Static SQL scripts for test database setup - no user input")]
    private static async Task ExecuteSqlScriptAsync(SqlConnection connection, string script)
    {
        try
        {
            await using var command = new SqlCommand(script, connection);
            command.CommandTimeout = 30;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute SQL script: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Get project root directory for file access
    /// </summary>
    private static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectRoot = currentDirectory;
        
        // Navigate up to find the solution root
        while (projectRoot != null && !File.Exists(Path.Combine(projectRoot, "EasyAuth.Framework.sln")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }
        
        return projectRoot ?? throw new DirectoryNotFoundException("Could not find project root directory");
    }
    
    /// <summary>
    /// Create test user data for integration tests
    /// </summary>
    protected async Task<int> CreateTestUserAsync(string email = "test@example.com", string provider = "google")
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        const string insertUserSql = @"
            INSERT INTO Users (Email, DisplayName, FirstName, LastName, IsAuthenticated, CreatedAt)
            OUTPUT INSERTED.UserId
            VALUES (@Email, @DisplayName, @FirstName, @LastName, @IsAuthenticated, @CreatedAt)";
            
        await using var command = new SqlCommand(insertUserSql, connection);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@DisplayName", "Test User");
        command.Parameters.AddWithValue("@FirstName", "Test");
        command.Parameters.AddWithValue("@LastName", "User");
        command.Parameters.AddWithValue("@IsAuthenticated", true);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        
        var userId = (int)await command.ExecuteScalarAsync();
        
        // Create user account for provider
        const string insertAccountSql = @"
            INSERT INTO UserAccounts (UserId, Provider, ProviderUserId, Email, CreatedAt)
            VALUES (@UserId, @Provider, @ProviderUserId, @Email, @CreatedAt)";
            
        await using var accountCommand = new SqlCommand(insertAccountSql, connection);
        accountCommand.Parameters.AddWithValue("@UserId", userId);
        accountCommand.Parameters.AddWithValue("@Provider", provider);
        accountCommand.Parameters.AddWithValue("@ProviderUserId", $"{provider}_{userId}");
        accountCommand.Parameters.AddWithValue("@Email", email);
        accountCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        
        await accountCommand.ExecuteNonQueryAsync();
        
        return userId;
    }
    
    /// <summary>
    /// Clean up test data after test execution
    /// </summary>
    protected async Task CleanupTestDataAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        // Clean up in reverse order of dependencies
        var cleanupTables = new[] { "AuditLog", "UserSessions", "UserRoles", "UserAccounts", "Users" };
        
        foreach (var table in cleanupTables)
        {
            // Table names are from a controlled list in test code, safe from injection
            #pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            await using var command = new SqlCommand($"DELETE FROM {table} WHERE Email LIKE 'test%'", connection);
            #pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            await command.ExecuteNonQueryAsync();
        }
    }
}