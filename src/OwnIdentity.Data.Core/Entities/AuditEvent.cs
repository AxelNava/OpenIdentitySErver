namespace OwnIdentity.Data.Core.Entities;

/// <summary>
/// Append-only audit event (S3).
/// <para>
/// Rows are only ever INSERTed: no code path updates or deletes audit events, and
/// <see cref="AuditDbContext"/> rejects any UPDATE/DELETE attempt at write time.
/// </para>
/// <para>
/// <see cref="PayloadJson"/> is redacted at write: the caller writing the event is
/// responsible for removing credentials, tokens, PII, and other secrets before save.
/// </para>
/// </summary>
public sealed class AuditEvent
{
    public long Id { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Subject that performed the action (e.g. "user:42" or "system:bootstrap").</summary>
    public required string Actor { get; set; }

    /// <summary>Short machine-readable action name (e.g. "user.created").</summary>
    public required string Action { get; set; }

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    /// <summary>Redacted-at-write JSON payload; never contains secrets.</summary>
    public string? PayloadJson { get; set; }

    public string? CorrelationId { get; set; }
}
