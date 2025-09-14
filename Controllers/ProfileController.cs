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

        [HttpGet]
        public IActionResult Index()
        {
            // Get current user's profile (simplified - you might want to get from database)
            var profile = new Profile
            {
                Id = 1,
                FullName = User.Identity?.Name ?? "Khách hàng",
                Email = User.Identity?.Name + "@example.com", // This should come from user claims or database
                PhoneNumber = "",
                Address = "",
                EmailVerified = true,
                AvatarUrl = "/images/default-avatar.png"
            };
            
            return View(profile);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            // Get current user's profile for editing
            var profile = new Profile
            {
                Id = 1,
                FullName = User.Identity?.Name ?? "Khách hàng",
                Email = User.Identity?.Name + "@example.com",
                PhoneNumber = "",
                Address = "",
                EmailVerified = true,
                AvatarUrl = "/images/default-avatar.png"
            };
            
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Profile model)
        {
            if (ModelState.IsValid)
            {
                // Here you would update the profile in database
                // For now, just redirect back to index
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Index");
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            // Get orders for current user
            // For now, we'll get all orders (in real app, filter by user)
            var orders = await _context.Orders
                .Include(o => o.OrdersDetails)
                .ThenInclude(od => od.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            // Get order details for current user
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrdersDetails)
                .ThenInclude(od => od.Items)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrdersDetails)
                .FirstOrDefaultAsync(o => o.OrdersId == id);
            
            if (order == null)
            {
                return NotFound();
            }

            // Check if order can be cancelled (only pending orders)
            if (order.Status?.ToLower() != "pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý!";
                return RedirectToAction("OrderDetail", new { id = id });
            }

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
                
                TempData["Success"] = "Đơn hàng đã được hủy và xóa thành công!";
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi hủy đơn hàng: {ex.Message}";
                return RedirectToAction("OrderDetail", new { id = id });
            }
        }
    }
}
