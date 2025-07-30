namespace RestaurantManagement.Models
{
    public class TableViewModel
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public string TableStatus { get; set; }
        public int FloorId { get; set; }
        public string FloorName { get; set; }
    }
}
