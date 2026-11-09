using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core;

namespace OwnIdentity.Data.SqlServer;

/// <summary>
/// SQL Server option wiring (S1). Each store context gets its own migration
/// history table (S3) and this assembly as <c>MigrationsAssembly</c>.
/// </summary>
public static class OwnIdentitySqlServerOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseOwnIdentitySqlServer<TContext>(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
        where TContext : DbContext
        => UseOwnIdentitySqlServer(optionsBuilder, connectionString, typeof(TContext));

    public static DbContextOptionsBuilder UseOwnIdentitySqlServer(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Type contextType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Apply(optionsBuilder, connectionString, contextType);
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseOwnIdentitySqlServer<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Apply(optionsBuilder, connectionString, typeof(TContext));
        return optionsBuilder;
    }

    internal static DbContextOptionsBuilder Apply(DbContextOptionsBuilder optionsBuilder, string connectionString, Type contextType) =>
        optionsBuilder.UseSqlServer(connectionString, sqlServer =>
        {
            sqlServer.MigrationsAssembly(typeof(OwnIdentitySqlServerOptionsBuilderExtensions).Assembly);
            sqlServer.MigrationsHistoryTable(OwnIdentityStoreConventions.HistoryTableName(contextType));
        });
}
