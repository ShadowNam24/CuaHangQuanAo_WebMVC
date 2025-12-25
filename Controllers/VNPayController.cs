using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Mvc;
using CuaHangQuanAo.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CuaHangQuanAo.Controllers
{
    public class VNPayController : Controller
    {
        private readonly VnPayLibrary _vnpay;
        private readonly CuaHangBanQuanAoContext _context;
        private readonly CartService _cartService;
        private readonly IConfiguration _config;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(
            VnPayLibrary vnpay,
            CuaHangBanQuanAoContext context,
            CartService cartService,
            IConfiguration config,
            ILogger<VNPayController> logger)
        {
            _vnpay = vnpay;
            _context = context;
            _cartService = cartService;
            _config = config;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePayment(
            decimal total,
            string? CustomerName,
            string? Phone,
            string? Address,
            string? DiscountCode,
            decimal? DiscountAmount,
            string? DiscountDescription)
        {
            // Store checkout info in session
            var checkoutInfo = new CheckoutInfo(
                CustomerName, Phone, Address,
                DiscountCode, DiscountAmount, DiscountDescription, total);

            HttpContext.Session.SetString("checkout-info", JsonSerializer.Serialize(checkoutInfo));

            // Read VNPay configuration
            var vnpOptions = _config.GetSection("VnPay").Get<VnPayOptions>();
            var vnpUrl = vnpOptions?.Url ?? _config["VnPay:Url"] ?? string.Empty;
            var vnp_HashSecret = vnpOptions?.HashSecret ?? _config["VnPay:HashSecret"] ?? string.Empty;
            var vnp_TmnCode = vnpOptions?.TmnCode ?? _config["VnPay:TmnCode"] ?? string.Empty;

            // Create VNPay payment request data and add to VnPayLibrary
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_Version"] = VnPayLibrary.VERSION,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = vnp_TmnCode,
                ["vnp_Amount"] = ((long)(total * 100)).ToString(), // VNPay requires amount in smallest currency unit (VND * 100)
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Thanh toan don hang #{Guid.NewGuid().ToString("N")[..8]}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = Url.Action("Return", "VNPay", null, Request.Scheme) ?? vnpOptions?.ReturnUrl ?? string.Empty,
                ["vnp_TxnRef"] = DateTime.Now.Ticks.ToString()
            };

            // Add request params to VnPayLibrary
            foreach (var kv in vnpayData)
            {
                _vnpay.AddRequestData(kv.Key, kv.Value);
            }

            // Build payment URL using base VNPay URL and secret
            var paymentUrl = _vnpay.CreateRequestUrl(vnpUrl, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Return()
        {
            // Add all query parameters into VnPayLibrary response data
            foreach (var key in Request.Query.Keys)
            {
                var value = Request.Query[key].ToString();
                _vnpay.AddResponseData(key, value);
            }

            // Read secret from configuration
            var vnp_HashSecret = _config.GetValue<string>("VnPay:HashSecret") ?? _config.GetSection("VnPay").Get<VnPayOptions>()?.HashSecret ?? string.Empty;

            // Validate signature
            var inputHash = Request.Query["vnp_SecureHash"].ToString();
            if (string.IsNullOrEmpty(inputHash) || !_vnpay.ValidateSignature(inputHash, vnp_HashSecret))
            {
                // Log useful debug info without exposing the secret
                var receivedHash = inputHash;
                var keys = string.Join(',', Request.Query.Keys.OrderBy(k => k));
                _logger.LogWarning("VNPay signature validation failed. ReceivedHash={ReceivedHash}, Keys={Keys}", receivedHash, keys);

                TempData["Error"] = "Chữ ký không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }

            // Check response code
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            if (responseCode != "00")
            {
                TempData["Error"] = GetVnPayErrorMessage(responseCode);
                return RedirectToAction("Index", "Cart");
            }

            // Payment successful - create order
            var infoJson = HttpContext.Session.GetString("checkout-info");
            var info = string.IsNullOrEmpty(infoJson)
                ? null
                : JsonSerializer.Deserialize<CheckoutInfo>(infoJson);

            var cart = _cartService.GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống. Không thể tạo đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Validate stock
            var (isValid, errorMessage) = _cartService.ValidateStock(_context);
            if (!isValid)
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("Index", "Cart");
            }

            var cartTotal = _cartService.GetCartTotal();
            var finalDiscount = info?.DiscountAmount ?? 0;
            var finalTotal = cartTotal - finalDiscount;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get transaction info from VNPay response (from Request.Query)
                var transactionId = Request.Query["vnp_TransactionNo"].ToString();
                var txnRef = Request.Query["vnp_TxnRef"].ToString();
                var bankCode = Request.Query["vnp_BankCode"].ToString();

                var order = new Order
                {
                    CustomerId = null,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    Total = finalTotal,
                    TotalAmount = finalTotal,
                    ShippingAddress = info?.Address,
                    PhoneNumber = info?.Phone,
                    CustomerName = info?.CustomerName,
                    Status = "Paid",
                    PaymentMethod = $"VNPay - {bankCode}",
                    Discount = finalDiscount,
                    DiscountAmount = finalDiscount,
                    DiscountCode = info?.DiscountCode,
                    DiscountDescription = info?.DiscountDescription,
                    TransactionId = string.IsNullOrEmpty(transactionId) ? null : $"VNP-{transactionId}"
                };

                // Auto-fill customer info if logged in
                if (User.Identity?.IsAuthenticated == true)
                {
                    var account = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.Username == User.Identity.Name);

                    if (account != null)
                    {
                        var customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.AccId == account.AccId);

                        if (customer != null)
                        {
                            order.CustomerId = customer.CustomerId;
                            order.CustomerName ??= $"{customer.FirstName} {customer.LastName}".Trim();
                            order.PhoneNumber ??= customer.PhoneNumber;
                            order.ShippingAddress ??= customer.AddressName;
                        }
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order details and update stock
                foreach (var ci in cart)
                {
                    _context.OrdersDetails.Add(new OrdersDetail
                    {
                        OrdersId = order.OrdersId,
                        ProductVariantId = ci.ItemsId,
                        Quantity = ci.Quantity,
                        Price = ci.SellPrice
                    });

                    // Update stock
                    if (ci.VariantId.HasValue)
                    {
                        var storageEntries = _context.Storages
                            .Where(s => s.ProductVariantsId == ci.VariantId.Value)
                            .OrderBy(s => s.StorageId)
                            .ToList();

                        var remaining = ci.Quantity;
                        foreach (var st in storageEntries)
                        {
                            if (remaining <= 0) break;
                            var deduct = Math.Min(st.Quantity ?? 0, remaining);
                            st.Quantity -= deduct;
                            remaining -= deduct;
                        }
                    }
                    else
                    {
                        var storage = _context.Storages
                            .FirstOrDefault(s => s.ProductVariantsId == ci.ItemsId);
                        if (storage != null)
                            storage.Quantity -= ci.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Clear cart and session
                _cartService.ClearCart();
                HttpContext.Session.Remove("checkout-info");

                TempData["Success"] = $"Thanh toán VNPay thành công! Mã đơn hàng: #{order.OrdersId}";
                return View("Success", order.OrdersId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Không thể tạo đơn hàng: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        public IActionResult Success(int orderId)
        {
            return View(orderId);
        }

        private string GetVnPayErrorMessage(string? responseCode)
        {
            return responseCode switch
            {
                "07" => "Giao dịch bị nghi ngờ gian lận",
                "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking",
                "10" => "Xác thực thông tin không đúng quá 3 lần",
                "11" => "Đã hết hạn chờ thanh toán",
                "12" => "Thẻ/Tài khoản bị khóa",
                "13" => "Mật khẩu xác thực giao dịch không đúng",
                "24" => "Khách hàng hủy giao dịch",
                "51" => "Tài khoản không đủ số dư",
                "65" => "Tài khoản vượt quá hạn mức giao dịch",
                "75" => "Ngân hàng thanh toán đang bảo trì",
                "79" => "Giao dịch vượt quá số lần nhập sai mật khẩu",
                _ => "Giao dịch thất bại. Vui lòng thử lại."
            };
        }

        private sealed record CheckoutInfo(
            string? CustomerName,
            string? Phone,
            string? Address,
            string? DiscountCode,
            decimal? DiscountAmount,
            string? DiscountDescription,
            decimal Total);
    }
}