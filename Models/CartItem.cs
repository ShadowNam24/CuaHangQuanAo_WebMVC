namespace CuaHangQuanAo.Models
{
    public class CartItem
    {
        public int ItemsID { get; set; }
        public string ItemsName { get; set; } = string.Empty;
        public int? SellPrice { get; set; }
        public int? Quantity { get; set; }

        public int? Total => SellPrice * Quantity;
    }
}
