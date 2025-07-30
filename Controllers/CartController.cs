using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration _configuration;

        public CartController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            var cartItems = new List<CartItemViewModel>();
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "AccountId");
            if (accountIdClaim == null)
                return RedirectToAction("Login", "Account");

            int accountId = int.Parse(accountIdClaim.Value);

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT c.Id, c.FoodId, f.FoodName, f.ImageUrl, f.Price, c.Quantity, c.TotalAmount
                         FROM Cart c
                         JOIN Food f ON c.FoodId = f.Id
                         WHERE c.AccountId = @AccountId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cartItems.Add(new CartItemViewModel
                            {
                                Id = (int)reader["Id"],
                                FoodId = (int)reader["FoodId"],
                                FoodName = reader["FoodName"].ToString(),
                                ImageUrl = reader["ImageUrl"].ToString(),
                                Price = (decimal)reader["Price"],
                                Quantity = (int)reader["Quantity"],
                                TotalAmount = (decimal)reader["TotalAmount"]
                            });
                        }
                    }
                }
            }

            return View(cartItems);
        }
        [HttpPost]
        public IActionResult Add(int foodId, int quantity = 0)
        {
            var accountIdClaim = User.Claims.FirstOrDefault(User => User.Type == "AccountId");
            if(accountIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int accountId = int.Parse(accountIdClaim.Value);
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_AddToCart", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@FoodId", foodId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Json(new {success = true, message = "Add successfully"});
        }
        [HttpPost]
        public IActionResult UpdateQuantity(int id, string action)
        {
            int quantity = 0;
            decimal price = 0;
            decimal total = 0;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // Lấy thông tin hiện tại
                using (SqlCommand cmd = new SqlCommand("SELECT Quantity, Price FROM Cart INNER JOIN Food ON Cart.FoodId = Food.Id WHERE Cart.Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            quantity = (int)reader["Quantity"];
                            price = (decimal)reader["Price"];
                        }
                    }
                }

                // Cập nhật số lượng
                if (action == "increase")
                    quantity++;
                else if (action == "decrease" && quantity > 1)
                    quantity--;

                total = quantity * price;

                using (SqlCommand cmd = new SqlCommand("UPDATE Cart SET Quantity = @Quantity, TotalAmount = @Total WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new
            {
                success = true,
                quantity = quantity,
                totalAmountFormatted = $"{total:N0} đ"
            });
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var accountIdClaim = User.FindFirst("AccountId");
            if (accountIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int accountId = int.Parse(accountIdClaim.Value);
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Cart WHERE Id = @Id AND AccountId = @AccountId", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                int rowsAffected = cmd.ExecuteNonQuery();

                return Json(new { success = rowsAffected > 0 });
            }
        }
    }
}
