using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OwnIdentity.IdentityService.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OwnIdentity.IdentityService.Data;

public sealed class DatabaseSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAdministratorRoleAsync();
        await EnsureAdminScopeAsync(cancellationToken);
        await EnsureAdminClientAsync(cancellationToken);
        await EnsureBootstrapAdministratorAsync();
    }

    private async Task EnsureAdministratorRoleAsync()
    {
        if (!await roleManager.RoleExistsAsync(IdentityConfiguration.AdministratorRole))
        {
            EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(IdentityConfiguration.AdministratorRole)));
        }
    }

    private async Task EnsureAdminScopeAsync(CancellationToken cancellationToken)
    {
        if (await scopeManager.FindByNameAsync(IdentityConfiguration.AdminScope, cancellationToken) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = IdentityConfiguration.AdminScope,
                DisplayName = "Identity administration API",
                Resources = { IdentityConfiguration.AdminScope }
            }, cancellationToken);
        }
    }

    private async Task EnsureAdminClientAsync(CancellationToken cancellationToken)
    {
        if (await applicationManager.FindByClientIdAsync(IdentityConfiguration.AdminClientId, cancellationToken) is not null)
        {
            return;
        }

        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = IdentityConfiguration.AdminClientId,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "OwnIdentity administration",
            RedirectUris = { new Uri(IdentityConfiguration.AdminRedirectUri) },
            PostLogoutRedirectUris = { new Uri(IdentityConfiguration.AdminPostLogoutRedirectUri) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + IdentityConfiguration.AdminScope
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        }, cancellationToken);
    }

    private async Task EnsureBootstrapAdministratorAsync()
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                return;
            }

            throw new InvalidOperationException(
                "Bootstrap administrator credentials are required in Development. " +
                "Set BootstrapAdmin:Email and BootstrapAdmin:Password with: " +
                "dotnet user-secrets set \"BootstrapAdmin:Email\" \"admin@example.com\" --project src/OwnIdentity.IdentityService; " +
                "dotnet user-secrets set \"BootstrapAdmin:Password\" \"<strong-local-password>\" --project src/OwnIdentity.IdentityService");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Bootstrap administrator configuration is incomplete. " +
                "Both BootstrapAdmin:Email and BootstrapAdmin:Password must be configured.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(user, password));
        }

        if (!await userManager.IsInRoleAsync(user, IdentityConfiguration.AdministratorRole))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, IdentityConfiguration.AdministratorRole));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
