using System.Net;
using Microsoft.AspNetCore.Hosting;
using Vas.ApiTests.Support;

namespace Vas.ApiTests;

public class HostingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string PublicHost = "dashboard.example.test";

    private HttpClient CreateProductionClient(bool forwardedHeadersEnabled) =>
        factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ForwardedHeaders_Enabled", forwardedHeadersEnabled ? "true" : "false");
            })
            .CreateClient();

    private static HttpRequestMessage HealthRequestForwardedAsHttps()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        request.Headers.Host = PublicHost;
        request.Headers.Add("X-Forwarded-Proto", "https");
        return request;
    }

    [Fact]
    public async Task Behind_the_app_service_proxy_an_https_request_gets_the_hsts_header()
    {
        var client = CreateProductionClient(forwardedHeadersEnabled: true);

        var response = await client.SendAsync(HealthRequestForwardedAsHttps());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Forwarded_headers_are_ignored_unless_the_host_enables_them()
    {
        var client = CreateProductionClient(forwardedHeadersEnabled: false);

        var response = await client.SendAsync(HealthRequestForwardedAsHttps());

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Api_starts_and_serves_requests_with_application_insights_configured()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting(
                "APPLICATIONINSIGHTS_CONNECTION_STRING",
                "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost/"))
            .CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
