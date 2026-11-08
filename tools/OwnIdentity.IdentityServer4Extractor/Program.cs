using OwnIdentity.IdentityServer4Extractor;

return await IdentityServer4ExtractorCli.RunAsync(
    args,
    Console.In,
    Console.Out,
    Console.Error,
    Console.IsInputRedirected,
    connectionString => new SqlServerIdentityServer4Source(connectionString),
    CancellationToken.None);
