using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OwnIdentity.IdentityService.Security;

namespace OwnIdentity.IdentityService.Tests;

public sealed class AdminAuthorizationPolicyTests
{
    [Fact]
    public async Task Policy_succeeds_only_with_administrator_role_and_admin_scope()
    {
        await using var provider = CreateProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity("test", OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, "user-1"));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, IdentityConfiguration.AdministratorRole));
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(IdentityConfiguration.AdminScope);

        var result = await authorization.AuthorizeAsync(
            principal,
            resource: null,
            IdentityConfiguration.AdminPolicy);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Policy_rejects_when_role_or_scope_is_missing(bool includeRole, bool includeScope)
    {
        await using var provider = CreateProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity("test", OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, "user-1"));
        if (includeRole)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, IdentityConfiguration.AdministratorRole));
        }

        if (includeScope)
        {
            new ClaimsPrincipal(identity).SetScopes(IdentityConfiguration.AdminScope);
        }

        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(identity),
            resource: null,
            IdentityConfiguration.AdminPolicy);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityAdministrationAuthorization();
        return services.BuildServiceProvider();
    }
}
