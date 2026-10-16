using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class SalesOrderHeader
{
    public int Id { get; set; }

    public DateTime? SaleDate { get; set; }

    public int? CustomerId { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? Total { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? SalespersonName { get; set; }

    public string? Notes { get; set; }
}
