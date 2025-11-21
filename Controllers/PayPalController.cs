using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class PayPalController : Controller
{
    private readonly PayPalApiService _paypal;
    private readonly CuaHangBanQuanAoContext _context;
    private readonly CartService _cartService;

    public PayPalController(PayPalApiService paypal,
                            CuaHangBanQuanAoContext context,
                            CartService cartService)
    {
        _paypal = paypal;
        _context = context;
        _cartService = cartService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(
        decimal total,
        string? CustomerName,
        string? Phone,
        string? Address,
        string? DiscountCode,
        decimal? DiscountAmount,
        string? DiscountDescription)
    {
        var checkoutInfo = new CheckoutInfo(
            CustomerName, Phone, Address,
            DiscountCode, DiscountAmount, DiscountDescription, total);

        HttpContext.Session.SetString("checkout-info", JsonSerializer.Serialize(checkoutInfo));

        var returnUrl = Url.Action("Success", "PayPal", null, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "PayPal", null, Request.Scheme);

        var orderResponse = await _paypal.CreateOrderAsync(total, returnUrl!, cancelUrl!);
        using var json = JsonDocument.Parse(orderResponse);

        var approvalUrl = json.RootElement
            .GetProperty("links")
            .EnumerateArray()
            .First(l => l.GetProperty("rel").GetString() == "approve")
            .GetProperty("href")
            .GetString();

        return Redirect(approvalUrl!);
    }

    public async Task<IActionResult> Success(string token, string PayerID)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Missing PayPal token.";
            return RedirectToAction("Index", "Cart");
        }

        var (ok, json, redirectUrl) = await _paypal.TryCaptureOrderAsync(token);
        if (!ok)
        {
            if (!string.IsNullOrEmpty(redirectUrl)) return Redirect(redirectUrl);
            TempData["Error"] = "PayPal payment not confirmed. Please try again.";
            return RedirectToAction("Index", "Cart");
        }

        // Read checkout info + cart
        var infoJson = HttpContext.Session.GetString("checkout-info");
        var info = string.IsNullOrEmpty(infoJson) ? null : JsonSerializer.Deserialize<CheckoutInfo>(infoJson);
        var cart = _cartService.GetCart();
        if (cart.Count == 0)
        {
            TempData["Error"] = "Cart is empty. Cannot create order.";
            return RedirectToAction("Index", "Cart");
        }

        // Validate stock (same as normal flow)
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
                PaymentMethod = "PayPal",
                Discount = finalDiscount,
                DiscountAmount = finalDiscount,
                DiscountCode = info?.DiscountCode,
                DiscountDescription = info?.DiscountDescription
            };

            // Trong Success ngay trước khi tạo Order (nếu bạn đã chuyển sang lưu DB ở đây) bổ sung tự động gán thông tin khách hàng nếu null
            if (string.IsNullOrWhiteSpace(order.CustomerName) ||
                string.IsNullOrWhiteSpace(order.PhoneNumber) ||
                string.IsNullOrWhiteSpace(order.ShippingAddress))
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == User.Identity!.Name);
                if (account != null)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);
                    if (customer != null)
                    {
                        order.CustomerName ??= $"{customer.FirstName} {customer.LastName}".Trim();
                        order.PhoneNumber ??= customer.PhoneNumber;
                        order.ShippingAddress ??= customer.AddressName;
                        order.CustomerId = customer.CustomerId;
                    }
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var ci in cart)
            {
                _context.OrdersDetails.Add(new OrdersDetail
                {
                    OrdersId = order.OrdersId,
                    ItemsId = ci.ItemsId,
                    Quantity = ci.Quantity,
                    Price = ci.SellPrice
                });

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
                    var storage = _context.Storages.FirstOrDefault(s => s.ProductVariantsId == ci.ItemsId);
                    if (storage != null) storage.Quantity -= ci.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // Clear cart + session state
            _cartService.ClearCart();
            HttpContext.Session.Remove("checkout-info");

            // Show success page with order id
            return View("Success", model: order.OrdersId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = $"Failed to create order: {ex.Message}";
            return RedirectToAction("Index", "Cart");
        }
    }

    public IActionResult Cancel()
    {
        TempData["Error"] = "You cancelled PayPal payment.";
        return RedirectToAction("Index", "Cart");
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
