using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Migrations;

namespace OwnIdentity.Data.SqlServer;

/// <summary>
/// Migrates every OwnIdentity store context against SQL Server (S4). Each context
/// applies its own migration set tracked in its own history table on the shared
/// database (S3).
/// </summary>
public sealed class SqlServerMigrationExecutor : IOwnIdentityMigrationExecutor
{
    public async Task MigrateAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await using (var control = new ControlDbContext(BuildOptions<ControlDbContext>(connectionString)))
        {
            await control.Database.MigrateAsync(cancellationToken);
        }

        await using (var protocol = new ProtocolDbContext(BuildOptions<ProtocolDbContext>(connectionString)))
        {
            await protocol.Database.MigrateAsync(cancellationToken);
        }

        await using (var session = new SessionDbContext(BuildOptions<SessionDbContext>(connectionString)))
        {
            await session.Database.MigrateAsync(cancellationToken);
        }

        await using (var audit = new AuditDbContext(BuildOptions<AuditDbContext>(connectionString)))
        {
            await audit.Database.MigrateAsync(cancellationToken);
        }
    }

    private static DbContextOptions<TContext> BuildOptions<TContext>(string connectionString)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        OwnIdentitySqlServerOptionsBuilderExtensions.Apply(builder, connectionString, typeof(TContext));
        return builder.Options;
    }
}
