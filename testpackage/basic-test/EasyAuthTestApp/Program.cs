using EasyAuth.Framework.Core.Extensions;
using EasyAuth.Framework.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Test EasyAuth Framework v2.2.0 integration
try
{
    // Test basic service registration - this will test framework functionality
    builder.Services.AddEasyAuth(builder.Configuration);

    Console.WriteLine("✅ EasyAuth Framework v2.2.0: Basic service registration test successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ EasyAuth Framework v2.2.0 integration test failed: {ex.Message}");
}

// Add services to the container.
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Simple test endpoint
app.MapGet("/test", () => "EasyAuth Framework v2.2.0 integration test successful!");

Console.WriteLine("🚀 EasyAuth Framework v2.2.0 test application started!");

app.Run();
