using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class OrdersDetail
{
    public int OrdersDetailsId { get; set; }

    public int? OrdersId { get; set; }

    public int? ItemsId { get; set; }

    public int? Quantity { get; set; }

    public int? Price { get; set; }

    public virtual Item? Items { get; set; }

    public virtual Order? Orders { get; set; }
}
