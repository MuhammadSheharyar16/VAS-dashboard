using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace Vas.Api.Startup;

public static class TelemetryExtensions
{
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Sends requests, dependencies, exceptions and logs to Application Insights.
    /// Off when no connection string is configured, which is the normal local setup.
    /// </summary>
    public static WebApplicationBuilder AddApiTelemetry(this WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration[ConnectionStringKey]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }
}
