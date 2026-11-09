using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core.Entities;
using OwnIdentity.IdentityService.Data;
using OwnIdentity.IdentityService.Security;

namespace OwnIdentity.IdentityService.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = IdentityConfiguration.AdminPolicy)]
public sealed class UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var response = new List<UserResponse>(users.Count);

        foreach (var user in users)
        {
            response.Add(ToResponse(user, await userManager.GetRolesAsync(user)));
        }

        return response;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> Create(CreateUserRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(Problem(title: "A user with this email already exists.", statusCode: StatusCodes.Status409Conflict));
        }

        var temporaryPassword = $"Aa1!{Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))}";
        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            return IdentityErrors(result);
        }

        return CreatedAtAction(nameof(List), new CreateUserResponse(
            ToResponse(user, []),
            temporaryPassword));
    }

    [HttpPut("{id}/enabled")]
    public async Task<ActionResult<UserResponse>> SetEnabled(string id, SetEnabledRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var result = await userManager.SetLockoutEndDateAsync(user, request.Enabled ? null : DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            return IdentityErrors(result);
        }

        return ToResponse(user, await userManager.GetRolesAsync(user));
    }

    [HttpPut("{id}/roles")]
    public async Task<ActionResult<UserResponse>> SetRoles(string id, SetRolesRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var desiredRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var role in desiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError(nameof(request.Roles), $"Role '{role}' does not exist.");
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(
            user,
            currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase));
        if (!removeResult.Succeeded)
        {
            return IdentityErrors(removeResult);
        }

        var addResult = await userManager.AddToRolesAsync(
            user,
            desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase));
        if (!addResult.Succeeded)
        {
            return IdentityErrors(addResult);
        }

        return ToResponse(user, await userManager.GetRolesAsync(user));
    }

    private BadRequestObjectResult IdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return BadRequest(new ValidationProblemDetails(ModelState));
    }

    private static UserResponse ToResponse(ApplicationUser user, IEnumerable<string> roles) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow,
        roles.Order(StringComparer.OrdinalIgnoreCase).ToArray());

    public sealed record CreateUserRequest([property: System.ComponentModel.DataAnnotations.EmailAddress] string Email);
    public sealed record SetEnabledRequest(bool Enabled);
    public sealed record SetRolesRequest(string[] Roles);
    public sealed record UserResponse(string Id, string Email, bool Enabled, string[] Roles);
    public sealed record CreateUserResponse(UserResponse User, string TemporaryPassword);
}
