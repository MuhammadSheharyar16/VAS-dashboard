namespace Vas.Api.Startup;

public static class HealthCheckExtensions
{
    public const string HealthPath = "/healthz";

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(HealthPath).AllowAnonymous();

        return app;
    }
}
