using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EasyAuth.Framework.Core.Extensions;
using Xunit;
using FluentAssertions;

namespace EasyAuth.Framework.Core.Tests;

/// <summary>
/// Unit tests for MapEasyAuth() method to ensure proper method behavior
/// </summary>
public class MapEasyAuthTests
{
    [Fact]
    public void MapEasyAuth_ShouldReturnWebApplication_ForMethodChaining()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:Framework:EnableHealthChecks"] = "false",
                ["EasyAuth:ConnectionString"] = "Data Source=:memory:",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();
        
        builder.Services.AddEasyAuth(config);
        builder.Services.AddControllers();
        
        var app = builder.Build();
        app.UseRouting();

        // Act
        var result = app.MapEasyAuth();

        // Assert
        result.Should().BeSameAs(app, "MapEasyAuth should return the same WebApplication instance for method chaining");
    }

    [Fact]
    public void MapEasyAuth_WithNullConfigureEndpoints_ShouldNotThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:Framework:EnableHealthChecks"] = "false",
                ["EasyAuth:ConnectionString"] = "Data Source=:memory:",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();
        
        builder.Services.AddEasyAuth(config);
        builder.Services.AddControllers();
        
        var app = builder.Build();
        app.UseRouting();

        // Act & Assert
        var exception = Record.Exception(() => app.MapEasyAuth(null));
        exception.Should().BeNull("MapEasyAuth should handle null configureEndpoints gracefully");
    }

    [Fact]
    public void MapEasyAuth_WithCustomEndpointConfiguration_ShouldExecuteConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EasyAuth:Framework:EnableHealthChecks"] = "false",
                ["EasyAuth:ConnectionString"] = "Data Source=:memory:",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();
        
        builder.Services.AddEasyAuth(config);
        builder.Services.AddControllers();
        
        var app = builder.Build();
        app.UseRouting();

        var configurationExecuted = false;

        // Act
        var result = app.MapEasyAuth(endpoints =>
        {
            configurationExecuted = true;
            // Add a test endpoint
            endpoints.MapGet("/test-endpoint", () => "Test successful");
        });

        // Assert
        result.Should().BeSameAs(app);
        configurationExecuted.Should().BeTrue("Custom endpoint configuration should be executed");
    }

    [Fact]
    public void MapEasyAuth_RequiresWebApplication_NotIApplicationBuilder()
    {
        // This test verifies that MapEasyAuth is an extension method on WebApplication
        // and not on IApplicationBuilder, ensuring proper typing
        
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var config = new ConfigurationBuilder().Build();
        
        builder.Services.AddEasyAuth(config);
        builder.Services.AddControllers();
        
        var app = builder.Build();
        app.UseRouting();

        // Act & Assert - This should compile because app is WebApplication
        var result = app.MapEasyAuth();
        result.Should().NotBeNull();
        result.Should().BeOfType<WebApplication>();
    }
}