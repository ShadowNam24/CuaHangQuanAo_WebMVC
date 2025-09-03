using CuaHangQuanAo.DesignPatterns;
using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Services
{
    public class StorageService : IStorageService
    {
        private readonly CuaHangBanQuanAoContext _context;
        private readonly IStorageFactoryProvider _storageFactoryProvider;

        public StorageService(CuaHangBanQuanAoContext context, IStorageFactoryProvider storageFactoryProvider)
        {
            _context = context;
            _storageFactoryProvider = storageFactoryProvider;
        }

        public async Task<Storage> CreateStorageEntryAsync(int productId, int supplierId, string size, string color, int quantity, int importCost)
        {
            // Get or create product variant
            var productVariant = await GetOrCreateProductVariantAsync(productId, size, color);

            // Get factory for this product category
            var factory = await _storageFactoryProvider.GetStorageFactoryByProductId(productId, _context);
            var storage = factory.CreateStorage();

            // Set specific values
            storage.ProductVariantsId = productVariant.ProductVariantsId;
            storage.SupplierId = supplierId;
            storage.Quantity = quantity;
            storage.ImportCost = importCost;

            _context.Storages.Add(storage);
            await _context.SaveChangesAsync();

            // Update variant stock quantity
            await UpdateProductVariantStockAsync(productVariant.ProductVariantsId);

            return storage;
        }

        public async Task<ProductVariant> GetOrCreateProductVariantAsync(int productId, string size, string color)
        {
            // Check if variant already exists
            var existingVariant = await _context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.Size == size && pv.Color == color);

            if (existingVariant != null)
                return existingVariant;

            // Create new variant using factory
            var product = await _context.Items.FindAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            var factory = _storageFactoryProvider.GetStorageFactory(product.CategoryId);
            var newVariant = factory.CreateProductVariant(productId, size, color);

            _context.ProductVariants.Add(newVariant);
            await _context.SaveChangesAsync();

            return newVariant;
        }

        public async Task<List<string>> GetRecommendedSizesAsync(int categoryId)
        {
            var factory = _storageFactoryProvider.GetStorageFactory(categoryId);
            await Task.CompletedTask;
            return factory.GetRecommendedSizes();
        }

        public async Task<List<string>> GetRecommendedColorsAsync(int categoryId)
        {
            var factory = _storageFactoryProvider.GetStorageFactory(categoryId);
            await Task.CompletedTask;
            return factory.GetRecommendedColors();
        }

        public async Task<decimal> GetEstimatedImportCostAsync(int categoryId)
        {
            var factory = _storageFactoryProvider.GetStorageFactory(categoryId);
            await Task.CompletedTask;
            return factory.GetEstimatedImportCost();
        }

        public async Task<List<ProductVariantsInfo>> GetAvailableVariantsAsync(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .Include(pv => pv.Product)
                .Include(pv => pv.Storages)
                .Select(pv => new ProductVariantsInfo
                {
                    VariantId = pv.ProductVariantsId,
                    Size = pv.Size,
                    Color = pv.Color,
                    PriceModifier = pv.PriceModifier,
                    StockQuantity = pv.StockQuantity,
                    AvailableInStorage = pv.Storages.Sum(s => s.Quantity ?? 0),
                    FinalPrice = (int)(pv.Product.SellPrice + pv.Product.SellPrice * pv.PriceModifier)
                })
                .Where(v => v.AvailableInStorage > 0)
                .OrderBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToListAsync();

            return variants;
        }

        public async Task<StorageCreateVm> PrepareCreateViewModelAsync()
        {
            var suppliers = await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
            var categories = await _context.Categories.OrderBy(c => c.CategoryId).ToListAsync();

            var itemsByCategory = new Dictionary<string, List<ItemSelectInfo>>();

            foreach (var category in categories)
            {
                var items = await _context.Items
                    .Where(i => i.CategoryId == category.CategoryId && i.IsAvailable)
                    .Select(i => new ItemSelectInfo
                    {
                        ItemsId = i.ItemsId,
                        DisplayName = i.ItemsName,
                        SellPrice = (int)i.SellPrice,
                        CategoryId = i.CategoryId
                    })
                    .OrderBy(i => i.DisplayName)
                    .ToListAsync();

                if (items.Any())
                {
                    itemsByCategory.Add(category.NameCategory ?? "Khác", items);
                }
            }

            return new StorageCreateVm
            {
                Suppliers = suppliers,
                ItemsByCategory = itemsByCategory,
                Categories = categories
            };
        }

        public async Task<bool> UpdateProductVariantStockAsync(int variantId)
        {
            var totalStock = await _context.Storages
                .Where(s => s.ProductVariantsId == variantId)
                .SumAsync(s => s.Quantity ?? 0);

            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant != null)
            {
                variant.StockQuantity = totalStock;
                _context.ProductVariants.Update(variant);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
