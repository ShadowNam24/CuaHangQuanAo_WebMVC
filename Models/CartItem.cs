namespace CuaHangQuanAo.Models
{
    public class CartItem
    {
        public int ItemsId { get; set; }
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int SellPrice { get; set; }
        public string ItemsName { get; set; } = string.Empty;
        public int TotalPrice => Quantity * SellPrice;
    }
}
