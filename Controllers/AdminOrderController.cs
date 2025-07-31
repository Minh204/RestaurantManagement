using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    public class AdminOrderController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminOrderController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var list = new List<OrderAdminViewModel>();

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT o.Id AS OrderId, a.FullName, o.Addr, o.OrderDate, o.TotalAmount, o.OrderStatus
                FROM Orders o
                JOIN Account a ON o.AccountId = a.Id
                ORDER BY o.OrderDate DESC", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OrderAdminViewModel
                    {
                        OrderId = (int)reader["OrderId"],
                        FullName = reader["FullName"].ToString(),
                        Addr = reader["Addr"].ToString(),
                        OrderDate = (DateTime)reader["OrderDate"],
                        TotalAmount = (decimal)reader["TotalAmount"],
                        OrderStatus = reader["OrderStatus"].ToString()
                    });
                }
            }

            return View(list);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("UPDATE Orders SET OrderStatus = @Status WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        public IActionResult Detail(int id)
        {
            var order = new OrderDetail();
            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT o.Id AS OrderId, a.FullName,a.Phone, a.Email, o.Addr, o.OrderDate, o.TotalAmount, o.Note, o.OrderStatus,
	                    od.FoodName, od.ImageUrl, od.Quantity, (SELECT Price FROM Food WHERE FoodName = od.FoodName ) AS UnitPrice 
                FROM	Orders o
                JOIN	Account a ON o.AccountId = a.Id
                JOIN	OrderDetail od ON od.OrderId = o.Id
                WHERE	o.Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                var reader = cmd.ExecuteReader();
                while(reader.Read())
                {
                    if (order.OrderId == 0) // Chỉ lấy thông tin đơn hàng một lần
                    {
                        order.OrderId = (int)reader["OrderId"];
                        order.FullName = reader["FullName"].ToString();
                        order.Phone = reader["Phone"].ToString();
                        order.Email = reader["Email"].ToString();
                        order.Addr = reader["Addr"].ToString();
                        order.OrderDate = (DateTime)reader["OrderDate"];
                        order.TotalAmount = (decimal)reader["TotalAmount"];
                        order.Note = reader["Note"]?.ToString();
                        order.OrderStatus = reader["OrderStatus"].ToString();
                    }
                    // Thêm món ăn vào danh sách
                    if (order.Foods == null)
                    {
                        order.Foods = new List<CartItemViewModel>();
                    }
                    order.Foods.Add(new CartItemViewModel
                    {
                        
                        FoodName = reader["FoodName"].ToString(),
                        ImageUrl = reader["ImageUrl"]?.ToString() ?? "/images/no-image.png",
                        Price = (decimal)reader["UnitPrice"],
                        Quantity = (int)reader["Quantity"],
                        TotalAmount = (decimal)reader["UnitPrice"] * (int)reader["Quantity"]
                    });
                }
                reader.Close();
            }
            return View(order);
        }
        [HttpGet]
        public IActionResult Export()
        {
            var orders = new List<OrderAdminViewModel>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT o.Id AS OrderId, a.FullName, o.Addr, o.OrderDate, o.TotalAmount, o.OrderStatus
                FROM Orders o
                JOIN Account a ON o.AccountId = a.Id", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new OrderAdminViewModel
                        {
                            OrderId = (int)reader["OrderId"],
                            FullName = reader["FullName"].ToString(),
                            Addr = reader["Addr"].ToString(),
                            OrderDate = (DateTime)reader["OrderDate"],
                            TotalAmount = (decimal)reader["TotalAmount"],
                            OrderStatus = reader["OrderStatus"].ToString()
                        });
                    }
                }
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách đơn hàng");
            worksheet.Cell(1, 1).Value = "Mã đơn";
            worksheet.Cell(1, 2).Value = "Người đặt";
            worksheet.Cell(1, 3).Value = "Địa chỉ";
            worksheet.Cell(1, 4).Value = "Thời gian đặt";
            worksheet.Cell(1, 5).Value = "Tổng tiền";
            worksheet.Cell(1, 6).Value = "Trạng thái";

            for (int i = 0; i < orders.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = orders[i].OrderId;
                worksheet.Cell(row, 2).Value = orders[i].FullName;
                worksheet.Cell(row, 3).Value = orders[i].Addr;
                worksheet.Cell(row, 4).Value = orders[i].OrderDate.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 5).Value = orders[i].TotalAmount;
                worksheet.Cell(row, 6).Value = orders[i].OrderStatus switch
                {
                    "Pending" => "Chờ xử lý",
                    "Paid" => "Đã hoàn thành",
                    "Cancelled" => "Đã hủy",
                    _ => "Không rõ"
                };
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"DanhSachDonHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
