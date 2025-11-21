using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ProfileController : Controller
{
    private readonly CuaHangBanQuanAoContext _context;

    public ProfileController(CuaHangBanQuanAoContext context)
    {
        _context = context;
    }

    // Trang hiển thị thông tin (Index)
    public async Task<IActionResult> Index()
    {
        var profile = await LoadProfileAsync();
        return View(profile);
    }

    // GET: Edit
    public async Task<IActionResult> Edit()
    {
        var profile = await LoadProfileAsync();
        return View(profile);
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Profile model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == User.Identity!.Name);
        if (account == null)
        {
            ModelState.AddModelError("", "Account not found.");
            return View(model);
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);
        if (customer == null)
        {
            // Tạo mới nếu chưa tồn tại
            customer = new Customer
            {
                AccId = account.AccId,
                FirstName = ExtractFirstName(model.FullName),
                LastName = ExtractLastName(model.FullName),
                PhoneNumber = model.PhoneNumber,
                AddressName = model.Address,
                City = "" // Có thể thêm field nhập city sau
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer.FirstName = ExtractFirstName(model.FullName);
            customer.LastName = ExtractLastName(model.FullName);
            customer.PhoneNumber = model.PhoneNumber;
            customer.AddressName = model.Address;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật thông tin cá nhân thành công.";
        return RedirectToAction(nameof(Edit));
    }

    private async Task<Profile> LoadProfileAsync()
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == User.Identity!.Name);
        if (account == null)
        {
            return new Profile { FullName = User.Identity!.Name ?? "", Email = "", PhoneNumber = "", Address = "" };
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);

        return new Profile
        {
            FullName = customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : account.Username,
            Email = account.Email,
            PhoneNumber = customer?.PhoneNumber ?? "",
            Address = customer?.AddressName ?? ""
        };
    }

    private static string ExtractFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];
        return parts.Last(); // FirstName = tên (phần cuối)
    }

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];
        return string.Join(' ', parts.Take(parts.Length - 1)); // Họ + đệm
    }
}
