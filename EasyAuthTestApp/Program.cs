using EasyAuth.Framework.Core.Extensions;
using EasyAuth.Framework.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Test EasyAuth integration
try
{
    // Configure EasyAuth options
    var eauthOptions = new EAuthOptions
    {
        Framework = new FrameworkOptions
        {
            EnableHealthChecks = true,
            EnableSwagger = true
        },
        Providers = new ProvidersOptions
        {
            Google = new GoogleOptions
            {
                Enabled = true,
                ClientId = "test-client-id",
                ClientSecret = "test-secret"
            }
        },
        Cors = new EAuthCorsOptions
        {
            AllowedOrigins = new List<string> { "http://localhost:3000" },
            AllowedMethods = new List<string> { "GET", "POST" },
            AllowedHeaders = new List<string> { "Authorization", "Content-Type" }
        }
    };

    // Add EasyAuth services - this will test package functionality
    builder.Services.AddEasyAuth(eauthOptions);

    Console.WriteLine("✅ EasyAuth Framework v2.2.0 package test: Basic integration successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ EasyAuth Framework v2.2.0 package test failed: {ex.Message}");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Test application with EasyAuth v2.2.0 started successfully!");

app.Run();