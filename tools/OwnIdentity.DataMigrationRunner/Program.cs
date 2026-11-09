using OwnIdentity.DataMigrationRunner;

return await MigrationRunnerCli.RunAsync(
    args,
    Console.In,
    Console.Out,
    Console.Error,
    Console.IsInputRedirected,
    MigrationRunnerCli.CreateDefaultServices);
