using Microsoft.AspNetCore.Mvc;
using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Services;

namespace CuaHangQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly CuaHangBanQuanAoContext _context;

        public CartController(CartService cartService, CuaHangBanQuanAoContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewBag.Total = cart.Sum(c => c.TotalPrice);
            return View(cart);
        }

        [HttpPost]
        public IActionResult Add(int id, string size, int quantity = 1)
        {
            var item = _context.Items.FirstOrDefault(i => i.ItemsId == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var cartItem = new CartItem
            {
                ItemsId = item.ItemsId,
                ItemsName = item.ItemsName ?? "Không tên",
                Size = size,
                Quantity = quantity,
                SellPrice = item.SellPrice ?? 0
            };

            _cartService.AddToCart(cartItem);

            return Json(new { success = true, message = "Đã thêm vào giỏ", cartCount = _cartService.GetCart().Count });
        }

        public IActionResult Remove(int id, string size)
        {
            _cartService.RemoveFromCart(id, size);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            _cartService.ClearCart();
            return RedirectToAction("Index");
        }
    }
}
