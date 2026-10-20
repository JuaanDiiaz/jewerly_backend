using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

/// <summary>
/// App account. The store's staff logs in here; sales stamp the user's
/// DisplayName as the sale's salespersonName (a string column on
/// SalesOrderHeader, intentionally NOT an FK — renamed users keep history
/// coherent via deactivation-never-delete).
/// </summary>
public partial class AppUser
{
    public int Id { get; set; }

    /// <summary>Login name, unique, never changes after creation.</summary>
    public string Username { get; set; } = "";

    /// <summary>Shown in the UI and stamped on sales as the salesperson.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>BCrypt hash (work factor 11) — never leaves the server.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>True after an admin reset — the client prompts for a new one at next login.</summary>
    public bool MustChangePassword { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public virtual ICollection<AppRole> Roles { get; set; } = [];
}