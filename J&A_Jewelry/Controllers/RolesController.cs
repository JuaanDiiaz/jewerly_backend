using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using J_A_Jewelry.Models;
using J_A_Jewelry.Services.Identity;

namespace J_A_Jewelry.Controllers;

/// <summary>Role management + the permission grid the admin edits. The
/// whole controller requires the 'users' permission. Codes are validated
/// against Permissions.All so a typo can't strand a user without access.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Permissions.UsersAdmin)]
public class RolesController : ControllerBase
{
    private readonly IsetechcJewelryInventoryContext _context;

    public RolesController(IsetechcJewelryInventoryContext context) => _context = context;

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Permissions { get; set; } = [];
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
    }

    public class SaveRoleRequest
    {
        public string Name { get; set; } = "";
        public List<string> Permissions { get; set; } = [];
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        var roles = await _context.AppRoles
            .AsNoTracking()
            .Include(r => r.Users)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync();
        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Permissions = RavelPermissions(r.Permissions),
            IsSystem = r.IsSystem,
            UserCount = r.Users.Count,
        }).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] SaveRoleRequest request)
    {
        var validation = ValidateName(request.Name);
        if (validation != null) return BadRequest(validation);

        var codes = ValidatePermissions(request.Permissions);
        if (codes == null) return BadRequest("Invalid permission code in the list");

        var taken = await _context.AppRoles.AnyAsync(r => r.Name == request.Name.Trim());
        if (taken) return BadRequest("A role with that name already exists");

        var role = new AppRole
        {
            Name = request.Name.Trim(),
            Permissions = string.Join(',', codes),
            IsSystem = false,
            SortOrder = 100,
        };
        _context.AppRoles.Add(role);
        await _context.SaveChangesAsync();
        return Ok(new RoleDto
        {
            Id = role.Id, Name = role.Name,
            Permissions = RavelPermissions(string.Join(',', codes)),
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] SaveRoleRequest request)
    {
        var role = await _context.AppRoles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        // The Administrator's wildcard must stay — editing it down is the
        // one way an admin could lose control of the system.
        if (role.Permissions == Permissions.Wildcard &&
            !request.Permissions.Contains(Permissions.Wildcard))
            return BadRequest("The administrator role keeps the wildcard permission");

        var validation = ValidateName(request.Name);
        if (validation != null) return BadRequest(validation);

        List<string> codes;
        if (role.Permissions == Permissions.Wildcard)
        {
            codes = [Permissions.Wildcard];
        }
        else
        {
            codes = ValidatePermissions(request.Permissions);
            if (codes == null) return BadRequest("Invalid permission code in the list");
            if (codes.Count == 0)
                return BadRequest("At least one permission is required — deactivate users instead");
        }

        var taken = await _context.AppRoles
            .AnyAsync(r => r.Id != id && r.Name == request.Name.Trim());
        if (taken) return BadRequest("A role with that name already exists");

        role.Name = request.Name.Trim();
        role.Permissions = codes.Count == 1 && codes[0] == Permissions.Wildcard
            ? Permissions.Wildcard
            : string.Join(',', codes);
        await _context.SaveChangesAsync();
        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Permissions = RavelPermissions(role.Permissions),
            IsSystem = role.IsSystem,
            UserCount = role.Users.Count,
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        var role = await _context.AppRoles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();
        if (role.IsSystem) return BadRequest("System roles cannot be deleted");
        if (role.Users.Count > 0)
            return BadRequest("The role still has users — remove it from their accounts first");

        _context.AppRoles.Remove(role);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static List<string>? ValidatePermissions(List<string>? permissions)
    {
        if (permissions == null || permissions.Count == 0) return [];
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var code in permissions)
        {
            if (code == Permissions.Wildcard) return null; // only the shipped admin carries '*'
            if (!Permissions.All.Contains(code)) return null;
            codes.Add(code);
        }
        return [.. codes];
    }

    private static List<string> RavelPermissions(string joined) =>
        (joined ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static string? ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2
            ? "Role name must be at least 2 characters"
            : null;
}