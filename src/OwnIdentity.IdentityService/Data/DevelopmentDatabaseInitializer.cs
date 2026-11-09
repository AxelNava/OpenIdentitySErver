using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Migrations;

namespace OwnIdentity.IdentityService.Data;

/// <summary>
/// Development-only database initialization. Non-Development replicas never
/// migrate inline (S4: "Startup replicas no longer race migrations") — run
/// <c>OwnIdentity.DataMigrationRunner</c> before rolling out startup replicas.
/// <list type="bullet">
/// <item>Sqlite (dev-only, no migrations by design): rebuild the local database
/// from the current models with EnsureDeleted + EnsureCreated, then create the
/// remaining stores' tables explicitly so all four stores share one database.</item>
/// <item>SqlServer/PostgreSql in Development: inline <c>MigrateAsync</c> for
/// convenience, each store context against its own history table.</item>
/// </list>
/// </summary>
internal static class DevelopmentDatabaseInitializer
{
    private static readonly Type[] StoreContextTypes =
    [
        typeof(ControlDbContext),
        typeof(ProtocolDbContext),
        typeof(SessionDbContext),
        typeof(AuditDbContext),
    ];

    public static async Task InitializeAsync(
        IServiceProvider services,
        OwnIdentityDataStoreOptions storeOptions,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var migrationState = provider.GetRequiredService<MigrationState>();

        if (!storeOptions.UsesMigrations)
        {
            // Development Sqlite rebuilds from the current models; the local dev
            // database is disposable by design (no SQLite migrations exist).
            var control = provider.GetRequiredService<ControlDbContext>();
            await control.Database.EnsureDeletedAsync(cancellationToken);
            await control.Database.EnsureCreatedAsync(cancellationToken);

            foreach (var contextType in StoreContextTypes.Skip(1))
            {
                // EnsureCreated only materializes the model of the first context it
                // sees, so the remaining stores create their tables explicitly.
                var storeContext = (DbContext)provider.GetRequiredService(contextType);
                var creator = storeContext.Database.GetService<IRelationalDatabaseCreator>();
                await creator.CreateTablesAsync(cancellationToken);
            }
        }
        else
        {
            foreach (var contextType in StoreContextTypes)
            {
                var database = ((DbContext)provider.GetRequiredService(contextType)).Database;
                await database.MigrateAsync(cancellationToken);
            }
        }

        migrationState.MarkDevelopmentSchemaReady();
    }
}
