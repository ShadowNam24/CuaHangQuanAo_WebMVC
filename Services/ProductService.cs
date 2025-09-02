using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Factory;
using CuaHangQuanAo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Services
{
    public class ProductService : IProductService
    {
        private readonly CuaHangBanQuanAoContext _context;
        private readonly IProductFactoryProvider _factoryProvider;

        public ProductService(CuaHangBanQuanAoContext context, IProductFactoryProvider factoryProvider)
        {
            _context = context;
            _factoryProvider = factoryProvider;
        }

        public async Task<Item> CreateProductAsync(int categoryId, string name, int price, string? imagePath = null)
        {
            var factory = _factoryProvider.GetFactory(categoryId);
            var product = factory.CreateProduct();

            product.ItemsName = name;
            product.SellPrice = price;
            if (!string.IsNullOrEmpty(imagePath))
                product.Image = imagePath;

            _context.Items.Add(product);
            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<ProductVariant> CreateProductVariantAsync(int productId, string size, string color, decimal priceModifier = 0)
        {
            var product = await _context.Items.FindAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            var factory = _factoryProvider.GetFactory(product.CategoryId);
            var variant = factory.CreateProductVariant(product, size, color);
            variant.PriceModifier = priceModifier;

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return variant;
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
                    FinalPrice = pv.Product.SellPrice + pv.PriceModifier
                })
                .Where(v => v.AvailableInStorage > 0) // Only show variants with stock
                .OrderBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToListAsync();

            return variants;
        }

        public async Task<List<string>> GetAvailableSizesFromStorageAsync(int productId)
        {
            var sizes = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .Include(pv => pv.Storages)
                .Where(pv => pv.Storages.Sum(s => s.Quantity ?? 0) > 0)
                .Select(pv => pv.Size)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return sizes;
        }

        public async Task<List<string>> GetAvailableColorsFromStorageAsync(int productId)
        {
            var colors = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .Include(pv => pv.Storages)
                .Where(pv => pv.Storages.Sum(s => s.Quantity ?? 0) > 0)
                .Select(pv => pv.Color)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return colors;
        }

        public async Task<ProductDetailVm> GetProductDetailWithStorageAsync(int productId)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ProductVariants)
                    .ThenInclude(pv => pv.Storages)
                .FirstOrDefaultAsync(i => i.ItemsId == productId);

            if (item == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            var related = await _context.Items
                .Where(x => x.CategoryId == item.CategoryId && x.ItemsId != productId && x.IsAvailable)
                .OrderBy(x => x.ItemsName)
                .Take(8)
                .ToListAsync();

            // Get available variants from storage
            var availableVariants = await GetAvailableVariantsAsync(productId);
            var availableColors = availableVariants.Select(v => v.Color).Distinct().OrderBy(c => c).ToList();
            var availableSizes = availableVariants.Select(v => v.Size).Distinct().OrderBy(s => s).ToList();

            // Generate gallery images
            var gallery = new List<string>();
            for (int i = 1; i <= 4; i++)
            {
                gallery.Add($"{item.ItemsId}_{i}.jpg");
            }

            return new ProductDetailVm
            {
                Item = item,
                Related = related,
                Gallery = gallery,
                Rating = 4.2,
                RatingCount = 156,
                AvailableVariants = availableVariants,
                AvailableColors = availableColors,
                AvailableSizes = availableSizes
            };
        }

        public async Task<int> GetVariantStockQuantityAsync(int variantId)
        {
            var stockQuantity = await _context.Storages
                .Where(s => s.ProductVariantsId == variantId)
                .SumAsync(s => s.Quantity ?? 0);

            return stockQuantity;
        }

        public async Task<ProductVariantsInfo?> GetVariantInfoAsync(int productId, string size, string color)
        {
            var variant = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId && pv.Size == size && pv.Color == color)
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
                    FinalPrice = pv.Product.SellPrice + pv.PriceModifier
                })
                .FirstOrDefaultAsync();

            return variant;
        }
    }
}