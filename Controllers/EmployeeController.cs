using CuaHangQuanAo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CuaHangQuanAo.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class EmployeeController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public EmployeeController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        // GET: Employee/Index
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Acc)
                .ToListAsync();
            return View("EmployeeList", employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Acc)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View("EmployeeDetails", employee);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("CreateEmployee");
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Firstname,Lastname,Position,PhoneNumber,Address")] Employee employee, string username, string password, string email)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (await _context.Accounts.AnyAsync(a => a.Username == username))
                {
                    ModelState.AddModelError("username", "Tên đăng nhập đã tồn tại");
                    return View("CreateEmployee", employee);
                }
                if (await _context.Accounts.AnyAsync(a => a.Email == email))
                {
                    ModelState.AddModelError("email", "Email đã tồn tại");
                    return View("CreateEmployee", employee);
                }

                // Generate salt and hash password
                string salt = GenerateSalt();
                string passwordHash = HashPassword(password, salt);

                // Create account with a default email based on username
                var account = new Account
                {
                    Username = username,
                    Email = email,
                    Pass = passwordHash,
                    Salt = salt,
                    AccRole = "Employee",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                // Assign account ID to employee
                employee.AccId = account.AccId;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View("CreateEmployee", employee);
        }


        // POST: Employee/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Acc)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee != null)
            {
                // Check if employee has any orders
                var hasOrders = await _context.Orders.AnyAsync(o => o.EmployeeId == id);
                if (hasOrders)
                {
                    ModelState.AddModelError("", "Không thể xóa nhân viên vì có đơn hàng liên quan");
                    return View("EmployeeList", employee);
                }

                // Get the account associated with the employee
                var account = employee.Acc;

                // Delete the employee
                _context.Employees.Remove(employee);

                // Delete the associated account
                if (account != null)
                {
                    _context.Accounts.Remove(account);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to generate salt
        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

