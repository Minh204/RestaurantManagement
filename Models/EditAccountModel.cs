using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    public class EditAccountModel
    {
        public int Id { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        public int RoleId { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (nếu muốn đổi)")]
        public string? NewPassword { get; set; }
    }
}
