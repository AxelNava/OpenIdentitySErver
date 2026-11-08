using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OwnIdentity.IdentityService.Security;

namespace OwnIdentity.IdentityService.Controllers.Admin;

[ApiController]
[Route("api/admin/scopes")]
[Authorize(Policy = IdentityConfiguration.AdminPolicy)]
public sealed class ScopesController(IOpenIddictScopeManager scopeManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScopeResponse>>> List(CancellationToken cancellationToken)
    {
        var response = new List<ScopeResponse>();
        await foreach (var scope in scopeManager.ListAsync(cancellationToken: cancellationToken))
        {
            response.Add(new ScopeResponse(
                await scopeManager.GetNameAsync(scope, cancellationToken) ?? string.Empty,
                await scopeManager.GetDisplayNameAsync(scope, cancellationToken) ?? string.Empty,
                (await scopeManager.GetResourcesAsync(scope, cancellationToken)).ToArray()));
        }

        return response.OrderBy(scope => scope.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<ScopeResponse>> Create(CreateScopeRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await scopeManager.FindByNameAsync(name, cancellationToken) is not null)
        {
            return Conflict(Problem(title: "This scope already exists.", statusCode: StatusCodes.Status409Conflict));
        }

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = request.DisplayName.Trim()
        };
        descriptor.Resources.UnionWith(request.Resources.Where(resource => !string.IsNullOrWhiteSpace(resource)));
        await scopeManager.CreateAsync(descriptor, cancellationToken);

        return CreatedAtAction(nameof(List), new ScopeResponse(name, descriptor.DisplayName ?? string.Empty, descriptor.Resources.ToArray()));
    }

    public sealed record CreateScopeRequest(
        [property: System.ComponentModel.DataAnnotations.Required] string Name,
        [property: System.ComponentModel.DataAnnotations.Required] string DisplayName,
        string[] Resources);
    public sealed record ScopeResponse(string Name, string DisplayName, string[] Resources);
}
