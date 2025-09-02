using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models.ViewModels;

namespace CuaHangQuanAo.Services
{
    public interface IStorageService
    {
        Task<Storage> CreateStorageEntryAsync(int productId, int supplierId, string size, string color, int quantity, int importCost);
        Task<ProductVariant> GetOrCreateProductVariantAsync(int productId, string size, string color);
        Task<List<string>> GetRecommendedSizesAsync(int categoryId);
        Task<List<string>> GetRecommendedColorsAsync(int categoryId);
        Task<List<ProductVariantsInfo>> GetAvailableVariantsAsync(int productId);
        Task<decimal> GetEstimatedImportCostAsync(int categoryId);
        Task<StorageCreateVm> PrepareCreateViewModelAsync();
        Task<bool> UpdateProductVariantStockAsync(int variantId);
    }
}
