using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class DiscountCode
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string DiscountType { get; set; } = null!;

    public decimal? DiscountAmount { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int MaxUsage { get; set; }

    public int UsedCount { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
