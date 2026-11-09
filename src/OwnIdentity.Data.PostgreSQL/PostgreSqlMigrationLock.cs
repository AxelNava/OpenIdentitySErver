using System.Diagnostics;
using Npgsql;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Migrations;

namespace OwnIdentity.Data.PostgreSQL;

/// <summary>
/// PostgreSQL exclusive migration lock (S4) via
/// <c>pg_advisory_lock(hashtext('OwnIdentity.Migrations'))</c>. The non-blocking
/// <c>pg_try_advisory_lock</c> variant is polled inside a bounded wait so a busy
/// database fails fast with <c>null</c> instead of blocking forever.
/// </summary>
public sealed class PostgreSqlMigrationLock : IOwnIdentityMigrationLock
{
    public async Task<IOwnIdentityMigrationLockLease?> TryAcquireAsync(
        string connectionString,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);

            while (true)
            {
                if (await TryAcquireOnceAsync(connection, cancellationToken))
                {
                    return new Lease(connection);
                }

                if (Stopwatch.GetTimestamp() >= deadline)
                {
                    await connection.DisposeAsync();
                    return null; // Bounded wait elapsed: another migration is running.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<bool> TryAcquireOnceAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(hashtext(@resource));";
        command.Parameters.AddWithValue("@resource", OwnIdentityStoreConventions.MigrationLockResource);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private sealed class Lease(NpgsqlConnection connection) : IOwnIdentityMigrationLockLease
    {
        public async Task ReleaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(hashtext(@resource));";
                command.Parameters.AddWithValue("@resource", OwnIdentityStoreConventions.MigrationLockResource);
                await command.ExecuteScalarAsync(cancellationToken);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }
}
