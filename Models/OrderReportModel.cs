namespace RestaurantManagement.Models
{
    public class OrderReportModel
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
