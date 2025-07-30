namespace RestaurantManagement.Models
{
    public class BookingHistoryViewModel
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public string TimeSlot { get; set; }
        public DateTime BookingDate { get; set; }
        public int NumberOfPeople { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsCancelled { get; set; }
    }
}
