using System;
using System.Collections.Generic;
using CuaHangQuanAo.Enums;

namespace CuaHangQuanAo.Entities;

public partial class Item
{
    public int ItemsId { get; set; }

    public int CategoryId { get; set; }

    public string ItemsName { get; set; } = null!;

    public int? SellPrice { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsAvailable { get; set; }
   
    public int Status { get; set; } = (int)ProductStatus.Active;


    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
