using System.Diagnostics;
using CuaHangQuanAo.Models;
using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CuaHangQuanAo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CuaHangBanQuanAoContext _db;

        public HomeController(ILogger<HomeController> logger, CuaHangBanQuanAoContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Top 10 newly added products by CreatedDate
            var newProducts = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ProductVariants)
                .OrderByDescending(i => i.CreatedDate)
                .Take(10)
                .Select(i => new HomeProductVm
                {
                    ItemsId = i.ItemsId,
                    ItemsName = i.ItemsName,
                    SellPrice = i.SellPrice,
                    CategoryName = i.Category.NameCategory,
                    Image = i.ProductVariants
                    .Where(pv => !string.IsNullOrEmpty(pv.Image))
                    .OrderByDescending(pv => pv.ProductVariantsId)  // Get the most recent
                    .Select(pv => pv.Image)
                    .FirstOrDefault() ?? "no-image.png",
                    SoldQuantity = (int)i.OrdersDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // Top 10 best-selling products by total quantity sold
            var hotProducts = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ProductVariants)
                .Include(i => i.OrdersDetails)
                .OrderByDescending(i => i.OrdersDetails.Sum(od => od.Quantity))
                .Take(10)
                .Select(i => new HomeProductVm
                {
                    ItemsId = i.ItemsId,
                    ItemsName = i.ItemsName,
                    SellPrice = i.SellPrice,
                    CategoryName = i.Category.NameCategory,
                    Image = i.ProductVariants
                    .Where(pv => !string.IsNullOrEmpty(pv.Image))
                    .OrderByDescending(pv => pv.ProductVariantsId)  // Get the most recent
                    .Select(pv => pv.Image)
                    .FirstOrDefault() ?? "no-image.png",
                    SoldQuantity = (int)i.OrdersDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            var vm = new HomeIndexVm
            {
                NewProducts = newProducts,
                HotProducts = hotProducts
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
