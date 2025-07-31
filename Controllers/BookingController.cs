using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IConfiguration _configuration;
        public BookingController(IConfiguration config) => _configuration = config;

        [HttpGet]
        public IActionResult Book()
        {
            var model = new BookingViewModel
            {
                Tables = GetAvailableTables(),
                TimeSlots = GetTimeSlots()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Book(BookingViewModel model)
        {
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "AccountId");
            if (accountIdClaim == null) return RedirectToAction("Login", "Account");

            int accountId = int.Parse(accountIdClaim.Value);

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("INSERT INTO Booking(AccountId, TableId, BookingDate, TimeSlot, NumberOfPeople) VALUES (@AccountId, @TableId, GETDATE(), @TimeSlot, @NumPeople)", conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@TableId", model.TableId);
                cmd.Parameters.AddWithValue("@TimeSlot", model.TimeSlot);
                cmd.Parameters.AddWithValue("@NumPeople", model.NumberOfPeople);
                cmd.ExecuteNonQuery();

                var updateTableCmd = new SqlCommand("UPDATE ResTable SET TableStatus = 'Reserved' WHERE Id = @Id", conn);
                updateTableCmd.Parameters.AddWithValue("@Id", model.TableId);
                updateTableCmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Đặt bàn thành công!";
            return RedirectToAction("History", "Booking");
        }

        private List<SelectListItem> GetAvailableTables()
        {
            var list = new List<SelectListItem>();
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, TableName FROM ResTable WHERE TableStatus = 'Empty'", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SelectListItem
                {
                    Value = reader["Id"].ToString(),
                    Text = reader["TableName"].ToString()
                });
            }
            return list;
        }

        private List<SelectListItem> GetTimeSlots()
        {
            return new List<SelectListItem>
        {
            new SelectListItem { Text = "08:00 - 09:00", Value = "08:00 - 09:00" },
            new SelectListItem { Text = "09:00 - 10:00", Value = "09:00 - 10:00" },
            new SelectListItem { Text = "10:00 - 11:00", Value = "10:00 - 11:00" },
            new SelectListItem { Text = "11:00 - 12:00", Value = "11:00 - 12:00" },
            new SelectListItem { Text = "12:00 - 13:00", Value = "12:00 - 13:00" },
            new SelectListItem { Text = "13:00 - 14:00", Value = "13:00 - 14:00" },
            new SelectListItem { Text = "18:00 - 19:00", Value = "18:00 - 19:00" },
            new SelectListItem { Text = "19:00 - 20:00", Value = "19:00 - 20:00" },
            new SelectListItem { Text = "20:00 - 21:00", Value = "20:00 - 21:00" },
            new SelectListItem { Text = "21:00 - 22:00", Value = "21:00 - 22:00" }
        };
        }
        [HttpGet]
        public IActionResult History()
        {
            var accountIdClaim = User.FindFirst("AccountId");
            if (accountIdClaim == null)
                return RedirectToAction("Login", "Account");

            int accountId = int.Parse(accountIdClaim.Value);
            var bookings = new List<BookingHistoryViewModel>();

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT b.Id, r.TableName, b.BookingDate, b.TimeSlot, b.NumberOfPeople, b.IsConfirmed, b.IsCancelled
                FROM Booking b
                JOIN ResTable r ON r.Id = b.TableId
                WHERE b.AccountId = @AccountId
                ORDER BY b.BookingDate DESC", conn);
                cmd.Parameters.AddWithValue("@AccountId", accountId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bookings.Add(new BookingHistoryViewModel
                    {
                        Id = (int)reader["Id"],
                        TableName = reader["TableName"].ToString(),
                        BookingDate = (DateTime)reader["BookingDate"],
                        TimeSlot = reader["TimeSlot"].ToString(),
                        NumberOfPeople = (int)reader["NumberOfPeople"],
                        IsConfirmed = (bool)reader["IsConfirmed"],
                        IsCancelled = (bool)reader["IsCancelled"]
                    });
                }
            }

            return View(bookings);
        }

        [HttpPost]
        public IActionResult Cancel(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand("UPDATE Booking SET IsCancelled = 1 WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            return RedirectToAction("History");
        }
    }
}
