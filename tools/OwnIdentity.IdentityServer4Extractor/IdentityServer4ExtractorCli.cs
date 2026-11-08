using Microsoft.Data.SqlClient;

namespace OwnIdentity.IdentityServer4Extractor;

internal static class IdentityServer4ExtractorCli
{
    internal static async Task<int> RunAsync(
        string[] args,
        TextReader input,
        TextWriter output,
        TextWriter error,
        bool isInputRedirected,
        Func<string, IIdentityServer4ConfigurationSource> sourceFactory,
        CancellationToken cancellationToken)
    {
        if (args.Length != 0)
        {
            await error.WriteLineAsync("ERROR EXT001: Command-line arguments are prohibited.");
            await error.WriteLineAsync("Usage: <secure-connection-string-provider> | OwnIdentity.IdentityServer4Extractor");
            return 1;
        }

        if (!isInputRedirected)
        {
            await error.WriteLineAsync("ERROR EXT002: A connection string must be supplied through redirected standard input.");
            return 1;
        }

        string? connectionString;
        try
        {
            connectionString = await input.ReadLineAsync(cancellationToken);
            var remainder = await input.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(connectionString) || remainder.Length != 0)
            {
                await error.WriteLineAsync("ERROR EXT003: Standard input must contain exactly one connection-string line.");
                return 1;
            }

            connectionString = NormalizeConnectionString(connectionString);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException)
        {
            await error.WriteLineAsync("ERROR EXT004: The connection input is invalid or unavailable.");
            return 1;
        }

        try
        {
            var source = sourceFactory(connectionString);
            var snapshot = await source.ReadAsync(cancellationToken);
            var projected = IdentityServer4ManifestProjector.Project(snapshot);
            await output.WriteLineAsync(projected.Json);

            if (!projected.Validation.IsValid)
            {
                await error.WriteLineAsync("ERROR EXT200: The extracted manifest contains migration blockers requiring operator remediation.");
                return 2;
            }

            return 0;
        }
        catch (UnsupportedSourceSchemaException)
        {
            await error.WriteLineAsync($"ERROR EXT101: Source schema does not exactly match {IdentityServer4SchemaBaseline.BaselineName}.");
            return 2;
        }
        catch (ManifestTooLargeException)
        {
            await error.WriteLineAsync("ERROR EXT102: Sanitized manifest exceeds the 1 MiB preflight boundary.");
            return 2;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync("ERROR EXT103: Extraction was canceled.");
            return 1;
        }
        catch (Exception)
        {
            await error.WriteLineAsync("ERROR EXT104: Read-only source extraction failed.");
            return 1;
        }
    }

    internal static string NormalizeConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || string.IsNullOrWhiteSpace(builder.InitialCatalog)
            || !string.IsNullOrWhiteSpace(builder.AttachDBFilename)
            || builder.UserInstance)
        {
            throw new ArgumentException("A server and database are required.", nameof(connectionString));
        }

        builder.ApplicationIntent = ApplicationIntent.ReadOnly;
        builder.ApplicationName = "OwnIdentity.IdentityServer4Extractor";
        builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
        builder.TrustServerCertificate = false;
        return builder.ConnectionString;
    }
}
