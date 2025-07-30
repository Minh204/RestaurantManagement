namespace RestaurantManagement.Models
{
    public class FoodDetailViewModel
    {
        public int Id { get; set; }
        public string FoodName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string Detail { get; set; }
        public string Ingredient { get; set; }
        public string Ration { get; set; }
        public string CookTime { get; set; }
    }
}
