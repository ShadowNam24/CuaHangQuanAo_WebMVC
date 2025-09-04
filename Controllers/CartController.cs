using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly CuaHangBanQuanAoContext _context;

        public CartController(IHttpContextAccessor httpContextAccessor, CuaHangBanQuanAoContext context)
        {
            _cartService = CartService.GetInstance(httpContextAccessor);
            _context = context;
        }

        public class AddToCartRequest
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewBag.Total = _cartService.GetCartTotal();
            return View(cart);
        }

        // New method compatible with product detail page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Validate request
                if (request.VariantId <= 0 || request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Get variant from database
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .Include(v => v.Storages)
                    .FirstOrDefaultAsync(v => v.ProductVariantsId == request.VariantId);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Use CartService to add item
                var success = _cartService.AddToCartByVariant(request.VariantId, request.Quantity, variant, variant.Product);

                if (!success)
                {
                    var availableStock = variant.Storages?.Sum(s => s.Quantity) ?? 0;
                    return Json(new
                    {
                        success = false,
                        message = availableStock <= 0 ? "Sản phẩm đã hết hàng" : $"Chỉ còn {availableStock} sản phẩm trong kho"
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartCount = _cartService.GetCartCount()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }
        }

        // Legacy method for backward compatibility
        [HttpPost]
        public IActionResult Add(int id, string size, int quantity = 1)
        {
            try
            {
                var item = _context.Items.FirstOrDefault(i => i.ItemsId == id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var cartItem = new CartItem(id, item.ItemsName ?? "Không tên", size, quantity, (int)item.SellPrice);
                var success = _cartService.AddToCart(cartItem);

                if (!success)
                {
                    return Json(new { success = false, message = "Không thể thêm vào giỏ hàng" });
                }

                return Json(new { success = true, message = "Đã thêm vào giỏ", cartCount = _cartService.GetCartCount() });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in legacy add: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var count = _cartService.GetCartCount();
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int variantId, int quantity)
        {
            try
            {
                var cart = _cartService.GetCart();
                var item = cart.FirstOrDefault(c => c.VariantId == variantId);

                if (item == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng" });
                }

                bool success;
                if (item.VariantId.HasValue)
                {
                    // Use variant-based update (not implemented in service yet, so we'll update directly)
                    success = _cartService.UpdateQuantity(item.ItemsId, item.Size, item.Color, quantity);
                }
                else
                {
                    success = _cartService.UpdateQuantity(item.ItemsId, item.Size, item.Color, quantity);
                }

                if (!success)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Chỉ còn {item.MaxQuantity} sản phẩm trong kho"
                    });
                }

                var total = _cartService.GetCartTotal();
                var itemCount = _cartService.GetCartCount();
                var updatedItem = _cartService.GetCart().FirstOrDefault(c => c.VariantId == variantId);

                return Json(new
                {
                    success = true,
                    total = total.ToString("N0"),
                    cartCount = itemCount,
                    itemTotal = (updatedItem?.TotalPrice ?? 0).ToString("N0")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating cart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int variantId)
        {
            try
            {
                var success = _cartService.RemoveFromCartByVariant(variantId);

                if (success)
                {
                    var total = _cartService.GetCartTotal();
                    var itemCount = _cartService.GetCartCount();

                    return Json(new
                    {
                        success = true,
                        total = total.ToString("N0"),
                        cartCount = itemCount
                    });
                }

                return Json(new { success = false, message = "Không thể xóa sản phẩm" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from cart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // Legacy remove method
        public IActionResult Remove(int id, string size)
        {
            _cartService.RemoveFromCart(id, size, "Default");
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            _cartService.ClearCart();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            return Json(new { success = true });
        }

        // Stock validation before checkout
        [HttpPost]
        public IActionResult CheckStock()
        {
            var (isValid, errorMessage) = _cartService.ValidateStock(_context);

            if (!isValid)
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("Index");
            }

            return RedirectToAction("Checkout");
        }

        // Display checkout form
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            ViewBag.Total = _cartService.GetCartTotal();
            ViewBag.CartItems = cart; // Pass cart items to view
            return View(new CheckoutVm());
        }

        // Process checkout
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
                ViewBag.Total = _cartService.GetCartTotal();
                ViewBag.CartItems = cart;
                return View(model);
            }

            // Final stock validation
            var (isValid, errorMessage) = _cartService.ValidateStock(_context);
            if (!isValid)
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("Index");
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Create order
                var order = new Order
                {
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Discount = model.Discount,
                    Total = _cartService.GetCartTotal() - model.Discount
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                // Add order details and update stock
                foreach (var cartItem in cart)
                {
                    _context.OrdersDetails.Add(new OrdersDetail
                    {
                        OrdersId = order.OrdersId,
                        ItemsId = cartItem.ItemsId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.SellPrice
                    });

                    // Update stock
                    if (cartItem.VariantId.HasValue)
                    {
                        // Update variant-based storage
                        var storageEntries = _context.Storages
                            .Where(s => s.ProductVariantsId == cartItem.VariantId.Value)
                            .OrderBy(s => s.StorageId)
                            .ToList();

                        var remainingQuantity = cartItem.Quantity;
                        foreach (var storage in storageEntries)
                        {
                            if (remainingQuantity <= 0) break;
                            var deduction = Math.Min(storage.Quantity ?? 0, remainingQuantity);
                            storage.Quantity -= deduction;
                            remainingQuantity -= deduction;
                        }
                    }
                    else
                    {
                        // Legacy storage update
                        var storage = _context.Storages.FirstOrDefault(s => s.ProductVariantsId == cartItem.ItemsId);
                        if (storage != null)
                        {
                            storage.Quantity -= cartItem.Quantity;
                        }
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                _cartService.ClearCart();

                TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrdersId}";
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Checkout error: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }
    }
}
