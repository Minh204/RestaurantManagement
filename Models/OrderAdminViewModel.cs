namespace RestaurantManagement.Models
{
    public class OrderAdminViewModel
    {
        public int OrderId { get; set; }
        public string FullName { get; set; }
        public string Addr { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
    }
}
