using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class Warehouse
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
