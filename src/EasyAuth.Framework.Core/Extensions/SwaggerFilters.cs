using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using EasyAuth.Framework.Core.Models;
using Microsoft.OpenApi.Any;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Custom schema filter for enhanced EasyAuth API documentation
/// </summary>
public class EasyAuthSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            schema.Description = "Standard API response wrapper for all EasyAuth endpoints";
            schema.Example = new OpenApiObject
            {
                ["success"] = new OpenApiBoolean(true),
                ["message"] = new OpenApiString("Operation completed successfully"),
                ["data"] = new OpenApiNull(),
                ["errors"] = new OpenApiArray(),
                ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
            };
        }
        else if (context.Type == typeof(TokenResponse))
        {
            schema.Description = "JWT token response containing authentication tokens and user information";
            schema.Example = new OpenApiObject
            {
                ["accessToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"),
                ["refreshToken"] = new OpenApiString("refresh_eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."),
                ["expiresIn"] = new OpenApiInteger(3600),
                ["tokenType"] = new OpenApiString("Bearer")
            };
        }

        // Enhance schema for common types
        if (context.Type.Name.Contains("Request") || context.Type.Name.Contains("Model"))
        {
            AddExampleValues(schema, context.Type);
        }
    }

    private static void AddExampleValues(OpenApiSchema schema, Type type)
    {
        if (schema.Properties == null) return;

        foreach (var property in schema.Properties)
        {
            var propertyInfo = type.GetProperty(property.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null)
            {
                AddPropertyExample(property.Value, propertyInfo);
            }
        }
    }

    private static void AddPropertyExample(OpenApiSchema propertySchema, PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name.ToLowerInvariant();
        var propertyType = propertyInfo.PropertyType;

        // Add examples based on property name patterns
        if (propertyName.Contains("email"))
        {
            propertySchema.Example = new OpenApiString("user@example.com");
        }
        else if (propertyName.Contains("name") && propertyType == typeof(string))
        {
            propertySchema.Example = new OpenApiString("John Doe");
        }
        else if (propertyName.Contains("provider"))
        {
            propertySchema.Example = new OpenApiString("google");
        }
        else if (propertyName.Contains("code") && propertyType == typeof(string))
        {
            propertySchema.Example = new OpenApiString("auth_code_123456");
        }
        else if (propertyName.Contains("redirect") && propertyName.Contains("uri"))
        {
            propertySchema.Example = new OpenApiString("https://myapp.com/auth/callback");
        }
        else if (propertyName.Contains("state"))
        {
            propertySchema.Example = new OpenApiString("random_state_string_123");
        }
    }
}

/// <summary>
/// Custom operation filter for enhanced EasyAuth endpoint documentation
/// </summary>
public class EasyAuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerName = context.MethodInfo.DeclaringType?.Name;
        var actionName = context.MethodInfo.Name;

        // Add enhanced documentation for EasyAuth endpoints
        if (controllerName?.Contains("EAuth") == true || controllerName?.Contains("Auth") == true)
        {
            AddEasyAuthDocumentation(operation, actionName, context);
        }

        // Add common response examples
        AddCommonResponseExamples(operation);

        // Add provider parameter documentation
        AddProviderParameterDocumentation(operation);
    }

    private static void AddEasyAuthDocumentation(OpenApiOperation operation, string actionName, OperationFilterContext context)
    {
        var actionLower = actionName.ToLowerInvariant();

        if (actionLower.Contains("login"))
        {
            operation.Summary ??= "Initiate authentication with external provider";
            operation.Description = @"
🔐 **Start the authentication process** with the specified provider.

**Supported Providers:**
- `google` - Google OAuth 2.0
- `facebook` - Facebook Login
- `apple` - Sign in with Apple
- `azureb2c` - Azure AD B2C

**Flow:**
1. Call this endpoint with provider name
2. Follow the returned redirect URL
3. Complete OAuth flow on provider's site
4. Get redirected back with tokens

**Example:**
```
POST /api/auth/login/google
{
  ""redirectUri"": ""https://myapp.com/auth/callback"",
  ""state"": ""optional_state_parameter""
}
```
";
        }
        else if (actionLower.Contains("callback"))
        {
            operation.Summary ??= "Handle OAuth callback from provider";
            operation.Description = @"
↩️ **Process the OAuth callback** from the external provider.

This endpoint is called automatically by the OAuth provider after user authorization.
You typically don't need to call this directly - it's part of the OAuth flow.

**Returns:** JWT tokens for authenticated user
";
        }
        else if (actionLower.Contains("logout"))
        {
            operation.Summary ??= "Logout user and invalidate tokens";
            operation.Description = @"
🚪 **Logout the authenticated user** and invalidate their session.

Clears server-side session and invalidates tokens.
Client should also clear local storage/cookies.
";
        }
        else if (actionLower.Contains("refresh"))
        {
            operation.Summary ??= "Refresh expired access token";
            operation.Description = @"
🔄 **Refresh an expired access token** using a valid refresh token.

Use this when your access token expires to get a new one without re-authentication.
";
        }
        else if (actionLower.Contains("profile") || actionLower.Contains("user"))
        {
            operation.Summary ??= "Get authenticated user profile";
            operation.Description = @"
👤 **Get the profile information** for the authenticated user.

Returns user details from the authentication provider.
Requires valid Bearer token in Authorization header.
";
        }
    }

    private static void AddCommonResponseExamples(OpenApiOperation operation)
    {
        // Add success response example
        if (operation.Responses.ContainsKey("200"))
        {
            var response = operation.Responses["200"];
            if (response.Content?.ContainsKey("application/json") == true)
            {
                var mediaType = response.Content["application/json"];
                if (mediaType.Example == null)
                {
                    mediaType.Example = new OpenApiObject
                    {
                        ["success"] = new OpenApiBoolean(true),
                        ["message"] = new OpenApiString("Operation completed successfully"),
                        ["data"] = new OpenApiObject(),
                        ["errors"] = new OpenApiArray(),
                        ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    };
                }
            }
        }

        // Add common error response examples
        AddErrorResponseExample(operation, "400", "Bad Request", "Invalid request parameters");
        AddErrorResponseExample(operation, "401", "Unauthorized", "Invalid or missing authentication token");
        AddErrorResponseExample(operation, "403", "Forbidden", "Insufficient permissions");
        AddErrorResponseExample(operation, "500", "Internal Server Error", "An unexpected error occurred");
    }

    private static void AddErrorResponseExample(OpenApiOperation operation, string statusCode, string title, string detail)
    {
        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses[statusCode] = new OpenApiResponse
            {
                Description = title,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiObject
                        {
                            ["success"] = new OpenApiBoolean(false),
                            ["message"] = new OpenApiString(title),
                            ["data"] = new OpenApiNull(),
                            ["errors"] = new OpenApiArray
                            {
                                new OpenApiString(detail)
                            },
                            ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                        }
                    }
                }
            };
        }
    }

    private static void AddProviderParameterDocumentation(OpenApiOperation operation)
    {
        var providerParam = operation.Parameters?.FirstOrDefault(p => 
            p.Name.Equals("provider", StringComparison.OrdinalIgnoreCase));

        if (providerParam != null)
        {
            providerParam.Description = @"
**Authentication Provider**

Supported values:
- `google` - Google OAuth 2.0 (most popular)
- `facebook` - Facebook Login  
- `apple` - Sign in with Apple (required for iOS apps)
- `azureb2c` - Azure AD B2C (enterprise)

**Example:** `google`
";
            providerParam.Example = new OpenApiString("google");
            
            // Add enum values if schema exists
            if (providerParam.Schema != null)
            {
                providerParam.Schema.Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("google"),
                    new OpenApiString("facebook"), 
                    new OpenApiString("apple"),
                    new OpenApiString("azureb2c")
                };
            }
        }
    }
}