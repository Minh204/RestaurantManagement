using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestaurantManagement.Helpers;
using RestaurantManagement.Models;
using System.Data;
using System.Drawing;

namespace RestaurantManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly string _connStr;

        public AdminController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection");
        }
        public IActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalRevenue = GetTotalRevenue(),
                OrderCount = GetOrderCount(),
                DailyRevenue = GetRevenueByDay(),
                BestSellingFoods = GetBestSellingFoods()
            };

            return View(model);
        }

        private decimal GetTotalRevenue()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var cmd = new SqlCommand("SELECT SUM(TotalAmount) FROM Orders WHERE OrderStatus = 'Paid'", conn);
            var result = cmd.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        private int GetOrderCount()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Orders WHERE OrderStatus = 'Paid'", conn);
            return (int)cmd.ExecuteScalar();
        }

        private List<RevenueByDayViewModel> GetRevenueByDay()
        {
            var list = new List<RevenueByDayViewModel>();
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(@"
                SELECT 
                    CAST(OrderDate AS DATE) AS Day,
                    SUM(TotalAmount) AS Revenue
                FROM Orders
                WHERE OrderStatus = 'Paid'
                GROUP BY CAST(OrderDate AS DATE)
                ORDER BY Day", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RevenueByDayViewModel
                {
                    Date = Convert.ToDateTime(reader["Day"]),
                    Revenue = Convert.ToDecimal(reader["Revenue"])
                });
            }
            return list;
        }

        private List<BestSellingFoodViewModel> GetBestSellingFoods()
        {
            var list = new List<BestSellingFoodViewModel>();
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand(@"
                SELECT TOP 5 FoodName, SUM(Quantity) AS Quantity
                FROM OrderDetail od
                JOIN Orders o ON o.Id = od.OrderId
                WHERE o.OrderStatus = 'Paid'
                GROUP BY FoodName
                ORDER BY Quantity DESC", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BestSellingFoodViewModel
                {
                    FoodName = reader["FoodName"].ToString(),
                    Quantity = Convert.ToInt32(reader["Quantity"])
                });
            }

            return list;
        }


        [HttpGet]
        public IActionResult ExportExcel()
        {
            var data = new List<OrderReportModel>();

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT 
                    CAST(OrderDate AS DATE) AS OrderDate,
                    COUNT(*) AS OrderCount,
                    SUM(TotalAmount) AS Revenue
                FROM Orders
                WHERE OrderStatus = 'Paid'
                GROUP BY CAST(OrderDate AS DATE)
                ORDER BY OrderDate", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new OrderReportModel
                        {
                            Date = Convert.ToDateTime(reader["OrderDate"]),
                            OrderCount = Convert.ToInt32(reader["OrderCount"]),
                            Revenue = Convert.ToDecimal(reader["Revenue"])
                        });
                    }
                }
            }

            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Thống kê doanh thu");
                worksheet.Cell(1, 1).Value = "Ngày";
                worksheet.Cell(1, 2).Value = "Số lượng đơn hàng";
                worksheet.Cell(1, 3).Value = "Doanh thu";

                for (int i = 0; i < data.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = data[i].Date.ToString("yyyy-MM-dd");
                    worksheet.Cell(row, 2).Value = data[i].OrderCount;
                    worksheet.Cell(row, 3).Value = data[i].Revenue;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0 đ";
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(stream);
            }

            stream.Position = 0; // Đặt lại vị trí đầu để hệ thống đọc được
            var fileName = $"ThongKe_DoanhThu_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        public IActionResult ManageAccount(int? roleId)
        {
            int selectedRoleId = roleId ?? 2; // Mặc định hiển thị Nhân viên
            List<Account> accounts = new();

            using (SqlConnection conn = new(_connStr))
            {
                SqlCommand cmd = new("sp_GetAccountsByRole", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@RoleId", selectedRoleId);
                conn.Open();

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    accounts.Add(new Account
                    {
                        Id = reader.GetInt32(0),
                        UserName = reader.GetString(1),
                        FullName = reader.GetString(2),
                        Phone = reader.GetString(3),
                        Email = reader.GetString(4),
                        IsActive = reader.GetBoolean(5),
                        RoleId = reader.GetInt32(6),
                        Role = new Role { RoleName = reader.GetString(7) }
                    });
                }
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Nhân viên", Value = "2" },
                new SelectListItem { Text = "Thành viên", Value = "3" },
                new SelectListItem { Text = "Quản trị viên", Value = "1" }
            };

            ViewBag.SelectedRoleId = selectedRoleId;
            return View(accounts);
        }

        [HttpPost]
        public IActionResult ToggleActive(int id, bool isActive)
        {
            using SqlConnection conn = new(_connStr);
            SqlCommand cmd = new("sp_UpdateAccountStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            conn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("ManageAccount");
        }

        [HttpPost]
        public IActionResult UpdateRole(int id, int newRoleId)
        {
            using SqlConnection conn = new(_connStr);
            SqlCommand cmd = new("sp_UpdateAccountRole", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@RoleId", newRoleId);
            conn.Open();
            cmd.ExecuteNonQuery();
            return Ok(); // <-- Trả về 200 OK cho AJAX
        }
        public IActionResult EditAccount(int id)
        {
            EditAccountModel acc = new();
            using (SqlConnection conn = new(_connStr))
            {
                SqlCommand cmd = new("sp_GetAccountById", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    acc.Id = reader.GetInt32(0);
                    acc.UserName = reader.GetString(1);
                    acc.FullName = reader.GetString(2);
                    acc.Phone = reader.GetString(3);
                    acc.Email = reader.GetString(4);
                    acc.RoleId = reader.GetInt32(5);
                }
            }

            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = 1, Text = "Admin" },
                new { Value = 2, Text = "Nhân viên" },
                new { Value = 3, Text = "Thành viên" }
            }, "Value", "Text", acc.RoleId);

            return View(acc);
        }
        [HttpPost]
        public IActionResult EditAccount(EditAccountModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            string? hashedPassword = null;
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                hashedPassword = PasswordHelper.HashPassword(model.NewPassword);
            }

            using SqlConnection conn = new(_connStr);
            SqlCommand cmd = new("sp_UpdateAccountWithPassword", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Id", model.Id);
            cmd.Parameters.AddWithValue("@FullName", model.FullName);
            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(model.Phone) ? DBNull.Value : model.Phone);
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(model.Email) ? DBNull.Value : model.Email);
            cmd.Parameters.AddWithValue("@RoleId", model.RoleId);
            cmd.Parameters.AddWithValue("@NewPassword", (object?)hashedPassword ?? DBNull.Value);

            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Message"] = "Cập nhật thành công";
            return RedirectToAction("ManageAccount");
        }
    }
}
