using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Employee, Admin")]
    public class AdminController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public AdminController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy 5 đơn hàng gần nhất
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Lấy 5 sản phẩm sắp hết hàng (tổng tồn kho <= 10)
            var lowStockItems = await _context.Storages
                .GroupBy(s => s.ProductVariantsId)
                .Select(g => new
                {
                    ItemsId = g.Key,
                    TotalQuantity = g.Sum(s => s.Quantity ?? 0)
                })
                .Where(x => x.TotalQuantity > 0 && x.TotalQuantity <= 10)
                .Join(_context.Items, x => x.ItemsId, i => i.ItemsId, (x, i) => i)
                .Take(5)
                .ToListAsync();

            // Lấy 5 nhà cung cấp có nhiều lần nhập kho nhất
            var topSuppliers = await _context.Suppliers
                .OrderByDescending(s => s.Storages.Count)
                .Take(5)
                .ToListAsync();

            var dashboard = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalProducts = await _context.Items.CountAsync(),
                TotalInventory = await _context.Storages.SumAsync(s => s.Quantity ?? 0),
                TotalSuppliers = await _context.Suppliers.CountAsync(),
                RecentOrders = recentOrders,
                LowStockItems = lowStockItems,
                TopSuppliers = topSuppliers
            };
            return View(dashboard);
        }

        [Authorize(Roles = "Employee, Admin")]
        public IActionResult Chat()
        {
            return View();
        }
    }
}
