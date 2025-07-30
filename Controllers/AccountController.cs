using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Helpers;
using RestaurantManagement.Models;
using System.Data;
using System.Security.Claims;

namespace RestaurantManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly string _connStr;
        public AccountController(IConfiguration config)
        {
            _config = config;
            _connStr = _config.GetConnectionString("DefaultConnection");
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }
            string hashedPassword = Helpers.PasswordHelper.HashPassword(model.Password);
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("sp_RegisterAccount", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@UserName", model.UserName);
                    cmd.Parameters.AddWithValue("@Pass", hashedPassword);
                    cmd.Parameters.AddWithValue("@Phone", model.Phone);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@RoleId", 3); // Khách hàng mặc định
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Đăng ký thành công. Bạn có thể đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string hashedPass = PasswordHelper.HashPassword(model.Password);

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_LoginAccount", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", model.UserName);
                cmd.Parameters.AddWithValue("@PassHash", hashedPass);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Lấy thông tin tài khoản
                    int accountId = (int)reader["Id"];
                    string userName = reader["UserName"].ToString();
                    string fullName = reader["FullName"].ToString();
                    int roleId = (int)reader["RoleId"];
                    string roleName = reader["RoleName"].ToString();

                    // Tạo Claims
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim("AccountId", accountId.ToString()),
                    new Claim("FullName", fullName)
                };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Sai tên đăng nhập hoặc mật khẩu.";
                    return View(model);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult EditProfile()
        {
            var accountIdClaim = User.FindFirst("AccountId");
            if (accountIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int accountId = int.Parse(accountIdClaim.Value);

            EditProfileViewModel model = null;
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Account WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", accountId);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model = new EditProfileViewModel
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"]?.ToString(),
                        Phone = reader["Phone"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        Password = reader["Pass"]?.ToString()
                    };
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_UpdateAccountInfo", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@AccountId", model.Id);
                cmd.Parameters.AddWithValue("@FullName", model.FullName);
                cmd.Parameters.AddWithValue("@Phone", model.Phone);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Password", PasswordHelper.HashPassword( model.Password)); // Bạn có thể hash mật khẩu tại đây nếu cần

                cmd.ExecuteNonQuery();
            }

            ViewBag.Success = "Cập nhật thành công!";
            return RedirectToAction("EditProfile");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
