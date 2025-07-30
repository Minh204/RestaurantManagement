using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    [Authorize(Roles = "Admin, Staff")]
    public class BookingAdminController : Controller
    {
        private readonly IConfiguration _config;
        public BookingAdminController(IConfiguration config) => _config = config;

        public IActionResult BookingList()
        {
            var list = new List<BookingAdminViewModel>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT b.Id, a.FullName AS CustomerName, r.TableName, b.TimeSlot, b.BookingDate, 
                       b.NumberOfPeople, b.IsConfirmed, b.IsCancelled, b.IsCompleted
                FROM Booking b
                JOIN Account a ON a.Id = b.AccountId
                JOIN ResTable r ON r.Id = b.TableId
                ORDER BY b.BookingDate DESC", conn);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new BookingAdminViewModel
                    {
                        Id = (int)reader["Id"],
                        CustomerName = reader["CustomerName"].ToString(),
                        TableName = reader["TableName"].ToString(),
                        TimeSlot = reader["TimeSlot"].ToString(),
                        BookingDate = (DateTime)reader["BookingDate"],
                        NumberOfPeople = (int)reader["NumberOfPeople"],
                        IsConfirmed = reader["IsConfirmed"] != DBNull.Value ? Convert.ToBoolean(reader["IsConfirmed"]) : false,
                        IsCancelled = reader["IsCancelled"] != DBNull.Value ? Convert.ToBoolean(reader["IsCancelled"]) : false,
                        IsCompleted = reader["IsCompleted"] != DBNull.Value ? Convert.ToBoolean(reader["IsCompleted"]) : false
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult CompleteBooking(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var getTableCmd = new SqlCommand("SELECT TableId FROM Booking WHERE Id = @Id", conn);
            getTableCmd.Parameters.AddWithValue("@Id", id);
            int tableId = (int)getTableCmd.ExecuteScalar();

            var updateCmd = new SqlCommand(@"
            UPDATE Booking SET IsCompleted = 1 WHERE Id = @Id;
            UPDATE ResTable SET TableStatus = 'Empty' WHERE Id = @TableId;", conn);
            updateCmd.Parameters.AddWithValue("@Id", id);
            updateCmd.Parameters.AddWithValue("@TableId", tableId);
            updateCmd.ExecuteNonQuery();

            return RedirectToAction("BookingList");
        }

        [HttpPost]
        public IActionResult ConfirmBooking(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var getTableCmd = new SqlCommand("SELECT TableId FROM Booking WHERE Id = @Id", conn);
            getTableCmd.Parameters.AddWithValue("@Id", id);
            int tableId = (int)getTableCmd.ExecuteScalar();

            var updateCmd = new SqlCommand(@"
            UPDATE Booking SET IsConfirmed = 1 WHERE Id = @Id;
            UPDATE ResTable SET TableStatus = 'Occupied' WHERE Id = @TableId;", conn);
            updateCmd.Parameters.AddWithValue("@Id", id);
            updateCmd.Parameters.AddWithValue("@TableId", tableId);
            updateCmd.ExecuteNonQuery();

            return RedirectToAction("BookingList");
        }

        [HttpPost]
        public IActionResult CancelBooking(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var cmd = new SqlCommand("UPDATE Booking SET IsCancelled = 1 WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            return RedirectToAction("BookingList");
        }
    }
}
