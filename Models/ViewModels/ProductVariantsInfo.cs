namespace CuaHangQuanAo.Models.ViewModels
{
    public class ProductVariantsInfo
    {
        public int VariantId { get; set; }
        public string Size { get; set; } = null!;
        public string Color { get; set; } = null!;
        public decimal PriceModifier { get; set; }
        public int StockQuantity { get; set; }
        public int AvailableInStorage { get; set; }
        public decimal FinalPrice { get; set; }
        public bool IsInStock => AvailableInStorage > 0;
        public string? Image { get; set; }
    }
}
