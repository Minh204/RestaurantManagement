namespace RestaurantManagement.Models
{
    public class FoodSearchViewModel
    {
        public int Id { get; set; }
        public string FoodName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
    }
}
