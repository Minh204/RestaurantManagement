namespace RestaurantManagement.Models
{
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public int FoodId { get; set; }
        public string FoodName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
