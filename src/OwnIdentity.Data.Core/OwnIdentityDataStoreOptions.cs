namespace OwnIdentity.Data.Core;

/// <summary>
/// Resolved data-store configuration: one shared database (S2) addressed by a single
/// connection string, split into four store contexts (S3) with provider-selected engine.
/// </summary>
public sealed class OwnIdentityDataStoreOptions(string engine, string connectionString)
{
    public string Engine { get; } = engine;

    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// Sqlite (development-only) builds its schema with EnsureCreated and owns no
    /// migrations; production engines migrate via <c>OwnIdentity.DataMigrationRunner</c>.
    /// </summary>
    public bool UsesMigrations { get; } = engine != OwnIdentityDataEngines.Sqlite;
}
