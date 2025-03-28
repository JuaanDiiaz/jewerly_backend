﻿using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class ProductImage
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }
}
