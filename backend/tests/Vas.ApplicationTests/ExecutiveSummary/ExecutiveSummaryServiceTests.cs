using FluentValidation;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.ApplicationTests.Support;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.ApplicationTests.ExecutiveSummary;

public class ExecutiveSummaryServiceTests
{
    private static readonly ExecutiveSummaryQueryParams June = new()
    {
        From = new DateOnly(2026, 6, 1),
        ToExclusive = new DateOnly(2026, 6, 11),
    };

    /// <summary>
    /// Current period (1 to 10 June): 6 received, 3 approved, 1 rejected, 1 withdrawn, 1 pending.
    /// Previous period (22 to 31 May): 4 received, 1 approved, 3 rejected.
    /// </summary>
    private static readonly VisaApplication[] Applications =
    [
        GoldRows.Application("2026-06-01", ApplicationOutcome.Approved, 4),
        GoldRows.Application("2026-06-01", ApplicationOutcome.Approved, 6),
        GoldRows.Application("2026-06-03", ApplicationOutcome.Approved, 8, country: "IN"),
        GoldRows.Application("2026-06-03", ApplicationOutcome.Rejected, 10, country: "IN", visaType: "WORK"),
        GoldRows.Application("2026-06-05", ApplicationOutcome.Withdrawn, 2),
        GoldRows.Application("2026-06-10", outcome: null),
        GoldRows.Application("2026-05-22", ApplicationOutcome.Approved, 20),
        GoldRows.Application("2026-05-25", ApplicationOutcome.Rejected, 20),
        GoldRows.Application("2026-05-28", ApplicationOutcome.Rejected, 20),
        GoldRows.Application("2026-05-31", ApplicationOutcome.Rejected, 20),
        GoldRows.Application("2026-06-11", ApplicationOutcome.Approved, 1),
        GoldRows.Application("2026-05-21", ApplicationOutcome.Approved, 1),
    ];

    private static readonly VasRevenueTransaction[] Revenue =
    [
        GoldRows.Revenue("2026-06-01", 350m),
        GoldRows.Revenue("2026-06-03", 150.50m),
        GoldRows.Revenue("2026-05-25", 250m),
        GoldRows.Revenue("2026-06-11", 999m),
    ];

    private static Task<ExecutiveSummaryDto> GetAsync(ExecutiveSummaryQueryParams query) =>
        ReportingTestHost.ExecutiveSummary(GoldRows.DataSet(Applications, Revenue)).GetAsync(query, CancellationToken.None);

    private static KpiCardDto Kpi(ExecutiveSummaryDto summary, string key) =>
        Assert.Single(summary.Kpis, card => card.Key == key);

    [Fact]
    public async Task Volume_kpis_count_only_applications_submitted_inside_the_half_open_range()
    {
        var summary = await GetAsync(June);

        Assert.Equal(6, Kpi(summary, KpiKeys.ApplicationsReceived).Value);
        Assert.Equal(5, Kpi(summary, KpiKeys.ApplicationsProcessed).Value);
        Assert.Equal(1, Kpi(summary, KpiKeys.ApplicationsPending).Value);
    }

    [Fact]
    public async Task Approval_rate_is_approved_over_approved_plus_rejected()
    {
        var summary = await GetAsync(June);

        var approvalRate = Kpi(summary, KpiKeys.ApprovalRate);
        Assert.Equal(75.0m, approvalRate.Value);
        Assert.Equal(KpiUnit.Percent, approvalRate.Unit);
        Assert.Equal(25.0m, approvalRate.PreviousValue);
    }

    [Fact]
    public async Task Average_turnaround_ignores_applications_without_a_decision()
    {
        var summary = await GetAsync(June);

        // (4 + 6 + 8 + 10 + 2) / 5 decided applications
        Assert.Equal(6.0m, Kpi(summary, KpiKeys.AverageTurnaroundDays).Value);
    }

    [Fact]
    public async Task Comparison_uses_the_period_of_equal_length_immediately_before()
    {
        var summary = await GetAsync(June);

        var received = Kpi(summary, KpiKeys.ApplicationsReceived);
        Assert.Equal(4, received.PreviousValue);
        Assert.Equal(50.0m, received.ChangePercent);
        Assert.Equal(TrendDirection.Up, received.Trend);
        Assert.Equal(new DateOnly(2026, 5, 22), summary.Meta.Filter.PreviousFrom);
        Assert.Equal(new DateOnly(2026, 6, 1), summary.Meta.Filter.PreviousToExclusive);
    }

    [Fact]
    public async Task Comparison_can_be_switched_off()
    {
        var summary = await GetAsync(June with { Compare = ComparisonPeriod.None });

        Assert.All(summary.Kpis, card =>
        {
            Assert.Null(card.PreviousValue);
            Assert.Null(card.ChangePercent);
            Assert.Equal(TrendDirection.Flat, card.Trend);
        });
        Assert.Null(summary.Meta.Filter.PreviousFrom);
    }

    [Fact]
    public async Task Status_comes_from_the_configured_thresholds()
    {
        var summary = await GetAsync(June);

        // 75% sits exactly on the warning line; 6.0 days is under the 10-day warning line.
        Assert.Equal(KpiStatus.Warning, Kpi(summary, KpiKeys.ApprovalRate).Status);
        Assert.Equal(KpiStatus.Good, Kpi(summary, KpiKeys.AverageTurnaroundDays).Status);
        Assert.Equal(KpiStatus.Neutral, Kpi(summary, KpiKeys.ApplicationsReceived).Status);
        Assert.True(summary.Meta.ThresholdsArePlaceholders);
    }

    [Fact]
    public async Task Revenue_is_totalled_for_the_range_in_its_own_currency()
    {
        var summary = await GetAsync(June);

        var revenue = Kpi(summary, KpiKeys.VasRevenue);
        Assert.Equal(500.50m, revenue.Value);
        Assert.Equal("AED", revenue.Currency);
        Assert.Equal(250m, revenue.PreviousValue);
    }

    [Fact]
    public async Task Revenue_in_two_currencies_gives_two_cards_and_is_never_added_together()
    {
        var revenue = Revenue.Append(GoldRows.Revenue("2026-06-02", 100m, currency: "USD"));
        var service = ReportingTestHost.ExecutiveSummary(GoldRows.DataSet(Applications, revenue));

        var summary = await service.GetAsync(June, CancellationToken.None);

        var cards = summary.Kpis.Where(card => card.Key == KpiKeys.VasRevenue).ToList();
        Assert.Equal(["AED", "USD"], cards.Select(card => card.Currency));
        Assert.Equal([500.50m, 100m], cards.Select(card => card.Value));
    }

    [Fact]
    public async Task Country_filter_narrows_applications_and_revenue()
    {
        var summary = await GetAsync(June with { CountryCode = "IN" });

        Assert.Equal(2, Kpi(summary, KpiKeys.ApplicationsReceived).Value);
        Assert.Equal(50.0m, Kpi(summary, KpiKeys.ApprovalRate).Value);
        Assert.Equal(0m, Kpi(summary, KpiKeys.VasRevenue).Value);
        Assert.Equal("IN", Assert.Single(summary.TopCountries).CountryCode);
    }

    [Fact]
    public async Task Visa_type_filter_narrows_the_figures()
    {
        var summary = await GetAsync(June with { VisaTypeCode = "WORK" });

        Assert.Equal(1, Kpi(summary, KpiKeys.ApplicationsReceived).Value);
        Assert.Equal(0.0m, Kpi(summary, KpiKeys.ApprovalRate).Value);
    }

    [Fact]
    public async Task Daily_trend_has_one_point_per_day_with_zeros_for_days_without_data()
    {
        var summary = await GetAsync(June);

        var trend = summary.ApplicationsTrend;
        Assert.Equal(TrendGranularity.Day, trend.Granularity);
        Assert.Equal(10, trend.Points.Count);
        Assert.Equal(new TrendPointDto(new DateOnly(2026, 6, 1), 2, 2), trend.Points[0]);
        Assert.Equal(new TrendPointDto(new DateOnly(2026, 6, 2), 0, 0), trend.Points[1]);
        Assert.Equal(new TrendPointDto(new DateOnly(2026, 6, 10), 1, 0), trend.Points[9]);
    }

    [Fact]
    public async Task Trend_switches_to_months_for_a_long_range()
    {
        var summary = await GetAsync(new ExecutiveSummaryQueryParams
        {
            From = new DateOnly(2026, 3, 15),
            ToExclusive = new DateOnly(2026, 6, 15),
        });

        var trend = summary.ApplicationsTrend;
        Assert.Equal(TrendGranularity.Month, trend.Granularity);
        Assert.Equal(
            [new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 1)],
            trend.Points.Select(point => point.PeriodStart));
        Assert.Equal([0, 0, 5, 7], trend.Points.Select(point => point.Received));
    }

    [Fact]
    public async Task Outcomes_share_the_received_total_and_top_countries_are_ranked()
    {
        var summary = await GetAsync(June);

        Assert.Equal(
            [("Approved", 3, 50.0m), ("Rejected", 1, 16.7m), ("Withdrawn", 1, 16.7m), ("Pending", 1, 16.7m)],
            summary.Outcomes.Select(outcome => (outcome.Outcome, outcome.Count, outcome.SharePercent!.Value)));

        Assert.Equal(["AE", "IN"], summary.TopCountries.Select(country => country.CountryCode));
        Assert.Equal("United Arab Emirates", summary.TopCountries[0].CountryName);
        Assert.Equal(66.7m, summary.TopCountries[0].SharePercent);
    }

    [Fact]
    public async Task A_range_without_data_gives_zero_counts_and_no_rates()
    {
        var summary = await GetAsync(new ExecutiveSummaryQueryParams
        {
            From = new DateOnly(2024, 1, 1),
            ToExclusive = new DateOnly(2024, 1, 8),
        });

        Assert.Equal(0, Kpi(summary, KpiKeys.ApplicationsReceived).Value);
        Assert.Null(Kpi(summary, KpiKeys.ApprovalRate).Value);
        Assert.Null(Kpi(summary, KpiKeys.AverageTurnaroundDays).Value);
        Assert.Equal(KpiStatus.Neutral, Kpi(summary, KpiKeys.ApprovalRate).Status);
        Assert.Equal(0m, Kpi(summary, KpiKeys.VasRevenue).Value);
        Assert.Empty(summary.TopCountries);
        Assert.All(summary.ApplicationsTrend.Points, point => Assert.Equal(0, point.Received));
    }

    [Fact]
    public async Task Default_view_is_the_30_days_ending_at_the_last_successful_refresh()
    {
        var data = GoldRows.DataSet(Applications, Revenue, refreshLogs:
        [
            GoldRows.Refresh("2026-06-09T02:00:00Z"),
            GoldRows.Refresh("2026-06-10T02:00:00Z"),
            GoldRows.Refresh("2026-06-11T02:00:00Z", RefreshStatus.Failed),
        ]);

        var summary = await ReportingTestHost.ExecutiveSummary(data)
            .GetAsync(new ExecutiveSummaryQueryParams(), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 5, 11), summary.Meta.Filter.From);
        Assert.Equal(new DateOnly(2026, 6, 10), summary.Meta.Filter.ToExclusive);
        Assert.Equal(new DateTime(2026, 6, 10, 2, 0, 0, DateTimeKind.Utc), summary.Meta.Freshness.LastRefreshedAtUtc);
        Assert.Equal(DateTimeKind.Utc, summary.Meta.Freshness.LastRefreshedAtUtc!.Value.Kind);
    }

    [Theory]
    [InlineData(7, "2026-06-03")]
    [InlineData(90, "2026-03-12")]
    public async Task A_preset_period_ends_at_the_last_successful_refresh(int lastDays, string expectedFrom)
    {
        var data = GoldRows.DataSet(Applications, Revenue, refreshLogs: [GoldRows.Refresh("2026-06-10T02:00:00Z")]);

        var summary = await ReportingTestHost.ExecutiveSummary(data)
            .GetAsync(new ExecutiveSummaryQueryParams { LastDays = lastDays }, CancellationToken.None);

        Assert.Equal(DateOnly.Parse(expectedFrom), summary.Meta.Filter.From);
        Assert.Equal(new DateOnly(2026, 6, 10), summary.Meta.Filter.ToExclusive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(731)]
    public async Task A_preset_period_outside_1_to_730_days_is_rejected(int lastDays)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => GetAsync(new ExecutiveSummaryQueryParams { LastDays = lastDays }));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(ReportFilter.LastDays));
    }

    [Fact]
    public async Task A_preset_period_cannot_be_combined_with_a_date_range()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => GetAsync(June with { LastDays = 30 }));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(ReportFilter.LastDays));
    }

    [Fact]
    public async Task Default_view_falls_back_to_the_clock_when_no_refresh_is_recorded()
    {
        var summary = await GetAsync(new ExecutiveSummaryQueryParams());

        Assert.Null(summary.Meta.Freshness.LastRefreshedAtUtc);
        Assert.Equal(DateOnly.FromDateTime(ReportingTestHost.Now).AddDays(1), summary.Meta.Filter.ToExclusive);
        Assert.Equal(30, summary.Meta.Filter.ToExclusive.DayNumber - summary.Meta.Filter.From.DayNumber);
    }

    [Fact]
    public async Task Meta_reports_the_data_source_and_generation_time()
    {
        var summary = await GetAsync(June);

        Assert.Equal(ReportingDataSource.Mock, summary.Meta.DataSource);
        Assert.Equal(ReportingTestHost.Now, summary.Meta.GeneratedAtUtc);
    }

    [Theory]
    [InlineData("2026-06-10", "2026-06-10")]
    [InlineData("2026-06-10", "2026-06-01")]
    [InlineData("2024-01-01", "2026-01-02")]
    public async Task Empty_reversed_or_over_24_month_ranges_are_rejected(string from, string toExclusive)
    {
        var query = new ExecutiveSummaryQueryParams { From = DateOnly.Parse(from), ToExclusive = DateOnly.Parse(toExclusive) };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => GetAsync(query));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(ReportFilter.From));
    }

    [Fact]
    public async Task A_range_of_exactly_24_months_is_accepted()
    {
        var summary = await GetAsync(new ExecutiveSummaryQueryParams
        {
            From = new DateOnly(2024, 6, 1),
            ToExclusive = new DateOnly(2026, 6, 1),
        });

        Assert.Equal(TrendGranularity.Month, summary.ApplicationsTrend.Granularity);
    }

    [Theory]
    [InlineData("AE'; DROP", null, null, nameof(ReportFilter.CountryCode))]
    [InlineData(null, "THIS-CODE-IS-FAR-TOO-LONG", null, nameof(ReportFilter.CentreCode))]
    [InlineData(null, null, "<script>", nameof(ReportFilter.VisaTypeCode))]
    public async Task Codes_with_unsafe_characters_or_over_20_characters_are_rejected(
        string? country, string? centre, string? visaType, string expectedField)
    {
        var query = June with { CountryCode = country, CentreCode = centre, VisaTypeCode = visaType };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => GetAsync(query));

        Assert.Contains(exception.Errors, error => error.PropertyName == expectedField);
    }

    [Theory]
    [InlineData("ZZ", null, null, nameof(ReportFilter.CountryCode))]
    [InlineData(null, "ZZ-NOPE", null, nameof(ReportFilter.CentreCode))]
    [InlineData(null, null, "NOPE", nameof(ReportFilter.VisaTypeCode))]
    public async Task Well_formed_codes_that_are_not_in_the_lookups_are_rejected(
        string? country, string? centre, string? visaType, string expectedField)
    {
        var query = June with { CountryCode = country, CentreCode = centre, VisaTypeCode = visaType };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => GetAsync(query));

        Assert.Equal(expectedField, Assert.Single(exception.Errors).PropertyName);
    }
}
