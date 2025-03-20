using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class Inventory
{
    public int Id { get; set; }

    public int? WarehouseId { get; set; }

    public int? ProductId { get; set; }

    public string? Location { get; set; }

    public decimal? Weight { get; set; }
}
