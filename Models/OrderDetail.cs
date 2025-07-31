namespace RestaurantManagement.Models
{
    public class OrderDetail
    {
        public int OrderId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Addr { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Note { get; set; }
        public string OrderStatus { get; set; }
        public List<CartItemViewModel> Foods { get; set; }
    }
}
