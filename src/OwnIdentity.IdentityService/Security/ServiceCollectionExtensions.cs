using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace OwnIdentity.IdentityService.Security;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAdministrationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityConfiguration.AdminPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(IdentityConfiguration.AdministratorRole);
                policy.RequireAssertion(context => context.User.HasScope(IdentityConfiguration.AdminScope));
            });
        });

        return services;
    }
}
