using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantManagement.Models
{
    public class BookingViewModel
    {
        public int TableId { get; set; }
        public string TimeSlot { get; set; }
        public int NumberOfPeople { get; set; }
        public List<SelectListItem> Tables { get; set; }
        public List<SelectListItem> TimeSlots { get; set; }
    }
}
