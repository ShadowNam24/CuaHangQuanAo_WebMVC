using CuaHangQuanAo.Models;
using Microsoft.AspNetCore.Mvc;

namespace CuaHangQuanAo.Controllers
{
    public class ProfileController : Controller
    {
        private static Profile _profile = new Profile
        {
            FullName = "",
            PhoneNumber = "",
        };

        [HttpGet]
        public IActionResult Index()
        {
            return View(_profile);
        }
    }
}
