using Microsoft.AspNetCore.Http;
using CuaHangQuanAo.Models;

namespace CuaHangQuanAo.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartKey = "CartSession";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<CartItem> GetCart()
        {
            var data = Session.GetString(CartKey);
            var cart = new List<CartItem>();

            if (!string.IsNullOrEmpty(data))
            {
                var items = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    var parts = item.Split('|');
                    if (parts.Length == 5)
                    {
                        cart.Add(new CartItem
                        {
                            ItemsId = int.Parse(parts[0]),
                            ItemsName = parts[1],
                            Size = parts[2],
                            Quantity = int.Parse(parts[3]),
                            SellPrice = int.Parse(parts[4])
                        });
                    }
                }
            }

            return cart;
        }

        private void SaveCart(List<CartItem> cart)
        {
            var data = string.Join(";", cart.Select(c => $"{c.ItemsId}|{c.ItemsName}|{c.Size}|{c.Quantity}|{c.SellPrice}"));
            Session.SetString(CartKey, data);
        }

        public void AddToCart(CartItem item)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemsId == item.ItemsId && c.Size == item.Size);

            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                cart.Add(item);

            SaveCart(cart);
        }

        public void RemoveFromCart(int id, string size)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemsId == id && c.Size == size);
            if (existing != null) cart.Remove(existing);
            SaveCart(cart);
        }

        public void ClearCart()
        {
            Session.Remove(CartKey);
        }
    }
}
