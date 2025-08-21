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

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;

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
                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (returnUrl != null)
                return Redirect(returnUrl);

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

        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
