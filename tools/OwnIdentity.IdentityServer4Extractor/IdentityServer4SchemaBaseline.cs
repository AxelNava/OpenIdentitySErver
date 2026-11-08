namespace OwnIdentity.IdentityServer4Extractor;

internal static class IdentityServer4SchemaBaseline
{
    internal const string BaselineName = "SQL Server / IdentityServer4.EntityFramework 4.1";
    internal const string MigrationId = "20200522172542_Config";
    internal const string MigrationProductVersion = "3.1.0";

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedTableColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["__EFMigrationsHistory"] = ["MigrationId", "ProductVersion"],
            ["ApiResourceClaims"] = ["Id", "Type", "ApiResourceId"],
            ["ApiResourceProperties"] = ["Id", "Key", "Value", "ApiResourceId"],
            ["ApiResources"] =
            [
                "Id", "Enabled", "Name", "DisplayName", "Description",
                "AllowedAccessTokenSigningAlgorithms", "ShowInDiscoveryDocument", "Created", "Updated",
                "LastAccessed", "NonEditable"
            ],
            ["ApiResourceScopes"] = ["Id", "Scope", "ApiResourceId"],
            ["ApiResourceSecrets"] = ["Id", "Description", "Value", "Expiration", "Type", "Created", "ApiResourceId"],
            ["ApiScopeClaims"] = ["Id", "Type", "ScopeId"],
            ["ApiScopeProperties"] = ["Id", "Key", "Value", "ScopeId"],
            ["ApiScopes"] = ["Id", "Enabled", "Name", "DisplayName", "Description", "Required", "Emphasize", "ShowInDiscoveryDocument"],
            ["ClientClaims"] = ["Id", "Type", "Value", "ClientId"],
            ["ClientCorsOrigins"] = ["Id", "Origin", "ClientId"],
            ["ClientGrantTypes"] = ["Id", "GrantType", "ClientId"],
            ["ClientIdPRestrictions"] = ["Id", "Provider", "ClientId"],
            ["ClientPostLogoutRedirectUris"] = ["Id", "PostLogoutRedirectUri", "ClientId"],
            ["ClientProperties"] = ["Id", "Key", "Value", "ClientId"],
            ["ClientRedirectUris"] = ["Id", "RedirectUri", "ClientId"],
            ["Clients"] =
            [
                "Id", "Enabled", "ClientId", "ProtocolType", "RequireClientSecret", "ClientName",
                "Description", "ClientUri", "LogoUri", "RequireConsent", "AllowRememberConsent",
                "AlwaysIncludeUserClaimsInIdToken", "RequirePkce", "AllowPlainTextPkce",
                "RequireRequestObject", "AllowAccessTokensViaBrowser", "FrontChannelLogoutUri",
                "FrontChannelLogoutSessionRequired", "BackChannelLogoutUri",
                "BackChannelLogoutSessionRequired", "AllowOfflineAccess", "IdentityTokenLifetime",
                "AllowedIdentityTokenSigningAlgorithms", "AccessTokenLifetime", "AuthorizationCodeLifetime",
                "ConsentLifetime", "AbsoluteRefreshTokenLifetime", "SlidingRefreshTokenLifetime",
                "RefreshTokenUsage", "UpdateAccessTokenClaimsOnRefresh", "RefreshTokenExpiration",
                "AccessTokenType", "EnableLocalLogin", "IncludeJwtId", "AlwaysSendClientClaims",
                "ClientClaimsPrefix", "PairWiseSubjectSalt", "Created", "Updated", "LastAccessed",
                "UserSsoLifetime", "UserCodeType", "DeviceCodeLifetime", "NonEditable"
            ],
            ["ClientScopes"] = ["Id", "Scope", "ClientId"],
            ["ClientSecrets"] = ["Id", "Description", "Value", "Expiration", "Type", "Created", "ClientId"],
            ["IdentityResourceClaims"] = ["Id", "Type", "IdentityResourceId"],
            ["IdentityResourceProperties"] = ["Id", "Key", "Value", "IdentityResourceId"],
            ["IdentityResources"] =
            [
                "Id", "Enabled", "Name", "DisplayName", "Description", "Required", "Emphasize",
                "ShowInDiscoveryDocument", "Created", "Updated", "NonEditable"
            ]
        };

    internal static IReadOnlyList<SourceColumn> ExpectedColumns => ExpectedTableColumns
        .SelectMany(table => table.Value.Select(column => new SourceColumn("dbo", table.Key, column)))
        .ToArray();

    internal static void ValidateStructure(IReadOnlyList<SourceColumn> actualColumns)
    {
        var expected = ExpectedColumns
            .Select(ColumnKey)
            .ToHashSet(StringComparer.Ordinal);
        var actual = actualColumns
            .Select(ColumnKey)
            .ToHashSet(StringComparer.Ordinal);

        if (actualColumns.Count != actual.Count || !actual.SetEquals(expected))
        {
            throw new UnsupportedSourceSchemaException();
        }
    }

    internal static void ValidateMigrations(IReadOnlyList<SourceMigration> migrations)
    {
        if (migrations.Count != 1
            || !string.Equals(migrations[0].MigrationId, MigrationId, StringComparison.Ordinal)
            || !string.Equals(migrations[0].ProductVersion, MigrationProductVersion, StringComparison.Ordinal))
        {
            throw new UnsupportedSourceSchemaException();
        }
    }

    private static string ColumnKey(SourceColumn column) =>
        $"{column.SchemaName}\u001f{column.TableName}\u001f{column.ColumnName}";
}
