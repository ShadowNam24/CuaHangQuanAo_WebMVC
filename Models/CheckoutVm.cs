using System.ComponentModel.DataAnnotations;

namespace CuaHangQuanAo.Models
{
    public class CheckoutVm
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public string Phone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string Address { get; set; } = string.Empty;

        public decimal Discount { get; set; } = 0;
        
        // Discount code properties - không bắt buộc
        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public string? DiscountDescription { get; set; }
    }
}