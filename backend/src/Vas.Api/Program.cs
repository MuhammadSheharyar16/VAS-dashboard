using Vas.Api.Middlewares;
using Vas.Api.Startup;
using Vas.Application;
using Vas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddApiControllers();
builder.Services.AddApiOpenApi();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSecurityHeaders();
app.UseSpaStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.MapApiHealthChecks();
app.MapApiOpenApi();
app.MapControllers().RequireRateLimiting(RateLimitingExtensions.ApiPolicy);
app.MapSpaFallback();

app.Run();
