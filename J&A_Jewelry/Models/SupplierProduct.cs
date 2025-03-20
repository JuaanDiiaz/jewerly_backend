using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class SupplierProduct
{
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    public int? ProductId { get; set; }

    public string? ArtCode { get; set; }

    public string? StyleCode { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Supplier? Supplier { get; set; }
}
