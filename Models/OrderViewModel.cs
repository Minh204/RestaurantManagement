namespace RestaurantManagement.Models
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
        public List<CartItemViewModel> Foods { get; set; }
    }
}
