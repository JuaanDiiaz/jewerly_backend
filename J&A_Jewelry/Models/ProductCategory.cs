﻿using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class ProductCategory
{
    public int Id { get; set; }

    public int? CategoryId { get; set; }

    public int? ProductId { get; set; }
}
