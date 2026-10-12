using FluentValidation;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.Centres.DTOs;
using Vas.Application.Features.Centres.Services;
using Vas.Application.Features.Lookups.DTOs;
using Vas.Application.Features.Lookups.Services;
using Vas.Application.Features.VasApplications.DTOs;
using Vas.Application.Features.VasApplications.Services;
using Vas.Application.Features.VasOutcomes.DTOs;
using Vas.Application.Features.VasOutcomes.Services;
using Vas.Application.Features.VasProcessing.DTOs;
using Vas.Application.Features.VasProcessing.Services;
using Vas.Application.Features.VasRevenue.DTOs;
using Vas.Application.Features.VasRevenue.Services;
using Vas.Application.Features.VisaTypes.DTOs;
using Vas.Application.Features.VisaTypes.Services;
using Vas.ApplicationTests.Support;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;
using Vas.Infrastructure.Persistence.Mock;

namespace Vas.ApplicationTests.VasReports;

/// <summary>
/// All figures below come from one small data set.
/// Current period, 1 to 10 June (10 days), six applications:
///   A1 AE TOUR Digital approved in 4 days     A2 AE TOUR Centre approved in 6 days
///   A3 IN TOUR Digital approved in 8 days     A4 IN WORK Centre rejected in 10 days
///   A5 AE TOUR Digital withdrawn in 2 days    A6 AE TOUR Centre still pending
/// Previous period, 22 to 31 May: P1 AE Centre approved in 20 days, P2 AE Digital rejected in 10 days.
/// </summary>
public class VasReportServiceTests
{
    private static readonly DateOnly From = new(2026, 6, 1);
    private static readonly DateOnly ToExclusive = new(2026, 6, 11);

    private static readonly MockGoldDataSet Data = GoldRows.DataSet(
        applications:
        [
            GoldRows.Application("2026-06-01", ApplicationOutcome.Approved, 4, id: "A1"),
            GoldRows.Application("2026-06-01", ApplicationOutcome.Approved, 6, channel: ApplicationChannel.Centre, id: "A2"),
            GoldRows.Application("2026-06-03", ApplicationOutcome.Approved, 8, country: "IN", id: "A3"),
            GoldRows.Application("2026-06-03", ApplicationOutcome.Rejected, 10, country: "IN", visaType: "WORK", channel: ApplicationChannel.Centre, id: "A4"),
            GoldRows.Application("2026-06-05", ApplicationOutcome.Withdrawn, 2, id: "A5"),
            GoldRows.Application("2026-06-10", outcome: null, channel: ApplicationChannel.Centre, id: "A6"),
            GoldRows.Application("2026-05-25", ApplicationOutcome.Approved, 20, channel: ApplicationChannel.Centre, id: "P1"),
            GoldRows.Application("2026-05-28", ApplicationOutcome.Rejected, 10, id: "P2"),
        ],
        stages:
        [
            GoldRows.Stage("A1", ApplicationStageName.Submitted),
            GoldRows.Stage("A1", ApplicationStageName.Decision),
            GoldRows.Stage("A6", ApplicationStageName.Submitted),
            GoldRows.Stage("A6", ApplicationStageName.Review, exited: false),
            GoldRows.Stage("P1", ApplicationStageName.Submitted),
        ],
        revenue:
        [
            GoldRows.Revenue("2026-06-01", 350m),
            GoldRows.Revenue("2026-06-03", 150.50m, country: "IN"),
            GoldRows.Revenue("2026-06-04", 60m, serviceName: "Courier Return"),
            GoldRows.Revenue("2026-06-02", 100m, currency: "USD"),
            GoldRows.Revenue("2026-05-25", 250m),
        ]);

    private static TService Service<TService>()
        where TService : notnull => ReportingTestHost.Service<TService>(Data);

    private static KpiCardDto Kpi(IEnumerable<KpiCardDto> kpis, string key, string? currency = null) =>
        Assert.Single(kpis, card => card.Key == key && card.Currency == currency);

    // ---- Applications ----------------------------------------------------

    [Fact]
    public async Task Applications_report_counts_received_processed_and_pending_against_the_previous_period()
    {
        var report = await Service<IVasApplicationsService>().GetAsync(
            new VasApplicationsQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal((6m, 2m), (Kpi(report.Kpis, KpiKeys.ApplicationsReceived).Value!.Value, Kpi(report.Kpis, KpiKeys.ApplicationsReceived).PreviousValue!.Value));
        Assert.Equal(5, Kpi(report.Kpis, KpiKeys.ApplicationsProcessed).Value);
        Assert.Equal(1, Kpi(report.Kpis, KpiKeys.ApplicationsPending).Value);
        Assert.Equal(200.0m, Kpi(report.Kpis, KpiKeys.ApplicationsReceived).ChangePercent);
        Assert.Equal(10, report.Trend.Points.Count);
    }

    [Fact]
    public async Task Applications_report_breaks_down_by_country_visa_type_and_channel_largest_first()
    {
        var report = await Service<IVasApplicationsService>().GetAsync(
            new VasApplicationsQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(["AE", "IN"], report.ByCountry.Select(row => row.Key));
        var uae = report.ByCountry[0];
        Assert.Equal("United Arab Emirates", uae.Name);
        Assert.Equal("Middle East", uae.Group);
        Assert.Equal((4, 3, 1, 66.7m), (uae.Received, uae.Processed, uae.Pending, uae.SharePercent!.Value));

        Assert.Equal(["TOUR", "WORK"], report.ByVisaType.Select(row => row.Key));
        Assert.Equal("Tourist", report.ByVisaType[0].Name);

        Assert.Equal([ApplicationChannel.Centre, ApplicationChannel.Digital], report.ByChannel.Select(row => row.Key));
        Assert.All(report.ByChannel, row => Assert.Equal(50.0m, row.SharePercent));
    }

    // ---- Outcomes --------------------------------------------------------

    [Fact]
    public async Task Outcomes_report_gives_counts_rates_and_their_previous_values()
    {
        var report = await Service<IVasOutcomesService>().GetAsync(
            new VasOutcomesQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(3, Kpi(report.Kpis, KpiKeys.ApplicationsApproved).Value);
        Assert.Equal(1, Kpi(report.Kpis, KpiKeys.ApplicationsRejected).Value);
        Assert.Equal(1, Kpi(report.Kpis, KpiKeys.ApplicationsWithdrawn).Value);
        Assert.Equal(75.0m, Kpi(report.Kpis, KpiKeys.ApprovalRate).Value);
        Assert.Equal(25.0m, Kpi(report.Kpis, KpiKeys.RejectionRate).Value);
        Assert.Equal(50.0m, Kpi(report.Kpis, KpiKeys.ApprovalRate).PreviousValue);
        Assert.Equal(KpiStatus.Warning, Kpi(report.Kpis, KpiKeys.ApprovalRate).Status);
    }

    [Fact]
    public async Task Outcomes_report_trends_each_outcome_and_rates_each_visa_type()
    {
        var report = await Service<IVasOutcomesService>().GetAsync(
            new VasOutcomesQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(new OutcomeTrendPointDto(new DateOnly(2026, 6, 1), 2, 0, 0), report.Trend.Points[0]);
        Assert.Equal(new OutcomeTrendPointDto(new DateOnly(2026, 6, 3), 1, 1, 0), report.Trend.Points[2]);
        Assert.Equal(new OutcomeTrendPointDto(new DateOnly(2026, 6, 5), 0, 0, 1), report.Trend.Points[4]);

        var tourist = report.ByVisaType.Single(row => row.Key == "TOUR");
        var work = report.ByVisaType.Single(row => row.Key == "WORK");
        Assert.Equal((100.0m, 0.0m), (tourist.ApprovalRate!.Value, tourist.RejectionRate!.Value));
        Assert.Equal((0.0m, 100.0m), (work.ApprovalRate!.Value, work.RejectionRate!.Value));
        Assert.Equal(4, report.Outcomes.Count);
    }

    // ---- Processing ------------------------------------------------------

    [Fact]
    public async Task Processing_report_compares_digital_and_centre_turnaround()
    {
        var report = await Service<IVasProcessingService>().GetAsync(
            new VasProcessingQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(6.0m, Kpi(report.Kpis, KpiKeys.AverageTurnaroundDays).Value);
        Assert.Equal(4.7m, Kpi(report.Kpis, KpiKeys.DigitalTurnaroundDays).Value);
        Assert.Equal(8.0m, Kpi(report.Kpis, KpiKeys.CentreTurnaroundDays).Value);
        Assert.Equal(10.0m, Kpi(report.Kpis, KpiKeys.DigitalTurnaroundDays).PreviousValue);
        Assert.Equal(20.0m, Kpi(report.Kpis, KpiKeys.CentreTurnaroundDays).PreviousValue);
        Assert.Equal(50.0m, Kpi(report.Kpis, KpiKeys.DigitalSharePercent).Value);
    }

    [Fact]
    public async Task Processing_report_shows_statuses_the_pending_stage_and_the_stage_funnel_in_stage_order()
    {
        var report = await Service<IVasProcessingService>().GetAsync(
            new VasProcessingQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(
            [new CountShareDto(ApplicationStatus.Processed, 5, 83.3m), new CountShareDto(ApplicationStatus.Pending, 1, 16.7m)],
            report.Statuses);
        Assert.Equal([new CountShareDto(ApplicationStageName.Review, 1, 100.0m)], report.PendingByStage);

        // Stage rows of P1 are outside the period and must not be counted.
        Assert.Equal(
            [
                new StageFunnelDto(ApplicationStageName.Submitted, 2, 2, 100.0m),
                new StageFunnelDto(ApplicationStageName.Review, 1, 0, 0.0m),
                new StageFunnelDto(ApplicationStageName.Decision, 1, 1, 100.0m),
            ],
            report.StageFunnel);
        Assert.Equal(8.0m, report.Channels.Single(row => row.Key == ApplicationChannel.Centre).AverageTurnaroundDays);
    }

    // ---- Revenue ---------------------------------------------------------

    [Fact]
    public async Task Revenue_report_totals_each_currency_separately()
    {
        var report = await Service<IVasRevenueService>().GetAsync(
            new VasRevenueQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        var aed = Kpi(report.Kpis, KpiKeys.VasRevenue, "AED");
        var usd = Kpi(report.Kpis, KpiKeys.VasRevenue, "USD");
        Assert.Equal((560.50m, 250m), (aed.Value!.Value, aed.PreviousValue!.Value));
        Assert.Equal((100m, 0m), (usd.Value!.Value, usd.PreviousValue!.Value));
        Assert.Null(usd.ChangePercent);
        Assert.Equal((4m, 1m), (Kpi(report.Kpis, KpiKeys.RevenueTransactions).Value!.Value, Kpi(report.Kpis, KpiKeys.RevenueTransactions).PreviousValue!.Value));
    }

    [Fact]
    public async Task Revenue_breakdowns_share_within_their_own_currency()
    {
        var report = await Service<IVasRevenueService>().GetAsync(
            new VasRevenueQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(
            [
                new RevenueBreakdownRowDto("AE", "United Arab Emirates", "AED", 410m, 2, 73.1m),
                new RevenueBreakdownRowDto("IN", "India", "AED", 150.50m, 1, 26.9m),
                new RevenueBreakdownRowDto("AE", "United Arab Emirates", "USD", 100m, 1, 100.0m),
            ],
            report.ByCountry);

        Assert.Equal(
            [("Service Fee", "AED", 500.50m), ("Courier Return", "AED", 60m), ("Service Fee", "USD", 100m)],
            report.ByService.Select(row => (row.Key, row.Currency, row.Amount)));
        Assert.Equal("Dubai Centre", report.ByCentre[0].Name);
    }

    [Fact]
    public async Task Revenue_trend_has_a_point_per_period_for_every_currency()
    {
        var report = await Service<IVasRevenueService>().GetAsync(
            new VasRevenueQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(20, report.Trend.Points.Count);
        Assert.Contains(new RevenueTrendPointDto(new DateOnly(2026, 6, 1), "AED", 350m, 1), report.Trend.Points);
        Assert.Contains(new RevenueTrendPointDto(new DateOnly(2026, 6, 1), "USD", 0m, 0), report.Trend.Points);
        Assert.Contains(new RevenueTrendPointDto(new DateOnly(2026, 6, 2), "USD", 100m, 1), report.Trend.Points);
    }

    // ---- Visa types ------------------------------------------------------

    [Fact]
    public async Task Visa_types_report_lists_every_visa_type_including_those_without_applications()
    {
        var report = await Service<IVisaTypesService>().GetAsync(
            new VisaTypesQueryParams { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

        Assert.Equal(["TOUR", "WORK", "STU"], report.VisaTypes.Select(row => row.Key));
        var student = report.VisaTypes[2];
        Assert.Equal(("Student", "Long stay", 0), (student.Name, student.Group, student.Received));
        Assert.Null(student.ApprovalRate);
        Assert.Equal(83.3m, report.VisaTypes[0].SharePercent);
    }

    [Fact]
    public async Task Visa_types_report_with_a_visa_type_filter_shows_only_that_type()
    {
        var report = await Service<IVisaTypesService>().GetAsync(
            new VisaTypesQueryParams { From = From, ToExclusive = ToExclusive, VisaTypeCode = "STU" }, CancellationToken.None);

        Assert.Equal("STU", Assert.Single(report.VisaTypes).Key);
    }

    // ---- Centres ---------------------------------------------------------

    private static Task<CentresDto> Centres(CentresQueryParams query) =>
        Service<ICentresService>().GetAsync(query with { From = From, ToExclusive = ToExclusive }, CancellationToken.None);

    [Fact]
    public async Task Centres_report_gives_volumes_turnaround_and_capacity_use_per_centre()
    {
        var report = await Centres(new CentresQueryParams());

        Assert.Equal(["AE-DXB", "IN-BOM"], report.Centres.Items.Select(row => row.CentreCode));

        var dubai = report.Centres.Items[0];
        Assert.Equal(("Dubai Centre", "United Arab Emirates", "Dubai", 6), (dubai.CentreName, dubai.CountryName, dubai.City, dubai.DailyCapacity));
        Assert.Equal((4, 3, 1), (dubai.Received, dubai.Processed, dubai.Pending));
        Assert.Equal(100.0m, dubai.ApprovalRate);
        Assert.Equal(4.0m, dubai.AverageTurnaroundDays);
        Assert.Equal(0.4m, dubai.AverageDailyApplications);

        // 4 applications over 10 days against a capacity of 6 a day
        Assert.Equal(6.7m, dubai.CapacityUtilisationPercent);
        Assert.Equal(KpiStatus.Good, dubai.CapacityStatus);
    }

    [Theory]
    [InlineData("received", SortDirection.Descending, "AE-DXB")]
    [InlineData("received", SortDirection.Ascending, "IN-BOM")]
    [InlineData("averageTurnaroundDays", SortDirection.Descending, "IN-BOM")]
    [InlineData("CAPACITYUTILISATION", SortDirection.Descending, "AE-DXB")]
    [InlineData("centreName", SortDirection.Descending, "IN-BOM")]
    public async Task Centres_report_sorts_by_the_allowed_fields(string sortBy, SortDirection direction, string expectedFirst)
    {
        var report = await Centres(new CentresQueryParams { SortBy = sortBy, SortDirection = direction });

        Assert.Equal(expectedFirst, report.Centres.Items[0].CentreCode);
    }

    [Fact]
    public async Task Centres_report_refuses_a_sort_field_that_is_not_on_the_list()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => Centres(new CentresQueryParams { SortBy = "dailyCapacity; DROP TABLE" }));

        Assert.Equal(nameof(CentresQueryParams.SortBy), Assert.Single(exception.Errors).PropertyName);
    }

    [Fact]
    public async Task Centres_report_pages_the_table()
    {
        var report = await Centres(new CentresQueryParams { Page = 2, PageSize = 1 });

        Assert.Equal("IN-BOM", Assert.Single(report.Centres.Items).CentreCode);
        Assert.Equal((2, 2, 1, 2), (report.Centres.TotalCount, report.Centres.Page, report.Centres.PageSize, report.Centres.TotalPages));
        Assert.True(report.Centres.HasPreviousPage);
        Assert.False(report.Centres.HasNextPage);
    }

    [Theory]
    [InlineData(0, 5_000, 1, 100)]
    [InlineData(-3, 0, 1, 1)]
    [InlineData(1, null, 1, 20)]
    public async Task Centres_report_clamps_the_page_and_the_page_size(int page, int? pageSize, int expectedPage, int expectedPageSize)
    {
        var report = await Centres(new CentresQueryParams { Page = page, PageSize = pageSize });

        Assert.Equal((expectedPage, expectedPageSize), (report.Centres.Page, report.Centres.PageSize));
    }

    [Fact]
    public async Task Centres_report_keeps_a_centre_with_no_applications_and_respects_the_country_filter()
    {
        var workOnly = await Centres(new CentresQueryParams { VisaTypeCode = "WORK" });
        var dubai = workOnly.Centres.Items.Single(row => row.CentreCode == "AE-DXB");
        Assert.Equal((0, 0m), (dubai.Received, dubai.CapacityUtilisationPercent!.Value));
        Assert.Null(dubai.AverageTurnaroundDays);

        var indiaOnly = await Centres(new CentresQueryParams { CountryCode = "IN" });
        Assert.Equal("IN-BOM", Assert.Single(indiaOnly.Centres.Items).CentreCode);
    }

    [Theory]
    [InlineData(50, KpiStatus.Good)]
    [InlineData(85, KpiStatus.Warning)]
    [InlineData(99.9, KpiStatus.Warning)]
    [InlineData(100, KpiStatus.Critical)]
    public void Capacity_status_follows_the_threshold_where_lower_is_better(double utilisation, KpiStatus expected)
    {
        var status = Service<IKpiCardService>().StatusFor(KpiKeys.CapacityUtilisation, (decimal)utilisation);

        Assert.Equal(expected, status);
    }

    // ---- Lookups ---------------------------------------------------------

    [Fact]
    public async Task Lookups_are_sorted_by_name_and_carry_their_group()
    {
        var lookups = Service<ILookupService>();

        var countries = await lookups.GetCountriesAsync(CancellationToken.None);
        Assert.Equal([new LookupItemDto("IN", "India", "South Asia"), new LookupItemDto("AE", "United Arab Emirates", "Middle East")], countries);

        var visaTypes = await lookups.GetVisaTypesAsync(CancellationToken.None);
        Assert.Equal(["Employment", "Student", "Tourist"], visaTypes.Select(item => item.Name));
    }

    [Fact]
    public async Task Centre_lookup_can_be_limited_to_a_country_and_rejects_an_unsafe_code()
    {
        var lookups = Service<ILookupService>();

        Assert.Equal(2, (await lookups.GetCentresAsync(new CentreLookupQueryParams(), CancellationToken.None)).Count);

        var uae = await lookups.GetCentresAsync(new CentreLookupQueryParams { CountryCode = "AE" }, CancellationToken.None);
        Assert.Equal(new LookupItemDto("AE-DXB", "Dubai Centre", "AE"), Assert.Single(uae));

        await Assert.ThrowsAsync<ValidationException>(
            () => lookups.GetCentresAsync(new CentreLookupQueryParams { CountryCode = "AE' OR 1=1" }, CancellationToken.None));
    }
}
