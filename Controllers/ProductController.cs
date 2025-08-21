using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly CuaHangBanQuanAoContext _db;
        public ProductController(CuaHangBanQuanAoContext db) => _db = db;

        // QUẦN ÁO: CategoryId 1..7
        public async Task<IActionResult> QuanAo(ProductListVm f)
        {
            var cats = await _db.Categories
                .Where(c => c.CategoryId >= 1 && c.CategoryId <= 7)
                .OrderBy(c => c.CategoryId).ToListAsync();

            var q = _db.Items.Include(i => i.Category)
                .Where(i => i.CategoryId >= 1 && i.CategoryId <= 7)
                .AsQueryable();

            ApplyFilters(ref q, f);

            var total = await q.CountAsync();
            var items = await ApplySort(q, f.Sort)
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .ToListAsync();

            f.Categories = cats;
            f.Items = items;
            f.Total = total;

            return View("List", ("Quần áo", f)); // dùng chung view List.cshtml
        }

        // PHỤ KIỆN: CategoryId 8..9
        public async Task<IActionResult> PhuKien(ProductListVm f)
        {
            var cats = await _db.Categories
                .Where(c => c.CategoryId == 8 || c.CategoryId == 9)
                .OrderBy(c => c.CategoryId).ToListAsync();

            var q = _db.Items.Include(i => i.Category)
                .Where(i => i.CategoryId == 8 || i.CategoryId == 9)
                .AsQueryable();

            ApplyFilters(ref q, f);

            var total = await q.CountAsync();
            var items = await ApplySort(q, f.Sort)
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .ToListAsync();

            f.Categories = cats;
            f.Items = items;
            f.Total = total;

            return View("List", ("Phụ kiện", f)); // dùng chung view
        }

        // ---- helpers ----
        private static void ApplyFilters(ref IQueryable<Item> q, ProductListVm f)
        {
            if (!string.IsNullOrWhiteSpace(f.Q))
                q = q.Where(i => i.ItemsName.Contains(f.Q));

            if (f.CategoryId is not null)
                q = q.Where(i => i.CategoryId == f.CategoryId);

            if (f.MinPrice is not null)
                q = q.Where(i => i.SellPrice >= f.MinPrice);

            if (f.MaxPrice is not null)
                q = q.Where(i => i.SellPrice <= f.MaxPrice);

            if (!string.IsNullOrWhiteSpace(f.Size))
                q = q.Where(i => i.Size == f.Size);
        }
        [HttpGet("/Product/Detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var item = await _db.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.ItemsId == id);

            if (item == null) return NotFound();

            var related = await _db.Items
                .Where(x => x.CategoryId == item.CategoryId && x.ItemsId != id)
                .OrderBy(x => x.ItemsName)
                .Take(8)
                .ToListAsync();

            var vm = new ProductDetailVm { Item = item, Related = related };
            return View(vm);
        }
    
        private static IQueryable<Item> ApplySort(IQueryable<Item> q, string? sort) =>
            sort switch
            {
                "price_asc" => q.OrderBy(i => i.SellPrice),
                "price_desc" => q.OrderByDescending(i => i.SellPrice),
                "name_desc" => q.OrderByDescending(i => i.ItemsName),
                _ => q.OrderBy(i => i.ItemsName) // name_asc (default)
            };
    }
}
