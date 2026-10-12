using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Vas.ApiTests.Support;

namespace Vas.ApiTests;

public class OpenApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string DocumentUrl = "/openapi/v1.json";

    [Fact]
    public async Task Document_declares_the_bearer_scheme_and_requires_it_on_protected_operations()
    {
        var response = await factory.CreateClient().GetAsync(DocumentUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        var operation = root.GetProperty("paths").GetProperty("/api/v1/auth/me").GetProperty("get");
        var requirement = Assert.Single(operation.GetProperty("security").EnumerateArray());
        Assert.True(requirement.TryGetProperty("Bearer", out _));
        Assert.True(operation.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
    }

    [Fact]
    public async Task Document_is_not_served_outside_development()
    {
        var production = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var response = await production.GetAsync(DocumentUrl);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
