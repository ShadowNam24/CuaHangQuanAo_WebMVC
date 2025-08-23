using Microsoft.AspNetCore.Mvc;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Entities; // DbContext
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;
        private const string CARTKEY = "CART";

        public CartController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        // Lấy cart từ Session
        private Cart GetCart()
        {
            var cart = HttpContext.Session.Get<Cart>(CARTKEY);
            if (cart == null)
            {
                cart = new Cart();
                HttpContext.Session.Set(CARTKEY, cart);
            }
            return cart;
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCart();
            return View("~/Views/Product/Cart.cshtml", cart);
        }

        // Thêm sản phẩm
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            var product = await _context.Items.FirstOrDefaultAsync(x => x.ItemsId == id);
            if (product == null) return NotFound();

            var cart = GetCart();
            cart.AddItem(new CartItem
            {
                ItemsID = product.ItemsId,
                ItemsName = product.ItemsName,
                SellPrice = product.SellPrice,
                Quantity = quantity
            });

            HttpContext.Session.Set(CARTKEY, cart);
            return RedirectToAction("Index");
        }

        // Xóa sản phẩm
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.RemoveItem(id);
            HttpContext.Session.Set(CARTKEY, cart);
            return RedirectToAction("Index");
        }

        // Checkout
        public IActionResult Checkout()
        {
            var cart = GetCart();
            return View("~/Views/Product/Checkout.cshtml", cart);
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutConfirm()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            //TODO: Lưu vào bảng Orders & OrderDetails
            cart.Clear();
            HttpContext.Session.Set(CARTKEY, cart);

            return RedirectToAction("Index", "Home");
        }
    }
}
