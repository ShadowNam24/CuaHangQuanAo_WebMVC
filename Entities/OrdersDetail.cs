using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class OrdersDetail
{
    public int OrdersDetailsId { get; set; }

    public int? OrdersId { get; set; }

    public int? ProductVariantId { get; set; }

    public int? Quantity { get; set; }

    public int? Price { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public virtual Order? Orders { get; set; }

    public virtual ProductVariant? ProductVariant { get; set; }
}
