using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class OrderController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;
        private static readonly string[] ValidStatuses = { "pending", "processing", "fulfilled", "cancelled" };

        public OrderController(CuaHangBanQuanAoContext context) => _context = context;

        // List orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Details
        public async Task<IActionResult> Functions_Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrdersDetails)
                .ThenInclude(od => od.Items)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();
            return View("OrderDetail",order);
        }

        // OrderDetail action for public access
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrdersDetails)
                .ThenInclude(od => od.Items)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // Create (GET)
        public IActionResult Functions_Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Items = _context.Items.ToList();
            ViewBag.Employees = _context.Employees
                .Select(e => new { e.EmployeeId, FullName = (e.Firstname ?? "") + " " + (e.Lastname ?? "") })
                .ToList();
            var variants = _context.ProductVariants
                .Include(v => v.Product)
                .ToList();
            ViewBag.Variants = variants;
            return View("CreateOrders");
        }

        // Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_Create(Order order)
        {
            // Validate customer
            if (!order.CustomerId.HasValue)
            {
                ModelState.AddModelError("CustomerId", "Vui lòng chọn khách hàng");
            }

            // Validate order items
            if (order.OrdersDetails == null || !order.OrdersDetails.Any(od => od.Quantity > 0))
            {
                ModelState.AddModelError("OrdersDetails", "Vui lòng chọn ít nhất một sản phẩm với số lượng hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Items = _context.Items.ToList();
                ViewBag.Employees = _context.Employees
                    .Select(e => new { e.EmployeeId, FullName = (e.Firstname ?? "") + " " + (e.Lastname ?? "") })
                    .ToList();
                var variants = _context.ProductVariants
                    .Include(v => v.Product)
                    .ToList();
                ViewBag.Variants = variants;
                return View("CreateOrders", order);
            }

            order.OrderDate = DateOnly.FromDateTime(DateTime.Now);

            // Calculate total
            decimal subtotal = 0;
            foreach (var detail in order.OrdersDetails)
            {
                // Get the price from the database to ensure accuracy
                var item = await _context.Items.FindAsync(detail.ItemsId);
                var price = item?.SellPrice ?? 0;
                detail.Price = (int)price; // Save price to OrdersDetail if needed
                subtotal += price * (detail.Quantity ?? 0);
            }

            var discount = order.Discount ?? 0;
            var discountAmount = subtotal * (discount / 100);
            order.Total = subtotal - discountAmount;

            // Ensure fields are populated if a customer is selected
            if (order.CustomerId.HasValue && 
   (string.IsNullOrWhiteSpace(order.CustomerName) ||
    string.IsNullOrWhiteSpace(order.PhoneNumber) ||
    string.IsNullOrWhiteSpace(order.ShippingAddress)))
            {
                var cust = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId.Value);
                if (cust != null)
                {
                    order.CustomerName ??= $"{cust.FirstName} {cust.LastName}".Trim();
                    order.PhoneNumber ??= cust.PhoneNumber;
                    order.ShippingAddress ??= cust.AddressName;
                }
            }

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi tạo đơn hàng: {ex.Message}");
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Items = _context.Items.ToList();
                ViewBag.Employees = _context.Employees
                    .Select(e => new { e.EmployeeId, FullName = (e.Firstname ?? "") + " " + (e.Lastname ?? "") })
                    .ToList();
                var variants = _context.ProductVariants
                    .Include(v => v.Product)
                    .ToList();
                ViewBag.Variants = variants;
                return View("CreateOrders", order);
            }
        }

        // EDIT (GET): navigate to edit page
        [HttpGet]
        public async Task<IActionResult> Functions_Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrdersDetails).ThenInclude(od => od.Items)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();

            ViewBag.Customers = await _context.Customers.ToListAsync();
            return View("EditOrder", order);
        }

        // EDIT (POST): persist changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_Edit(int id, Order input)
        {
            if (id != input.OrdersId) return BadRequest();

            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();

            // Update editable fields
            order.CustomerName = input.CustomerName;
            order.PhoneNumber = input.PhoneNumber;
            order.ShippingAddress = input.ShippingAddress;
            order.Discount = input.Discount;
            order.Status = input.Status;

            // Recalculate totals from details (if discount changed)
            decimal subtotal = 0;
            foreach (var d in order.OrdersDetails)
            {
                var price = d.Price ?? 0;
                subtotal += price * (d.Quantity ?? 0);
            }
            var discount = order.Discount ?? 0;
            order.Total = subtotal - (subtotal * (discount / 100));

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật đơn hàng #{order.OrdersId}.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE (GET): show confirmation popup page
        [HttpGet]
        public async Task<IActionResult> Functions_Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();
            return View("DeleteOrderConfirm", order);
        }

        // DELETE (POST): confirmed deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order != null)
            {
                if (order.OrdersDetails?.Any() == true)
                    _context.OrdersDetails.RemoveRange(order.OrdersDetails);

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa đơn hàng #{id}.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrdersId == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeStatus(int id, string status)
        {
            status = (status ?? "").ToLowerInvariant();
            if (!ValidStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var order = _context.Orders.FirstOrDefault(o => o.OrdersId == id);
            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = status;
            _context.SaveChanges();
            TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{id} thành '{status}'.";

            // Prevent caching to avoid stale back navigation
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            TempData["StatusMessage"] = $"Cập nhật trạng thái đơn hàng #{id} thành công: {status}";
            // Redirect to Admin home (adjust controller/action to your Admin dashboard)
            return RedirectToAction("Index", "Order"); // or RedirectToAction("Dashboard", "Admin")
        }

       
    }
}
