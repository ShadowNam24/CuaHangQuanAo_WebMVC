using System.ComponentModel.DataAnnotations;

namespace CuaHangQuanAo.Models
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
    }
}
