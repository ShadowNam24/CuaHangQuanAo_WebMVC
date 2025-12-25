using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        public async Task<IActionResult> Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            ViewBag.CartItems = cart;
            ViewBag.Total = _cartService.GetCartTotal();

            var vm = new CheckoutVm();

            if (User.Identity!.IsAuthenticated)
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == User.Identity.Name);
                if (account != null)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);
                    if (customer != null)
                    {
                        vm.CustomerName = $"{customer.FirstName} {customer.LastName}".Trim();
                        vm.Phone = customer.PhoneNumber;
                        vm.Address = customer.AddressName;
                    }
                }
            }

            return View(vm);
        }

        // Process checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVm model)
        {
            try
            {
                Console.WriteLine("=== CHECKOUT STARTED ===");
                Console.WriteLine($"CustomerName: {model.CustomerName}");
                Console.WriteLine($"Phone: {model.Phone}");
                Console.WriteLine($"Address: {model.Address}");
                Console.WriteLine($"DiscountAmount: {model.DiscountAmount}");
                Console.WriteLine($"DiscountCode: {model.DiscountCode}");

                var cart = _cartService.GetCart();
                Console.WriteLine($"Cart items count: {cart.Count}");

                if (!cart.Any())
                {
                    Console.WriteLine("Cart is empty!");
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToAction("Index");
                }

                // Clear validation errors for optional discount fields
                if (string.IsNullOrEmpty(model.DiscountCode))
                {
                    ModelState.Remove("DiscountCode");
                }
                if (string.IsNullOrEmpty(model.DiscountDescription))
                {
                    ModelState.Remove("DiscountDescription");
                }

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Error: {error.ErrorMessage}");
                    }

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

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Calculate total with discount
                    var cartTotal = _cartService.GetCartTotal();
                    var finalDiscount = model.DiscountAmount > 0 ? model.DiscountAmount : model.Discount;
                    var finalTotal = cartTotal - finalDiscount;

                    // Create order
                    var order = new Order
                    {
                        CustomerId = null, // Assuming guest checkout for now
                        OrderDate = DateOnly.FromDateTime(DateTime.Now),
                        Total = finalTotal, // Sử dụng Total thay vì TotalAmount
                        TotalAmount = finalTotal,
                        ShippingAddress = model.Address,
                        PhoneNumber = model.Phone,
                        CustomerName = model.CustomerName,
                        Status = "Pending",
                        PaymentMethod = "COD", // Default payment method
                        Discount = finalDiscount, // Sử dụng Discount thay vì DiscountAmount
                        DiscountAmount = finalDiscount, // Save discount amount
                        DiscountCode = model.DiscountCode, // Save applied discount code
                        DiscountDescription = model.DiscountDescription // Save discount description
                    };
                    if (User.Identity!.IsAuthenticated)
                    {
                        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == User.Identity.Name);
                        if (account != null)
                        {
                            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);
                            if (customer != null)
                            {
                                order.CustomerId = customer.CustomerId;
                            }
                        }
                    }


                    Console.WriteLine($"Creating order with Total: {finalTotal}, Discount: {finalDiscount}");
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Order created successfully with ID: {order.OrdersId}");

                    // Add order details and update stock
                    foreach (var cartItem in cart)
                    {
                        Console.WriteLine($"Adding order detail for item {cartItem.ItemsId}, quantity {cartItem.Quantity}");
                        _context.OrdersDetails.Add(new OrdersDetail
                        {
                            OrdersId = order.OrdersId,
                            ProductVariantId = cartItem.ItemsId,
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

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _cartService.ClearCart();

                    // Success message with discount info
                    var successMessage = $"Đặt hàng thành công! Mã đơn: {order.OrdersId}";
                    if (!string.IsNullOrEmpty(model.DiscountCode))
                    {
                        successMessage += $" - Đã áp dụng mã giảm giá: {model.DiscountCode}";
                    }

                    TempData["Success"] = successMessage;
                    return RedirectToAction("Checkout");
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync();
                    }
                    Console.WriteLine($"Checkout error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Log chi tiết hơn
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }

                    TempData["Error"] = $"Đặt hàng thất bại: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== CHECKOUT FAILED ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                TempData["Error"] = $"Đặt hàng thất bại: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyDiscountCode(string discountCode, decimal currentTotal)
        {
            // This is a simplified example. In a real application, you would fetch discount codes from a database
            // and apply more robust validation (e.g., expiry date, max usage, minimum order value).

            var validDiscounts = new Dictionary<string, (string description, string type, decimal value)>
            {
                { "WELCOME10", ("Chào mừng - Giảm 10%", "percentage", 10m) },
                { "SAVE50K", ("Tiết kiệm 50K", "fixed", 50000m) },
                { "NEWUSER", ("Khách hàng mới - Giảm 15%", "percentage", 15m) },
                { "FREESHIP", ("Miễn phí vận chuyển", "freeship", 0m) } // Example for freeship, though not fully implemented in UI calc
            };

            if (validDiscounts.TryGetValue(discountCode.ToUpper(), out var discountInfo))
            {
                decimal discountAmount = 0;
                if (discountInfo.type == "percentage")
                {
                    discountAmount = currentTotal * (discountInfo.value / 100m);
                }
                else if (discountInfo.type == "fixed")
                {
                    discountAmount = discountInfo.value;
                }
                // For 'freeship', discountAmount remains 0, but you might handle shipping cost separately

                // Ensure discount doesn't exceed total
                discountAmount = Math.Min(discountAmount, currentTotal);

                return Json(new { success = true, message = "Mã giảm giá đã được áp dụng!", discountAmount = discountAmount, description = discountInfo.description });
            }
            else
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }
        }
    }
}
