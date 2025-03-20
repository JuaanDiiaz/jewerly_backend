using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class InventoryMovement
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? WarehouseId { get; set; }

    public string? MovementType { get; set; }

    public int? Quantity { get; set; }

    public DateTime? MovementDate { get; set; }

    public int? PurchaseOrderId { get; set; }

    public int? SalesOrderId { get; set; }

    public string? ManualEntryReason { get; set; }

    public string? Notes { get; set; }

    public virtual Product? Product { get; set; }

    public virtual PurchaseOrderHeader? PurchaseOrder { get; set; }

    public virtual SalesOrderHeader? SalesOrder { get; set; }

    public virtual Warehouse? Warehouse { get; set; }
}
