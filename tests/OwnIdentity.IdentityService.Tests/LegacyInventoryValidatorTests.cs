using OwnIdentity.LegacyInventory;

namespace OwnIdentity.IdentityService.Tests;

public sealed class LegacyInventoryValidatorTests
{
    [Fact]
    public void LegacyInventory_complete_manifest_is_valid_but_keeps_ropc_cutover_blocker()
    {
        var result = LegacyInventoryValidator.Validate(CompleteManifest);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, result.Summary.LegacyTableCount);
        Assert.Equal(1, result.Summary.CustomExtensionCount);
        Assert.Equal(1, result.Summary.CredentialCount);
        Assert.Equal(1, result.Summary.KeyCount);
        Assert.Equal(2, result.Summary.ClientCount);
        Assert.Equal(1, result.Summary.CustomGrantCount);
        Assert.Equal(1, result.Summary.AdrCount);
        Assert.Equal(1, result.Summary.RopcBlockerCount);
    }

    [Theory]
    [InlineData("\"disposition\": \"transform\"", "$.legacyTables[0].disposition")]
    [InlineData("\"disposition\": \"quarantine-for-review\"", "$.customExtensions[0].disposition")]
    [InlineData("\"disposition\": \"re-register\"", "$.credentials[0].disposition")]
    [InlineData("\"disposition\": \"security-review\"", "$.keys[0].disposition")]
    [InlineData("\"migrationClassification\": \"authorization-code-pkce\"", "$.clients[0].migrationClassification")]
    public void LegacyInventory_requires_dispositions_and_client_classification(
        string populatedField,
        string expectedPath)
    {
        var emptyField = populatedField[..(populatedField.IndexOf(':') + 1)] + " \"\"";
        var json = CompleteManifest.Replace(populatedField, emptyField, StringComparison.Ordinal);

        var result = LegacyInventoryValidator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Path == expectedPath);
    }

    [Fact]
    public void LegacyInventory_ropc_requires_owner_and_non_ropc_target()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "legacyTables": [],
              "customExtensions": [],
              "credentials": [],
              "keys": [],
              "clients": [
                {
                  "clientId": "sample-client",
                  "migrationClassification": "ropc-blocker",
                  "grantTypes": ["password"],
                  "migrationTarget": "ropc"
                }
              ],
              "customGrants": [],
              "adrs": []
            }
            """;

        var result = LegacyInventoryValidator.Validate(json);

        Assert.Collection(
            result.Diagnostics,
            diagnostic => Assert.Equal("LIV122 $.clients[0].owner", $"{diagnostic.Code} {diagnostic.Path}"),
            diagnostic => Assert.Equal("LIV123 $.clients[0].migrationTarget", $"{diagnostic.Code} {diagnostic.Path}"));
    }

    [Fact]
    public void LegacyInventory_custom_grants_must_be_quarantined_and_unresolved_adrs_owned()
    {
        var json = CompleteManifest
            .Replace("\"disposition\": \"quarantine\"", "\"disposition\": \"map-automatically\"", StringComparison.Ordinal)
            .Replace("\"owner\": \"platform-security\"", "\"owner\": \"\"", StringComparison.Ordinal);

        var result = LegacyInventoryValidator.Validate(json);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LIV130");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LIV140");
    }

    [Fact]
    public void LegacyInventory_rejects_secret_bearing_and_unknown_fields_without_echoing_values()
    {
        var sensitiveValue = $"sentinel-{Guid.NewGuid():N}";
        var json = CompleteManifest.Replace(
            "\"kind\": \"shared-secret-hash-metadata\"",
            $"\"kind\": \"shared-secret-hash-metadata\", \"clientSecret\": \"{sensitiveValue}\"",
            StringComparison.Ordinal);

        var result = LegacyInventoryValidator.Validate(json);
        var renderedDiagnostics = string.Join(Environment.NewLine, result.Diagnostics);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LIV001");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "LIV002");
        Assert.DoesNotContain(sensitiveValue, renderedDiagnostics, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyInventory_cli_uses_contract_exit_codes_and_redacted_stable_output()
    {
        var manifestPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(manifestPath, CompleteManifest.Replace(
                "\"owner\": \"sample-application-team\",",
                string.Empty,
                StringComparison.Ordinal));
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = LegacyInventoryCli.Run([manifestPath], output, error);

            Assert.Equal(2, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains("status: INVALID", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("LIV122 $.clients[1].owner", output.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("sample-password-dependent-client", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(manifestPath);
        }
    }

    [Fact]
    public void LegacyInventory_cli_returns_operational_failure_for_malformed_json()
    {
        var manifestPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(manifestPath, "{");
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = LegacyInventoryCli.Run([manifestPath], output, error);

            Assert.Equal(1, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Equal($"ERROR JSON001: Manifest is not valid JSON.{Environment.NewLine}", error.ToString());
        }
        finally
        {
            File.Delete(manifestPath);
        }
    }

    internal const string CompleteManifest = """
        {
          "schemaVersion": 1,
          "legacyTables": [
            { "name": "Clients", "disposition": "transform" }
          ],
          "customExtensions": [
            { "name": "TenantSettings", "disposition": "quarantine-for-review" }
          ],
          "credentials": [
            { "id": "credential-metadata-001", "kind": "shared-secret-hash-metadata", "disposition": "re-register" }
          ],
          "keys": [
            { "id": "key-metadata-001", "purpose": "signing", "disposition": "security-review" }
          ],
          "clients": [
            {
              "clientId": "sample-interactive-client",
              "migrationClassification": "authorization-code-pkce",
              "grantTypes": ["authorization_code"]
            },
            {
              "clientId": "sample-password-dependent-client",
              "migrationClassification": "ropc-blocker",
              "grantTypes": ["password"],
              "owner": "sample-application-team",
              "migrationTarget": "authorization-code-pkce"
            }
          ],
          "customGrants": [
            { "name": "delegation", "disposition": "quarantine" }
          ],
          "adrs": [
            { "id": "ADR-secret-key-provider", "status": "unresolved", "owner": "platform-security" }
          ]
        }
        """;
}
