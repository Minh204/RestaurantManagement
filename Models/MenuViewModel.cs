namespace RestaurantManagement.Models
{
    public class MenuViewModel
    {
        public List<Category> Categories { get; set; }
        public List<FoodViewModel> Foods { get; set; }
        public int? SelectedCategoryId { get; set; }
    }
}
