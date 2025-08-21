using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int AccId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AddressName { get; set; }

    public string? City { get; set; }

    public virtual Account Acc { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
