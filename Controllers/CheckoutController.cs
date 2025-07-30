using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;
using System.Data;

namespace RestaurantManagement.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IConfiguration _configuration;
        public CheckoutController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult CheckoutOrder()
        {
            var accountId = int.Parse(User.FindFirst("AccountId").Value);

            var model = new CheckoutViewModel
            {
                CartItems = GetCartItems(accountId)
            };

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT FullName, Email, Phone FROM Account WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", accountId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.FullName = reader["FullName"].ToString();
                        model.Email = reader["Email"].ToString();
                        model.PhoneNumber = reader["Phone"].ToString();
                    }
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult CheckoutOrder(CheckoutViewModel model)
        {
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "AccountId");
            if (accountIdClaim == null)
            {
                TempData["Error"] = "Không thể lấy thông tin tài khoản. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }
            int accountId = int.Parse(accountIdClaim.Value);
            model.CartItems = GetCartItems(accountId);

            var orderId = 0;
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                var cmd = new SqlCommand("sp_CreateOrder", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@Addr", model.Address ?? "");
                cmd.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
                cmd.Parameters.AddWithValue("@Note", model.Note ?? "");
                var outParam = new SqlParameter("@OrderId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outParam);
                cmd.ExecuteNonQuery();
                orderId = (int)outParam.Value;

                // Add order details
                foreach (var item in model.CartItems)
                {
                    var cmdDetail = new SqlCommand("sp_AddOrderDetail", conn);
                    cmdDetail.CommandType = CommandType.StoredProcedure;
                    cmdDetail.Parameters.AddWithValue("@OrderId", orderId);
                    cmdDetail.Parameters.AddWithValue("@FoodId", item.FoodId);
                    cmdDetail.Parameters.AddWithValue("@FoodName", item.FoodName);
                    cmdDetail.Parameters.AddWithValue("@ImageUrl", item.ImageUrl);
                    cmdDetail.Parameters.AddWithValue("@Quantity", item.Quantity);
                    cmdDetail.Parameters.AddWithValue("@UnitPrice", item.Price);
                    cmdDetail.Parameters.AddWithValue("@TotalAmount", item.TotalAmount);
                    cmdDetail.ExecuteNonQuery();
                }
                //Xóa giỏ hàng sau khi đặt hàng
                var cmdClearCart = new SqlCommand("DELETE FROM Cart WHERE AccountId = @AccountId", conn);
                cmdClearCart.Parameters.AddWithValue("@AccountId", accountId);
                cmdClearCart.ExecuteNonQuery();

            }
            TempData["Success"] = "Đặt hàng thành công";
            return RedirectToAction("Index", "Order");
        }
        private List<CartItemViewModel> GetCartItems(int accountId)
        {
            var list = new List<CartItemViewModel>();
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT c.Id, f.FoodName, f.ImageUrl, c.Quantity, c.TotalAmount
                FROM Cart c
                JOIN Food f ON f.Id = c.FoodId
                WHERE c.AccountId = @accountId", conn); 

                cmd.Parameters.AddWithValue("@accountId", accountId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CartItemViewModel
                    {
                        Id = (int)reader["Id"],
                        FoodName = reader["FoodName"].ToString(),
                        ImageUrl = reader["ImageUrl"].ToString(),
                        Quantity = (int)reader["Quantity"],
                        TotalAmount = (decimal)reader["TotalAmount"]
                    });
                }
            }
            return list;
        }
    }
}
