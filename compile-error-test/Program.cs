using EasyAuth.Framework.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ✅ CORRECT: Both parameters included - should compile successfully
builder.Services.AddEasyAuth(builder.Configuration, builder.Environment);

var app = builder.Build();
app.Run();