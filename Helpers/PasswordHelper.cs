using System.Security.Cryptography;
using System.Text;

namespace RestaurantManagement.Helpers
{
    public class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);

                // Chuyển sang chuỗi hex
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2")); // x2 = 2 ký tự hex

                return sb.ToString();
            }
        }
    }
}
