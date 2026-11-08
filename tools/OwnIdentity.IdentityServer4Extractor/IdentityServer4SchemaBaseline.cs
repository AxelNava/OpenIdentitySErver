namespace OwnIdentity.IdentityServer4Extractor;

internal sealed record SchemaBaselineMatch(string BaselineName, IReadOnlyList<string> QuarantinedTables);

internal static class IdentityServer4SchemaBaseline
{
    internal const string BaselineName = "SQL Server / IdentityServer4.EntityFramework 4.1";
    internal const string SkorubaBeta7BaselineName =
        "SQL Server / Skoruba IdentityServer4.Admin 1.0.0-beta7 (IdentityServer4.EntityFramework 2.4)";
    internal const string MigrationId = "20200522172542_Config";
    internal const string MigrationProductVersion = "3.1.0";

    // Skoruba hosts apply these migrations with their own EF Core 2.2 runtime, so the product
    // version is only pinned to the 2.2 line; the migration identity is pinned exactly.
    internal const string SkorubaBeta7MigrationProductVersionPrefix = "2.2.";
    internal static readonly IReadOnlyList<string> SkorubaBeta7MigrationIds =
    [
        "20190619072343_AspNetIdentityDbInit",
        "20190619072401_LoggingDbInit",
        "20190619072418_IdentityServerConfigurationDbInit",
        "20190619072435_IdentityServerPersistedGrantsDbInit"
    ];

    private static readonly IReadOnlySet<string> SkorubaBeta7QuarantinedTables = new HashSet<string>(StringComparer.Ordinal)
    {
        "DeviceCodes", "Log", "PersistedGrants", "RoleClaims", "Roles",
        "UserClaims", "UserLogins", "UserRoles", "Users", "UserTokens"
    };

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

    // Skoruba IdentityServer4.Admin 1.0.0-beta7 pins IdentityServer4.EntityFramework 2.4.0:
    // pre-3.x table names (ApiClaims/ApiSecrets/ApiScopeClaims, ApiScopes owned by ApiResourceId,
    // no AllowedIdentityTokenSigningAlgorithms/RequireRequestObject columns) and no ApiResourceScopes.
    private static readonly IReadOnlyDictionary<string, string[]> SkorubaBeta7TableColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["__EFMigrationsHistory"] = ["MigrationId", "ProductVersion"],
            ["ApiClaims"] = ["Id", "Type", "ApiResourceId"],
            ["ApiProperties"] = ["Id", "Key", "Value", "ApiResourceId"],
            ["ApiResources"] =
            [
                "Id", "Enabled", "Name", "DisplayName", "Description", "Created", "Updated",
                "LastAccessed", "NonEditable"
            ],
            ["ApiScopeClaims"] = ["Id", "Type", "ApiScopeId"],
            ["ApiScopes"] =
            [
                "Id", "Name", "DisplayName", "Description", "Required", "Emphasize",
                "ShowInDiscoveryDocument", "ApiResourceId"
            ],
            ["ApiSecrets"] = ["Id", "Description", "Value", "Expiration", "Type", "Created", "ApiResourceId"],
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
                "AllowAccessTokensViaBrowser", "FrontChannelLogoutUri",
                "FrontChannelLogoutSessionRequired", "BackChannelLogoutUri",
                "BackChannelLogoutSessionRequired", "AllowOfflineAccess", "IdentityTokenLifetime",
                "AccessTokenLifetime", "AuthorizationCodeLifetime", "ConsentLifetime",
                "AbsoluteRefreshTokenLifetime", "SlidingRefreshTokenLifetime", "RefreshTokenUsage",
                "UpdateAccessTokenClaimsOnRefresh", "RefreshTokenExpiration", "AccessTokenType",
                "EnableLocalLogin", "IncludeJwtId", "AlwaysSendClientClaims", "ClientClaimsPrefix",
                "PairWiseSubjectSalt", "Created", "Updated", "LastAccessed", "UserSsoLifetime",
                "UserCodeType", "DeviceCodeLifetime", "NonEditable"
            ],
            ["ClientScopes"] = ["Id", "Scope", "ClientId"],
            ["ClientSecrets"] = ["Id", "Description", "Value", "Expiration", "Type", "Created", "ClientId"],
            ["IdentityClaims"] = ["Id", "Type", "IdentityResourceId"],
            ["IdentityProperties"] = ["Id", "Key", "Value", "IdentityResourceId"],
            ["IdentityResources"] =
            [
                "Id", "Enabled", "Name", "DisplayName", "Description", "Required", "Emphasize",
                "ShowInDiscoveryDocument", "Created", "Updated", "NonEditable"
            ]
        };

    internal static IReadOnlyList<string> ConfigurationTableNames(SchemaBaselineMatch baseline) =>
        baseline.BaselineName == SkorubaBeta7BaselineName
            ? SkorubaBeta7TableColumns.Keys
                .Where(table => table != "__EFMigrationsHistory")
                .Order(StringComparer.Ordinal)
                .ToArray()
            : ExpectedTableColumns.Keys
                .Where(table => table != "__EFMigrationsHistory")
                .Order(StringComparer.Ordinal)
                .ToArray();

    internal static IReadOnlyList<SourceColumn> ExpectedColumns => ExpectedTableColumns
        .SelectMany(table => table.Value.Select(column => new SourceColumn("dbo", table.Key, column)))
        .ToArray();

    internal static SchemaBaselineMatch TryMatch(IReadOnlyList<SourceColumn> actualColumns)
    {
        if (MatchesExactly(actualColumns, ExpectedTableColumns))
        {
            return new SchemaBaselineMatch(BaselineName, []);
        }

        if (MatchesExactly(actualColumns, SkorubaBeta7TableColumns, allowQuarantinedExtras: SkorubaBeta7QuarantinedTables))
        {
            return new SchemaBaselineMatch(
                SkorubaBeta7BaselineName,
                SkorubaBeta7QuarantinedTables.Order(StringComparer.Ordinal).ToArray());
        }

        throw new UnsupportedSourceSchemaException();
    }

    internal static void ValidateStructure(IReadOnlyList<SourceColumn> actualColumns)
    {
        var actual = actualColumns
            .Select(ColumnKey)
            .ToHashSet(StringComparer.Ordinal);
        var expected = ExpectedColumns
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

    internal static void ValidateMigrations(SchemaBaselineMatch baseline, IReadOnlyList<SourceMigration> migrations)
    {
        if (baseline.BaselineName == BaselineName)
        {
            ValidateMigrations(migrations);
            return;
        }

        if (baseline.BaselineName != SkorubaBeta7BaselineName
            || migrations.Count != SkorubaBeta7MigrationIds.Count
            || !migrations
                .Select(migration => migration.MigrationId)
                .Order(StringComparer.Ordinal)
                .SequenceEqual(SkorubaBeta7MigrationIds.Order(StringComparer.Ordinal))
            || migrations.Any(migration => !migration.ProductVersion.StartsWith(
                   SkorubaBeta7MigrationProductVersionPrefix,
                   StringComparison.Ordinal)))
        {
            throw new UnsupportedSourceSchemaException();
        }
    }

    internal static string CredentialsSqlFor(SchemaBaselineMatch baseline) =>
        baseline.BaselineName == SkorubaBeta7BaselineName
            ? SqlServerIdentityServer4Source.CredentialsSql.Replace(
                  "[dbo].[ApiResourceSecrets]", "[dbo].[ApiSecrets]", StringComparison.Ordinal)
            : SqlServerIdentityServer4Source.CredentialsSql;

    private static bool MatchesExactly(
        IReadOnlyList<SourceColumn> actualColumns,
        IReadOnlyDictionary<string, string[]> expectedTableColumns,
        IReadOnlySet<string>? allowQuarantinedExtras = null)
    {
        var actual = actualColumns
            .Where(column => column.SchemaName.Equals("dbo", StringComparison.Ordinal))
            .GroupBy(column => column.TableName, StringComparer.Ordinal)
            .ToDictionary(
                table => table.Key,
                table => table.Select(column => column.ColumnName).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        foreach (var (tableName, expectedColumns) in expectedTableColumns)
        {
            if (!actual.TryGetValue(tableName, out var actualColumnsOfTable)
                || !actualColumnsOfTable.SetEquals(expectedColumns))
            {
                return false;
            }

            actual.Remove(tableName);
        }

        if (allowQuarantinedExtras is not null)
        {
            return actual.Keys.All(allowQuarantinedExtras.Contains);
        }

        return actual.Count == 0;
    }

    private static string ColumnKey(SourceColumn column) =>
        $"{column.SchemaName}\u001f{column.TableName}\u001f{column.ColumnName}";
}
