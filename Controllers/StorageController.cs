using CuaHangQuanAo.DesignPatterns;
using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models.ViewModels;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StorageController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;
        private readonly IStorageService _storageService;
        private readonly IStorageFactoryProvider _storageFactoryProvider;

        public StorageController(
            CuaHangBanQuanAoContext context,
            IStorageService storageService,
            IStorageFactoryProvider storageFactoryProvider)
        {
            _context = context;
            _storageService = storageService;
            _storageFactoryProvider = storageFactoryProvider;
        }

        public async Task<IActionResult> StockRefill(string searchTerm, bool lowStockOnly = false, int page = 1, int pageSize = 15)
        {
            var query = _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.ProductVariants)
                    .ThenInclude(pv => pv.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    (s.Supplier.SupplierName != null && s.Supplier.SupplierName.Contains(searchTerm)) ||
                    (s.ProductVariants.Product.ItemsName.Contains(searchTerm))
                );
            }

            if (lowStockOnly)
            {
                query = query.Where(s => s.Quantity < 10 && s.Quantity > 0);
            }

            query = query.OrderByDescending(s => s.ImportDate).ThenByDescending(s => s.StorageId);

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var storages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new StorageListVm
            {
                SearchTerm = searchTerm,
                LowStockOnly = lowStockOnly,
                Page = page,
                PageSize = pageSize,
                Storages = storages,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                TotalCost = await query.SumAsync(s => (decimal)(s.ImportCost ?? 0) * (decimal)(s.Quantity ?? 0)),
                LatestImportDate = (await query.MaxAsync(s => s.ImportDate))?.ToString("dd/MM/yyyy")
            };

            return View(vm);
        }

        public async Task<IActionResult> Functions_Create()
        {
            var vm = await _storageService.PrepareCreateViewModelAsync();
            return View("~/Views/Storage/CreateStock.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_Create(int productId, int supplierId, string size, string color,
            int quantity, int importCost, decimal estimatedSellPrice, bool updateItemPrice = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(color))
                {
                    ModelState.AddModelError("", "Size và Color là bắt buộc");
                    var vm = await _storageService.PrepareCreateViewModelAsync();
                    return View("~/Views/Storage/CreateStock.cshtml", vm);
                }

                var storage = await _storageService.CreateStorageEntryAsync(productId, supplierId, size, color, quantity, importCost);

                if (updateItemPrice && estimatedSellPrice > 0)
                {
                    var item = await _context.Items.FindAsync(productId);
                    if (item != null)
                    {
                        item.SellPrice = (int)estimatedSellPrice;
                        _context.Update(item);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã thêm phiếu nhập kho và cập nhật giá bán sản phẩm thành công!";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã thêm phiếu nhập kho thành công!";
                }

                return RedirectToAction(nameof(StockRefill));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                var vm = await _storageService.PrepareCreateViewModelAsync();
                return View("~/Views/Storage/CreateStock.cshtml", vm);
            }
        }

        // API methods for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetRecommendedSizes(int categoryId)
        {
            try
            {
                var sizes = await _storageService.GetRecommendedSizesAsync(categoryId);
                return Json(sizes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendedColors(int categoryId)
        {
            try
            {
                var colors = await _storageService.GetRecommendedColorsAsync(categoryId);
                return Json(colors);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEstimatedImportCost(int categoryId)
        {
            try
            {
                var cost = await _storageService.GetEstimatedImportCostAsync(categoryId);
                return Json(new { estimatedCost = cost });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int productId)
        {
            try
            {
                var product = await _context.Items
                    .Include(i => i.Category)
                    .FirstOrDefaultAsync(i => i.ItemsId == productId);

                if (product == null)
                    return NotFound();

                var factory = _storageFactoryProvider.GetStorageFactory(product.CategoryId);

                return Json(new
                {
                    productId = product.ItemsId,
                    productName = product.ItemsName,
                    categoryId = product.CategoryId,
                    categoryName = product.Category?.NameCategory,
                    currentPrice = product.SellPrice,
                    recommendedSizes = factory.GetRecommendedSizes(),
                    recommendedColors = factory.GetRecommendedColors(),
                    estimatedImportCost = factory.GetEstimatedImportCost(),
                    defaultQuantity = factory.GetDefaultQuantity()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Rest of existing methods...
        public async Task<IActionResult> Functions_Edit(int id)
        {
            var storage = await _context.Storages
                .Include(s => s.ProductVariants)
                    .ThenInclude(pv => pv.Product)
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync(s => s.StorageId == id);

            if (storage == null) return NotFound();

            var vm = await _storageService.PrepareCreateViewModelAsync();
            vm.Storage = storage;

            return View("~/Views/Storage/EditStock.cshtml", vm);
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

                // Update variant stock quantity
                if (storage.ProductVariantsId.HasValue)
                {
                    await _storageService.UpdateProductVariantStockAsync(storage.ProductVariantsId.Value);
                }

                TempData["SuccessMessage"] = "Cập nhật thông tin kho thành công!";
                return RedirectToAction(nameof(StockRefill));
            }

            var vm = await _storageService.PrepareCreateViewModelAsync();
            vm.Storage = storage;
            return View("~/Views/Storage/EditStock.cshtml", vm);
        }

        public async Task<IActionResult> Functions_Details(int id)
        {
            var storage = await _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.ProductVariants)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(m => m.StorageId == id);

            if (storage == null) return NotFound();
            return View("~/Views/Storage/StockDetails.cshtml", storage);
        }

        public async Task<IActionResult> Functions_Delete(int id)
        {
            var storage = await _context.Storages
                .Include(s => s.Supplier)
                .Include(s => s.ProductVariants)
                    .ThenInclude(pv => pv.Product)
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
                    var variantId = storage.ProductVariantsId;

                    _context.Storages.Remove(storage);
                    await _context.SaveChangesAsync();

                    // Update variant stock quantity after deletion
                    if (variantId.HasValue)
                    {
                        await _storageService.UpdateProductVariantStockAsync(variantId.Value);
                    }

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

        // Bulk import method using factory pattern
        [HttpPost]
        public async Task<IActionResult> BulkImport(int productId, int supplierId, string sizes, string colors, int baseQuantity, int baseImportCost)
        {
            // Split comma-separated values into lists
            var sizeList = string.IsNullOrWhiteSpace(sizes)
                ? new List<string>()
                : sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var colorList = string.IsNullOrWhiteSpace(colors)
                ? new List<string>()
                : colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // Validate input
            if (productId <= 0 || supplierId <= 0)
                return Json(new { success = false, message = "Sản phẩm hoặc nhà cung cấp không hợp lệ." });

            if (sizeList.Count == 0)
                return Json(new { success = false, message = "Danh sách kích thước không được để trống." });

            if (colorList.Count == 0)
                return Json(new { success = false, message = "Danh sách màu sắc không được để trống." });

            if (baseQuantity <= 0 || baseImportCost < 0)
                return Json(new { success = false, message = "Số lượng hoặc chi phí nhập không hợp lệ." });

            var importedCount = 0;
            var errors = new List<string>();

            foreach (var size in sizeList)
            {
                foreach (var color in colorList)
                {
                    try
                    {
                        await _storageService.CreateStorageEntryAsync(productId, supplierId, size, color, baseQuantity, baseImportCost);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Size {size}, Color {color}: {ex.Message}");
                    }
                }
            }

            var success = importedCount > 0 && errors.Count == 0;
            var message = success
                ? $"Đã nhập thành công {importedCount} biến thể sản phẩm."
                : (importedCount > 0
                    ? $"Nhập thành công {importedCount} biến thể, {errors.Count} lỗi."
                    : $"Không thể nhập bất kỳ biến thể nào. {errors.Count} lỗi.");

            return Json(new
            {
                success = importedCount > 0,
                importedCount,
                errors,
                message
            });
        }
    }
}
