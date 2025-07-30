namespace RestaurantManagement.Models
{
    public class FoodViewModel
    {
        public int Id { get; set; }
        public string FoodName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Describe { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
