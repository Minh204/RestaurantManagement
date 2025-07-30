namespace RestaurantManagement.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; }

        // Navigation
        public List<Account> Accounts { get; set; }
    }
}
