using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vas.Application.Common.Interfaces;

namespace Vas.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string UserId =>
        (User is null ? null : EntraClaims.ObjectId(User)) ?? ICurrentUser.SystemUserId;

    public string? DisplayName => User is null ? null : EntraClaims.DisplayName(User);

    public string? Username => User is null ? null : EntraClaims.Username(User);

    public IReadOnlyCollection<string> Roles => User is null ? [] : EntraClaims.Roles(User);

    public IReadOnlyCollection<string> Scopes => User is null ? [] : EntraClaims.Scopes(User);
}
