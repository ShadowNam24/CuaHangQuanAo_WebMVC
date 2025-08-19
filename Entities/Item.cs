using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Item
{
    public int ItemsId { get; set; }

    public int? CategoryId { get; set; }

    public string? ItemsName { get; set; }

    public string? Size { get; set; }

    public int? SellPrice { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<Storage> Storages { get; set; } = new List<Storage>();
}
