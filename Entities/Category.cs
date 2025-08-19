using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string? NameCategory { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
