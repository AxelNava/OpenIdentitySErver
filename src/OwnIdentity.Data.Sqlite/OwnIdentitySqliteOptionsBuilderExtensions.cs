using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core;

namespace OwnIdentity.Data.Sqlite;

/// <summary>
/// Development-only SQLite option wiring (S1). SQLite is local development only;
/// there are no SQLite migrations — the development flow rebuilds the schema with
/// EnsureDeleted/EnsureCreated (see <c>DevelopmentDatabaseInitializer</c> in the
/// Identity service), so this project ships no migration artifacts.
/// </summary>
public static class OwnIdentitySqliteOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseOwnIdentitySqlite<TContext>(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
        where TContext : DbContext
        => UseOwnIdentitySqlite(optionsBuilder, connectionString, typeof(TContext));

    public static DbContextOptionsBuilder UseOwnIdentitySqlite(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Type contextType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return optionsBuilder.UseSqlite(connectionString, sqlite =>
        {
            sqlite.MigrationsAssembly(typeof(OwnIdentitySqliteOptionsBuilderExtensions).Assembly);
            sqlite.MigrationsHistoryTable(OwnIdentityStoreConventions.HistoryTableName(contextType));
        });
    }
}
