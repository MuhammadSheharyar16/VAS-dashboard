using Microsoft.Extensions.Options;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.Services;
using Vas.Application.Features.ExecutiveSummary.Validators;
using Vas.Domain.Reporting;
using Vas.Infrastructure.Persistence.Mock;

namespace Vas.ApplicationTests.Support;

public sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}

/// <summary>Builds the real services over any <see cref="IReportingDbContext"/>, with fixed time and thresholds.</summary>
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
        },
    };

    public static ExecutiveSummaryService ExecutiveSummary(IReportingDbContext db)
    {
        var thresholds = Options.Create(Thresholds);

        return new ExecutiveSummaryService(
            db,
            new ReportContextService(db, new FixedClock(Now), thresholds),
            new KpiCardService(thresholds),
            new ExecutiveSummaryQueryParamsValidator());
    }

    public static ExecutiveSummaryService ExecutiveSummary(MockGoldDataSet data) =>
        ExecutiveSummary(new JsonReportingSource(data));
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
    ];

    private static int _sequence;

    public static VisaApplication Application(
        string submitted,
        string? outcome = ApplicationOutcome.Approved,
        int? turnaroundDays = 5,
        string country = "AE",
        string visaType = "TOUR")
    {
        var submittedDate = DateOnly.Parse(submitted);
        var isDecided = outcome is not null;

        return new VisaApplication
        {
            ApplicationId = $"APP-{Interlocked.Increment(ref _sequence):D6}",
            SubmittedDate = submittedDate,
            CountryCode = country,
            CentreCode = country == "AE" ? "AE-DXB" : "IN-BOM",
            VisaTypeCode = visaType,
            Channel = ApplicationChannel.Digital,
            CurrentStage = isDecided ? ApplicationStageName.Decision : ApplicationStageName.Review,
            Status = isDecided ? ApplicationStatus.Processed : ApplicationStatus.Pending,
            Outcome = outcome,
            DecisionDate = isDecided ? submittedDate.AddDays(turnaroundDays ?? 0) : null,
            TurnaroundDays = isDecided ? turnaroundDays : null,
        };
    }

    public static VasRevenueTransaction Revenue(string date, decimal amount, string currency = "AED", string country = "AE") =>
        new()
        {
            TransactionId = $"TXN-{Interlocked.Increment(ref _sequence):D7}",
            TransactionDate = DateOnly.Parse(date),
            CountryCode = country,
            CentreCode = country == "AE" ? "AE-DXB" : "IN-BOM",
            VisaTypeCode = "TOUR",
            ServiceName = "Service Fee",
            Amount = amount,
            Currency = currency,
        };

    public static RefreshLog Refresh(string atUtc, string status = RefreshStatus.Succeeded) =>
        new() { RefreshedAtUtc = DateTime.Parse(atUtc).ToUniversalTime(), Status = status };

    public static MockGoldDataSet DataSet(
        IEnumerable<VisaApplication>? applications = null,
        IEnumerable<VasRevenueTransaction>? revenue = null,
        IEnumerable<RefreshLog>? refreshLogs = null) =>
        new(Countries, Centres, VisaTypes, [.. applications ?? []], [], [.. revenue ?? []], [.. refreshLogs ?? []]);
}
