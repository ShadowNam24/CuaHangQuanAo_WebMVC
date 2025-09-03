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
                SellPrice = (int)item.SellPrice
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

        // Kiểm tra tồn kho trước khi qua Checkout
        [HttpPost]
        public IActionResult CheckStock()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            foreach (var c in cart)
            {
                var storage = _context.Storages.FirstOrDefault(s => s.ProductVariantsId == c.ItemsId);
                if (storage == null || storage.Quantity < c.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {c.ItemsName} không đủ số lượng (Còn {storage?.Quantity ?? 0})";
                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Checkout");
        }

        // Hiển thị form Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            ViewBag.Total = cart.Sum(c => c.TotalPrice);
            return View(new CheckoutVm());
        }

        // Xử lý đặt hàng
        [HttpPost]
        public IActionResult Checkout(CheckoutVm model)
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Total = cart.Sum(c => c.TotalPrice);
                return View(model);
            }

            // Kiểm tra tồn kho lần cuối
            foreach (var c in cart)
            {
                var storage = _context.Storages.FirstOrDefault(s => s.ProductVariantsId == c.ItemsId);
                if (storage == null || storage.Quantity < c.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {c.ItemsName} không đủ số lượng (Còn {storage?.Quantity ?? 0})";
                    return RedirectToAction("Index");
                }
            }

            // Tạo đơn hàng
            var order = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Discount = model.Discount,
                Total = cart.Sum(c => c.TotalPrice) - model.Discount
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Thêm chi tiết + trừ kho
            foreach (var c in cart)
            {
                _context.OrdersDetails.Add(new OrdersDetail
                {
                    OrdersId = order.OrdersId,
                    ItemsId = c.ItemsId,
                    Quantity = c.Quantity,
                    Price = c.SellPrice
                });

                var storage = _context.Storages.FirstOrDefault(s => s.ProductVariantsId == c.ItemsId);
                if (storage != null)
                {
                    storage.Quantity -= c.Quantity;
                }
            }

            _context.SaveChanges();

            _cartService.ClearCart();

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrdersId}";
            return RedirectToAction("Checkout");
        }
    }
}
