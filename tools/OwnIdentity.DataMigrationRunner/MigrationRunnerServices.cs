using OwnIdentity.Data.Core.Migrations;

namespace OwnIdentity.DataMigrationRunner;

/// <summary>Engine-specific services wired by <c>MigrationRunnerCli</c> by default and replaced by test doubles in tests.</summary>
internal sealed record MigrationRunnerServices(IOwnIdentityMigrationLock Lock, IOwnIdentityMigrationExecutor Executor);
