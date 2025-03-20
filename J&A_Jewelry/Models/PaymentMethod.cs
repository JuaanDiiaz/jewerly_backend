using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class PaymentMethod
{
    public int Id { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    public virtual ICollection<SalesOrderHeader> SalesOrderHeaders { get; set; } = new List<SalesOrderHeader>();
}
