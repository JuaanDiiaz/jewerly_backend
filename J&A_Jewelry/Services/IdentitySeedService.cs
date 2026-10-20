using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using J_A_Jewelry.Models;
using J_A_Jewelry.Services.Identity;
using Microsoft.EntityFrameworkCore;
// Plain 'using Serilog;' would resolve to J_A_Jewelry.Serilog (the sink's
// namespace) — alias instead.
using Log = Serilog.Log;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace J_A_Jewelry.Services;

/// <summary>
/// One-shot at startup: when the Users table is empty (first run on a fresh
/// database), ships the two system roles and the bootstrap admin account.
/// Deliberately NOT HasData seed data — BCrypt hashes can't be computed at
/// design time, and the seeded password is a secret, not a migration value.
/// Idempotent: an existing database with users passes through untouched.
/// </summary>
public class IdentitySeedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public IdentitySeedService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IsetechcJewelryInventoryContext>();

        if (await context.AppUsers.AsNoTracking().AnyAsync(cancellationToken))
            return;

        var administrator = await UpsertRoleAsync(context, "Administrator",
            Permissions.Wildcard, isSystem: true, sortOrder: 0, cancellationToken);
        var sales = await UpsertRoleAsync(context, "Sales",
            $"{Permissions.Sales},{Permissions.Customers}", isSystem: true, sortOrder: 10, cancellationToken);

        var adminPassword = _configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            // Documented bootstrap default; MUST be rotated from user-secrets
            // (dev) / Seed__AdminPassword env (prod) — see runbook section.
            adminPassword = "admin123";
            Log.Warning(
                "Seed:AdminPassword is not configured — creating bootstrap 'admin' with the documented default. Set Seed:AdminPassword before first run.");
        }

        context.AppUsers.Add(new AppUser
        {
            Username = "admin",
            DisplayName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            // Forces the change-password flow on the very first login.
            MustChangePassword = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Roles = { administrator, sales },
        });
        await context.SaveChangesAsync(cancellationToken);

        Log.Information("Identity seeded: roles Administrator+Sales, bootstrap user 'admin' created (must change password at first login)");
    }

    private static async Task<AppRole> UpsertRoleAsync(
        IsetechcJewelryInventoryContext context, string name,
        string permissions, bool isSystem, int sortOrder, CancellationToken ct)
    {
        var existing = await context.AppRoles.FirstOrDefaultAsync(r => r.Name == name, ct);
        if (existing != null) return existing;
        var role = new AppRole
        {
            Name = name,
            Permissions = permissions,
            IsSystem = isSystem,
            SortOrder = sortOrder,
        };
        context.AppRoles.Add(role);
        return role;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}