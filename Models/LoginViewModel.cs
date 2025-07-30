using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    public class LoginViewModel
    {
        [Display(Name = "Tên đăng nhập")]
        [Required]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
