using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Infrastructure.Common;
using Vas.Infrastructure.Identity;
using Vas.Infrastructure.Persistence;
using Vas.Infrastructure.Persistence.Interceptors;
using Vas.Infrastructure.Persistence.Mock;

namespace Vas.Infrastructure;

public static class DependencyInjection
{
    private const int FabricMaxRetries = 3;

    private static readonly TimeSpan FabricMaxRetryDelay = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddReporting(configuration);

        return services;
    }

    private static void AddReporting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ReportingOptions>()
            .Bind(configuration.GetSection(ReportingOptions.SectionName))
            .Validate<IHostEnvironment>(
                (options, environment) =>
                    options.DataSource != ReportingDataSource.Mock || environment.IsDevelopment(),
                "Reporting:DataSource=Mock is only allowed in the Development environment. "
                + "UAT and Production must read from Fabric.")
            .ValidateOnStart();

        services.AddOptions<FabricOptions>()
            .Bind(configuration.GetSection(FabricOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<IOptions<ReportingOptions>>(
                (fabric, reporting) =>
                    reporting.Value.DataSource != ReportingDataSource.Fabric || fabric.IsConfigured,
                "Fabric:SqlEndpoint and Fabric:Database are required when Reporting:DataSource=Fabric.")
            .ValidateOnStart();

        services.AddOptions<KpiThresholdOptions>()
            .Bind(configuration.GetSection(KpiThresholdOptions.SectionName))
            .Validate(
                options => options.Kpis.Values.All(threshold => threshold.IsConsistent),
                "Each KPI threshold needs Warning and Critical on the correct sides of each other.")
            .ValidateOnStart();

        services.AddOptions<PaginationOptions>()
            .Bind(configuration.GetSection(PaginationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.DefaultPageSize <= options.MaxPageSize,
                "Pagination:DefaultPageSize cannot be larger than Pagination:MaxPageSize.")
            .ValidateOnStart();

        // Mock source: the embedded JSON files.
        services.AddSingleton(_ => new JsonReportingSource(MockGoldDataSet.FromEmbeddedFiles()));

        // Fabric source: EF Core over the SQL analytics endpoint, authenticated with an Entra token.
        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddSingleton<FabricAccessTokenInterceptor>();
        services.AddDbContext<ReportingDbContext>((provider, options) =>
        {
            var fabric = provider.GetRequiredService<IOptions<FabricOptions>>().Value;

            options
                .UseSqlServer(
                    BuildConnectionString(fabric),
                    sqlServer => sqlServer
                        .CommandTimeout(fabric.CommandTimeoutSeconds)
                        // Every query is a read, so repeating one after a transient fault is safe.
                        .EnableRetryOnFailure(FabricMaxRetries, FabricMaxRetryDelay, errorNumbersToAdd: null))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .AddInterceptors(provider.GetRequiredService<FabricAccessTokenInterceptor>());
        });

        // Only the configured source is ever constructed.
        services.AddScoped<IReportingDbContext>(provider =>
            provider.GetRequiredService<IOptions<ReportingOptions>>().Value.DataSource == ReportingDataSource.Mock
                ? provider.GetRequiredService<JsonReportingSource>()
                : provider.GetRequiredService<ReportingDbContext>());
    }

    /// <summary>Server and database only. There is no user, password or secret in it.</summary>
    public static string BuildConnectionString(FabricOptions fabric) =>
        new SqlConnectionStringBuilder
        {
            DataSource = $"tcp:{fabric.SqlEndpoint},1433",
            InitialCatalog = fabric.Database,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = false,
            ConnectTimeout = 30,
            ApplicationName = "Vas.Dashboard",
        }.ConnectionString;
}
