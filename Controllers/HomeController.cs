using System.Diagnostics;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Entities;            // <-- thêm
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;     // <-- thêm

namespace CuaHangQuanAo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CuaHangBanQuanAoContext _db;   // <-- thêm

        public HomeController(ILogger<HomeController> logger, CuaHangBanQuanAoContext db) // <-- sửa ctor
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // NewProducts: lấy 8 món mới nhất (có thể sửa logic theo cột CreatedAt nếu có)
            var newProducts = await _db.Items
                .Include(i => i.Category)
                .OrderByDescending(i => i.ItemsId)
                .Take(8)
                .ToListAsync();

            // HotProducts: tạm thời lấy 8 món giá cao nhất (placeholder cho “bán chạy”)
            var hotProducts = await _db.Items
                .Include(i => i.Category)
                .OrderByDescending(i => i.SellPrice)
                .Take(8)
                .ToListAsync();

            var vm = new HomeIndexVm
            {
                NewProducts = newProducts,
                HotProducts = hotProducts
            };
            return View(vm); // <-- truyền model
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
