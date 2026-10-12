using Microsoft.Extensions.Options;
using Vas.Application.Common.Exceptions;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Features.Auth.Rules;
using Vas.Application.Features.Auth.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;

namespace Vas.ApplicationTests.Auth;

public class AuthServiceTests
{
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; } = true;
        public string UserId { get; init; } = "user-1";
        public string? DisplayName { get; init; } = "Test Executive";
        public string? Username { get; init; } = "executive@example.test";
        public IReadOnlyCollection<string> Roles { get; init; } = [];
        public IReadOnlyCollection<string> Scopes { get; init; } = [];
    }

    private static AuthService CreateService(FakeCurrentUser user) =>
        new(user, Options.Create(new EntraOptions
        {
            TenantId = "tenant",
            ClientId = "api-client",
            SpaClientId = "spa-client",
        }));

    [Fact]
    public void Anonymous_caller_cannot_read_a_profile_permissions_or_menu()
    {
        var service = CreateService(new FakeCurrentUser { IsAuthenticated = false });

        Assert.Throws<UnauthenticatedException>(() => service.GetCurrentUser());
        Assert.Throws<UnauthenticatedException>(() => service.GetPermissions());
        Assert.Throws<UnauthenticatedException>(() => service.GetMenu());
    }

    [Fact]
    public void User_without_roles_has_no_permissions_and_sees_only_coming_soon_items()
    {
        var service = CreateService(new FakeCurrentUser());

        Assert.Empty(service.GetPermissions());
        Assert.All(service.GetMenu(), item => Assert.True(item.IsComingSoon));
    }

    [Fact]
    public void Viewer_menu_is_ordered_and_starts_with_the_active_screens()
    {
        var service = CreateService(new FakeCurrentUser { Roles = [AppRoles.Viewer] });

        var menu = service.GetMenu();

        Assert.Equal(6, menu.Count);
        Assert.Equal(menu.OrderBy(item => item.Order), menu);
        Assert.Equal("/executive-summary", menu[0].Route);
        Assert.Equal("/vas", menu[1].Route);
    }

    [Fact]
    public void Client_config_exposes_the_spa_client_id_not_the_api_client_id()
    {
        var config = CreateService(new FakeCurrentUser()).GetClientConfig();

        Assert.Equal("spa-client", config.ClientId);
        Assert.Equal("https://login.microsoftonline.com/tenant", config.Authority);
        Assert.Equal("api://api-client/Dashboard.Read", config.ApiScope);
    }

    [Fact]
    public void Viewer_role_grants_view_on_the_active_screens_only()
    {
        string[] roles = [AppRoles.Viewer];

        Assert.True(RolePermissionMap.IsGranted(roles, Screens.ExecutiveSummary, PermissionAction.View));
        Assert.True(RolePermissionMap.IsGranted(roles, Screens.Vas, PermissionAction.View));
        Assert.False(RolePermissionMap.IsGranted(roles, Screens.Finance, PermissionAction.View));
        Assert.False(RolePermissionMap.IsGranted(["viewer"], Screens.Vas, PermissionAction.View));
    }

    [Theory]
    [InlineData("Permission:VAS:View", true)]
    [InlineData("Permission:VAS:Delete", false)]
    [InlineData("Permission:VAS", false)]
    [InlineData("Permission::View", false)]
    [InlineData("SomethingElse", false)]
    public void Policy_name_parses_only_when_it_names_a_screen_and_a_known_action(string policyName, bool expected)
    {
        var parsed = PermissionRequirement.TryParse(policyName, out var requirement);

        Assert.Equal(expected, parsed);
        if (expected)
        {
            Assert.Equal(policyName, requirement!.PolicyName);
        }
    }
}
