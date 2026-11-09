using System.Data;
using Microsoft.Data.SqlClient;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Migrations;

namespace OwnIdentity.Data.SqlServer;

/// <summary>
/// SQL Server exclusive migration lock (S4) via
/// <c>sp_getapplock @Resource='OwnIdentity.Migrations' @LockOwner='Session'</c>.
/// The <c>@LockTimeout</c> parameter bounds the wait, so a busy database fails
/// fast with <c>null</c> instead of blocking forever.
/// </summary>
public sealed class SqlServerMigrationLock : IOwnIdentityMigrationLock
{
    public async Task<IOwnIdentityMigrationLockLease?> TryAcquireAsync(
        string connectionString,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = @lockTimeoutMilliseconds;
                SELECT @result;
                """;
            command.Parameters.AddWithValue("@resource", OwnIdentityStoreConventions.MigrationLockResource);
            var timeoutParameter = command.Parameters.Add("@lockTimeoutMilliseconds", SqlDbType.Int);
            timeoutParameter.Value = Math.Clamp((int)timeout.TotalMilliseconds, 0, int.MaxValue);

            var result = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
            if (result is 0 or 1)
            {
                return new Lease(connection);
            }

            await connection.DisposeAsync();
            if (result == -1)
            {
                return null; // Lock request timed out: another migration is running.
            }

            throw new InvalidOperationException($"sp_getapplock failed with result {result}.");
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Lease(SqlConnection connection) : IOwnIdentityMigrationLockLease
    {
        public async Task ReleaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    "EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';";
                command.Parameters.AddWithValue("@resource", OwnIdentityStoreConventions.MigrationLockResource);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }
}
