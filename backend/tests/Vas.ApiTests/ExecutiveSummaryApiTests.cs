using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vas.ApiTests.Support;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Domain.Enums;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class ExecutiveSummaryApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Url = "/api/v1/executive-summary";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient Viewer => factory.CreateClientWithToken(TestTokens.Viewer());

    [Fact]
    public async Task Request_without_a_token_gets_401()
    {
        var response = await factory.CreateClient().GetAsync(Url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_user_without_the_viewer_role_gets_403_and_no_data()
    {
        var response = await factory.CreateClientWithToken(TestTokens.Create(roles: [])).GetAsync(Url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Json);
        Assert.Null(body!.Data);
    }

    [Fact]
    public async Task Viewer_gets_the_default_view_from_mock_data_in_the_envelope()
    {
        var response = await Viewer.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ExecutiveSummaryDto>>(Json);
        Assert.True(body!.Success);

        var summary = body.Data!;
        Assert.Equal(ReportingDataSource.Mock, summary.Meta.DataSource);
        Assert.NotNull(summary.Meta.Freshness.LastRefreshedAtUtc);
        Assert.Equal(30, summary.Meta.Filter.ToExclusive.DayNumber - summary.Meta.Filter.From.DayNumber);
        Assert.Equal(ComparisonPeriod.PreviousPeriod, summary.Meta.Filter.Compare);
        Assert.Equal(
            [
                KpiKeys.ApplicationsReceived, KpiKeys.ApplicationsProcessed, KpiKeys.ApplicationsPending,
                KpiKeys.ApprovalRate, KpiKeys.AverageTurnaroundDays, KpiKeys.VasRevenue,
            ],
            summary.Kpis.Select(card => card.Key));
        Assert.Equal(30, summary.ApplicationsTrend.Points.Count);
    }

    [Fact]
    public async Task Enums_and_dates_are_sent_as_readable_text()
    {
        var json = await Viewer.GetStringAsync(Url);

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("Mock", data.GetProperty("meta").GetProperty("dataSource").GetString());
        Assert.Equal("Day", data.GetProperty("applicationsTrend").GetProperty("granularity").GetString());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", data.GetProperty("meta").GetProperty("filter").GetProperty("from").GetString()!);
        Assert.Equal("Count", data.GetProperty("kpis")[0].GetProperty("unit").GetString());
    }

    [Fact]
    public async Task Filters_from_the_query_string_are_applied_and_echoed_back()
    {
        var response = await Viewer.GetAsync(
            $"{Url}?from=2026-08-01&toExclusive=2026-09-01&countryCode=AE&visaTypeCode=TOUR&compare=None");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = (await response.Content.ReadFromJsonAsync<ApiResponse<ExecutiveSummaryDto>>(Json))!.Data!;

        Assert.Equal(new DateOnly(2026, 8, 1), summary.Meta.Filter.From);
        Assert.Equal(new DateOnly(2026, 9, 1), summary.Meta.Filter.ToExclusive);
        Assert.Equal("AE", summary.Meta.Filter.CountryCode);
        Assert.Equal("TOUR", summary.Meta.Filter.VisaTypeCode);
        Assert.Null(summary.Meta.Filter.PreviousFrom);
        Assert.Equal("AE", Assert.Single(summary.TopCountries).CountryCode);
        Assert.All(summary.Kpis, card => Assert.Null(card.PreviousValue));
    }

    [Theory]
    [InlineData("from=not-a-date", "from")]
    [InlineData("from=06/01/2026&toExclusive=2026-06-11", "from")]
    [InlineData("from=2026-06-01T10:00:00&toExclusive=2026-06-11", "from")]
    [InlineData("from=2026-06-01&toExclusive=June 11 2026", "toExclusive")]
    [InlineData("from=2026-09-01&toExclusive=2026-08-01", "from")]
    [InlineData("from=2024-01-01&toExclusive=2026-06-01", "from")]
    [InlineData("countryCode=ZZ", "countryCode")]
    [InlineData("countryCode=A%27%3B--", "countryCode")]
    [InlineData("centreCode=AAAAAAAAAAAAAAAAAAAAA", "centreCode")]
    [InlineData("compare=Sideways", "compare")]
    public async Task Invalid_filters_get_400_with_the_field_named_in_the_envelope(string queryString, string expectedField)
    {
        var response = await Viewer.GetAsync($"{Url}?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Json);
        Assert.False(body!.Success);
        Assert.Contains(body.Errors, error => string.Equals(error.Field, expectedField, StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrEmpty(body.TraceId));
    }

    [Fact]
    public void Startup_is_refused_when_mock_data_is_selected_in_uat_or_production()
    {
        foreach (var environment in new[] { "UAT", "Production" })
        {
            var host = factory.InEnvironment(
                environment, builder => builder.UseSetting("Reporting:DataSource", "Mock"));

            var exception = Assert.ThrowsAny<Exception>(() => host.CreateClient());
            Assert.Contains("DataSource=Mock", exception.ToString());
        }
    }

    [Fact]
    public void Startup_is_refused_when_fabric_is_selected_without_its_endpoint()
    {
        var host = factory.InEnvironment(
            "Production", builder => builder.UseSetting("Fabric:SqlEndpoint", ""));

        var exception = Assert.ThrowsAny<Exception>(() => host.CreateClient());
        Assert.Contains("Fabric:SqlEndpoint", exception.ToString());
    }

    [Fact]
    public async Task Api_starts_in_production_with_fabric_selected_and_serves_health_without_connecting()
    {
        var response = await factory.InEnvironment("Production").CreateClient().GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
