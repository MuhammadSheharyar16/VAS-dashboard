using Vas.Api.Middlewares;
using Vas.Api.Startup;
using Vas.Application;
using Vas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.AddApiTelemetry();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiControllers();
builder.Services.AddApiOpenApi();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseApiStatusCodeEnvelope();
app.UseSecurityHeaders();
app.UseSpaStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapApiHealthChecks();
app.MapApiOpenApi();
app.MapClientConfig();
app.MapControllers().RequireRateLimiting(RateLimitingExtensions.ApiPolicy);
app.MapSpaFallback();

app.Run();
