using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.DesignPatterns
{
    public abstract class StorageFactory
    {
        public abstract Storage CreateStorage();
        public abstract ProductVariant CreateProductVariant(int productId, string size, string color);
        public abstract List<string> GetRecommendedSizes();
        public abstract List<string> GetRecommendedColors();
        public abstract int GetDefaultQuantity();
        public abstract decimal GetEstimatedImportCost();
        public abstract string GetStorageType();
    }

    // Clothing Storage Factory
    public class ClothingStorageFactory : StorageFactory
    {
        private readonly int _categoryId;
        private readonly string _categoryName;

        public ClothingStorageFactory(int categoryId, string categoryName)
        {
            _categoryId = categoryId;
            _categoryName = categoryName;
        }

        public override Storage CreateStorage()
        {
            return new Storage
            {
                ImportDate = DateOnly.FromDateTime(DateTime.Now),
                Quantity = GetDefaultQuantity(),
                ImportCost = (int)GetEstimatedImportCost()
            };
        }

        public override ProductVariant CreateProductVariant(int productId, string size, string color)
        {
            return new ProductVariant
            {
                ProductId = productId,
                Size = size,
                Color = color,
                PriceModifier = 0,
                StockQuantity = 0 // Will be updated from storage
            };
        }

        public override List<string> GetRecommendedSizes()
        {
            return _categoryId switch
            {
                1 or 2 => new List<string> { "46", "48", "50", "52", "54", "56" }, // Suits
                3 => new List<string> { "28", "30", "32", "34", "36", "38", "40" }, // Pants
                4 or 5 or 6 or 7 => new List<string> { "S", "M", "L", "XL", "XXL" }, // Shirts, Polos, T-shirts
                _ => new List<string> { "S", "M", "L", "XL", "XXL" }
            };
        }

        public override List<string> GetRecommendedColors()
        {
            return _categoryId switch
            {
                1 or 2 => new List<string> { "Đen", "Xám than", "Navy", "Xám nhạt" }, // Suits
                3 => new List<string> { "Đen", "Xám", "Navy", "Nâu", "Xanh nhạt" }, // Pants
                4 => new List<string> { "Trắng", "Navy", "Đen", "Xám", "Đỏ", "Xanh dương" }, // Polo
                5 or 6 => new List<string> { "Trắng", "Xanh nhạt", "Hồng nhạt", "Xám", "Navy" }, // Shirts
                7 => new List<string> { "Trắng", "Đen", "Xám", "Navy", "Đỏ", "Xanh lá" }, // T-shirts
                _ => new List<string> { "Trắng", "Đen", "Xám", "Navy" }
            };
        }

        public override int GetDefaultQuantity()
        {
            return _categoryId switch
            {
                1 or 2 => 10, // Suits - lower quantity, higher value
                3 => 20, // Pants
                4 or 5 or 6 => 25, // Shirts, Polos
                7 => 50, // T-shirts - higher quantity, lower value
                _ => 20
            };
        }

        public override decimal GetEstimatedImportCost()
        {
            return _categoryId switch
            {
                1 or 2 => 800000m, // Suits
                3 => 300000m, // Pants
                4 => 200000m, // Polo
                5 or 6 => 180000m, // Shirts
                7 => 80000m, // T-shirts
                _ => 200000m
            };
        }

        public override string GetStorageType() => "Clothing";
    }

    // Accessory Storage Factory
    public class AccessoryStorageFactory : StorageFactory
    {
        private readonly int _categoryId;
        private readonly string _categoryName;

        public AccessoryStorageFactory(int categoryId, string categoryName)
        {
            _categoryId = categoryId;
            _categoryName = categoryName;
        }

        public override Storage CreateStorage()
        {
            return new Storage
            {
                ImportDate = DateOnly.FromDateTime(DateTime.Now),
                Quantity = GetDefaultQuantity(),
                ImportCost = (int)GetEstimatedImportCost()
            };
        }

        public override ProductVariant CreateProductVariant(int productId, string size, string color)
        {
            return new ProductVariant
            {
                ProductId = productId,
                Size = size,
                Color = color,
                PriceModifier = 0,
                StockQuantity = 0
            };
        }

        public override List<string> GetRecommendedSizes()
        {
            return _categoryId switch
            {
                8 => new List<string> { "38", "39", "40", "41", "42", "43", "44", "45" }, // Shoes
                9 => new List<string> { "80", "85", "90", "95", "100", "105", "110" }, // Belts (cm)
                _ => new List<string> { "OneSize" }
            };
        }

        public override List<string> GetRecommendedColors()
        {
            return _categoryId switch
            {
                8 => new List<string> { "Đen", "Nâu", "Cognac", "Đỏ burgundy", "Xám" }, // Shoes
                9 => new List<string> { "Đen", "Nâu", "Cognac", "Tan" }, // Belts
                _ => new List<string> { "Đen", "Nâu" }
            };
        }

        public override int GetDefaultQuantity()
        {
            return _categoryId switch
            {
                8 => 15, // Shoes
                9 => 25, // Belts
                _ => 20
            };
        }

        public override decimal GetEstimatedImportCost()
        {
            return _categoryId switch
            {
                8 => 600000m, // Shoes
                9 => 150000m, // Belts
                _ => 300000m
            };
        }

        public override string GetStorageType() => "Accessory";
    }

    // Storage Factory Provider
    public interface IStorageFactoryProvider
    {
        StorageFactory GetStorageFactory(int categoryId);
        StorageFactory GetStorageFactoryByType(string storageType);
        Task<StorageFactory> GetStorageFactoryByProductId(int productId, CuaHangBanQuanAoContext context);
    }

    public class StorageFactoryProvider : IStorageFactoryProvider
    {
        private readonly Dictionary<int, (string name, string type)> _categoryMap = new()
        {
            { 1, ("BST Vest Nam 2025", "clothing") },
            { 2, ("Suit Luxury", "clothing") },
            { 3, ("Quần Tây", "clothing") },
            { 4, ("Áo Polo", "clothing") },
            { 5, ("Sơ Mi", "clothing") },
            { 6, ("Sơ Mi tay ngắn", "clothing") },
            { 7, ("Áo Thun", "clothing") },
            { 8, ("Giày da", "accessory") },
            { 9, ("Thắt lưng", "accessory") }
        };

        public StorageFactory GetStorageFactory(int categoryId)
        {
            if (_categoryMap.TryGetValue(categoryId, out var category))
            {
                return category.type switch
                {
                    "clothing" => new ClothingStorageFactory(categoryId, category.name),
                    "accessory" => new AccessoryStorageFactory(categoryId, category.name),
                    _ => throw new ArgumentException($"Unknown category type for ID: {categoryId}")
                };
            }
            throw new ArgumentException($"Category ID {categoryId} not found");
        }

        public StorageFactory GetStorageFactoryByType(string storageType)
        {
            return storageType.ToLower() switch
            {
                "clothing" => new ClothingStorageFactory(1, "Clothing"),
                "accessory" => new AccessoryStorageFactory(8, "Accessory"),
                _ => throw new ArgumentException($"Unknown storage type: {storageType}")
            };
        }

        public async Task<StorageFactory> GetStorageFactoryByProductId(int productId, CuaHangBanQuanAoContext context)
        {
            var product = await context.Items.FindAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            return GetStorageFactory(product.CategoryId);
        }
    }
}
