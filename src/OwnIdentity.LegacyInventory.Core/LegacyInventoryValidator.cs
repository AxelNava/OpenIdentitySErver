using System.Text.Json;
using System.Text.Json.Serialization;

namespace OwnIdentity.LegacyInventory;

public static class LegacyInventoryValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly HashSet<string> NonRopcMigrationTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization-code-pkce",
        "device-authorization",
        "client-credentials",
        "external-federation"
    };

    public static LegacyInventoryValidationResult Validate(string json)
    {
        using var document = JsonDocument.Parse(json);
        var diagnostics = new List<LegacyInventoryDiagnostic>();
        InspectJson(document.RootElement, "$", diagnostics);

        LegacyInventoryManifest? manifest = null;
        try
        {
            manifest = document.RootElement.Deserialize<LegacyInventoryManifest>(SerializerOptions);
        }
        catch (JsonException)
        {
            diagnostics.Add(new LegacyInventoryDiagnostic(
                "LIV002",
                "$",
                "Manifest contains an unknown field or an invalid field type."));
        }

        if (manifest is null)
        {
            if (!diagnostics.Any(diagnostic => diagnostic.Code == "LIV002"))
            {
                diagnostics.Add(new LegacyInventoryDiagnostic(
                    "LIV002",
                    "$",
                    "Manifest root must be an object."));
            }
        }
        else
        {
            ValidateManifest(manifest, diagnostics);
        }

        var orderedDiagnostics = diagnostics
            .Distinct()
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new LegacyInventoryValidationResult(
            orderedDiagnostics.Length == 0,
            CreateSummary(manifest),
            orderedDiagnostics);
    }

    private static void InspectJson(
        JsonElement element,
        string path,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = $"{path}.{property.Name}";
                if (!propertyNames.Add(property.Name))
                {
                    diagnostics.Add(new LegacyInventoryDiagnostic(
                        "LIV003",
                        propertyPath,
                        "Duplicate JSON fields are not allowed."));
                }

                if (IsSensitiveFieldName(property.Name))
                {
                    diagnostics.Add(new LegacyInventoryDiagnostic(
                        "LIV001",
                        propertyPath,
                        "Secret-bearing fields are prohibited; retain metadata and disposition only."));
                }

                InspectJson(property.Value, propertyPath, diagnostics);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                InspectJson(item, $"{path}[{index}]", diagnostics);
                index++;
            }
        }
    }

    private static bool IsSensitiveFieldName(string name)
    {
        var normalized = string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        return normalized.Contains("password", StringComparison.Ordinal)
            || normalized.Contains("passphrase", StringComparison.Ordinal)
            || normalized.Contains("clientsecret", StringComparison.Ordinal)
            || normalized.Contains("sharedsecret", StringComparison.Ordinal)
            || normalized.Contains("privatekey", StringComparison.Ordinal)
            || normalized.EndsWith("secretvalue", StringComparison.Ordinal)
            || normalized.EndsWith("keymaterial", StringComparison.Ordinal)
            || normalized is "secret"
                or "accesstoken"
                or "refreshtoken"
                or "idtoken"
                or "authorizationcode"
                or "devicecode"
                or "usercode"
                or "clientassertion";
    }

    private static void ValidateManifest(
        LegacyInventoryManifest manifest,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (manifest.SchemaVersion != 1)
        {
            AddRequired(diagnostics, "LIV100", "$.schemaVersion", "Schema version must be 1.");
        }

        ValidateDispositionItems(manifest.LegacyTables, "legacyTables", diagnostics);
        ValidateDispositionItems(manifest.CustomExtensions, "customExtensions", diagnostics);
        ValidateCredentials(manifest.Credentials, diagnostics);
        ValidateKeys(manifest.Keys, diagnostics);
        ValidateClients(manifest.Clients, diagnostics);
        ValidateCustomGrants(manifest.CustomGrants, diagnostics);
        ValidateAdrs(manifest.Adrs, diagnostics);
    }

    private static void ValidateDispositionItems(
        IReadOnlyList<DispositionItem?>? items,
        string section,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (items is null)
        {
            AddRequired(diagnostics, "LIV101", $"$.{section}", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var path = $"$.{section}[{index}]";
            if (item is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(item.Name, $"{path}.name", "Name is required.", diagnostics);
            RequireText(item.Disposition, $"{path}.disposition", "Explicit disposition is required.", diagnostics);
        }
    }

    private static void ValidateCredentials(
        IReadOnlyList<CredentialInventoryItem?>? credentials,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (credentials is null)
        {
            AddRequired(diagnostics, "LIV101", "$.credentials", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < credentials.Count; index++)
        {
            var item = credentials[index];
            var path = $"$.credentials[{index}]";
            if (item is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(item.Id, $"{path}.id", "Credential identifier is required.", diagnostics);
            RequireText(item.Kind, $"{path}.kind", "Credential kind is required.", diagnostics);
            RequireText(item.Disposition, $"{path}.disposition", "Explicit disposition is required.", diagnostics);
        }
    }

    private static void ValidateKeys(
        IReadOnlyList<KeyInventoryItem?>? keys,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (keys is null)
        {
            AddRequired(diagnostics, "LIV101", "$.keys", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < keys.Count; index++)
        {
            var item = keys[index];
            var path = $"$.keys[{index}]";
            if (item is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(item.Id, $"{path}.id", "Key identifier is required.", diagnostics);
            RequireText(item.Purpose, $"{path}.purpose", "Key purpose is required.", diagnostics);
            RequireText(item.Disposition, $"{path}.disposition", "Explicit disposition is required.", diagnostics);
        }
    }

    private static void ValidateClients(
        IReadOnlyList<ClientInventoryItem?>? clients,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (clients is null)
        {
            AddRequired(diagnostics, "LIV101", "$.clients", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < clients.Count; index++)
        {
            var client = clients[index];
            var path = $"$.clients[{index}]";
            if (client is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(client.ClientId, $"{path}.clientId", "Client identifier is required.", diagnostics);
            RequireText(
                client.MigrationClassification,
                $"{path}.migrationClassification",
                "Explicit migration classification is required.",
                diagnostics);

            if (client.GrantTypes is null)
            {
                AddRequired(diagnostics, "LIV120", $"{path}.grantTypes", "Grant types must be present, using an empty array when none are declared.");
            }

            var isRopc = IsRopcClient(client);
            if (!isRopc)
            {
                continue;
            }

            if (!string.Equals(client.MigrationClassification, "ropc-blocker", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new LegacyInventoryDiagnostic(
                    "LIV121",
                    $"{path}.migrationClassification",
                    "ROPC clients must be classified as non-executable ropc-blocker records."));
            }

            RequireText(client.Owner, $"{path}.owner", "ROPC blocker owner is required.", diagnostics, "LIV122");
            if (string.IsNullOrWhiteSpace(client.MigrationTarget)
                || !NonRopcMigrationTargets.Contains(client.MigrationTarget))
            {
                diagnostics.Add(new LegacyInventoryDiagnostic(
                    "LIV123",
                    $"{path}.migrationTarget",
                    "ROPC blocker requires a non-ROPC migration target defined by the modernization plan."));
            }
        }
    }

    private static bool IsRopcClient(ClientInventoryItem client)
    {
        if (string.Equals(client.MigrationClassification, "ropc-blocker", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return client.GrantTypes?.Any(grantType =>
            grantType is not null
            && (grantType.Equals("password", StringComparison.OrdinalIgnoreCase)
                || grantType.Equals("ropc", StringComparison.OrdinalIgnoreCase)
                || grantType.Equals("resource-owner-password-credentials", StringComparison.OrdinalIgnoreCase))) == true;
    }

    private static void ValidateCustomGrants(
        IReadOnlyList<CustomGrantInventoryItem?>? grants,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (grants is null)
        {
            AddRequired(diagnostics, "LIV101", "$.customGrants", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < grants.Count; index++)
        {
            var grant = grants[index];
            var path = $"$.customGrants[{index}]";
            if (grant is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(grant.Name, $"{path}.name", "Custom grant name is required.", diagnostics);
            if (!string.Equals(grant.Disposition, "quarantine", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new LegacyInventoryDiagnostic(
                    "LIV130",
                    $"{path}.disposition",
                    "Custom grants must have the quarantine disposition."));
            }
        }
    }

    private static void ValidateAdrs(
        IReadOnlyList<AdrInventoryItem?>? adrs,
        ICollection<LegacyInventoryDiagnostic> diagnostics)
    {
        if (adrs is null)
        {
            AddRequired(diagnostics, "LIV101", "$.adrs", "Section must be present, using an empty array when none are declared.");
            return;
        }

        for (var index = 0; index < adrs.Count; index++)
        {
            var adr = adrs[index];
            var path = $"$.adrs[{index}]";
            if (adr is null)
            {
                AddRequired(diagnostics, "LIV102", path, "Entry must be an object.");
                continue;
            }

            RequireText(adr.Id, $"{path}.id", "ADR identifier is required.", diagnostics);
            RequireText(adr.Status, $"{path}.status", "ADR status is required.", diagnostics);
            if (string.Equals(adr.Status, "unresolved", StringComparison.OrdinalIgnoreCase))
            {
                RequireText(adr.Owner, $"{path}.owner", "Unresolved ADR owner is required.", diagnostics, "LIV140");
            }
        }
    }

    private static void RequireText(
        string? value,
        string path,
        string message,
        ICollection<LegacyInventoryDiagnostic> diagnostics,
        string code = "LIV110")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddRequired(diagnostics, code, path, message);
        }
    }

    private static void AddRequired(
        ICollection<LegacyInventoryDiagnostic> diagnostics,
        string code,
        string path,
        string message)
    {
        diagnostics.Add(new LegacyInventoryDiagnostic(code, path, message));
    }

    private static LegacyInventorySummary CreateSummary(LegacyInventoryManifest? manifest)
    {
        return new LegacyInventorySummary(
            manifest?.LegacyTables?.Count ?? 0,
            manifest?.CustomExtensions?.Count ?? 0,
            manifest?.Credentials?.Count ?? 0,
            manifest?.Keys?.Count ?? 0,
            manifest?.Clients?.Count ?? 0,
            manifest?.CustomGrants?.Count ?? 0,
            manifest?.Adrs?.Count ?? 0,
            manifest?.Clients?.Count(client => client is not null && IsRopcClient(client)) ?? 0);
    }
}

public sealed record LegacyInventoryDiagnostic(string Code, string Path, string Message);

public sealed record LegacyInventorySummary(
    int LegacyTableCount,
    int CustomExtensionCount,
    int CredentialCount,
    int KeyCount,
    int ClientCount,
    int CustomGrantCount,
    int AdrCount,
    int RopcBlockerCount);

public sealed record LegacyInventoryValidationResult(
    bool IsValid,
    LegacyInventorySummary Summary,
    IReadOnlyList<LegacyInventoryDiagnostic> Diagnostics);
