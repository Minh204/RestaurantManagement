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
