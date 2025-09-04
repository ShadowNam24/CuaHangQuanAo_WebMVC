using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CuaHangQuanAo.Services
{
    public class CartService
    {
        private static CartService? _instance;
        private static readonly object _lock = new object();
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartKey = "CartSession";

        // Private constructor for singleton
        private CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Singleton instance getter
        public static CartService GetInstance(IHttpContextAccessor httpContextAccessor)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new CartService(httpContextAccessor);
                }
            }
            return _instance;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<CartItem> GetCart()
        {
            var cartJson = Session.GetString(CartKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                // If deserialization fails, return empty cart and clear corrupted data
                Session.Remove(CartKey);
                return new List<CartItem>();
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            try
            {
                var cartJson = JsonSerializer.Serialize(cart);
                Session.SetString(CartKey, cartJson);
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error saving cart: {ex.Message}");
            }
        }

        public bool AddToCart(CartItem item)
        {
            try
            {
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c =>
                    c.ItemsId == item.ItemsId &&
                    c.Size == item.Size &&
                    c.Color == item.Color);

                if (existing != null)
                {
                    // Check if adding quantity would exceed max
                    if (existing.Quantity + item.Quantity > item.MaxQuantity)
                    {
                        return false; // Cannot add more than available stock
                    }
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    cart.Add(item);
                }

                SaveCart(cart);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddToCartByVariant(int variantId, int quantity, ProductVariant variant, Item item)
        {
            try
            {
                // Calculate available stock from storage
                var availableStock = variant.Storages?.Sum(s => s.Quantity) ?? 0;

                if (availableStock < quantity)
                {
                    return false;
                }

                var cartItem = new CartItem
                {
                    VariantId = variantId,
                    ItemsId = item.ItemsId,
                    ItemsName = item.ItemsName ?? "Không tên",
                    Size = variant.Size,
                    Color = variant.Color,
                    Quantity = quantity,
                    SellPrice = (int)(item.SellPrice + variant.PriceModifier),
                    Image = variant.Image,
                    MaxQuantity = availableStock
                };

                return AddToCart(cartItem);
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateQuantity(int itemsId, string size, string color, int quantity)
        {
            try
            {
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c =>
                    c.ItemsId == itemsId &&
                    c.Size == size &&
                    c.Color == color);

                if (existing == null)
                {
                    return false;
                }

                if (quantity <= 0)
                {
                    cart.Remove(existing);
                }
                else if (quantity > existing.MaxQuantity)
                {
                    return false;
                }
                else
                {
                    existing.Quantity = quantity;
                }

                SaveCart(cart);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveFromCart(int itemsId, string size, string color)
        {
            try
            {
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c =>
                    c.ItemsId == itemsId &&
                    c.Size == size &&
                    c.Color == color);

                if (existing != null)
                {
                    cart.Remove(existing);
                    SaveCart(cart);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveFromCartByVariant(int variantId)
        {
            try
            {
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c => c.VariantId == variantId);

                if (existing != null)
                {
                    cart.Remove(existing);
                    SaveCart(cart);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void ClearCart()
        {
            try
            {
                Session.Remove(CartKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart: {ex.Message}");
            }
        }

        public int GetCartCount()
        {
            try
            {
                var cart = GetCart();
                return cart.Sum(c => c.Quantity);
            }
            catch
            {
                return 0;
            }
        }

        public decimal GetCartTotal()
        {
            try
            {
                var cart = GetCart();
                return cart.Sum(c => c.TotalPrice);
            }
            catch
            {
                return 0;
            }
        }

        // Validate stock before checkout
        public (bool isValid, string errorMessage) ValidateStock(CuaHangBanQuanAoContext context)
        {
            try
            {
                var cart = GetCart();
                if (!cart.Any())
                {
                    return (false, "Giỏ hàng trống!");
                }

                foreach (var item in cart)
                {
                    // Check by variant ID if available
                    if (item.VariantId.HasValue)
                    {
                        var variant = context.ProductVariants
                            .Include(v => v.Storages)
                            .FirstOrDefault(v => v.ProductVariantsId == item.VariantId);

                        if (variant == null)
                        {
                            return (false, $"Sản phẩm {item.ItemsName} không tồn tại");
                        }

                        var availableStock = variant.Storages?.Sum(s => s.Quantity) ?? 0;
                        if (availableStock < item.Quantity)
                        {
                            return (false, $"Sản phẩm {item.ItemsName} ({item.Size}, {item.Color}) không đủ số lượng (Còn {availableStock})");
                        }
                    }
                    else
                    {
                        // Fallback to old storage check
                        var storage = context.Storages.FirstOrDefault(s => s.ProductVariantsId == item.ItemsId);
                        if (storage == null || storage.Quantity < item.Quantity)
                        {
                            return (false, $"Sản phẩm {item.ItemsName} không đủ số lượng (Còn {storage?.Quantity ?? 0})");
                        }
                    }
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kiểm tra tồn kho: {ex.Message}");
            }
        }
    }
}
