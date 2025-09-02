using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Factory;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly CuaHangBanQuanAoContext _db;
        private readonly IProductService _productService;
        private readonly IProductFactoryProvider _factoryProvider;

            public ProductController(
                CuaHangBanQuanAoContext db,
                IProductService productService,
                IProductFactoryProvider factoryProvider)
            {
                _db = db;
                _productService = productService;
                _factoryProvider = factoryProvider;
            }

            [HttpGet("/Product/Detail/{id:int}")]
            public async Task<IActionResult> Detail(int id)
            {
                try
                {
                    var vm = await _productService.GetProductDetailWithStorageAsync(id);
                    return View(vm);
                }
                catch (ArgumentException)
                {
                    return NotFound();
                }
            }

            // Get available variants for a product (including stock info)
            [HttpGet]
            public async Task<IActionResult> GetAvailableVariants(int productId)
            {
                try
                {
                    var variants = await _productService.GetAvailableVariantsAsync(productId);
                    return Json(variants);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            // Get available sizes from storage
            [HttpGet]
            public async Task<IActionResult> GetAvailableSizesFromStorage(int productId)
            {
                try
                {
                    var sizes = await _productService.GetAvailableSizesFromStorageAsync(productId);
                    return Json(sizes);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            // Get available colors from storage
            [HttpGet]
            public async Task<IActionResult> GetAvailableColorsFromStorage(int productId)
            {
                try
                {
                    var colors = await _productService.GetAvailableColorsFromStorageAsync(productId);
                    return Json(colors);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            // Get specific variant info (price, stock) for selected size/color combination
            [HttpGet]
            public async Task<IActionResult> GetVariantInfo(int productId, string size, string color)
            {
                try
                {
                    var variant = await _productService.GetVariantInfoAsync(productId, size, color);
                    if (variant == null)
                    {
                        return NotFound(new { message = "Variant not found or out of stock" });
                    }
                    return Json(variant);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            // Check stock for specific variant
            [HttpGet]
            public async Task<IActionResult> CheckStock(int variantId)
            {
                try
                {
                    var stock = await _productService.GetVariantStockQuantityAsync(variantId);
                    return Json(new { variantId, stockQuantity = stock, inStock = stock > 0 });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            // Rest of your existing methods...
            [HttpGet]
            public async Task<IActionResult> Suggest(string term)
            {
                if (string.IsNullOrWhiteSpace(term))
                    return Json(Enumerable.Empty<object>());

                var data = await _db.Items
                    .AsNoTracking()
                    .Where(x => x.ItemsName.Contains(term) && x.IsAvailable)
                    .OrderBy(x => x.ItemsName)
                    .Select(x => new
                    {
                        id = x.ItemsId,
                        name = x.ItemsName,
                        price = x.SellPrice,
                        img = Url.Content($"~/Images/Products/{x.ItemsId}.jpg")
                    })
                    .Take(8)
                    .ToListAsync();

                return Json(data);
            }

            [HttpGet]
            public async Task<IActionResult> AjaxSearch(string q)
            {
                var query = _db.Items.AsNoTracking().Where(x => x.IsAvailable);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    query = query.Where(x => x.ItemsName.Contains(q));
                }

                var results = await query
                    .OrderByDescending(x => x.ItemsId)
                    .ThenBy(x => x.ItemsName)
                    .Take(24)
                    .ToListAsync();

                return PartialView("_SearchResults", results);
            }

            public async Task<IActionResult> QuanAo(ProductListVm f)
            {
                var cats = await _db.Categories
                    .Where(c => c.CategoryId >= 1 && c.CategoryId <= 7)
                    .OrderBy(c => c.CategoryId).ToListAsync();

                var q = _db.Items.Include(i => i.Category)
                    .Where(i => i.CategoryId >= 1 && i.CategoryId <= 7 && i.IsAvailable)
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

                return View("List", ("Quần áo", f));
            }

            public async Task<IActionResult> PhuKien(ProductListVm f)
            {
                var cats = await _db.Categories
                    .Where(c => c.CategoryId == 8 || c.CategoryId == 9)
                    .OrderBy(c => c.CategoryId).ToListAsync();

                var q = _db.Items.Include(i => i.Category)
                    .Where(i => (i.CategoryId == 8 || i.CategoryId == 9) && i.IsAvailable)
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

                return View("List", ("Phụ kiện", f));
            }

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
            }

            private static IQueryable<Item> ApplySort(IQueryable<Item> q, string? sort) =>
                sort switch
                {
                    "price_asc" => q.OrderBy(i => i.SellPrice),
                    "price_desc" => q.OrderByDescending(i => i.SellPrice),
                    "name_desc" => q.OrderByDescending(i => i.ItemsName),
                    _ => q.OrderBy(i => i.ItemsName)
                };
        }
    }
