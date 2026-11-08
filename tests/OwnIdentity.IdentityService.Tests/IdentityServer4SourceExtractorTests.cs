using System.Text.Json;
using Microsoft.Data.SqlClient;
using OwnIdentity.IdentityServer4Extractor;

namespace OwnIdentity.IdentityService.Tests;

public sealed class IdentityServer4SourceExtractorTests
{
    [Fact]
    public void IdentityServer4Extractor_accepts_only_exact_41_configuration_schema_evidence()
    {
        IdentityServer4SchemaBaseline.ValidateStructure(IdentityServer4SchemaBaseline.ExpectedColumns);
        IdentityServer4SchemaBaseline.ValidateMigrations(
            [new SourceMigration(IdentityServer4SchemaBaseline.MigrationId, IdentityServer4SchemaBaseline.MigrationProductVersion)]);

        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateStructure(IdentityServer4SchemaBaseline.ExpectedColumns.Skip(1).ToArray()));
        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateStructure(
                [.. IdentityServer4SchemaBaseline.ExpectedColumns, new SourceColumn("dbo", "SkorubaTenants", "Id")]));
        Assert.Throws<UnsupportedSourceSchemaException>(() =>
            IdentityServer4SchemaBaseline.ValidateMigrations(
                [new SourceMigration("ambiguous", IdentityServer4SchemaBaseline.MigrationProductVersion)]));
    }

    [Fact]
    public void IdentityServer4Extractor_projects_sanitized_metadata_without_secret_or_operational_queries()
    {
        var snapshot = new IdentityServer4SourceSnapshot(
            [
                new SourceClient(1, "interactive-client", true, ["authorization_code"]),
                new SourceClient(2, "custom-client", false, ["delegation"])
            ],
            [
                new SourceCredential("client", 7, "SharedSecret"),
                new SourceCredential("api-resource", 8, "unrecognized-type")
            ]);

        var result = IdentityServer4ManifestProjector.Project(snapshot);

        Assert.True(result.Validation.IsValid);
        Assert.Contains("\"migrationClassification\": \"authorization-code-pkce\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"migrationClassification\": \"custom-grant-quarantine\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"shared-secret-hash-metadata\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"unknown-credential-type-metadata\"", result.Json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"delegation\"", result.Json, StringComparison.Ordinal);
        Assert.DoesNotContain("unrecognized-type", result.Json, StringComparison.Ordinal);

        var productionSql = string.Join(
            Environment.NewLine,
            SqlServerIdentityServer4Source.SchemaSql,
            SqlServerIdentityServer4Source.MigrationsSql,
            SqlServerIdentityServer4Source.ClientsSql,
            SqlServerIdentityServer4Source.CredentialsSql);
        Assert.DoesNotContain("[Value]", SqlServerIdentityServer4Source.CredentialsSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PersistedGrant", productionSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceCode", productionSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdentityServer4Extractor_emits_ropc_as_unowned_cutover_blocker()
    {
        var snapshot = new IdentityServer4SourceSnapshot(
            [new SourceClient(1, "password-dependent-client", false, ["password"])],
            []);

        var result = IdentityServer4ManifestProjector.Project(snapshot);

        Assert.False(result.Validation.IsValid);
        Assert.Equal(1, result.Validation.Summary.RopcBlockerCount);
        Assert.Contains(result.Validation.Diagnostics, diagnostic => diagnostic.Code == "LIV122");
        Assert.Contains(result.Validation.Diagnostics, diagnostic => diagnostic.Code == "LIV123");
        Assert.Contains("\"migrationClassification\": \"ropc-blocker\"", result.Json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"owner\"", result.Json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"migrationTarget\"", result.Json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityServer4Extractor_cli_rejects_arguments_and_non_redirected_input()
    {
        var sourceFactoryCalled = false;
        IIdentityServer4ConfigurationSource Factory(string _)
        {
            sourceFactoryCalled = true;
            return new FakeSource(new IdentityServer4SourceSnapshot([], []));
        }

        using var argumentError = new StringWriter();
        var argumentExit = await IdentityServer4ExtractorCli.RunAsync(
            ["Server=forbidden"],
            new StringReader(string.Empty),
            TextWriter.Null,
            argumentError,
            true,
            Factory,
            CancellationToken.None);
        using var interactiveError = new StringWriter();
        var interactiveExit = await IdentityServer4ExtractorCli.RunAsync(
            [],
            new StringReader(string.Empty),
            TextWriter.Null,
            interactiveError,
            false,
            Factory,
            CancellationToken.None);

        Assert.Equal(1, argumentExit);
        Assert.Equal(1, interactiveExit);
        Assert.False(sourceFactoryCalled);
        Assert.Contains("arguments are prohibited", argumentError.ToString(), StringComparison.Ordinal);
        Assert.Contains("redirected standard input", interactiveError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityServer4Extractor_cli_rejects_more_than_one_input_line()
    {
        using var error = new StringWriter();
        var exitCode = await IdentityServer4ExtractorCli.RunAsync(
            [],
            new StringReader("Server=sql.example;Database=IdentityConfiguration;Integrated Security=true\n\n"),
            TextWriter.Null,
            error,
            true,
            _ => new FakeSource(new IdentityServer4SourceSnapshot([], [])),
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"ERROR EXT003: Standard input must contain exactly one connection-string line.{Environment.NewLine}",
            error.ToString());
    }

    [Fact]
    public async Task IdentityServer4Extractor_cli_hardens_connection_and_emits_only_manifest_on_stdout()
    {
        string? normalizedConnection = null;
        IIdentityServer4ConfigurationSource Factory(string value)
        {
            normalizedConnection = value;
            return new FakeSource(new IdentityServer4SourceSnapshot(
                [new SourceClient(1, "machine-client", false, ["client_credentials"])],
                []));
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await IdentityServer4ExtractorCli.RunAsync(
            [],
            new StringReader("Server=sql.example;Database=IdentityConfiguration;Integrated Security=true\n"),
            output,
            error,
            true,
            Factory,
            CancellationToken.None);

        var builder = new SqlConnectionStringBuilder(normalizedConnection);
        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(ApplicationIntent.ReadOnly, builder.ApplicationIntent);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
        Assert.False(builder.TrustServerCertificate);
        Assert.Equal("OwnIdentity.IdentityServer4Extractor", builder.ApplicationName);
    }

    [Fact]
    public async Task IdentityServer4Extractor_cli_returns_blocker_exit_and_redacts_source_failures()
    {
        using var blockerOutput = new StringWriter();
        using var blockerError = new StringWriter();
        var blockerExit = await IdentityServer4ExtractorCli.RunAsync(
            [],
            new StringReader("Server=sql.example;Database=IdentityConfiguration;Integrated Security=true\n"),
            blockerOutput,
            blockerError,
            true,
            _ => new FakeSource(new IdentityServer4SourceSnapshot(
                [new SourceClient(1, "password-client", false, ["password"])],
                [])),
            CancellationToken.None);

        const string privateDiagnostic = "private source diagnostic";
        using var failureOutput = new StringWriter();
        using var failureError = new StringWriter();
        var failureExit = await IdentityServer4ExtractorCli.RunAsync(
            [],
            new StringReader("Server=sql.example;Database=IdentityConfiguration;Integrated Security=true\n"),
            failureOutput,
            failureError,
            true,
            _ => new ThrowingSource(privateDiagnostic),
            CancellationToken.None);

        Assert.Equal(2, blockerExit);
        Assert.Contains("ropc-blocker", blockerOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("migration blockers", blockerError.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, failureExit);
        Assert.Equal(string.Empty, failureOutput.ToString());
        Assert.DoesNotContain(privateDiagnostic, failureError.ToString(), StringComparison.Ordinal);
        Assert.Equal($"ERROR EXT104: Read-only source extraction failed.{Environment.NewLine}", failureError.ToString());
    }

    private sealed class FakeSource(IdentityServer4SourceSnapshot snapshot) : IIdentityServer4ConfigurationSource
    {
        public Task<IdentityServer4SourceSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class ThrowingSource(string message) : IIdentityServer4ConfigurationSource
    {
        public Task<IdentityServer4SourceSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);
    }
}
