using System;
using System.Collections.Generic;

namespace CuaHangQuanAo.Entities;

public partial class Order
{
    public int OrdersId { get; set; }

    public int? CustomerId { get; set; }

    public decimal? Total { get; set; }

    public int? EmployeeId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public decimal? Discount { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? ShippingAddress { get; set; }

    public string? PhoneNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? DiscountCode { get; set; }

    public string? DiscountDescription { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();
}
