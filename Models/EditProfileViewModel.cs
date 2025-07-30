using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // để người dùng đổi
    }
}
