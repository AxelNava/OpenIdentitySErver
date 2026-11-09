using OwnIdentity.Data.Core.Migrations;
using OwnIdentity.Data.Core;
using OwnIdentity.DataMigrationRunner;

namespace OwnIdentity.IdentityService.Tests;

/// <summary>
/// Unit coverage for the gated migration runner's arg/stdin validation and its
/// lock-busy fail-fast exit path (T3/S4). The engine lock and migrator are
/// test doubles; the real-engine path is covered by T5 integration tests.
/// </summary>
public sealed class DataMigrationRunnerCliTests
{
    private const string ConnectionString = "Server=localhost;Database=OwnIdentity";

    private sealed class TrackingLease : IOwnIdentityMigrationLockLease
    {
        public int Releases { get; private set; }
        public int Disposes { get; private set; }

        public Task ReleaseAsync(CancellationToken cancellationToken)
        {
            Releases++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposes++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeLock(bool acquired) : IOwnIdentityMigrationLock
    {
        public TrackingLease? Lease { get; private set; }
        public TimeSpan? RequestedTimeout { get; private set; }
        public int AcquireCalls { get; private set; }

        public Task<IOwnIdentityMigrationLockLease?> TryAcquireAsync(
            string connectionString, TimeSpan timeout, CancellationToken cancellationToken)
        {
            AcquireCalls++;
            RequestedTimeout = timeout;
            if (!acquired)
            {
                return Task.FromResult<IOwnIdentityMigrationLockLease?>(null);
            }

            Lease = new TrackingLease();
            return Task.FromResult<IOwnIdentityMigrationLockLease?>(Lease);
        }
    }

    private sealed class FakeExecutor : IOwnIdentityMigrationExecutor
    {
        public List<string> ConnectionStrings { get; } = [];
        public Exception? Throw { get; init; }

        public Task MigrateAsync(string connectionString, CancellationToken cancellationToken)
        {
            ConnectionStrings.Add(connectionString);
            if (Throw is not null)
            {
                throw Throw;
            }

            return Task.CompletedTask;
        }
    }

    private static (int ExitCode, string Output, string Error) Run(
        string[] args,
        string stdin,
        bool isInputRedirected,
        FakeLock lockDouble,
        FakeExecutor executor)
    {
        var outputBuffer = new StringWriter();
        var errorBuffer = new StringWriter();
        var exitCode = MigrationRunnerCli.RunAsync(
            args,
            new StringReader(stdin),
            outputBuffer,
            errorBuffer,
            isInputRedirected,
            engine =>
            {
                Assert.True(
                    engine is OwnIdentityDataEngines.SqlServer or OwnIdentityDataEngines.PostgreSql,
                    $"The runner resolved unknown engine '{engine}'.");
                return new MigrationRunnerServices(lockDouble, executor);
            }).GetAwaiter().GetResult();
        return (exitCode, outputBuffer.ToString(), errorBuffer.ToString());
    }

    [Fact]
    public void RunAsync_withoutArguments_tells_usage_and_returns_input_error()
    {
        var (exitCode, _, error) = Run([], string.Empty, isInputRedirected: true, new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("Usage:", error);
        Assert.Contains("DM001", error);
    }

    [Fact]
    public void RunAsync_withUnknownEngine_returns_input_error()
    {
        var (exitCode, _, error) = Run(
            ["MySql"], ConnectionString, isInputRedirected: true, new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("DM101", error);
    }

    [Fact]
    public void RunAsync_withInvalidLockWaitSeconds_returns_input_error()
    {
        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.SqlServer, "0"], ConnectionString, isInputRedirected: true,
            new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("DM102", error);
    }

    [Fact]
    public void RunAsync_withoutRedirectedStdin_requires_piped_connection_string()
    {
        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.PostgreSql], ConnectionString, isInputRedirected: false,
            new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("DM002", error);
    }

    [Fact]
    public void RunAsync_withEmptyStdin_returns_single_line_error()
    {
        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.SqlServer], string.Empty, isInputRedirected: true,
            new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("DM003", error);
        Assert.Contains("exactly one connection-string line", error);
    }

    [Fact]
    public void RunAsync_withMultipleStdinLines_returns_single_line_error()
    {
        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.SqlServer], ConnectionString + "\nextra", isInputRedirected: true,
            new FakeLock(acquired: true), new FakeExecutor());

        Assert.Equal(MigrationRunnerCli.ExitInputError, exitCode);
        Assert.Contains("DM003", error);
    }

    [Fact]
    public void RunAsync_whenLockBusy_reports_another_migration_running_fails_fast_and_never_migrates()
    {
        var fakeLock = new FakeLock(acquired: false);
        var executor = new FakeExecutor();

        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.PostgreSql], ConnectionString, isInputRedirected: true, fakeLock, executor);

        Assert.Equal(MigrationRunnerCli.ExitLockBusy, exitCode);
        Assert.Contains("another migration is running", error);
        Assert.Empty(executor.ConnectionStrings);
        Assert.Equal(1, fakeLock.AcquireCalls);
        Assert.NotNull(fakeLock.RequestedTimeout);
        Assert.True(fakeLock.RequestedTimeout.Value > TimeSpan.Zero, "The lock wait must remain bounded but positive.");
    }

    [Fact]
    public void RunAsync_whenLockAcquired_passes_connection_string_to_migrator_releases_lock_and_exits_successfully()
    {
        var fakeLock = new FakeLock(acquired: true);
        var executor = new FakeExecutor();

        var (exitCode, output, error) = Run(
            [OwnIdentityDataEngines.SqlServer], ConnectionString, isInputRedirected: true, fakeLock, executor);

        Assert.Equal(MigrationRunnerCli.ExitSuccess, exitCode);
        Assert.Empty(error);
        Assert.Equal([ConnectionString], executor.ConnectionStrings);
        Assert.NotNull(fakeLock.Lease);
        Assert.Equal(1, fakeLock.Lease.Releases);
        Assert.Equal(1, fakeLock.Lease.Disposes);
        Assert.NotEmpty(output);
    }

    [Fact]
    public void RunAsync_whenMigrationFails_releases_lock_and_returns_migration_error()
    {
        var fakeLock = new FakeLock(acquired: true);
        var executor = new FakeExecutor { Throw = new InvalidOperationException("boom") };

        var (exitCode, _, error) = Run(
            [OwnIdentityDataEngines.PostgreSql], ConnectionString, isInputRedirected: true, fakeLock, executor);

        Assert.Equal(MigrationRunnerCli.ExitMigrationFailed, exitCode);
        Assert.Contains("DM300", error);
        Assert.NotNull(fakeLock.Lease);
        Assert.Equal(1, fakeLock.Lease.Releases);
    }
}
