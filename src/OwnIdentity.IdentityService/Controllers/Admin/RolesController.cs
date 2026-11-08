using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OwnIdentity.IdentityService.Security;

namespace OwnIdentity.IdentityService.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = IdentityConfiguration.AdminPolicy)]
public sealed class RolesController(RoleManager<IdentityRole> roleManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> List(CancellationToken cancellationToken) =>
        await roleManager.Roles.AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleResponse(role.Id, role.Name ?? string.Empty))
            .ToListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await roleManager.RoleExistsAsync(name))
        {
            return Conflict(Problem(title: "This role already exists.", statusCode: StatusCodes.Status409Conflict));
        }

        var role = new IdentityRole(name);
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(List), new RoleResponse(role.Id, role.Name ?? name));
    }

    public sealed record CreateRoleRequest([property: System.ComponentModel.DataAnnotations.Required] string Name);
    public sealed record RoleResponse(string Id, string Name);
}
