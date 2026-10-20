using System;

namespace J_A_Jewelry.Models;

/// <summary>Join table for the many-to-many user↔role assignment.</summary>
public partial class AppUserRole
{
    public int UserId { get; set; }
    public virtual AppUser User { get; set; } = null!;

    public int RoleId { get; set; }
    public virtual AppRole Role { get; set; } = null!;
}