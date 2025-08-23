using CuaHangQuanAo.Models;
using CuaHangQuanAo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CuaHangQuanAo.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AccountController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnURL = returnUrl ?? Url.Content("~/");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message, account) = await _authService.LoginAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            // Create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.AccRole)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message, account) = await _authService.RegisterAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, token) = await _authService.GeneratePasswordResetTokenAsync(model.Email);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Email not found or account inactive.");
                return View(model);
            }

            var resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
            await _emailService.SendEmailAsync(model.Email, "Password Reset", $"Click <a href='{resetLink}'>here</a> to reset your password.");

            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(EmailSent));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var (success, account) = await _authService.ValidatePasswordResetTokenAsync(token);
            if (!success || account == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired password reset link.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ChangePasswordVM
            {
                Email = account.Email,
                Token = token
            };
            return View("ChangePassword", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, account) = await _authService.ValidatePasswordResetTokenAsync(model.Token);
            if (!success || account == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired token.");
                return View(model);
            }

            var resetSuccess = await _authService.ResetPasswordAsync(model.Email, model.NewPassword);
            if (!resetSuccess)
            {
                ModelState.AddModelError(string.Empty, "Failed to reset password.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult EmailSent()
        {
            var email = TempData["Email"] as string;
            var vm = new ForgotPasswordVM { Email = email };
            return View(vm);
        }
    }
}
