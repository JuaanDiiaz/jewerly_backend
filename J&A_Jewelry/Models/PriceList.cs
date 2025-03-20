using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class PriceList
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<PriceListDetail> PriceListDetails { get; set; } = new List<PriceListDetail>();
}
