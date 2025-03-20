using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class SalesOrderDetail
{
    public int Id { get; set; }

    public int? SalesOrderId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Total { get; set; }

    public virtual Product? Product { get; set; }

    public virtual SalesOrderHeader? SalesOrder { get; set; }
}
