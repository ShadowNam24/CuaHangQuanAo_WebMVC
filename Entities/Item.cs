using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Item
{
    public int ItemsId { get; set; }

    public string ItemsName { get; set; } = null!;

    public int? SellPrice { get; set; }

    public int CategoryId { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsAvailable { get; set; }

    public int Status { get; set; }

    // Thêm trường mới cho ảnh bìa
    public string? CoverImage { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
