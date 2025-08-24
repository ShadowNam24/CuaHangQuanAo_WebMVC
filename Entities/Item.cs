using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Item
{
    public int ItemsId { get; set; }

    public int CategoryId { get; set; }

    public string ItemsName { get; set; } = null!;

    public string Size { get; set; } = null!;

    public int SellPrice { get; set; }

    public string? Image { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<Storage> Storages { get; set; } = new List<Storage>();
}
