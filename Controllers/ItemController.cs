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
        public async Task<IActionResult> CreateItems(Item item, IFormFile ImageFile)
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

            if (item.SellPrice <= 0)
            {
                ModelState.AddModelError("SellPrice", "Giá bán phải lớn hơn 0");
                ViewBag.Categories = _context.Categories.ToList();
                return View("CreateItems", item);
            }

            // Xử lý tải lên hình ảnh
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Kiểm tra kích thước file (tối đa 2MB)
                if (ImageFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Kích thước tệp quá lớn. Vui lòng chọn tệp nhỏ hơn 2MB.");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View("CreateItems", item);
                }

                // Kiểm tra định dạng file
                var extension = Path.GetExtension(ImageFile.FileName).ToLower();
                if (!(extension == ".jpg" || extension == ".jpeg" || extension == ".png"))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận các định dạng JPG, JPEG hoặc PNG.");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View("CreateItems", item);
                }

                try
                {
                    // Tạo tên file duy nhất để tránh trùng lặp
                    string uniqueFileName = Guid.NewGuid().ToString() + extension;

                    // Tạo thư mục lưu trữ nếu chưa tồn tại
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Lưu file
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    // Lưu đường dẫn vào đối tượng sản phẩm
                    item.Image = uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi tải ảnh lên: {ex.Message}");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View("CreateItems", item);
                }
            }

            try
            {
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


        // GET: /CreateItems/Functions_Details/5
        public async Task<IActionResult> ItemDetails(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.OrdersDetails)
                .FirstOrDefaultAsync(m => m.ItemsId == id);

            if (item == null) return NotFound();
            return View(item);
        }


        public async Task<IActionResult> EditItems(int id, string? returnUrl)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.ItemsId == id);

            if (item == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ReturnUrl = returnUrl; // Lưu returnUrl vào ViewBag
            return View(item);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItems(int id, Item item)
        {
            if (id != item.ItemsId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
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
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View("EditItem", item);
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
            if (item != null)
            {
                try
                {
                    _context.Items.Remove(item);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Items));
                }
                catch (DbUpdateException ex)
                {
                    bool hasOrderDetails = await _context.OrdersDetails.AnyAsync(od => od.ItemsId == id);
                    bool hasStorage = await _context.Storages.AnyAsync(s => s.ProductVariantsId == id);

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

                    return View("DeleteItem", itemToShow);
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
