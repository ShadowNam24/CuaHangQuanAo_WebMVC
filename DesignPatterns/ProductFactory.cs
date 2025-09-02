using CuaHangQuanAo.Entities;

namespace CuaHangQuanAo.Factory
{
    public abstract class ProductFactory
    {
        public abstract Item CreateProduct();
        public abstract ProductVariant CreateProductVariant(Item product, string size, string color);
        public abstract List<string> GetAvailableSizes();
        public abstract List<string> GetAvailableColors();
        public abstract string GetCategoryName();
        public abstract int GetCategoryIdRange();
    }

    public class ClothingFactory : ProductFactory
    {
        private readonly int _categoryId;
        private readonly string _categoryName;

        public ClothingFactory(int categoryId, string categoryName)
        {
            _categoryId = categoryId;
            _categoryName = categoryName;
        }

        public override Item CreateProduct()
        {
            return new Item
            {
                CategoryId = _categoryId,
                ItemsName = $"New {_categoryName} Item",
                SellPrice = 500000,
                Image = "default-clothing.jpg",
                CreatedDate = DateTime.Now,
                IsAvailable = true,
                Category = new Category
                {
                    CategoryId = _categoryId,
                    NameCategory = _categoryName
                }
            };
        }

        public override ProductVariant CreateProductVariant(Item product, string size, string color)
        {
            return new ProductVariant
            {
                ProductId = product.ItemsId,
                Product = product,
                Size = size,
                Color = color,
                PriceModifier = 0,
                StockQuantity = 50
            };
        }

        public override List<string> GetAvailableSizes()
        {
            return new List<string> { "S", "M", "L", "XL", "XXL" };
        }

        public override List<string> GetAvailableColors()
        {
            return new List<string> { "Đen", "Trắng", "Xám", "Navy", "Nâu" };
        }

        public override string GetCategoryName() => _categoryName;
        public override int GetCategoryIdRange() => _categoryId;
    }

    public class AccessoryFactory : ProductFactory
    {
        private readonly int _categoryId;
        private readonly string _categoryName;

        public AccessoryFactory(int categoryId, string categoryName)
        {
            _categoryId = categoryId;
            _categoryName = categoryName;
        }

        public override Item CreateProduct()
        {
            return new Item
            {
                CategoryId = _categoryId,
                ItemsName = $"New {_categoryName} Item",
                SellPrice = 300000,
                Image = "default-accessory.jpg",
                CreatedDate = DateTime.Now,
                IsAvailable = true,
                Category = new Category
                {
                    CategoryId = _categoryId,
                    NameCategory = _categoryName
                }
            };
        }

        public override ProductVariant CreateProductVariant(Item product, string size, string color)
        {
            return new ProductVariant
            {
                ProductId = product.ItemsId,
                Product = product,
                Size = size,
                Color = color,
                PriceModifier = 0,
                StockQuantity = 30
            };
        }

        public override List<string> GetAvailableSizes()
        {
            return _categoryId == 8 ?
                new List<string> { "38", "39", "40", "41", "42", "43", "44" } : // Shoes
                new List<string> { "S", "M", "L", "XL" }; // Belts
        }

        public override List<string> GetAvailableColors()
        {
            return new List<string> { "Đen", "Nâu", "Cognac", "Đỏ burgundy" };
        }

        public override string GetCategoryName() => _categoryName;
        public override int GetCategoryIdRange() => _categoryId;
    }

    // Factory Provider (Abstract Factory Pattern)
    public interface IProductFactoryProvider
    {
        ProductFactory GetFactory(int categoryId);
        ProductFactory GetFactory(string categoryType);
    }

    public class ProductFactoryProvider : IProductFactoryProvider
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

        public ProductFactory GetFactory(int categoryId)
        {
            if (_categoryMap.TryGetValue(categoryId, out var category))
            {
                return category.type switch
                {
                    "clothing" => new ClothingFactory(categoryId, category.name),
                    "accessory" => new AccessoryFactory(categoryId, category.name),
                    _ => throw new ArgumentException($"Unknown category type for ID: {categoryId}")
                };
            }
            throw new ArgumentException($"Category ID {categoryId} not found");
        }

        public ProductFactory GetFactory(string categoryType)
        {
            return categoryType.ToLower() switch
            {
                "clothing" => new ClothingFactory(1, "Quần áo"),
                "accessory" => new AccessoryFactory(8, "Phụ kiện"),
                _ => throw new ArgumentException($"Unknown category type: {categoryType}")
            };
        }
    }
}
