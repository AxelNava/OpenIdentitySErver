using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OwnIdentity.IdentityService.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OwnIdentity.IdentityService.Controllers;

public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request is unavailable.");

        if (User.Identity?.IsAuthenticated is not true)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.PathBase + Request.Path + Request.QueryString
            }, Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.PathBase + Request.Path + Request.QueryString
            }, Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        }

        if (!await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Request.PathBase + Request.Path + Request.QueryString
            }, Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        }

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);
        identity.AddClaim(Claims.Subject, user.Id);
        identity.AddClaim(Claims.Name, user.UserName ?? user.Email ?? user.Id);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(Claims.Email, user.Email);
        }

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            identity.AddClaim(Claims.Role, role);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources("identity.admin");
        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Name when principal.HasScope(Scopes.Profile) =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email when principal.HasScope(Scopes.Email) =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Role when principal.HasScope(Scopes.Roles) =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public IActionResult LogoutEndpoint()
    {
        return SignOut(
            new AuthenticationProperties(),
            Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
