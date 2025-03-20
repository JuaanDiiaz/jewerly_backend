using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class PurchaseOrderHeader
{
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public decimal? Total { get; set; }

    public DateTime? ReceptionDate { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual Supplier? Supplier { get; set; }
}
