using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class PasswordResetToken
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public bool? IsUsed { get; set; }

    public virtual Account Account { get; set; } = null!;
}
