using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vas.ApiTests.Support;
using Vas.Application.Features.Centres.DTOs;
using Vas.Application.Features.Lookups.DTOs;
using Vas.Application.Features.VasApplications.DTOs;
using Vas.Application.Features.VasOutcomes.DTOs;
using Vas.Application.Features.VasProcessing.DTOs;
using Vas.Application.Features.VasRevenue.DTOs;
using Vas.Application.Features.VisaTypes.DTOs;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class VasReportsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static TheoryData<string> ProtectedUrls => new()
    {
        "/api/v1/vas/applications",
        "/api/v1/vas/outcomes",
        "/api/v1/vas/processing",
        "/api/v1/vas/revenue",
        "/api/v1/vas/visa-types",
        "/api/v1/vas/centres",
        "/api/v1/lookups/countries",
        "/api/v1/lookups/centres",
        "/api/v1/lookups/visa-types",
    };

    private HttpClient Viewer => factory.CreateClientWithToken(TestTokens.Viewer());

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await Viewer.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json);
        Assert.True(body!.Success);
        return body.Data!;
    }

    [Theory]
    [MemberData(nameof(ProtectedUrls))]
    public async Task Every_report_and_lookup_needs_a_token(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedUrls))]
    public async Task Every_report_and_lookup_refuses_a_signed_in_user_without_the_viewer_role(string url)
    {
        var response = await factory.CreateClientWithToken(TestTokens.Create(roles: [])).GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Json);
        Assert.Null(body!.Data);
    }

    [Theory]
    [MemberData(nameof(ProtectedUrls))]
    public async Task Every_report_and_lookup_answers_a_viewer_in_the_envelope(string url)
    {
        var response = await Viewer.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, document.RootElement.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task Applications_report_over_the_mock_data_has_every_country_visa_type_and_channel()
    {
        var report = await GetAsync<VasApplicationsDto>("/api/v1/vas/applications");

        Assert.Equal(ReportingDataSource.Mock, report.Meta.DataSource);
        Assert.Equal(6, report.ByCountry.Count);
        Assert.Equal(8, report.ByVisaType.Count);
        Assert.Equal(2, report.ByChannel.Count);
        Assert.Equal(30, report.Trend.Points.Count);
    }

    [Fact]
    public async Task Outcomes_report_over_the_mock_data_has_rates_between_0_and_100()
    {
        var report = await GetAsync<VasOutcomesDto>("/api/v1/vas/outcomes");

        Assert.Equal(5, report.Kpis.Count);
        Assert.All(report.ByVisaType, row => Assert.InRange(row.ApprovalRate ?? 0, 0, 100));
    }

    [Fact]
    public async Task Processing_report_over_the_mock_data_shows_all_five_stages_in_order()
    {
        var report = await GetAsync<VasProcessingDto>("/api/v1/vas/processing");

        Assert.Equal(ApplicationStageName.Ordered, report.StageFunnel.Select(stage => stage.Stage));
        Assert.True(report.StageFunnel[0].Reached >= report.StageFunnel[^1].Reached);
        Assert.NotEmpty(report.PendingByStage);
    }

    [Fact]
    public async Task Revenue_report_over_the_mock_data_is_in_one_currency()
    {
        var report = await GetAsync<VasRevenueDto>("/api/v1/vas/revenue");

        Assert.Equal(["AED"], report.ByService.Select(row => row.Currency).Distinct());
        Assert.Contains(report.ByService, row => row.Key == "Service Fee");
        Assert.Equal(15, report.ByCentre.Count);
    }

    [Fact]
    public async Task Visa_types_report_over_the_mock_data_lists_all_eight_types()
    {
        var report = await GetAsync<VisaTypesDto>("/api/v1/vas/visa-types");

        Assert.Equal(8, report.VisaTypes.Count);
    }

    [Fact]
    public async Task Centres_report_pages_sorts_and_puts_the_busiest_centre_first()
    {
        var report = await GetAsync<CentresDto>(
            "/api/v1/vas/centres?from=2025-04-12&toExclusive=2026-10-12&sortBy=capacityUtilisation&sortDirection=Descending&pageSize=5");

        Assert.Equal((15, 5, 3), (report.Centres.TotalCount, report.Centres.Items.Count, report.Centres.TotalPages));
        Assert.True(report.Centres.HasNextPage);

        var busiest = report.Centres.Items[0];
        Assert.Equal("AE-DXB", busiest.CentreCode);
        Assert.True(busiest.CapacityUtilisationPercent > 75);
    }

    [Theory]
    [InlineData("sortBy=dailyCapacity", "sortBy")]
    [InlineData("sortDirection=Sideways", "sortDirection")]
    [InlineData("page=abc", "page")]
    [InlineData("centreCode=ZZ-NOPE", "centreCode")]
    public async Task Centres_report_rejects_invalid_paging_sorting_and_filters_with_400(string queryString, string expectedField)
    {
        var response = await Viewer.GetAsync($"/api/v1/vas/centres?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Json);
        Assert.Contains(body!.Errors, error => string.Equals(error.Field, expectedField, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Lookups_return_the_filter_choices()
    {
        Assert.Equal(6, (await GetAsync<List<LookupItemDto>>("/api/v1/lookups/countries")).Count);
        Assert.Equal(15, (await GetAsync<List<LookupItemDto>>("/api/v1/lookups/centres")).Count);
        Assert.Equal(8, (await GetAsync<List<LookupItemDto>>("/api/v1/lookups/visa-types")).Count);

        var uaeCentres = await GetAsync<List<LookupItemDto>>("/api/v1/lookups/centres?countryCode=AE");
        Assert.Equal(3, uaeCentres.Count);
        Assert.All(uaeCentres, centre => Assert.Equal("AE", centre.Group));
    }

    [Fact]
    public async Task Centre_lookup_rejects_an_unsafe_country_code_with_400()
    {
        var response = await Viewer.GetAsync("/api/v1/lookups/centres?countryCode=A%27%3B--");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
