using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

/// <summary>
/// A role groups permission codes (comma-joined in [Permissions]). The grid
/// the admin edits IS this string. System roles are shipped defaults:
/// Administrator carries the '*' wildcard and is locked in the UI; Sales is
/// locked as a starter role but its permission list is editable.
/// </summary>
public partial class AppRole
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    /// <summary>Comma-separated permission codes — see Services/Identity/Permissions.</summary>
    public string Permissions { get; set; } = "";

    public bool IsSystem { get; set; }

    public int SortOrder { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = [];
}