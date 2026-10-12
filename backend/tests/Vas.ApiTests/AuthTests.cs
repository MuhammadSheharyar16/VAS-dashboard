using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Vas.ApiTests.Support;
using Vas.Application.Features.Auth.DTOs;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MeUrl = "/api/v1/auth/me";

    [Fact]
    public async Task Request_without_a_token_gets_401_in_the_envelope()
    {
        var response = await factory.CreateClient().GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.False(string.IsNullOrEmpty(body.TraceId));
    }

    [Fact]
    public async Task Expired_token_gets_401()
    {
        var token = TestTokens.Create(roles: [AppRoles.Viewer], expires: DateTime.UtcNow.AddMinutes(-5));

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_for_another_audience_gets_401()
    {
        var token = TestTokens.Create(roles: [AppRoles.Viewer], audience: "99999999-9999-9999-9999-999999999999");

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_signed_with_an_untrusted_key_gets_401()
    {
        var token = TestTokens.Create(roles: [AppRoles.Viewer], signWithUntrustedKey: true);

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_token_gets_401()
    {
        var response = await factory.CreateClientWithToken("not-a-token").GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_user_without_the_viewer_role_gets_403_in_the_envelope()
    {
        var token = TestTokens.Create(roles: []);

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Signed_in_user_with_an_unknown_role_gets_403()
    {
        var token = TestTokens.Create(roles: ["SomethingElse.Admin"]);

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_token_without_the_required_scope_gets_403()
    {
        var token = TestTokens.Create(roles: [AppRoles.Viewer], scope: "Other.Scope");

        var response = await factory.CreateClientWithToken(token).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_gets_their_safe_profile()
    {
        var response = await factory.CreateClientWithToken(TestTokens.Viewer()).GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserDto>>();
        Assert.NotNull(body?.Data);
        Assert.True(body.Success);
        Assert.Equal(TestTokens.UserObjectId, body.Data.Id);
        Assert.Equal("Test Executive", body.Data.DisplayName);
        Assert.Equal("executive@example.test", body.Data.Username);
        Assert.Contains(AppRoles.Viewer, body.Data.Roles);
    }

    [Fact]
    public async Task Viewer_permissions_cover_the_two_active_screens()
    {
        var response = await factory.CreateClientWithToken(TestTokens.Viewer())
            .GetAsync("/api/v1/auth/me/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<PermissionDto>>>();
        Assert.NotNull(body?.Data);
        Assert.Equal(
            [Screens.Dashboard, Screens.ExecutiveSummary, Screens.Vas],
            body.Data.Select(permission => permission.Screen));
        Assert.All(body.Data, permission => Assert.Equal(["View"], permission.Actions));
    }

    [Fact]
    public async Task Viewer_menu_lists_two_active_screens_then_four_coming_soon()
    {
        var response = await factory.CreateClientWithToken(TestTokens.Viewer())
            .GetAsync("/api/v1/auth/me/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<MenuItemDto>>>();
        Assert.NotNull(body?.Data);
        Assert.Equal(
            [Screens.ExecutiveSummary, Screens.Vas, Screens.Operations, Screens.Finance, Screens.Alfit, Screens.Csat],
            body.Data.Select(item => item.Code));
        Assert.Equal(
            [false, false, true, true, true, true],
            body.Data.Select(item => item.IsComingSoon));
    }

    [Fact]
    public async Task Client_config_is_anonymous_and_carries_only_the_sign_in_settings()
    {
        var response = await factory.CreateClient().GetAsync("/config.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();
        Assert.NotNull(config);
        Assert.Equal(TestTokens.TenantId, config.TenantId);
        Assert.Equal(TestTokens.SpaClientId, config.ClientId);
        Assert.Equal($"https://login.microsoftonline.com/{TestTokens.TenantId}", config.Authority);
        Assert.Equal($"api://{TestTokens.ApiClientId}/{TestTokens.Scope}", config.ApiScope);
    }

    [Fact]
    public void Startup_fails_outside_development_when_the_entra_ids_are_placeholders()
    {
        const string placeholder = "00000000-0000-0000-0000-000000000000";
        var production = factory.InEnvironment(
            "Production", builder => builder.UseSetting("AzureAd:TenantId", placeholder));

        var exception = Assert.ThrowsAny<Exception>(() => production.CreateClient());
        Assert.Contains("AzureAd", exception.ToString());
    }
}
