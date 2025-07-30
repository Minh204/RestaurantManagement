namespace RestaurantManagement.Models
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public List<RevenueByDayViewModel> DailyRevenue { get; set; }
        public List<BestSellingFoodViewModel> BestSellingFoods { get; set; }
    }
}
