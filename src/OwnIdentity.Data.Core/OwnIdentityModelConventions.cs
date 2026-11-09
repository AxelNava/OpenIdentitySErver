using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Shared model conventions applied by every OwnIdentity store context so the four
/// stores agree on value semantics regardless of the selected provider (S3).
/// Provider projects only add provider-specific configuration on top of these.
/// </summary>
public static class OwnIdentityModelConventions
{
    /// <summary>
    /// Normalizes every <see cref="DateTime"/> property to UTC so SQL Server
    /// (datetime2) and PostgreSQL (timestamptz) agree on stored instants.
    /// Unspecified kinds are interpreted as UTC rather than drifting per machine.
    /// </summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter.Instance);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(NullableUtcDateTimeConverter.Instance);
                }
            }
        }
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public static readonly UtcDateTimeConverter Instance = new();

        public UtcDateTimeConverter()
            : base(
                value => value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                    : value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
        {
        }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public static readonly NullableUtcDateTimeConverter Instance = new();

        public NullableUtcDateTimeConverter()
            : base(
                value => value.HasValue
                    ? value.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                        : value.Value.ToUniversalTime()
                    : (DateTime?)null,
                value => value.HasValue
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : (DateTime?)null)
        {
        }
    }
}
