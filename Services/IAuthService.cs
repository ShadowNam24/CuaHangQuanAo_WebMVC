using CuaHangQuanAo.Models;
using CuaHangQuanAo.Entities;
using System.Threading.Tasks;

namespace CuaHangQuanAo.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, Account Account)> RegisterAsync(RegisterViewModel model);
        Task<(bool Success, string Message, Account Account)> LoginAsync(LoginViewModel model);
    }
}
