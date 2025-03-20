using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class PurchaseOrderDetail
{
    public int Id { get; set; }

    public int? PurchaseOrderId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Total { get; set; }
}
