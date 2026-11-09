namespace OwnIdentity.Data.Core;

/// <summary>
/// Supported persistence engines (S1): production engines SqlServer and PostgreSql,
/// plus the development-only Sqlite engine.
/// </summary>
public static class OwnIdentityDataEngines
{
    public const string SqlServer = "SqlServer";

    public const string PostgreSql = "PostgreSql";

    /// <summary>Development-only engine; rejected in non-Development environments.</summary>
    public const string Sqlite = "Sqlite";

    public static IReadOnlyList<string> All { get; } = [SqlServer, PostgreSql, Sqlite];

    public static bool IsKnownEngine(string? engine) =>
        engine is SqlServer or PostgreSql or Sqlite;
}
