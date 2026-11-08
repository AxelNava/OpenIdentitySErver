using OwnIdentity.IdentityServer4Extractor;

namespace OwnIdentity.IdentityService.Tests;

/// <summary>
/// Spec S5 (odd/tasks/is4-rc2-source-lab.md): the Unit 0C gate must also accept the
/// Skoruba IdentityServer4.Admin 1.0.0-beta7 configuration schema (IdentityServer4.EntityFramework
/// 2.4.0 table names), record its Skoruba identity/log/operational tables as quarantined extension
/// evidence, and keep failing closed on anything else. Column data mirrors
/// lab/skoruba-beta7/evidence/schema-columns.txt from the verified docker lab.
/// </summary>
public sealed class IdentityServer4SkorubaBeta7BaselineTests
{
    private const string SkorubaBeta7BaselineName =
        "SQL Server / Skoruba IdentityServer4.Admin 1.0.0-beta7 (IdentityServer4.EntityFramework 2.4)";

    [Fact]
    public void Gate_accepts_skoruba_beta7_configuration_schema_and_quarantines_extension_tables()
    {
        var match = IdentityServer4SchemaBaseline.TryMatch(Beta7Columns());

        Assert.Equal(SkorubaBeta7BaselineName, match.BaselineName);
        Assert.Equal(
            new[]
            {
                "DeviceCodes", "Log", "PersistedGrants", "RoleClaims", "Roles", "UserClaims",
                "UserLogins", "UserRoles", "UserTokens", "Users"
            },
            match.QuarantinedTables);
    }

    [Fact]
    public void Gate_still_accepts_canonical_41_schema_without_quarantine()
    {
        var match = IdentityServer4SchemaBaseline.TryMatch(IdentityServer4SchemaBaseline.ExpectedColumns);

        Assert.Equal(IdentityServer4SchemaBaseline.BaselineName, match.BaselineName);
        Assert.Empty(match.QuarantinedTables);
    }

    [Fact]
    public void Gate_fails_closed_on_unknown_tables_in_skoruba_beta7_variant()
    {
        var columns = Beta7Columns().Append(new SourceColumn("dbo", "SkorubaTenants", "Id")).ToArray();

        Assert.Throws<UnsupportedSourceSchemaException>(() => IdentityServer4SchemaBaseline.TryMatch(columns));
    }

    [Fact]
    public void Gate_fails_closed_when_beta7_configuration_table_columns_differ()
    {
        var columns = Beta7Columns()
            .Where(column => !(column.TableName == "ApiSecrets" && column.ColumnName == "Created"))
            .ToArray();

        Assert.Throws<UnsupportedSourceSchemaException>(() => IdentityServer4SchemaBaseline.TryMatch(columns));
    }

    [Fact]
    public void Gate_accepts_beta7_migration_history_and_rejects_variants()
    {
        var match = IdentityServer4SchemaBaseline.TryMatch(Beta7Columns());
        var migrations = IdentityServer4SchemaBaseline.SkorubaBeta7MigrationIds
            .Select(migrationId => new SourceMigration(migrationId, "2.2.6-servicing-10079"))
            .ToArray();

        IdentityServer4SchemaBaseline.ValidateMigrations(match, migrations);

        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateMigrations(match, migrations.Take(3).ToArray()));
        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateMigrations(
                match,
                migrations.Select(migration => migration with { ProductVersion = "3.1.0" }).ToArray()));
    }

    [Fact]
    public void Gate_accepts_canonical_migration_history_through_new_entry_point()
    {
        var match = IdentityServer4SchemaBaseline.TryMatch(IdentityServer4SchemaBaseline.ExpectedColumns);

        IdentityServer4SchemaBaseline.ValidateMigrations(match, [
            new(IdentityServer4SchemaBaseline.MigrationId, IdentityServer4SchemaBaseline.MigrationProductVersion)]);
        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateMigrations(
                match,
                [new("ambiguous", IdentityServer4SchemaBaseline.MigrationProductVersion)]));
    }

    [Fact]
    public void Credentials_sql_matches_selected_baseline_without_exposing_secret_values()
    {
        var canonical = IdentityServer4SchemaBaseline.TryMatch(IdentityServer4SchemaBaseline.ExpectedColumns);
        var beta7 = IdentityServer4SchemaBaseline.TryMatch(Beta7Columns());

        var canonicalSql = IdentityServer4SchemaBaseline.CredentialsSqlFor(canonical);
        var beta7Sql = IdentityServer4SchemaBaseline.CredentialsSqlFor(beta7);

        Assert.Contains("[dbo].[ApiResourceSecrets]", canonicalSql, StringComparison.Ordinal);
        Assert.Contains("[dbo].[ApiSecrets]", beta7Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("[dbo].[ApiResourceSecrets]", beta7Sql, StringComparison.Ordinal);
        foreach (var sql in new[] { canonicalSql, beta7Sql })
        {
            Assert.DoesNotContain("[Value]", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PersistedGrant", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DeviceCode", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Projector_records_beta7_baseline_adr_and_quarantined_custom_extensions()
    {
        var match = IdentityServer4SchemaBaseline.TryMatch(Beta7Columns());
        var snapshot = new IdentityServer4SourceSnapshot(
            [new SourceClient(1, "interactive-client", true, ["authorization_code"])],
            [new SourceCredential("client", 7, "SharedSecret")]);

        var result = IdentityServer4ManifestProjector.Project(snapshot, match);

        Assert.True(result.Validation.IsValid);
        Assert.Contains("\"name\": \"ApiClaims\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"Users\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"disposition\": \"quarantine\"", result.Json, StringComparison.Ordinal);
        Assert.Contains(
            "\"id\": \"ADR-source-baseline-skoruba-identityserver4-admin-1.0.0-beta7\"",
            result.Json,
            StringComparison.Ordinal);
    }

    private static SourceColumn[] Beta7Columns()
    {
        var tables = new Dictionary<string, string[]>(StringComparer.Ordinal)
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
            ],
            // Quarantined extension evidence: presence recognized, contents not fingerprinted.
            ["DeviceCodes"] = ["UserCode", "DeviceCode", "SubjectId", "ClientId", "CreationTime", "Expiration", "Data"],
            ["Log"] = ["Id", "Message", "Level", "TimeStamp", "Exception", "Properties"],
            ["PersistedGrants"] = ["Key", "Type", "SubjectId", "ClientId", "CreationTime", "Expiration", "Data"],
            ["RoleClaims"] = ["Id", "RoleId", "ClaimType", "ClaimValue"],
            ["Roles"] = ["Id", "Name", "NormalizedName", "ConcurrencyStamp"],
            ["UserClaims"] = ["Id", "UserId", "ClaimType", "ClaimValue"],
            ["UserLogins"] = ["LoginProvider", "ProviderKey", "ProviderDisplayName", "UserId"],
            ["UserRoles"] = ["UserId", "RoleId"],
            ["Users"] =
            [
                "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed",
                "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "LockoutEnd",
                "TwoFactorEnabled", "AccessFailedCount", "LockoutEnabled", "IsDeleted"
            ],
            ["UserTokens"] = ["UserId", "LoginProvider", "Name", "Value"]
        };

        return tables
            .SelectMany(table => table.Value.Select(column => new SourceColumn("dbo", table.Key, column)))
            .ToArray();
    }
}
