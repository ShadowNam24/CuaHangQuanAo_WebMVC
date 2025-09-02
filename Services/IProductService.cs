using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models.ViewModels;

namespace CuaHangQuanAo.Services
{
    public interface IProductService
    {
        Task<Item> CreateProductAsync(int categoryId, string name, int price, string? imagePath = null);
        Task<ProductVariant> CreateProductVariantAsync(int productId, string size, string color, decimal priceModifier = 0);
        Task<List<ProductVariantsInfo>> GetAvailableVariantsAsync(int productId);
        Task<List<string>> GetAvailableSizesFromStorageAsync(int productId);
        Task<List<string>> GetAvailableColorsFromStorageAsync(int productId);
        Task<ProductDetailVm> GetProductDetailWithStorageAsync(int productId);
        Task<int> GetVariantStockQuantityAsync(int variantId);
        Task<ProductVariantsInfo?> GetVariantInfoAsync(int productId, string size, string color);
    }
}
