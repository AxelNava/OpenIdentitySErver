namespace OwnIdentity.IdentityService.Security;

public static class IdentityConfiguration
{
    public const string AdministratorRole = "Administrator";
    public const string AdminScope = "identity.admin";
    public const string AdminPolicy = "IdentityAdministration";
    public const string AdminClientId = "ownidentity-admin";
    public const string AdminRedirectUri = "http://localhost:5173/auth/callback";
    public const string AdminPostLogoutRedirectUri = "http://localhost:5173/auth/logout-callback";
}
