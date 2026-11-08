var builder = DistributedApplication.CreateBuilder(args);

var identityService = builder.AddProject<Projects.OwnIdentity_IdentityService>(
        "identity-service",
        launchProfileName: "https")
    .WithHttpHealthCheck("/health/ready")
    .WithExternalHttpEndpoints();

builder.AddViteApp("admin-ui", "../src/ownidentity-admin")
    .WithBun(installArgs: ["--frozen-lockfile", "--ignore-scripts"])
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithEnvironment("VITE_AUTHORITY", "https://localhost:7043")
    .WithEnvironment("VITE_API_BASE_URL", "https://localhost:7043")
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithExternalHttpEndpoints();

builder.Build().Run();
