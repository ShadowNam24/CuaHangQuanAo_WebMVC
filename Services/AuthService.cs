using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;

namespace CuaHangQuanAo.Services
{
    public class AuthService : IAuthService
    {
        private readonly CuaHangBanQuanAoContext _context;

        public AuthService(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Account Account)> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Check if username or email already exists
                if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
                    return (false, "Username already exists", null);

                if (await _context.Accounts.AnyAsync(a => a.Email == model.Email))
                    return (false, "Email already exists", null);

                // Generate salt and hash password
                var salt = GenerateSalt();
                var passwordHash = HashPassword(model.Password, salt);

                // Create account
                var account = new Account
                {
                    Username = model.Username,
                    Email = model.Email,
                    Pass = passwordHash,
                    Salt = salt,
                    AccRole = "Customer",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                var customer = new Customer
                {
                    AccId = account.AccId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.Phone,
                    AddressName = model.Address,
                    City = model.City,
                };
                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
                return (true, "Registration successful", account);
            }
            catch (Exception ex)
            {
                return (false, $"Registration failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Account Account)> LoginAsync(LoginViewModel model)
        {
            try
            {
                // Find account by username or email
                var account = await _context.Accounts
                    .Include(a => a.Employees)
                    .Include(a => a.Customers)
                    .FirstOrDefaultAsync(a => a.Username == model.UsernameOrEmail ||
                                            a.Email == model.UsernameOrEmail);

                if (account == null)
                    return (false, "Invalid username or password", null);

                if (!account.IsActive)
                    return (false, "Account is deactivated", null);

                // Verify password
                var passwordHash = HashPassword(model.Password, account.Salt);
                if (passwordHash != account.Pass)
                {
                    await _context.SaveChangesAsync();
                    return (false, "Invalid username or password", null);
                }
                return (true, "Login successful", account);
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}", null);
            }
        }

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
