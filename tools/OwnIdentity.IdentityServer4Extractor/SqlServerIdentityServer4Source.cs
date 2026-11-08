using System.Data;
using Microsoft.Data.SqlClient;

namespace OwnIdentity.IdentityServer4Extractor;

internal sealed class SqlServerIdentityServer4Source(string connectionString) : IIdentityServer4ConfigurationSource
{
    internal const string SchemaSql = """
        SELECT s.[name], t.[name], c.[name]
        FROM [sys].[tables] AS t
        INNER JOIN [sys].[schemas] AS s ON s.[schema_id] = t.[schema_id]
        INNER JOIN [sys].[columns] AS c ON c.[object_id] = t.[object_id]
        WHERE t.[is_ms_shipped] = 0
        ORDER BY s.[name], t.[name], c.[column_id];
        """;

    internal const string MigrationsSql = """
        SELECT [MigrationId], [ProductVersion]
        FROM [dbo].[__EFMigrationsHistory]
        ORDER BY [MigrationId];
        """;

    internal const string ClientsSql = """
        SELECT c.[Id], c.[ClientId], c.[RequirePkce], g.[GrantType]
        FROM [dbo].[Clients] AS c
        LEFT JOIN [dbo].[ClientGrantTypes] AS g ON g.[ClientId] = c.[Id]
        ORDER BY c.[Id], g.[Id];
        """;

    internal const string CredentialsSql = """
        SELECT N'client', [Id], [Type]
        FROM [dbo].[ClientSecrets]
        UNION ALL
        SELECT N'api-resource', [Id], [Type]
        FROM [dbo].[ApiResourceSecrets]
        ORDER BY 1, 2;
        """;

    public async Task<IdentityServer4SourceSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var columns = await ReadColumnsAsync(connection, cancellationToken);
        IdentityServer4SchemaBaseline.ValidateStructure(columns);

        var migrations = await ReadMigrationsAsync(connection, cancellationToken);
        IdentityServer4SchemaBaseline.ValidateMigrations(migrations);

        var clients = await ReadClientsAsync(connection, cancellationToken);
        var credentials = await ReadCredentialsAsync(connection, cancellationToken);
        return new IdentityServer4SourceSnapshot(clients, credentials);
    }

    private static async Task<IReadOnlyList<SourceColumn>> ReadColumnsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var columns = new List<SourceColumn>();
        await using var command = CreateCommand(connection, SchemaSql);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new SourceColumn(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return columns;
    }

    private static async Task<IReadOnlyList<SourceMigration>> ReadMigrationsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var migrations = new List<SourceMigration>();
        await using var command = CreateCommand(connection, MigrationsSql);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            migrations.Add(new SourceMigration(reader.GetString(0), reader.GetString(1)));
        }

        return migrations;
    }

    private static async Task<IReadOnlyList<SourceClient>> ReadClientsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var clients = new Dictionary<int, ClientBuilder>();
        await using var command = CreateCommand(connection, ClientsSql);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            if (!clients.TryGetValue(id, out var client))
            {
                client = new ClientBuilder(id, reader.GetString(1), reader.GetBoolean(2));
                clients.Add(id, client);
            }

            if (!reader.IsDBNull(3))
            {
                client.GrantTypes.Add(reader.GetString(3));
            }
        }

        return clients.Values
            .Select(client => new SourceClient(
                client.Id,
                client.ClientId,
                client.RequirePkce,
                client.GrantTypes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()))
            .ToArray();
    }

    private static async Task<IReadOnlyList<SourceCredential>> ReadCredentialsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var credentials = new List<SourceCredential>();
        await using var command = CreateCommand(connection, CredentialsSql);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            credentials.Add(new SourceCredential(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
        }

        return credentials;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string sql) => new(sql, connection)
    {
        CommandTimeout = 30
    };

    private sealed record ClientBuilder(int Id, string ClientId, bool RequirePkce)
    {
        public List<string> GrantTypes { get; } = [];
    }
}
