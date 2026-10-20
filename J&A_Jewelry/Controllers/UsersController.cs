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

/// <summary>User accounts management — the whole controller requires the
/// 'users' permission. No hard delete in v1: salespersonName on sales is a
/// string column, so deactivation preserves the nameâ†”history mapping.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Permissions.UsersAdmin)]
public class UsersController : ControllerBase
{
    private readonly IsetechcJewelryInventoryContext _context;

    public UsersController(IsetechcJewelryInventoryContext context) => _context = context;

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<RoleRef> Roles { get; set; } = [];
    }

    public class RoleRef
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Password { get; set; } = "";
        public List<int> RoleIds { get; set; } = [];
    }

    public class UpdateUserRequest
    {
        public string DisplayName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = [];
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .OrderBy(u => u.Id)
            .ToListAsync();
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            CreatedAtUtc = u.CreatedAtUtc,
            Roles = u.Roles.Select(r => new RoleRef { Id = r.Id, Name = r.Name }).ToList(),
        }).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var username = (request.Username ?? "").Trim();
        if (username.Length < 3) return BadRequest("Username must be at least 3 characters");
        if (request.Password == null || request.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("Display name is required");
        if (request.RoleIds == null || request.RoleIds.Count == 0)
            return BadRequest("At least one role is required");

        var usernameTaken = await _context.AppUsers.AnyAsync(u => u.Username == username);
        if (usernameTaken) return BadRequest("Username already exists");

        var roles = await _context.AppRoles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync();
        if (roles.Count != request.RoleIds.Distinct().Count())
            return BadRequest("Invalid roleId in the list");

        var user = new AppUser
        {
            Username = username,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Roles = roles,
        };
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = user.Roles.Select(r => new RoleRef { Id = r.Id, Name = r.Name }).ToList(),
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.AppUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        if (request.RoleIds == null || request.RoleIds.Count == 0)
            return BadRequest("At least one role is required — deactivate the user instead of stripping roles");

        if (string.IsNullOrWhiteSpace(request.DisplayName)) return BadRequest("Display name is required");

        // The last-active administrator guard: an admin user (wildcard role)
        // cannot be deactivated or stripped while it is the only one left —
        // that would lock everyone out of user management for good.
        var admins = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .Where(u => u.IsActive)
            .ToListAsync();
        var wildcardRoleId = await _context.AppRoles
            .Where(r => r.Permissions == Permissions.Wildcard)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
        if (wildcardRoleId != null &&
            admins.Count(u => u.Id != id) == 0 &&
            admins.Any(u => u.Roles.Any(r => r.Id == wildcardRoleId.Value)))
        {
            return BadRequest("Cannot remove the last active administrator");
        }

        var roles = await _context.AppRoles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync();
        if (roles.Count != request.RoleIds.Distinct().Count())
            return BadRequest("Invalid roleId in the list");

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.Roles = roles;
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = user.Roles.Select(r => new RoleRef { Id = r.Id, Name = r.Name }).ToList(),
        });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("New password must be at least 8 characters");

        var user = await _context.AppUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}