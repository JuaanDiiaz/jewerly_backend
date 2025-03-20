using System;
using System.Collections.Generic;

namespace J_A_Jewelry.Models;

public partial class Category
{
    public int Id { get; set; }

    public string? CategoryNumber { get; set; }

    public string? Description { get; set; }

    public string? ExtraInformation { get; set; }

    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}
