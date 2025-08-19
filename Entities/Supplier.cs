using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? AddressContact { get; set; }

    public virtual ICollection<Storage> Storages { get; set; } = new List<Storage>();
}
