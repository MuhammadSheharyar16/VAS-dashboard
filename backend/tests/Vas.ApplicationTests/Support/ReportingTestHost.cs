using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vas.Application;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Features.ExecutiveSummary.Services;
using Vas.Domain.Reporting;
using Vas.Infrastructure.Persistence.Mock;

namespace Vas.ApplicationTests.Support;

public sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}

/// <summary>
/// Builds the real application services over any <see cref="IReportingDbContext"/>, registered the
/// way the API registers them, with a fixed clock and fixed thresholds.
/// </summary>
public static class ReportingTestHost
{
    public static readonly DateTime Now = new(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);

    public static KpiThresholdOptions Thresholds { get; } = new()
    {
        ArePlaceholders = true,
        Kpis =
        {
            ["approvalRate"] = new KpiThreshold { Warning = 75, Critical = 60, HigherIsBetter = true },
            ["averageTurnaroundDays"] = new KpiThreshold { Warning = 10, Critical = 15, HigherIsBetter = false },
            ["capacityUtilisation"] = new KpiThreshold { Warning = 85, Critical = 100, HigherIsBetter = false },
        },
    };

    public static TService Service<TService>(IReportingDbContext db)
        where TService : notnull
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(db);
        services.AddSingleton<IDateTimeProvider>(new FixedClock(Now));
        services.AddSingleton(Options.Create(Thresholds));
        services.AddSingleton(Options.Create(new PaginationOptions()));

        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<TService>();
    }

    public static TService Service<TService>(MockGoldDataSet data)
        where TService : notnull =>
        Service<TService>(new JsonReportingSource(data));

    public static IExecutiveSummaryService ExecutiveSummary(IReportingDbContext db) =>
        Service<IExecutiveSummaryService>(db);

    public static IExecutiveSummaryService ExecutiveSummary(MockGoldDataSet data) =>
        Service<IExecutiveSummaryService>(data);
}

/// <summary>Hand-made Gold rows, small enough to check every figure by eye.</summary>
public static class GoldRows
{
    public static readonly Country[] Countries =
    [
        new() { CountryCode = "AE", CountryName = "United Arab Emirates", Region = "Middle East" },
        new() { CountryCode = "IN", CountryName = "India", Region = "South Asia" },
    ];

    public static readonly Centre[] Centres =
    [
        new() { CentreCode = "AE-DXB", CentreName = "Dubai Centre", CountryCode = "AE", City = "Dubai", DailyCapacity = 6 },
        new() { CentreCode = "IN-BOM", CentreName = "Mumbai Centre", CountryCode = "IN", City = "Mumbai", DailyCapacity = 8 },
    ];

    public static readonly VisaType[] VisaTypes =
    [
        new() { VisaTypeCode = "TOUR", VisaTypeName = "Tourist", Category = "Short stay" },
        new() { VisaTypeCode = "WORK", VisaTypeName = "Employment", Category = "Long stay" },
        new() { VisaTypeCode = "STU", VisaTypeName = "Student", Category = "Long stay" },
    ];

    private static int _sequence;

    public static VisaApplication Application(
        string submitted,
        string? outcome = ApplicationOutcome.Approved,
        int? turnaroundDays = 5,
        string country = "AE",
        string visaType = "TOUR",
        string channel = ApplicationChannel.Digital,
        string? id = null)
    {
        var submittedDate = DateOnly.Parse(submitted);
        var isDecided = outcome is not null;

        return new VisaApplication
        {
            ApplicationId = id ?? $"APP-{Interlocked.Increment(ref _sequence):D6}",
            SubmittedDate = submittedDate,
            CountryCode = country,
            CentreCode = country == "AE" ? "AE-DXB" : "IN-BOM",
            VisaTypeCode = visaType,
            Channel = channel,
            CurrentStage = isDecided ? ApplicationStageName.Decision : ApplicationStageName.Review,
            Status = isDecided ? ApplicationStatus.Processed : ApplicationStatus.Pending,
            Outcome = outcome,
            DecisionDate = isDecided ? submittedDate.AddDays(turnaroundDays ?? 0) : null,
            TurnaroundDays = isDecided ? turnaroundDays : null,
        };
    }

    public static ApplicationStage Stage(string applicationId, string stage, bool exited = true) =>
        new()
        {
            ApplicationId = applicationId,
            Stage = stage,
            EnteredAtUtc = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            ExitedAtUtc = exited ? new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc) : null,
        };

    public static VasRevenueTransaction Revenue(
        string date,
        decimal amount,
        string currency = "AED",
        string country = "AE",
        string serviceName = "Service Fee") =>
        new()
        {
            TransactionId = $"TXN-{Interlocked.Increment(ref _sequence):D7}",
            TransactionDate = DateOnly.Parse(date),
            CountryCode = country,
            CentreCode = country == "AE" ? "AE-DXB" : "IN-BOM",
            VisaTypeCode = "TOUR",
            ServiceName = serviceName,
            Amount = amount,
            Currency = currency,
        };

    public static RefreshLog Refresh(string atUtc, string status = RefreshStatus.Succeeded) =>
        new() { RefreshedAtUtc = DateTime.Parse(atUtc).ToUniversalTime(), Status = status };

    public static MockGoldDataSet DataSet(
        IEnumerable<VisaApplication>? applications = null,
        IEnumerable<VasRevenueTransaction>? revenue = null,
        IEnumerable<RefreshLog>? refreshLogs = null,
        IEnumerable<ApplicationStage>? stages = null) =>
        new(Countries, Centres, VisaTypes, [.. applications ?? []], [.. stages ?? []], [.. revenue ?? []], [.. refreshLogs ?? []]);
}
