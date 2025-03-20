using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class PriceListDetail
{
    public int Id { get; set; }

    public int? PriceListId { get; set; }

    public int? ProductId { get; set; }

    public decimal? Price { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public virtual PriceList? PriceList { get; set; }

    public virtual Product? Product { get; set; }
}
