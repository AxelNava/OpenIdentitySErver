using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core.Entities;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Audit store (S3): append-only audit events on the shared database. There is no
/// UPDATE or DELETE path anywhere in code — any such mutation is rejected here at
/// write time, and the model carries no delete-side enforcement beyond that.
/// <see cref="Entities.AuditEvent.PayloadJson"/> is redacted at write by the caller.
/// </summary>
public class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents");
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.Id).ValueGeneratedOnAdd();
            entity.Property(auditEvent => auditEvent.Actor).HasMaxLength(256).IsRequired();
            entity.Property(auditEvent => auditEvent.Action).HasMaxLength(128).IsRequired();
            entity.Property(auditEvent => auditEvent.TargetType).HasMaxLength(128);
            entity.Property(auditEvent => auditEvent.TargetId).HasMaxLength(128);
            // Unbounded by design: redacted JSON payload written by the caller.
            entity.Property(auditEvent => auditEvent.PayloadJson);
            entity.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(64);
            entity.HasIndex(auditEvent => auditEvent.Timestamp);
            entity.HasIndex(auditEvent => auditEvent.CorrelationId);
        });
        OwnIdentityModelConventions.Apply(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectAuditMutations();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RejectAuditMutations();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejectAuditMutations()
    {
        var offenders = ChangeTracker.Entries<AuditEvent>()
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (offenders.Count > 0)
        {
            throw new InvalidOperationException(
                "Audit events are append-only: UPDATE and DELETE operations are not permitted.");
        }
    }
}
