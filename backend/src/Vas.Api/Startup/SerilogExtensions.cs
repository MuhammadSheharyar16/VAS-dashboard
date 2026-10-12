using Serilog;

namespace Vas.Api.Startup;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Serilog owns the console. Any provider added after this (Application Insights)
        // receives the same events through writeToProviders.
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog(
            (services, configuration) => configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName),
            preserveStaticLogger: true,
            writeToProviders: true);

        return builder;
    }
}
