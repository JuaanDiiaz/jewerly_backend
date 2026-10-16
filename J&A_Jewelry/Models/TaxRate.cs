using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

/// <summary>
/// Sales tax rate applied at checkout. Rates are IVA-exclusive (added on top
/// of the posted price) and keyed per product category through the
/// ProductCategory join table; a row with a null CategoryId is the fallback
/// rate for products that have no category assigned.
/// </summary>
public partial class TaxRate
{
    public int Id { get; set; }

    public int? CategoryId { get; set; }

    public string? Name { get; set; }

    public decimal? Rate { get; set; }

    public bool IsActive { get; set; }
}