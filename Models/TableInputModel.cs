namespace RestaurantManagement.Models
{
    public class TableInputModel
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public int FloorId { get; set; }
        public string TableStatus { get; set; }
    }
}
