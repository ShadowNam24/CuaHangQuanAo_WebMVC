using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class ProductVariant
{
    public int ProductVariantsId { get; set; }

    public int ProductId { get; set; }

    public string Size { get; set; } = null!;

    public string Color { get; set; } = null!;

    public decimal PriceModifier { get; set; }

    public int StockQuantity { get; set; }

    public string? Image { get; set; }

    public virtual Item Product { get; set; } = null!;

    public virtual ICollection<Storage> Storages { get; set; } = new List<Storage>();
}
