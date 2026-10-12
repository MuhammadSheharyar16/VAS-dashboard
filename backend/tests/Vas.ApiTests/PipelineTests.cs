using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class PipelineTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Response_carries_a_generated_correlation_id_when_none_is_sent()
    {
        var response = await _client.GetAsync("/healthz");

        var correlationId = Assert.Single(response.Headers.GetValues(HttpHeaders.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task Response_echoes_a_safe_incoming_correlation_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        request.Headers.Add(HttpHeaders.CorrelationId, "abc-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("abc-123", Assert.Single(response.Headers.GetValues(HttpHeaders.CorrelationId)));
    }

    [Fact]
    public async Task Response_replaces_an_unsafe_incoming_correlation_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        request.Headers.Add(HttpHeaders.CorrelationId, "bad value <script>");

        var response = await _client.SendAsync(request);

        var correlationId = Assert.Single(response.Headers.GetValues(HttpHeaders.CorrelationId));
        Assert.NotEqual("bad value <script>", correlationId);
    }

    [Fact]
    public async Task Response_carries_the_security_headers()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Contains("frame-ancestors 'none'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_in_the_envelope_with_the_trace_id()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal(Assert.Single(response.Headers.GetValues(HttpHeaders.CorrelationId)), body.TraceId);
    }

    [Fact]
    public async Task Requests_over_the_limit_get_429_in_the_envelope()
    {
        var limited = factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitLimit", "2"))
            .CreateClient();

        await limited.GetAsync("/api/v1/does-not-exist");
        await limited.GetAsync("/api/v1/does-not-exist");
        var response = await limited.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Health_endpoint_is_not_rate_limited()
    {
        var limited = factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitLimit", "1"))
            .CreateClient();

        await limited.GetAsync("/healthz");
        var response = await limited.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Startup_fails_when_the_rate_limit_configuration_is_invalid()
    {
        var broken = factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitLimit", "0"));

        Assert.ThrowsAny<Exception>(() => broken.CreateClient());
    }
}
