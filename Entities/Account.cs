using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Account
{
    public int AccId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Pass { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string AccRole { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
