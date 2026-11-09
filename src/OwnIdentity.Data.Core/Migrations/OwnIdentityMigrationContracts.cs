namespace OwnIdentity.Data.Core.Migrations;

/// <summary>
/// Exclusive lock held while database migrations run (S4). Migrating replicas may
/// only proceed when the lock is acquired; the acquisition wait must be bounded so
/// a busy database fails fast instead of blocking startup forever.
/// </summary>
public interface IOwnIdentityMigrationLock
{
    /// <summary>
    /// Acquires the exclusive migration lock, waiting at most <paramref name="timeout"/>.
    /// Returns <c>null</c> when another migration holds the lock (lock busy).
    /// </summary>
    Task<IOwnIdentityMigrationLockLease?> TryAcquireAsync(string connectionString, TimeSpan timeout, CancellationToken cancellationToken);
}

/// <summary>A held migration lock that must be released or disposed.</summary>
public interface IOwnIdentityMigrationLockLease : IAsyncDisposable
{
    Task ReleaseAsync(CancellationToken cancellationToken);
}

/// <summary>Applies the pending migrations of every OwnIdentity store context.</summary>
public interface IOwnIdentityMigrationExecutor
{
    Task MigrateAsync(string connectionString, CancellationToken cancellationToken);
}
