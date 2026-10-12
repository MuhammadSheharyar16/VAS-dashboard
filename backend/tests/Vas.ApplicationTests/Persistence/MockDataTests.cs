using Vas.ApplicationTests.Support;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Domain.Reporting;
using Vas.Infrastructure.Persistence.Mock;
using Vas.MockDataGenerator;

namespace Vas.ApplicationTests.Persistence;

public class MockDataTests
{
    private static readonly MockGoldDataSet Data = MockGoldDataSet.FromEmbeddedFiles();

    [Fact]
    public void Embedded_files_match_what_the_generator_produces()
    {
        // If this fails, the generator changed without the files being regenerated (or the reverse):
        //   dotnet run --project backend/mock-data/generator -- backend/src/Vas.Infrastructure/MockData
        foreach (var (fileName, expected) in GoldJson.SerializeAll(MockGoldGenerator.Generate()))
        {
            using var reader = new StreamReader(MockGoldDataSet.OpenEmbedded(fileName));
            var embedded = reader.ReadToEnd().ReplaceLineEndings("\n");

            Assert.True(expected == embedded, $"{fileName} is out of date.");
        }
    }

    [Fact]
    public void Dimensions_have_the_agreed_sizes()
    {
        Assert.Equal(6, Data.Countries.Count);
        Assert.Equal(15, Data.Centres.Count);
        Assert.Equal(8, Data.VisaTypes.Count);
    }

    [Fact]
    public void Facts_cover_about_20000_applications_over_18_months()
    {
        Assert.InRange(Data.VisaApplications.Count, 19_000, 21_500);
        Assert.Equal(MockGoldGenerator.FirstDay, Data.VisaApplications.Min(a => a.SubmittedDate));
        Assert.Equal(MockGoldGenerator.LastDay, Data.VisaApplications.Max(a => a.SubmittedDate));
    }

    [Fact]
    public void Every_fact_row_points_at_a_known_country_centre_and_visa_type()
    {
        var countries = Data.Countries.Select(c => c.CountryCode).ToHashSet();
        var centres = Data.Centres.ToDictionary(c => c.CentreCode, c => c.CountryCode);
        var visaTypes = Data.VisaTypes.Select(v => v.VisaTypeCode).ToHashSet();

        Assert.All(Data.VisaApplications, a =>
        {
            Assert.Contains(a.CountryCode, countries);
            Assert.Equal(a.CountryCode, centres[a.CentreCode]);
            Assert.Contains(a.VisaTypeCode, visaTypes);
        });
        Assert.All(Data.VasRevenue, t => Assert.Equal(t.CountryCode, centres[t.CentreCode]));
    }

    [Fact]
    public void Decided_applications_have_an_outcome_date_and_turnaround_and_pending_ones_have_none()
    {
        Assert.All(Data.VisaApplications, a =>
        {
            if (a.Status == ApplicationStatus.Pending)
            {
                Assert.Null(a.Outcome);
                Assert.Null(a.DecisionDate);
                Assert.Null(a.TurnaroundDays);
            }
            else
            {
                Assert.NotNull(a.Outcome);
                Assert.Equal(a.SubmittedDate.AddDays(a.TurnaroundDays!.Value), a.DecisionDate);
            }
        });
    }

    [Fact]
    public void Data_includes_every_outcome_a_pending_backlog_and_days_without_applications()
    {
        var outcomes = Data.VisaApplications.Select(a => a.Outcome).Distinct().ToList();
        Assert.Contains(ApplicationOutcome.Approved, outcomes);
        Assert.Contains(ApplicationOutcome.Rejected, outcomes);
        Assert.Contains(ApplicationOutcome.Withdrawn, outcomes);

        var oldPending = Data.VisaApplications.Count(a =>
            a.Status == ApplicationStatus.Pending && a.SubmittedDate < MockGoldGenerator.LastDay.AddDays(-90));
        Assert.True(oldPending > 100, "Expected a backlog of long-pending applications.");

        var daysWithData = Data.VisaApplications.Select(a => a.SubmittedDate).ToHashSet();
        Assert.All(MockGoldGenerator.NoDataDays, day => Assert.DoesNotContain(day, daysWithData));
    }

    [Fact]
    public void One_centre_runs_close_to_its_daily_capacity()
    {
        var centre = Data.Centres.Single(c => c.CentreCode == MockGoldGenerator.NearCapacityCentreCode);
        var days = MockGoldGenerator.LastDay.DayNumber - MockGoldGenerator.FirstDay.DayNumber + 1;
        var perDay = Data.VisaApplications.Count(a => a.CentreCode == centre.CentreCode) / (double)days;

        Assert.InRange(perDay / centre.DailyCapacity, 0.80, 1.10);
    }

    [Fact]
    public void Stage_rows_follow_the_stage_order_and_only_the_current_stage_of_a_pending_application_is_open()
    {
        var applications = Data.VisaApplications.ToDictionary(a => a.ApplicationId);

        foreach (var group in Data.ApplicationStages.GroupBy(s => s.ApplicationId))
        {
            var stages = group.ToList();
            var application = applications[group.Key];

            Assert.Equal(ApplicationStageName.Ordered.Take(stages.Count), stages.Select(s => s.Stage));
            Assert.Equal(application.CurrentStage, stages[^1].Stage);
            Assert.All(stages.SkipLast(1), s => Assert.NotNull(s.ExitedAtUtc));
            Assert.Equal(application.Status == ApplicationStatus.Pending, stages[^1].ExitedAtUtc is null);
        }
    }

    [Fact]
    public void Revenue_is_positive_and_in_one_currency()
    {
        Assert.All(Data.VasRevenue, t => Assert.True(t.Amount > 0));
        Assert.Equal([MockGoldGenerator.Currency], Data.VasRevenue.Select(t => t.Currency).Distinct());
    }

    [Fact]
    public void Refresh_log_has_a_failed_run_and_its_latest_run_succeeded()
    {
        Assert.Contains(Data.RefreshLogs, log => log.Status == RefreshStatus.Failed);
        Assert.Equal(RefreshStatus.Succeeded, Data.RefreshLogs.MaxBy(log => log.RefreshedAtUtc)!.Status);
    }

    [Fact]
    public async Task Default_view_over_the_embedded_data_is_populated_and_shows_the_no_data_day()
    {
        var summary = await ReportingTestHost.ExecutiveSummary(Data)
            .GetAsync(new ExecutiveSummaryQueryParams(), CancellationToken.None);

        Assert.Equal(MockGoldGenerator.LastDay.AddDays(1), summary.Meta.Filter.ToExclusive);
        Assert.True(summary.Kpis.Single(k => k.Key == KpiKeys.ApplicationsReceived).Value > 500);
        Assert.NotNull(summary.Kpis.Single(k => k.Key == KpiKeys.ApplicationsReceived).ChangePercent);
        Assert.Equal(0, summary.ApplicationsTrend.Points.Single(p => p.PeriodStart == new DateOnly(2026, 9, 27)).Received);
        Assert.Equal(5, summary.TopCountries.Count);
    }
}
