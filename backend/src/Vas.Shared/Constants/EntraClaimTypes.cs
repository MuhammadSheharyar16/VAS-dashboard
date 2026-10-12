namespace Vas.Shared.Constants;

// Entra claims arrive under their short names or, when inbound claim mapping is on, under the long URIs.
public static class EntraClaimTypes
{
    public const string ObjectId = "oid";
    public const string ObjectIdUri = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    public const string Scope = "scp";
    public const string ScopeUri = "http://schemas.microsoft.com/identity/claims/scope";
    public const string Roles = "roles";
    public const string Name = "name";
    public const string PreferredUsername = "preferred_username";
}
