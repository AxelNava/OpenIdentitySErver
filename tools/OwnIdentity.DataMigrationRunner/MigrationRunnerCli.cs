using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Migrations;
using OwnIdentity.Data.PostgreSQL;
using OwnIdentity.Data.SqlServer;

namespace OwnIdentity.DataMigrationRunner;

/// <summary>
/// Gated migration runner CLI (S4). Contract:
/// <list type="bullet">
/// <item>argv holds the engine (<c>SqlServer</c> | <c>PostgreSql</c>) and the optional bounded lock wait in seconds (30 by default).</item>
/// <item>the single connection string arrives on exactly one redirected stdin line (same stdin-only pattern as the extractor; no secrets on argv).</item>
/// <item>an exclusive database lock is acquired before migrating, with a bounded wait; lock-busy exits fast with a distinct error code and never migrates.</item>
/// <item>the four store contexts migrate, each against its own migration history table on the shared database (S3).</item>
/// </list>
/// Exit codes: 0 success; 1 usage/input validation; 2 migration failure; 3 lock busy.
/// </summary>
internal static class MigrationRunnerCli
{
    public const int ExitSuccess = 0;
    public const int ExitInputError = 1;
    public const int ExitMigrationFailed = 2;
    public const int ExitLockBusy = 3;

    internal const int DefaultLockWaitSeconds = 30;
    private const int MaximumLockWaitSeconds = 3600;

    internal static async Task<int> RunAsync(
        string[] args,
        TextReader input,
        TextWriter output,
        TextWriter error,
        bool isInputRedirected,
        Func<string, MigrationRunnerServices> servicesFactory,
        CancellationToken cancellationToken = default)
    {
        if (args.Length is 0 or > 2)
        {
            await error.WriteLineAsync(
                "ERROR DM001: Exactly one engine argument plus an optional lock wait in seconds is required.");
            await error.WriteLineAsync(
                "Usage: OwnIdentity.DataMigrationRunner <SqlServer|PostgreSql> [lockWaitSeconds] < connection-string-provider");
            return ExitInputError;
        }

        var engine = args[0];
        if (!OwnIdentityDataEngines.IsKnownEngine(engine))
        {
            await error.WriteLineAsync(
                $"ERROR DM101: Unknown engine '{engine}'. Supported engines: {string.Join(", ", OwnIdentityDataEngines.All)}.");
            return ExitInputError;
        }

        var lockWaitSeconds = DefaultLockWaitSeconds;
        if (args.Length == 2
            && (!int.TryParse(args[1], out lockWaitSeconds) || lockWaitSeconds is < 1 or > MaximumLockWaitSeconds))
        {
            await error.WriteLineAsync(
                $"ERROR DM102: The lock wait must be an integer number of seconds between 1 and {MaximumLockWaitSeconds}.");
            return ExitInputError;
        }

        if (!isInputRedirected)
        {
            await error.WriteLineAsync("ERROR DM002: A connection string must be supplied through redirected standard input.");
            return ExitInputError;
        }

        string? connectionString;
        try
        {
            connectionString = await input.ReadLineAsync(cancellationToken);
            var remainder = await input.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString) || remainder.Length != 0)
            {
                await error.WriteLineAsync("ERROR DM003: Standard input must contain exactly one connection-string line.");
                return ExitInputError;
            }

            connectionString = connectionString.Trim();
        }
        catch (Exception exception) when (exception is ArgumentException or IOException)
        {
            await error.WriteLineAsync("ERROR DM004: The connection input is invalid or unavailable.");
            return ExitInputError;
        }

        var services = servicesFactory(engine);

        IOwnIdentityMigrationLockLease? lease = null;
        try
        {
            lease = await services.Lock.TryAcquireAsync(
                connectionString,
                TimeSpan.FromSeconds(lockWaitSeconds),
                cancellationToken);
            if (lease is null)
            {
                await error.WriteLineAsync(
                    $"ERROR DM200: another migration is running (engine '{engine}', lock '{OwnIdentityStoreConventions.MigrationLockResource}').");
                return ExitLockBusy;
            }

            await output.WriteLineAsync(
                $"Acquired exclusive migration lock for engine '{engine}'; migrating OwnIdentity stores...");
            await services.Executor.MigrateAsync(connectionString, cancellationToken);
            await output.WriteLineAsync("Migration complete for all OwnIdentity stores.");
            return ExitSuccess;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync("ERROR DM301: Migration was canceled.");
            return ExitMigrationFailed;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"ERROR DM300: Migration failed. {exception.Message}");
            return ExitMigrationFailed;
        }
        finally
        {
            if (lease is not null)
            {
                try
                {
                    await lease.ReleaseAsync(CancellationToken.None);
                }
                catch (Exception releaseException)
                {
                    await error.WriteLineAsync($"ERROR DM302: The migration lock could not be released. {releaseException.Message}");
                }

                await lease.DisposeAsync();
            }
        }
    }

    internal static MigrationRunnerServices CreateDefaultServices(string engine) => engine switch
    {
        OwnIdentityDataEngines.SqlServer => new MigrationRunnerServices(
            new SqlServerMigrationLock(), new SqlServerMigrationExecutor()),
        OwnIdentityDataEngines.PostgreSql => new MigrationRunnerServices(
            new PostgreSqlMigrationLock(), new PostgreSqlMigrationExecutor()),
        _ => throw new InvalidOperationException($"Unknown engine '{engine}'."),
    };
}
