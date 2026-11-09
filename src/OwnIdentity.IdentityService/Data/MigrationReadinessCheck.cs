using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OwnIdentity.Data.Core;

namespace OwnIdentity.IdentityService.Data;

/// <summary>
/// Readiness reflects migration-complete state (S3/S4): for every store context
/// the latest migration history rows are compared against the migrations known
/// to the provider, and the check fails while any store is incomplete. Liveness
/// is intentionally left unchanged. In Development with the Sqlite engine (no
/// migrations by design), readiness relies on the dev initializer mark instead.
/// </summary>
public sealed class MigrationReadinessCheck(
    IServiceProvider serviceProvider,
    OwnIdentityDataStoreOptions storeOptions,
    MigrationState migrationState) : IHealthCheck
{
    private static readonly Type[] StoreContextTypes =
    [
        typeof(ControlDbContext),
        typeof(ProtocolDbContext),
        typeof(SessionDbContext),
        typeof(AuditDbContext),
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!storeOptions.UsesMigrations)
        {
            return migrationState.DevelopmentSchemaReady
                ? HealthCheckResult.Healthy("Development schema created (Sqlite has no migrations by design).")
                : HealthCheckResult.Unhealthy("The development schema has not been created yet.");
        }

        migrationState.ClearStoreObservations();
        var data = new Dictionary<string, object>();

        using (var scope = serviceProvider.CreateScope())
        {
            foreach (var contextType in StoreContextTypes)
            {
                var storeName = OwnIdentityStoreConventions.StoreName(contextType);
                try
                {
                    var database = ((DbContext)scope.ServiceProvider.GetRequiredService(contextType)).Database;
                    var pending = (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();

                    if (pending.Count > 0)
                    {
                        return HealthCheckResult.Unhealthy(
                            $"{storeName} store is not migration-complete: {pending.Count} pending migration(s).",
                            data: data);
                    }

                    migrationState.RecordStoreObservation(storeName, "migrations complete");
                    data[storeName] = "migrations complete";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    migrationState.RecordStoreObservation(storeName, $"unavailable: {exception.Message}");
                    return HealthCheckResult.Unhealthy(
                        $"{storeName} store migration state is unavailable: {exception.Message}",
                        data: data);
                }
            }
        }

        return HealthCheckResult.Healthy("All OwnIdentity stores are migration-complete.", data);
    }
}
