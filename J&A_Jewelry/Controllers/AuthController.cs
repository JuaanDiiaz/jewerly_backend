using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using J_A_Jewelry.Services.Identity;
using J_A_Jewelry.Models;

namespace J_A_Jewelry.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IsetechcJewelryInventoryContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(IsetechcJewelryInventoryContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    /// <summary>Login = username + password. Returns the JWT plus the user's
    /// profile; the client caches the profile so the sidebar can gate on
    /// permissions offline.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { message = "Username and password are required" });

        var user = await _context.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == request.Username.Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password" });
        if (!user.IsActive)
            return Unauthorized(new { message = "Account disabled" });

        return Ok(new
        {
            token = CreateToken(user, out var expiresUtc),
            expiresAtUtc = expiresUtc,
            user = new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.MustChangePassword,
                roles = user.Roles.Select(r => r.Name).ToList(),
                permissions = ResolvePermissions(user.Roles),
            }
        });
    }

    /// <summary>Current session profile — the app calls this to confirm a
    /// restored token and refresh its cached permissions.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var user = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized(new { message = "Unknown user" });

        return Ok(new
        {
            user.Id,
            user.Username,
            user.DisplayName,
            user.MustChangePassword,
            roles = user.Roles.Select(r => r.Name).ToList(),
            permissions = ResolvePermissions(user.Roles),
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("New password must be at least 8 characters");

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private string CreateToken(AppUser user, out DateTime expiresUtc)
    {
        var signingKey = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured (user-secrets in dev / Jwt__SigningKey env var in prod)");

        var minutes = _configuration.GetValue("Jwt:ExpireMinutes", 720);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
        };
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }
        // One permission claim per code — 'permission' claims are what the
        // policies check; a wildcard role yields exactly one '*' claim.
        foreach (var permission in ResolvePermissions(user.Roles))
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        expiresUtc = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresUtc,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Union of the user's roles' codes, with '*' collapsing the set
    /// (a wildcard makes the rest informational).</summary>
    private static List<string> ResolvePermissions(ICollection<AppRole> roles)
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            foreach (var code in (role.Permissions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (code == Permissions.Wildcard) return [Permissions.Wildcard];
                if (code.Length > 0) codes.Add(code);
            }
        }
        return [.. codes];
    }
}