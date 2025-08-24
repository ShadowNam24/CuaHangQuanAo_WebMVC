using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StorageController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public StorageController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> StockRefill(string searchTerm, bool lowStockOnly = false, int page = 1, int pageSize = 15)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.LowStockOnly = lowStockOnly;

            var query = _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.Items)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    (s.Items.ItemsName != null && s.Items.ItemsName.Contains(searchTerm)) ||
                    s.Items.ItemsId.ToString().Contains(searchTerm) ||
                    (s.Supplier.SupplierName != null && s.Supplier.SupplierName.Contains(searchTerm))
                );
            }

            if (lowStockOnly)
            {
                query = query.Where(s => s.Quantity < 10 && s.Quantity > 0);
            }
            query = query.OrderByDescending(s => s.ImportDate)
                 .ThenByDescending(s => s.StorageId);

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var storages = await query
                .OrderByDescending(s => s.ImportDate)
                .ThenByDescending(s => s.StorageId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCost = await query.SumAsync(s => (decimal)(s.ImportCost ?? 0) * (decimal)(s.Quantity ?? 0));
            var latestImportDate = await query.MaxAsync(s => s.ImportDate);
            ViewBag.LatestImportDate = latestImportDate?.ToString("dd/MM/yyyy");

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageSize = pageSize;

            return View(storages);
        }

        public IActionResult Functions_Create()
        {
            ViewBag.Suppliers = _context.Suppliers.ToList();

            // Lấy tất cả danh mục
            var categories = _context.Categories.ToList();

            // Tạo từ điển lưu trữ sản phẩm theo danh mục
            var itemsByCategory = new Dictionary<string, List<dynamic>>();

            foreach (var category in categories)
            {
                var items = _context.Items
                    .Where(i => i.CategoryId == category.CategoryId)
                    .Select(i => new
                    {
                        i.ItemsId,
                        DisplayName = i.ItemsName + (string.IsNullOrEmpty(i.Size) ? "" : " - Size: " + i.Size),
                        i.Size,
                        i.SellPrice,
                        i.CategoryId
                    })
                    .OrderBy(i => i.DisplayName)
                    .ThenBy(i => i.Size)
                    .ToList()
                    .Cast<dynamic>() // Explicitly cast the anonymous type list to dynamic
                    .ToList();

                if (items.Any())
                {
                    itemsByCategory.Add(category.NameCategory ?? "Khác", items);
                }
            }

            ViewBag.ItemsByCategory = itemsByCategory;

            return View("~/Views/Storage/CreateStock.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_Create(Storage storage, int estimatedSellPrice, bool updateItemPrice = false)
        {
            if (ModelState.IsValid)
            {
                _context.Add(storage);
                await _context.SaveChangesAsync();

                if (updateItemPrice && estimatedSellPrice > 0 && storage.ItemsId.HasValue)
                {
                    var item = await _context.Items.FindAsync(storage.ItemsId);
                    if (item != null)
                    {
                        item.SellPrice = estimatedSellPrice;
                        _context.Update(item);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã thêm phiếu nhập kho và cập nhật giá bán sản phẩm thành công!";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Đã thêm phiếu nhập kho thành công!";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã thêm phiếu nhập kho thành công!";
                }

                return RedirectToAction(nameof(StockRefill));
            }
            ViewBag.Suppliers = _context.Suppliers.ToList();
            ViewBag.Items = _context.Items.ToList();
            return View("~/Views/Storage/CreateStock.cshtml", storage);
        }

        public async Task<IActionResult> Functions_Edit(int id)
        {
            var storage = await _context.Storages.FindAsync(id);
            if (storage == null) return NotFound();

            ViewBag.Suppliers = _context.Suppliers.ToList();
            ViewBag.Items = _context.Items.ToList();
            return View("~/Views/Storage/EditStock.cshtml", storage);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_Edit(int id, Storage storage)
        {
            if (id != storage.StorageId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(storage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(StockRefill));
            }
            ViewBag.Suppliers = _context.Suppliers.ToList();
            ViewBag.Items = _context.Items.ToList();
            return View("~/Views/Storage/EditStock.cshtml", storage);
        }


        public async Task<IActionResult> Functions_Details(int id)
        {
            var storage = await _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(m => m.StorageId == id);

            if (storage == null) return NotFound();
            return View("~/Views/Storage/StockDetails.cshtml", storage);
        }


        public async Task<IActionResult> Functions_Delete(int id)
        {
            var storage = await _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.Items)
                .FirstOrDefaultAsync(m => m.StorageId == id);

            if (storage == null) return NotFound();
            return View("~/Views/Storage/DeleteStock.cshtml", storage);
        }


        [HttpPost, ActionName("Functions_DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_DeleteConfirmed(int id)
        {
            try
            {
                var storage = await _context.Storages
                    .FirstOrDefaultAsync(m => m.StorageId == id);

                if (storage != null)
                {
                    _context.Storages.Remove(storage);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Phiếu nhập kho đã được xóa thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập kho cần xóa";
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException?.Message);

                TempData["ErrorMessage"] = "Không thể xóa phiếu nhập kho này. Có thể nó đã được sử dụng ở nơi khác.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa phiếu nhập kho: " + ex.Message;
            }

            return RedirectToAction(nameof(StockRefill));
        }
    }
}
