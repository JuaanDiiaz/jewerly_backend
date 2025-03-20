using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class CustomerPayment
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public int? SalesOrderId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? Amount { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? Notes { get; set; }

    public virtual Customer? Customer { get; set; }
}
