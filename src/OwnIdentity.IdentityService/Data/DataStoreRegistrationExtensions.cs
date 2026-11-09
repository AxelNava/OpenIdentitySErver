using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.PostgreSQL;
using OwnIdentity.Data.SqlServer;
using OwnIdentity.Data.Sqlite;

namespace OwnIdentity.IdentityService.Data;

/// <summary>
/// Registers the four store contexts (S3) against the configured engine (S1):
/// <c>Data:Engine</c> = <c>SqlServer</c> | <c>PostgreSql</c> is required outside
/// Development; the development-only Sqlite engine keeps working with no
/// connection string configured. One shared database (S2), each context with its
/// own migration history table via the provider options extensions.
/// </summary>
public static class DataStoreRegistrationExtensions
{
    public static IServiceCollection AddOwnIdentityDataStores(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var engine = configuration["Data:Engine"];
        var isDevelopment = environment.IsDevelopment();

        if (string.IsNullOrWhiteSpace(engine))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "Data:Engine is required in non-Development environments. " +
                    "Configure it to SqlServer or PostgreSql (S1).");
            }

            engine = OwnIdentityDataEngines.Sqlite;
        }
        else if (!OwnIdentityDataEngines.IsKnownEngine(engine))
        {
            throw new InvalidOperationException(
                $"Data:Engine '{engine}' is not supported. Supported engines: {string.Join(", ", OwnIdentityDataEngines.All)}.");
        }

        if (engine == OwnIdentityDataEngines.Sqlite && !isDevelopment)
        {
            throw new InvalidOperationException(
                "The Sqlite engine is development-only. Configure Data:Engine to SqlServer or PostgreSql (S1).");
        }

        var connectionString = configuration.GetConnectionString("IdentityDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:IdentityDatabase is required in non-Development environments.");
            }

            var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dataDirectory);
            connectionString = $"Data Source={Path.Combine(dataDirectory, "identity.db")}";
        }

        var storeOptions = new OwnIdentityDataStoreOptions(engine, connectionString);
        services.TryAddSingleton(storeOptions);
        services.TryAddSingleton<MigrationState>();

        services.AddDbContext<ControlDbContext>(options =>
            ConfigureStore(options, storeOptions, typeof(ControlDbContext)));
        services.AddDbContext<ProtocolDbContext>(options =>
            ConfigureStore(options, storeOptions, typeof(ProtocolDbContext)));
        services.AddDbContext<SessionDbContext>(options =>
            ConfigureStore(options, storeOptions, typeof(SessionDbContext)));
        services.AddDbContext<AuditDbContext>(options =>
            ConfigureStore(options, storeOptions, typeof(AuditDbContext)));

        return services;
    }

    private static void ConfigureStore(
        DbContextOptionsBuilder optionsBuilder,
        OwnIdentityDataStoreOptions storeOptions,
        Type contextType)
    {
        switch (storeOptions.Engine)
        {
            case OwnIdentityDataEngines.SqlServer:
                OwnIdentitySqlServerOptionsBuilderExtensions.UseOwnIdentitySqlServer(
                    optionsBuilder, storeOptions.ConnectionString, contextType);
                break;
            case OwnIdentityDataEngines.PostgreSql:
                OwnIdentityPostgreSqlOptionsBuilderExtensions.UseOwnIdentityPostgreSql(
                    optionsBuilder, storeOptions.ConnectionString, contextType);
                break;
            case OwnIdentityDataEngines.Sqlite:
                OwnIdentitySqliteOptionsBuilderExtensions.UseOwnIdentitySqlite(
                    optionsBuilder, storeOptions.ConnectionString, contextType);
                break;
            default:
                throw new InvalidOperationException($"Unsupported engine '{storeOptions.Engine}'.");
        }
    }
}
