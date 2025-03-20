using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class SalesOrderHeader
{
    public int Id { get; set; }

    public DateTime? SaleDate { get; set; }

    public int? CustomerId { get; set; }

    public decimal? Total { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? Notes { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

    public virtual ICollection<SalesTaxDetail> SalesTaxDetails { get; set; } = new List<SalesTaxDetail>();
}
