namespace Vas.Application.Common.Interfaces;

public interface ICurrentUser
{
    /// <summary>Returned as <see cref="UserId"/> when there is no HTTP context.</summary>
    public const string SystemUserId = "system";

    bool IsAuthenticated { get; }

    string UserId { get; }

    string? DisplayName { get; }

    string? Username { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Scopes { get; }
}
