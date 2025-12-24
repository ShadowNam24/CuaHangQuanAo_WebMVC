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
            // Get all items with their related data first
            var allItems = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ProductVariants)
                    .ThenInclude(pv => pv.OrdersDetails)
                .Where(i => i.IsAvailable) // Only show available products
                .ToListAsync();

            // Top 10 newly added products by CreatedDate
            var newProducts = allItems
                .OrderByDescending(i => i.CreatedDate)
                .Take(10)
                .Select(i => new HomeProductVm
                {
                    ItemsId = i.ItemsId,
                    ItemsName = i.ItemsName,
                    SellPrice = i.SellPrice,
                    CategoryName = i.Category?.NameCategory ?? "Chưa phân loại",
                    Image = GetProductImage(i),
                    SoldQuantity = CalculateSoldQuantity(i)
                })
                .ToList();

            // Top 10 best-selling products by total quantity sold
            var hotProducts = allItems
                .OrderByDescending(i => CalculateSoldQuantity(i))
                .Take(10)
                .Select(i => new HomeProductVm
                {
                    ItemsId = i.ItemsId,
                    ItemsName = i.ItemsName,
                    SellPrice = i.SellPrice,
                    CategoryName = i.Category?.NameCategory ?? "Chưa phân loại",
                    Image = GetProductImage(i),
                    SoldQuantity = CalculateSoldQuantity(i)
                })
                .ToList();

            var vm = new HomeIndexVm
            {
                NewProducts = newProducts,
                HotProducts = hotProducts
            };
            return View(vm);
        }

        /// <summary>
        /// Get the product image from ProductVariants, or return default image
        /// Priority: CoverImage > Latest variant image > Default image
        /// </summary>
        private string GetProductImage(Item item)
        {
            // First, try to use the item's cover image if available
            if (!string.IsNullOrEmpty(item.CoverImage))
            {
                return item.CoverImage;
            }

            // Otherwise, get the most recent variant image
            var variantImage = item.ProductVariants?
                .Where(pv => !string.IsNullOrEmpty(pv.Image))
                .OrderByDescending(pv => pv.ProductVariantsId)
                .Select(pv => pv.Image)
                .FirstOrDefault();

            return variantImage ?? "no-image.png";
        }

        /// <summary>
        /// Calculate total quantity sold for an item across all its variants
        /// </summary>
        private int CalculateSoldQuantity(Item item)
        {
            if (item.ProductVariants == null || !item.ProductVariants.Any())
                return 0;

            return item.ProductVariants
                .SelectMany(pv => pv.OrdersDetails ?? new List<OrdersDetail>())
                .Sum(od => od.Quantity ?? 0);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}