using System.Text.Json;

namespace OwnIdentity.LegacyInventory;

public static class LegacyInventoryCli
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            error.WriteLine("ERROR CLI001: Exactly one JSON manifest path is required.");
            error.WriteLine("Usage: OwnIdentity.LegacyInventory <manifest.json>");
            return 1;
        }

        string json;
        try
        {
            json = File.ReadAllText(args[0]);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            error.WriteLine("ERROR IO001: Manifest could not be opened or read.");
            return 1;
        }

        LegacyInventoryValidationResult result;
        try
        {
            result = LegacyInventoryValidator.Validate(json);
        }
        catch (JsonException)
        {
            error.WriteLine("ERROR JSON001: Manifest is not valid JSON.");
            return 1;
        }

        WriteSummary(output, result);
        return result.IsValid ? 0 : 2;
    }

    private static void WriteSummary(TextWriter output, LegacyInventoryValidationResult result)
    {
        output.WriteLine($"status: {(result.IsValid ? "VALID" : "INVALID")}");
        output.WriteLine($"legacyTables: {result.Summary.LegacyTableCount}");
        output.WriteLine($"customExtensions: {result.Summary.CustomExtensionCount}");
        output.WriteLine($"credentials: {result.Summary.CredentialCount}");
        output.WriteLine($"keys: {result.Summary.KeyCount}");
        output.WriteLine($"clients: {result.Summary.ClientCount}");
        output.WriteLine($"customGrants: {result.Summary.CustomGrantCount}");
        output.WriteLine($"adrs: {result.Summary.AdrCount}");
        output.WriteLine($"ropcBlockers: {result.Summary.RopcBlockerCount}");
        output.WriteLine($"cutoverBlocked: {result.Summary.RopcBlockerCount > 0}");
        output.WriteLine($"diagnostics: {result.Diagnostics.Count}");

        foreach (var diagnostic in result.Diagnostics)
        {
            output.WriteLine($"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}");
        }
    }
}
