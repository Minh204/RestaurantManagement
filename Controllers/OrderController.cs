using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    public class OrderController : Controller
    {
        private readonly IConfiguration _configuration;
        public OrderController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            var accountId = int.Parse(User.FindFirst("AccountId").Value);
            var orders = new List<OrderViewModel>();

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // Lấy danh sách đơn hàng
                var cmd = new SqlCommand(@"
            SELECT o.Id, o.Addr, o.OrderDate, o.TotalAmount, o.OrderStatus, o.Note
            FROM Orders o
            WHERE o.AccountId = @accountId
            ORDER BY o.OrderDate DESC", conn);

                cmd.Parameters.AddWithValue("@accountId", accountId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    orders.Add(new OrderViewModel
                    {
                        Id = (int)reader["Id"],
                        Address = reader["Addr"].ToString(),
                        OrderDate = (DateTime)reader["OrderDate"],
                        TotalAmount = (decimal)reader["TotalAmount"],
                        Status = reader["OrderStatus"].ToString(),
                        Note = reader["Note"]?.ToString()
                    });
                }
                reader.Close();

                // Lấy chi tiết món ăn theo đơn hàng
                foreach (var order in orders)
                {
                    var cmdDetail = new SqlCommand(@"
                        SELECT FoodName, ImageUrl, Quantity, TotalAmount
                        FROM OrderDetail
                        WHERE OrderId = @OrderId", conn);

                    cmdDetail.Parameters.AddWithValue("@OrderId", order.Id);

                    var rd = cmdDetail.ExecuteReader();
                    order.Foods = new List<CartItemViewModel>();
                    while (rd.Read())
                    {
                        order.Foods.Add(new CartItemViewModel
                        {
                            FoodName = rd["FoodName"]?.ToString() ?? "[Đã xóa]",
                            ImageUrl = rd["ImageUrl"]?.ToString() ?? "/images/no-image.png",
                            Quantity = (int)rd["Quantity"],
                            TotalAmount = (decimal)rd["TotalAmount"]
                        });
                    }
                    rd.Close();
                }

            }

            return View(orders);
        }
        [HttpPost]
        public IActionResult Cancel(int id)
        {
            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("UPDATE Orders SET OrderStatus = 'Cancelled' WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Đã hủy đơn hàng";
            return RedirectToAction("Index");
        }
    }
}
