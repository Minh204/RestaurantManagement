using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RestaurantManagement.Views
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;

        public CartSummaryViewComponent(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int totalItem = 0;
            var accountIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "AccountId");
            if (accountIdClaim != null)
            {
                int accountId = int.Parse(accountIdClaim.Value);

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM Cart WHERE AccountId = @AccountId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountId", accountId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                            totalItem = Convert.ToInt32(result);
                    }
                }
            }

            return View("Default", totalItem);
        }
    }
}
