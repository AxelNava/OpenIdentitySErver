using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OwnIdentity.IdentityService.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OwnIdentity.IdentityService.Controllers.Admin;

[ApiController]
[Route("api/admin/applications")]
[Authorize(Policy = IdentityConfiguration.AdminPolicy)]
public sealed class ApplicationsController(IOpenIddictApplicationManager applicationManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse>>> List(CancellationToken cancellationToken)
    {
        var response = new List<ApplicationResponse>();
        await foreach (var application in applicationManager.ListAsync(cancellationToken: cancellationToken))
        {
            response.Add(new ApplicationResponse(
                await applicationManager.GetClientIdAsync(application, cancellationToken) ?? string.Empty,
                await applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? string.Empty,
                (await applicationManager.GetRedirectUrisAsync(application, cancellationToken)).Select(uri => uri.ToString()).ToArray(),
                (await applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken)).Select(uri => uri.ToString()).ToArray()));
        }

        return response.OrderBy(application => application.ClientId, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        if (await applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken) is not null)
        {
            return Conflict(Problem(title: "This client ID already exists.", statusCode: StatusCodes.Status409Conflict));
        }

        if (!TryParseUris(request.RedirectUris, nameof(request.RedirectUris), out var redirectUris) |
            !TryParseUris(request.PostLogoutRedirectUris, nameof(request.PostLogoutRedirectUris), out var logoutUris))
        {
            return ValidationProblem(ModelState);
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = request.ClientId.Trim(),
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = request.DisplayName.Trim(),
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
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        descriptor.RedirectUris.UnionWith(redirectUris);
        descriptor.PostLogoutRedirectUris.UnionWith(logoutUris);

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        return CreatedAtAction(nameof(List), new ApplicationResponse(
            descriptor.ClientId,
            descriptor.DisplayName ?? string.Empty,
            redirectUris.Select(uri => uri.ToString()).ToArray(),
            logoutUris.Select(uri => uri.ToString()).ToArray()));
    }

    private bool TryParseUris(IEnumerable<string> values, string key, out Uri[] uris)
    {
        var parsed = new List<Uri>();
        foreach (var value in values)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                ModelState.AddModelError(key, $"'{value}' is not an absolute URI.");
                continue;
            }

            parsed.Add(uri);
        }

        uris = parsed.ToArray();
        return ModelState.IsValid;
    }

    public sealed record CreateApplicationRequest(
        [property: System.ComponentModel.DataAnnotations.Required] string ClientId,
        [property: System.ComponentModel.DataAnnotations.Required] string DisplayName,
        string[] RedirectUris,
        string[] PostLogoutRedirectUris);
    public sealed record ApplicationResponse(string ClientId, string DisplayName, string[] RedirectUris, string[] PostLogoutRedirectUris);
}
