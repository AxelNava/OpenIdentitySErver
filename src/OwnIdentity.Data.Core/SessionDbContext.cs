using Microsoft.EntityFrameworkCore;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Session store (S3): empty model placeholder for now. Server-side authentication
/// sessions land in unit 5; the context exists so its store, history table, and
/// migration wiring are established from unit 1 onward.
/// </summary>
public class SessionDbContext(DbContextOptions<SessionDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        OwnIdentityModelConventions.Apply(modelBuilder);
    }
}
