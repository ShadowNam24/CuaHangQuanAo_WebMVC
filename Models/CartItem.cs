namespace CuaHangQuanAo.Models
{
    public class CartItem
    {
        public int? VariantId { get; set; } // New: For variant-based products
        public int ItemsId { get; set; }
        public string ItemsName { get; set; } = null!;
        public string Size { get; set; } = null!;
        public string Color { get; set; } = "Default"; // New: Color information
        public int Quantity { get; set; }
        public int SellPrice { get; set; }
        public string? Image { get; set; } // New: Product image
        public int MaxQuantity { get; set; } = int.MaxValue; // New: Maximum available quantity

        // Calculated properties
        public int TotalPrice => SellPrice * Quantity;
        public bool IsInStock => MaxQuantity > 0;

        // For backward compatibility
        public CartItem()
        {
            Color = "Default";
            MaxQuantity = int.MaxValue;
        }

        // Constructor for variant-based items
        public CartItem(int variantId, int itemsId, string itemsName, string size, string color,
                       int quantity, int sellPrice, string? image = null, int maxQuantity = int.MaxValue)
        {
            VariantId = variantId;
            ItemsId = itemsId;
            ItemsName = itemsName;
            Size = size;
            Color = color;
            Quantity = quantity;
            SellPrice = sellPrice;
            Image = image;
            MaxQuantity = maxQuantity;
        }

        // Constructor for legacy items (backward compatibility)
        public CartItem(int itemsId, string itemsName, string size, int quantity, int sellPrice)
        {
            ItemsId = itemsId;
            ItemsName = itemsName;
            Size = size;
            Color = "Default";
            Quantity = quantity;
            SellPrice = sellPrice;
            MaxQuantity = int.MaxValue;
        }
    }
}
