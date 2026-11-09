using System.Collections.ObjectModel;

namespace OwnIdentity.IdentityService.Data;

/// <summary>
/// Tracks migration/schema completion for readiness reporting (S3/S4). The
/// development initializer (and any future runner-to-app channel) marks state
/// here; the readiness check refreshes the state from the per-context migration
/// history tables on every probe, so readiness always reflects the database.
/// </summary>
public sealed class MigrationState
{
    private volatile bool _developmentSchemaReady;

    private readonly object _gate = new();
    private readonly Dictionary<string, string> _lastObservedByStore = new(StringComparer.Ordinal);

    /// <summary>
    /// Development-only flag: the Sqlite dev path creates the schema with
    /// EnsureCreated and owns no migrations, so readiness relies on this mark.
    /// </summary>
    public bool DevelopmentSchemaReady => _developmentSchemaReady;

    /// <summary>Last observed migration status per store, for diagnostics.</summary>
    public ReadOnlyDictionary<string, string> LastObservedByStore
    {
        get
        {
            lock (_gate)
            {
                return new ReadOnlyDictionary<string, string>(_lastObservedByStore);
            }
        }
    }

    public void MarkDevelopmentSchemaReady() => _developmentSchemaReady = true;

    public void RecordStoreObservation(string storeName, string observation)
    {
        lock (_gate)
        {
            _lastObservedByStore[storeName] = observation;
        }
    }

    public void ClearStoreObservations()
    {
        lock (_gate)
        {
            _lastObservedByStore.Clear();
        }
    }
}
