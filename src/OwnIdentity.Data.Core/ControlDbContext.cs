using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core.Entities;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Control store (S3): users, roles, and administrative data on the shared database.
/// The per-context history table is configured by the selected provider's options
/// extension, keyed on this context type.
/// </summary>
public class ControlDbContext(DbContextOptions<ControlDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        OwnIdentityModelConventions.Apply(modelBuilder);
    }
}
