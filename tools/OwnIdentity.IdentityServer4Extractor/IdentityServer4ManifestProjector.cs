using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OwnIdentity.LegacyInventory;

namespace OwnIdentity.IdentityServer4Extractor;

internal static class IdentityServer4ManifestProjector
{
    internal const int MaximumManifestBytes = 1_048_576;

    private static readonly HashSet<string> StandardGrantTypes = new(StringComparer.Ordinal)
    {
        "authorization_code",
        "client_credentials",
        "device_flow",
        "hybrid",
        "implicit",
        "password",
        "refresh_token"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    internal static ProjectedManifest Project(IdentityServer4SourceSnapshot source, SchemaBaselineMatch? baseline = null)
    {
        baseline ??= new SchemaBaselineMatch(IdentityServer4SchemaBaseline.BaselineName, []);
        var configurationTables = IdentityServer4SchemaBaseline.ConfigurationTableNames(baseline);
        var quarantinedTables = baseline.QuarantinedTables;

        var clients = source.Clients
            .OrderBy(client => client.ClientId, StringComparer.Ordinal)
            .Select(ProjectClient)
            .ToList<ClientInventoryItem?>();
        var customGrants = source.Clients
            .SelectMany(client => client.GrantTypes)
            .Where(grantType => !StandardGrantTypes.Contains(grantType))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(grantType => (CustomGrantInventoryItem?)new CustomGrantInventoryItem
            {
                Name = grantType,
                Disposition = "quarantine"
            })
            .ToList();

        var manifest = new LegacyInventoryManifest
        {
            SchemaVersion = 1,
            LegacyTables = configurationTables
                .Select(table => (DispositionItem?)new DispositionItem
                {
                    Name = table,
                    Disposition = table.EndsWith("Secrets", StringComparison.Ordinal)
                        ? "re-register"
                        : "transform"
                })
                .ToList(),
            CustomExtensions = quarantinedTables
                .Order(StringComparer.Ordinal)
                .Select(table => (DispositionItem?)new DispositionItem
                {
                    Name = table,
                    Disposition = "quarantine"
                })
                .ToList(),
            Credentials = source.Credentials
                .OrderBy(credential => credential.SourceKind, StringComparer.Ordinal)
                .ThenBy(credential => credential.Id)
                .Select(credential => (CredentialInventoryItem?)new CredentialInventoryItem
                {
                    Id = $"{credential.SourceKind}-credential-{credential.Id}",
                    Kind = CredentialKind(credential.Type),
                    Disposition = "re-register"
                })
                .ToList(),
            Keys = [],
            Clients = clients,
            CustomGrants = customGrants,
            Adrs =
            [
                new AdrInventoryItem
                {
                    Id = "ADR-source-extraction-operator-cli",
                    Status = "resolved"
                },
                baseline.BaselineName == IdentityServer4SchemaBaseline.SkorubaBeta7BaselineName
                    ? new AdrInventoryItem
                    {
                        Id = "ADR-source-baseline-skoruba-identityserver4-admin-1.0.0-beta7",
                        Status = "resolved"
                    }
                    : new AdrInventoryItem
                    {
                        Id = "ADR-source-baseline-sqlserver-identityserver4-ef-4.1",
                        Status = "resolved"
                    }
            ]
        };

        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaximumManifestBytes)
        {
            throw new ManifestTooLargeException();
        }

        return new ProjectedManifest(json, LegacyInventoryValidator.Validate(json));
    }

    private static ClientInventoryItem ProjectClient(SourceClient client)
    {
        var grantTypes = client.GrantTypes
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Cast<string?>()
            .ToList();
        return new ClientInventoryItem
        {
            ClientId = client.ClientId,
            MigrationClassification = Classify(client),
            GrantTypes = grantTypes
        };
    }

    private static string Classify(SourceClient client)
    {
        if (client.GrantTypes.Contains("password", StringComparer.Ordinal))
        {
            return "ropc-blocker";
        }

        if (client.GrantTypes.Any(grantType => !StandardGrantTypes.Contains(grantType)))
        {
            return "custom-grant-quarantine";
        }

        if (client.GrantTypes.Contains("hybrid", StringComparer.Ordinal))
        {
            return "hybrid-legacy";
        }

        if (client.GrantTypes.Contains("implicit", StringComparer.Ordinal))
        {
            return "implicit-legacy";
        }

        if (client.GrantTypes.Contains("device_flow", StringComparer.Ordinal))
        {
            return "device-authorization";
        }

        if (client.GrantTypes.Count == 1 && client.GrantTypes.Contains("client_credentials", StringComparer.Ordinal))
        {
            return "client-credentials";
        }

        if (client.GrantTypes.Contains("authorization_code", StringComparer.Ordinal))
        {
            return client.RequirePkce ? "authorization-code-pkce" : "authorization-code-pkce-remediation";
        }

        return "manual-review";
    }

    private static string CredentialKind(string sourceType) => sourceType switch
    {
        "SharedSecret" => "shared-secret-hash-metadata",
        "X509Thumbprint" or "X509Name" or "X509CertificateBase64" => "x509-certificate-metadata",
        "JWK" => "public-jwk-metadata",
        _ => "unknown-credential-type-metadata"
    };
}

internal sealed record ProjectedManifest(string Json, LegacyInventoryValidationResult Validation);

internal sealed class ManifestTooLargeException : Exception;
