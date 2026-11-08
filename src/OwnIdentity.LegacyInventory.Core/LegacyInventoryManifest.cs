namespace OwnIdentity.LegacyInventory;

public sealed class LegacyInventoryManifest
{
    public int? SchemaVersion { get; init; }

    public List<DispositionItem?>? LegacyTables { get; init; }

    public List<DispositionItem?>? CustomExtensions { get; init; }

    public List<CredentialInventoryItem?>? Credentials { get; init; }

    public List<KeyInventoryItem?>? Keys { get; init; }

    public List<ClientInventoryItem?>? Clients { get; init; }

    public List<CustomGrantInventoryItem?>? CustomGrants { get; init; }

    public List<AdrInventoryItem?>? Adrs { get; init; }
}

public sealed class DispositionItem
{
    public string? Name { get; init; }

    public string? Disposition { get; init; }
}

public sealed class CredentialInventoryItem
{
    public string? Id { get; init; }

    public string? Kind { get; init; }

    public string? Disposition { get; init; }
}

public sealed class KeyInventoryItem
{
    public string? Id { get; init; }

    public string? Purpose { get; init; }

    public string? Disposition { get; init; }
}

public sealed class ClientInventoryItem
{
    public string? ClientId { get; init; }

    public string? MigrationClassification { get; init; }

    public List<string?>? GrantTypes { get; init; }

    public string? Owner { get; init; }

    public string? MigrationTarget { get; init; }
}

public sealed class CustomGrantInventoryItem
{
    public string? Name { get; init; }

    public string? Disposition { get; init; }
}

public sealed class AdrInventoryItem
{
    public string? Id { get; init; }

    public string? Status { get; init; }

    public string? Owner { get; init; }
}
