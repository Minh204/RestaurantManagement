using System.Data;

namespace RestaurantManagement.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; } // 1: Admin, 2: Staff, 3: Customer  
        public bool IsActive { get; set; }

        // Navigation
        public Role Role { get; set; }
    }
}
