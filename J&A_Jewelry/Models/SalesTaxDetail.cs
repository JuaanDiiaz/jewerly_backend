using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class SalesTaxDetail
{
    public int Id { get; set; }

    public int? SalesOrderId { get; set; }

    public string? TaxType { get; set; }

    public decimal? TaxAmount { get; set; }

    public virtual SalesOrderHeader? SalesOrder { get; set; }
}
