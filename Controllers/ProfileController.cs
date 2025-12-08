using CuaHangQuanAo.Entities;
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

    // GET: Orders
    public async Task<IActionResult> Orders()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Orders", "Profile") });

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        var customer = account != null
            ? await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId)
            : null;

        if (customer == null && account != null)
        {
            customer = new Customer
            {
                AccId = account.AccId,
                FirstName = account.Username,
                LastName = "",
                PhoneNumber = "",
                AddressName = "",
                City = ""
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        // 1) Normal: orders linked by CustomerId
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customer.CustomerId)
            .Include(o => o.OrdersDetails).ThenInclude(od => od.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        // 2) Fallback: legacy orders (CustomerId NULL) matched by name/phone
        if (orders.Count == 0)
        {
            var fullname = $"{customer.FirstName} {customer.LastName}".Trim();
            var phone = (customer.PhoneNumber ?? "").Trim();

            var legacy = await _context.Orders
                .Where(o => !o.CustomerId.HasValue &&
                            ((fullname != "" && o.CustomerName == fullname) ||
                             (phone != "" && o.PhoneNumber == phone)))
                .Include(o => o.OrdersDetails).ThenInclude(od => od.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // merge unique
            foreach (var o in legacy)
                if (!orders.Any(x => x.OrdersId == o.OrdersId))
                    orders.Add(o);
        }

        return View(orders);
    }

    // GET: OrderDetails
    public async Task<IActionResult> OrderDetails(int id)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("OrderDetails", "Profile", new { id }) });

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        if (account == null) return NotFound();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccId == account.AccId);

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrdersDetails).ThenInclude(od => od.Items)
            .FirstOrDefaultAsync(o => o.OrdersId == id);

        if (order == null) return NotFound();

        if (customer != null)
        {
            var fullname = $"{customer.FirstName} {customer.LastName}".Trim();
            var phone = (customer.PhoneNumber ?? "").Trim();

            var isOwner = (order.CustomerId == customer.CustomerId) ||
                          (!order.CustomerId.HasValue &&
                           ((fullname != "" && order.CustomerName == fullname) ||
                            (phone != "" && order.PhoneNumber == phone)));

            if (!isOwner) return Forbid();
        }

        return View(order);
    }
}
