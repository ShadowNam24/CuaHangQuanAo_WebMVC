using CuaHangQuanAo.Controllers;

namespace CuaHangQuanAo.Models
{
    public class PaymentHistoryViewModel
    {
        public List<PaymentInfo> Payments { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public int TotalOrders { get; set; }
    }
    public class PaymentInfo
    {
        public int OrderId { get; set; }
        public string? TransactionId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? ShippingAddress { get; set; }
        public int ItemCount { get; set; }
    }
}
