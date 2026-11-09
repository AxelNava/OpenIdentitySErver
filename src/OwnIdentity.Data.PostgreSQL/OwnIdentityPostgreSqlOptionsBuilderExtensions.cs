using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core;

namespace OwnIdentity.Data.PostgreSQL;

/// <summary>
/// PostgreSQL option wiring (S1). Each store context gets its own migration
/// history table (S3) and this assembly as <c>MigrationsAssembly</c>.
/// </summary>
public static class OwnIdentityPostgreSqlOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseOwnIdentityPostgreSql<TContext>(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
        where TContext : DbContext
        => UseOwnIdentityPostgreSql(optionsBuilder, connectionString, typeof(TContext));

    public static DbContextOptionsBuilder UseOwnIdentityPostgreSql(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Type contextType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Apply(optionsBuilder, connectionString, contextType);
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseOwnIdentityPostgreSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        Apply(optionsBuilder, connectionString, typeof(TContext));
        return optionsBuilder;
    }

    internal static DbContextOptionsBuilder Apply(DbContextOptionsBuilder optionsBuilder, string connectionString, Type contextType) =>
        optionsBuilder.UseNpgsql(connectionString, postgreSql =>
        {
            postgreSql.MigrationsAssembly(typeof(OwnIdentityPostgreSqlOptionsBuilderExtensions).Assembly);
            postgreSql.MigrationsHistoryTable(OwnIdentityStoreConventions.HistoryTableName(contextType));
        });
}
