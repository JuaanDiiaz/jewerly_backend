﻿using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class Supplier
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ClientNumber { get; set; }

    public string? ShipVia { get; set; }
}
