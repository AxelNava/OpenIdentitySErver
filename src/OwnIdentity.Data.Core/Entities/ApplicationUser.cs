using Microsoft.AspNetCore.Identity;

namespace OwnIdentity.Data.Core.Entities;

/// <summary>
/// Application user identity, shared by every OwnIdentity persistence provider.
/// Moved from <c>OwnIdentity.IdentityService.Data</c> so all data projects own the entity (T1/S1).
/// </summary>
public sealed class ApplicationUser : IdentityUser;
