using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    public class RegisterViewModel
    {
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Plesse enter your full name.")]
        public string FullName { get; set; }
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Please enter your username.")]
        public string UserName { get; set; }
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; }
        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Please enter your phone number.")]
        public string Phone { get; set; }
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Please enter your email address.")]
        public string Email { get; set; }
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

    }
}
