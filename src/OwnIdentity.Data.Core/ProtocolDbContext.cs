using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;

namespace OwnIdentity.Data.Core;

/// <summary>
/// Protocol store (S3): OpenIddict core entities (applications, authorizations,
/// scopes, tokens) on the shared database.
/// </summary>
public class ProtocolDbContext(DbContextOptions<ProtocolDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
        OwnIdentityModelConventions.Apply(modelBuilder);
    }
}
