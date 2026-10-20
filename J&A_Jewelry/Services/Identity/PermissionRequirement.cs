using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace J_A_Jewelry.Services.Identity;

/// <summary>Policy requirement for one permission code.</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }
    public PermissionRequirement(string code) => Code = code;
}

/// <summary>
/// Passes when any 'permission' claim equals the required code or the '*'
/// wildcard — the shipped Administrator role passes every policy without
/// enumerating codes, so it can never be silently locked out of a NEW area
/// added later.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        foreach (var claim in context.User.FindAll("permission"))
        {
            if (claim.Value == requirement.Code || claim.Value == Permissions.Wildcard)
            {
                context.Succeed(requirement);
                break;
            }
        }
        return Task.CompletedTask;
    }
}