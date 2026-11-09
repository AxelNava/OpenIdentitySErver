using System.Diagnostics.CodeAnalysis;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Shared store conventions common to every provider: context names, per-context
/// migration history table names, and the distributed migration lock resource.
/// </summary>
public static class OwnIdentityStoreConventions
{
    /// <summary>Resource name for the exclusive migration lock (S4).</summary>
    public const string MigrationLockResource = "OwnIdentity.Migrations";

    /// <summary>Per-context history tables on one shared database (S3).</summary>
    public static string HistoryTableName(Type contextType) =>
        $"__EFMigrationsHistory_{StoreName(contextType)}";

    /// <summary>Short store name, e.g. <c>Control</c> for <c>ControlDbContext</c>.</summary>
    [return: NotNullIfNotNull(nameof(contextType))]
    public static string? StoreName(Type? contextType)
    {
        var name = contextType?.Name;
        return name is null ? null : name.EndsWith("DbContext", StringComparison.Ordinal) ? name[..^"DbContext".Length] : name;
    }
}
