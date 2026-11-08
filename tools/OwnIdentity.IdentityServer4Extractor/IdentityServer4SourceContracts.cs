namespace OwnIdentity.IdentityServer4Extractor;

internal interface IIdentityServer4ConfigurationSource
{
    Task<IdentityServer4SourceSnapshot> ReadAsync(CancellationToken cancellationToken);
}

internal sealed record IdentityServer4SourceSnapshot(
    IReadOnlyList<SourceClient> Clients,
    IReadOnlyList<SourceCredential> Credentials);

internal sealed record SourceClient(
    int Id,
    string ClientId,
    bool RequirePkce,
    IReadOnlyList<string> GrantTypes);

internal sealed record SourceCredential(string SourceKind, int Id, string Type);

internal sealed record SourceColumn(string SchemaName, string TableName, string ColumnName);

internal sealed record SourceMigration(string MigrationId, string ProductVersion);

internal sealed class UnsupportedSourceSchemaException : Exception;
