using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class ItemController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public ItemController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        public IActionResult Items(int page = 1, int pageSize = 20, string search = "")
        {
            var itemsQuery = _context.Items.Include(i => i.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                itemsQuery = itemsQuery.Where(i => i.ItemsName.Contains(search));
            }

            int totalItems = itemsQuery.Count();
            var items = itemsQuery
                .OrderBy(i => i.ItemsId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Search = search;

            return View(items);
        }

        public IActionResult CreateItems()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItems(Item item)
        {
            if (string.IsNullOrWhiteSpace(item.ItemsName))
            {
                ModelState.AddModelError("ItemsName", "Tên sản phẩm là bắt buộc");
                ViewBag.Categories = _context.Categories.ToList();
                return View("CreateItems", item);
            }

            if (item.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục sản phẩm");
                ViewBag.Categories = _context.Categories.ToList();
                return View("CreateItems", item);
            }

            try
            {
                item.IsAvailable = true;
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Items));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi lưu sản phẩm: {ex.Message}");

                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Chi tiết lỗi: {ex.InnerException.Message}");
                }

                ViewBag.Categories = _context.Categories.ToList();
                return View("CreateItems", item);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.NameCategory))
            {
                return BadRequest(new { success = false, message = "Tên danh mục không được để trống" });
            }

            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var allCategories = await _context.Categories.OrderBy(c => c.NameCategory).ToListAsync();
                return Json(new
                {
                    success = true,
                    message = "Tạo danh mục thành công",
                    newCategory = category,
                    categories = allCategories
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Lỗi khi tạo danh mục: {ex.Message}" });
            }
        }

        // GET: Item/ItemDetails/5
        public async Task<IActionResult> ItemDetails(int id)
        {
            // Item -> ProductVariants -> OrdersDetails
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ProductVariants)
                    .ThenInclude(pv => pv.OrdersDetails)
                .FirstOrDefaultAsync(m => m.ItemsId == id);

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> EditItems(int id, string? returnUrl)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.ItemsId == id);

            if (item == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View("EditItem", item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItems(int id, Item item, IFormFile? CoverImageFile, bool RemoveCoverImage = false)
        {
            if (id != item.ItemsId) return NotFound();

            var existingItem = await _context.Items.FindAsync(id);
            if (existingItem == null) return NotFound();

            ModelState.Clear();

            try
            {
                // Update basic fields
                existingItem.ItemsName = item.ItemsName;
                existingItem.CategoryId = item.CategoryId;
                existingItem.SellPrice = item.SellPrice;

                // Handle removing cover image
                if (RemoveCoverImage && !string.IsNullOrEmpty(existingItem.CoverImage))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/products", existingItem.CoverImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    existingItem.CoverImage = null;
                }

                // Handle uploading new image
                if (CoverImageFile != null && CoverImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingItem.CoverImage))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/products", existingItem.CoverImage);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Validate file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var fileExtension = Path.GetExtension(CoverImageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh JPG, PNG hoặc WEBP";
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View("EditItem", existingItem);
                    }

                    if (CoverImageFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View("EditItem", existingItem);
                    }

                    // Create unique filename
                    var fileName = $"product_{item.ItemsId}_{Guid.NewGuid()}{fileExtension}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "products");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await CoverImageFile.CopyToAsync(stream);
                    }

                    existingItem.CoverImage = fileName;
                }

                _context.Update(existingItem);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Items));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(item.ItemsId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi cập nhật sản phẩm: {ex.Message}";
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View("EditItem", existingItem);
            }
        }

        public async Task<IActionResult> Functions_Delete(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(m => m.ItemsId == id);

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Functions_DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            var variants = await _context.ProductVariants.Where(pv => pv.ProductId == id).ToListAsync();

            if (item != null)
            {
                try
                {
                    _context.ProductVariants.RemoveRange(variants);
                    _context.Items.Remove(item);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Items));
                }
                catch (DbUpdateException ex)
                {
                    // Check if any product variants of this item are used in orders or storage
                    bool hasOrderDetails = await _context.OrdersDetails
                        .AnyAsync(od => variants.Select(v => v.ProductVariantsId).Contains(od.ProductVariantId ?? 0));
                    bool hasStorage = await _context.Storages
                        .AnyAsync(s => variants.Select(v => v.ProductVariantsId).Contains(s.ProductVariantsId ?? 0));

                    if (hasOrderDetails || hasStorage)
                    {
                        ModelState.AddModelError("", "Không thể xóa sản phẩm này vì nó đang được sử dụng trong đơn hàng hoặc tồn kho");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Lỗi khi xóa sản phẩm: {ex.Message}");
                    }

                    var itemToShow = await _context.Items
                        .Include(i => i.Category)
                        .FirstOrDefaultAsync(m => m.ItemsId == id);

                    return View("Functions_Delete", itemToShow);
                }
            }
            return RedirectToAction(nameof(Items));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemsId == id);
        }
    }
}