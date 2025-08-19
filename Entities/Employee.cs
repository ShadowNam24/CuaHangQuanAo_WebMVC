using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int AccId { get; set; }

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public string? Position { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public virtual Account Acc { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
