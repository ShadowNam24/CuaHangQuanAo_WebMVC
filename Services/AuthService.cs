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
        public async Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string email)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
            if (account == null)
                return (false, null);

            // Generate a secure token
            var tokenBytes = new byte[48];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var token = Convert.ToBase64String(tokenBytes);

            // Store token in DB
            var resetToken = new PasswordResetToken
            {
                AccountId = account.AccId,
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            return (true, token);
        }

        public async Task<(bool Success, Account Account)> ValidatePasswordResetTokenAsync(string token)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Token == token && (t.IsUsed == false || t.IsUsed == null));

            if (resetToken == null || resetToken.ExpiryDate < DateTime.UtcNow)
                return (false, null);

            return (true, resetToken.Account);
        }
        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email && a.IsActive);
            if (account == null)
                return false;

            // Generate new salt and hash
            var salt = GenerateSalt();
            var passwordHash = HashPassword(newPassword, salt);

            account.Salt = salt;
            account.Pass = passwordHash;

            // Mark all unused tokens for this account as used
            var tokens = _context.PasswordResetTokens
                .Where(t => t.AccountId == account.AccId && (t.IsUsed == false || t.IsUsed == null));
            foreach (var t in tokens)
                t.IsUsed = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
