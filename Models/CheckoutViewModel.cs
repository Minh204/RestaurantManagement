namespace RestaurantManagement.Models
{
    public class CheckoutViewModel
    {
        // Thông tin tài khoản
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // Nhập từ người dùng
        public string Address { get; set; }
        public string Note { get; set; }

        public decimal TotalAmount => CartItems.Sum(x => x.TotalAmount);
        public List<CartItemViewModel> CartItems { get; set; } = new();
    }
}
