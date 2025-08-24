namespace CuaHangQuanAo.Models
{
    public class CheckoutVm
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public decimal Discount { get; set; } = 0;
    }
}