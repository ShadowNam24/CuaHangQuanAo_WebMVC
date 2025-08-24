using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CuaHangQuanAo.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly CuaHangBanQuanAoContext _context;

        public ProfileController(CuaHangBanQuanAoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
