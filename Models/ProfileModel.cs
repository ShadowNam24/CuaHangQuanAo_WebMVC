using System.ComponentModel.DataAnnotations;

namespace CuaHangQuanAo.Models
{
    public class Profile
    {
        public int Id { get; set; } = 1;

        [Display(Name = "Họ Tên")]
        [Required, StringLength(100)]
        public string FullName { get; set; } = "";

        [Display(Name = "Số điện thoại")]
        [Phone]
        public string PhoneNumber { get; set; } = "";

        [Display(Name = "Địa chỉ")]
        [StringLength(200)]
        public string Address { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        public bool EmailVerified { get; set; } = false;

        public string AvatarUrl { get; set; } = "/images/default-avatar.png";
    }
}
