using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class OrderController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public OrderController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

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

        // Delete (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order != null)
            {
                try
                {
                    // Remove all related order details first
                    if (order.OrdersDetails != null && order.OrdersDetails.Any())
                    {
                        _context.OrdersDetails.RemoveRange(order.OrdersDetails);
                    }

                    // Remove the order itself
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi xóa đơn hàng: {ex.Message}");
                    var orderToShow = await _context.Orders
                        .Include(o => o.Customer)
                        .FirstOrDefaultAsync(o => o.OrdersId == id);
                    return View("OrderDetail", orderToShow);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrdersId == id);
        }
    }
}
