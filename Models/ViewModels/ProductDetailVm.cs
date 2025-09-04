using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.Models.ViewModels
{
    public class ProductDetailVm
    {
        public Item Item { get; set; } = null!;
        public List<string> Gallery { get; set; } = new();
        public double Rating { get; set; } = 4.2;
        public int RatingCount { get; set; } = 156;
        public string? MainImage => Item.ProductVariants.FirstOrDefault()?.Image;

        // Storage-based variants
        public List<ProductVariantsInfo> AvailableVariants { get; set; } = new();
        public List<string> AvailableColors { get; set; } = new();
        public List<string> AvailableSizes { get; set; } = new();
    }
}
