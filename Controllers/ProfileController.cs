using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public ProfileController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            var profile = new Profile
            {
                FullName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}".Trim()
                    : username,
                Email = account.Email,
                PhoneNumber = customer?.PhoneNumber ?? "",
                Address = customer?.AddressName ?? ""
            };

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            var profile = new Profile
            {
                FullName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}".Trim()
                    : username,
                Email = account.Email,
                PhoneNumber = customer?.PhoneNumber ?? "",
                Address = customer?.AddressName ?? ""
            };

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Profile model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            if (customer != null)
            {
                var nameParts = model.FullName.Split(' ', 2);
                customer.FirstName = nameParts.Length > 0 ? nameParts[0] : model.FullName;
                customer.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                customer.PhoneNumber = model.PhoneNumber;
                customer.AddressName = model.Address;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(o => o.OrdersId == id && o.CustomerId == customer!.CustomerId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction(nameof(PaymentHistory));
            }

            // Use explicit physical path to ensure view is found
            return View("~/Views/Profile/OrderDetails.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrdersId == id && o.CustomerId == customer!.CustomerId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction(nameof(PaymentHistory));
            }

            if (order.Status?.ToLower() != "pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý!";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Restore stock
                foreach (var detail in order.OrdersDetails)
                {
                    var storage = await _context.Storages
                        .FirstOrDefaultAsync(s => s.ProductVariantsId == detail.ProductVariantId);

                    if (storage != null)
                    {
                        storage.Quantity += detail.Quantity;
                    }
                }

                // Delete order details
                _context.OrdersDetails.RemoveRange(order.OrdersDetails);

                // Delete order
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã hủy và xóa đơn hàng thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Không thể hủy đơn hàng: {ex.Message}";
            }

            return RedirectToAction(nameof(PaymentHistory));
        }

        // Payment History
        public async Task<IActionResult> PaymentHistory()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == username);

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccId == account.AccId);

            if (customer == null)
            {
                return View(new PaymentHistoryViewModel
                {
                    Payments = new List<PaymentInfo>(),
                    TotalPaid = 0,
                    TotalOrders = 0
                });
            }

            // Get all paid orders (PayPal or VNPay)
            var paidOrders = await _context.Orders
                .Include(o => o.OrdersDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .Where(o => o.CustomerId == customer.CustomerId &&
                           (o.Status == "Paid" || o.Status == "processing" ||   o.PaymentMethod != "COD"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var payments = paidOrders.Select(o => new PaymentInfo
            {
                OrderId = o.OrdersId,
                TransactionId = o.TransactionId,
                PaymentMethod = GetPaymentMethodName(o.PaymentMethod),
                Amount = o.Total ?? 0,
                OrderDate = o.OrderDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now,
                Status = o.Status ?? "Pending",
                CustomerName = o.CustomerName,
                ShippingAddress = o.ShippingAddress,
                ItemCount = o.OrdersDetails.Sum(od => od.Quantity) ?? 0
            }).ToList();

            var viewModel = new PaymentHistoryViewModel
            {
                Payments = payments,
                TotalPaid = payments.Sum(p => p.Amount),
                TotalOrders = payments.Count
            };
            return View(viewModel);
        }

        private string GetPaymentMethodName(string? paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return "Unknown";

            if (paymentMethod.Contains("PayPal", StringComparison.OrdinalIgnoreCase))
                return "PayPal";

            if (paymentMethod.Contains("VNPay", StringComparison.OrdinalIgnoreCase) ||
                paymentMethod.Contains("VNP", StringComparison.OrdinalIgnoreCase))
                return "VNPay";

            return paymentMethod;
        }
    }
}