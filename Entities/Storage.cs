using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Storage
{
    public int StorageId { get; set; }

    public int? SupplierId { get; set; }

    public int? ProductVariantsId { get; set; }

    public int? Quantity { get; set; }

    public int? ImportCost { get; set; }

    public DateOnly? ImportDate { get; set; }

    public virtual ProductVariant? ProductVariants { get; set; }

    public virtual Supplier? Supplier { get; set; }
}
