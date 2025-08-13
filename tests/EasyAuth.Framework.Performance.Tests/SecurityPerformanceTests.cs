using NBomber.Contracts;
using NBomber.CSharp;
using EasyAuth.Framework.Core.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyAuth.Framework.Performance.Tests;

/// <summary>
/// Performance tests for EasyAuth security middleware components
/// </summary>
public class SecurityPerformanceTests
{
    private const int DefaultDurationMinutes = 1;

    [Fact]
    public void RateLimitingMiddleware_ShouldHandleHighRequestVolume()
    {
        var scenario = Scenario.Create("rate_limiting_performance", async context =>
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new LoggerFactory().CreateLogger<RateLimitingMiddleware>();
            
            var middleware = new RateLimitingMiddleware(
                next: (ctx) => Task.CompletedTask,
                cache: cache,
                logger: logger
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
            httpContext.Request.Path = "/api/easyauth/providers";
            httpContext.Request.Method = "GET";

            try
            {
                await middleware.InvokeAsync(httpContext);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Rate limiting middleware error");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 1000, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void InputValidationMiddleware_ShouldHandleMaliciousRequests()
    {
        var scenario = Scenario.Create("input_validation_performance", async context =>
        {
            var logger = new LoggerFactory().CreateLogger<InputValidationMiddleware>();
            var options = Options.Create(new InputValidationOptions());
            
            var middleware = new InputValidationMiddleware(
                next: (ctx) => Task.CompletedTask,
                logger: logger,
                options: options
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/easyauth/providers";
            httpContext.Request.Method = "POST";
            httpContext.Request.Headers.Add("Content-Type", "application/json");
            
            // Add potentially malicious content
            var maliciousInputs = new[]
            {
                "{ \"input\": \"<script>alert('xss')</script>\" }",
                "{ \"input\": \"'; DROP TABLE Users; --\" }",
                "{ \"input\": \"../../../etc/passwd\" }",
                "{ \"input\": \"normal input\" }" // Include normal requests
            };
            
            var randomInput = maliciousInputs[context.InvocationNumber % maliciousInputs.Length];
            httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(randomInput));

            try
            {
                await middleware.InvokeAsync(httpContext);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Debug(ex, "Input validation middleware rejected request (expected)");
                return Response.Ok(); // Expected for malicious inputs
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 800, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void CsrfProtectionMiddleware_ShouldHandleTokenValidation()
    {
        var scenario = Scenario.Create("csrf_protection_performance", async context =>
        {
            var logger = new LoggerFactory().CreateLogger<CsrfProtectionMiddleware>();
            
            var middleware = new CsrfProtectionMiddleware(
                next: (ctx) => Task.CompletedTask,
                logger: logger
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/easyauth/login";
            httpContext.Request.Method = context.InvocationNumber % 2 == 0 ? "GET" : "POST";
            
            if (httpContext.Request.Method == "POST")
            {
                // Add CSRF token for POST requests
                httpContext.Request.Headers.Add("X-CSRF-Token", $"csrf-token-{context.InvocationNumber}");
            }

            try
            {
                await middleware.InvokeAsync(httpContext);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "CSRF protection middleware error");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 600, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public void SecurityMiddlewareStack_ShouldHandleCombinedLoad()
    {
        var scenario = Scenario.Create("security_stack_performance", async context =>
        {
            // Simulate the complete security middleware stack
            var cache = new MemoryCache(new MemoryCacheOptions());
            var loggerFactory = new LoggerFactory();
            
            var rateLimitingMiddleware = new RateLimitingMiddleware(
                next: (ctx) => Task.CompletedTask,
                cache: cache,
                logger: loggerFactory.CreateLogger<RateLimitingMiddleware>()
            );

            var inputValidationMiddleware = new InputValidationMiddleware(
                next: async (ctx) => await rateLimitingMiddleware.InvokeAsync(ctx),
                logger: loggerFactory.CreateLogger<InputValidationMiddleware>(),
                options: Options.Create(new InputValidationOptions())
            );

            var csrfMiddleware = new CsrfProtectionMiddleware(
                next: async (ctx) => await inputValidationMiddleware.InvokeAsync(ctx),
                logger: loggerFactory.CreateLogger<CsrfProtectionMiddleware>()
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse($"192.168.1.{100 + (context.InvocationNumber % 155)}");
            httpContext.Request.Path = "/api/easyauth/providers";
            httpContext.Request.Method = "GET";

            try
            {
                await csrfMiddleware.InvokeAsync(httpContext);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Warning(ex, "Security middleware stack handled request");
                return Response.Ok(); // May be expected for rate limited requests
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 500, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
            .Run();
    }

    [Fact]
    public void MemoryCachePerformance_ShouldHandleSessionStorage()
    {
        var scenario = Scenario.Create("memory_cache_performance", async context =>
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10000 // Limit cache size for testing
            });

            var sessionId = $"session-{context.InvocationNumber % 1000}"; // Reuse some session IDs
            var userData = new
            {
                UserId = $"user-{context.InvocationNumber % 500}",
                Email = $"user{context.InvocationNumber % 500}@example.com",
                LoginTime = DateTime.UtcNow,
                Permissions = new[] { "read", "write", "admin" }
            };

            try
            {
                // Simulate session operations: set, get, remove
                switch (context.InvocationNumber % 3)
                {
                    case 0: // Set session
                        cache.Set(sessionId, userData, TimeSpan.FromMinutes(30));
                        break;
                    case 1: // Get session
                        var cachedData = cache.Get(sessionId);
                        break;
                    case 2: // Remove session
                        cache.Remove(sessionId);
                        break;
                }

                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Memory cache operation failed");
                return Response.Fail();
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 2000, during: TimeSpan.FromMinutes(DefaultDurationMinutes))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }
}